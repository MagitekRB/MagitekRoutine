using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Summoner
    {
        public static bool OnGcd => Spells.Ruin.Cooldown.TotalMilliseconds > 100;

        public static uint[] BioAuras = { Auras.Bio, Auras.Bio2, Auras.Bio3 };
        public static uint[] MiasmaAuras = { Auras.Miasma, Auras.Miasma3 };

        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Summoner, Spells.Ruin);

        private const int DemiImminentMs = 5000;

        /// <summary>
        /// Reports SMN burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the SMN rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: the demi phase (Solar Bahamut/Bahamut/Phoenix, 15s every
        /// 60s; Searing Light cast inside the even-minute one), read from the
        /// pet/trance gauge timers since no self-aura exists for the summon.
        /// Window contents: demi summon, Enkindle, Astral Flow
        /// (Deathflare/Rekindle/Sunflare), the six demi GCDs whose casts drive demi
        /// autos, Searing Light. Sensitivity is moderate — all demi GCDs are instant
        /// but the 15s phase fits exactly six GCDs with zero slack and demi autos
        /// fire off the player's actions.
        /// Sources: The Balance SMN basic guide/openers, official job guide, live
        /// client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Demi/trance phase via the gauge: PetTimer covers the demi summons,
            // TranceTimer the low-level trances (both Int32 milliseconds). The gauge
            // is the ON edge — PetManager.ActivePetType lags the pet spawn. EVERY
            // demi phase reports (15s each, every 60s), not only the Searing-Light-
            // aligned ones, and the 58-59 trance windows are near-empty but report
            // anyway — they are still trances.
            var gaugeMs = Math.Max(ActionResourceManager.Summoner.PetTimer, ActionResourceManager.Summoner.TranceTimer);
            var remaining = gaugeMs > 0 ? TimeSpan.FromMilliseconds(gaugeMs) : TimeSpan.Zero;

            // Own-cast Searing Light (20s, cast inside the even-minute demi):
            // extends the window past the demi phase. It also lands from another
            // Summoner, so only own-cast records count.
            var searingLight = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.SearingLight && x.CasterId == Core.Me.ObjectId);
            if (searingLight != null && searingLight.TimespanLeft > remaining)
                remaining = searingLight.TimespanLeft;

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "SMN Demi");
                return;
            }

            // Summon almost off cooldown: the demi phase is about to open, so
            // nothing slow should start now. The button masks upward, so check the
            // highest tier actually known. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve
            // consumers.
            if (Core.Me.InCombat)
            {
                var summon = Spells.SummonSolarBahamut.IsKnown() ? Spells.SummonSolarBahamut
                    : Spells.SummonBahamut.IsKnown() ? Spells.SummonBahamut
                    : Spells.DreadwyrmTrance.IsKnown() ? Spells.DreadwyrmTrance
                    : Spells.Aethercharge;

                if (summon.IsKnown())
                {
                    var cooldownMs = summon.Cooldown.TotalMilliseconds;
                    if (cooldownMs > 0 && cooldownMs <= DemiImminentMs)
                        RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "SMN Demi");
                }
            }
        }

        public static bool NeedToInterruptCast()
        {

            if (Casting.CastingSpell == Spells.Resurrection && Casting.SpellTarget?.CurrentHealth > 1)
            {
                Logger.Error("Stopped Resurrection: Unit is now alive");
                return true;
            }

            return false;
        }

        public enum SmnPets
        {
            None,
            Carbuncle,
            Ruby,
            Topaz,
            Emerald,
            Ifrit,
            Titan,
            Garuda,
            Bahamut,
            SolarBahamut,
            Phoenix
        }


        public static SmnPets SummonedPet(this LocalPlayer me)
        {
            if ((int)PetManager.ActivePetType == 10)
                return SmnPets.Bahamut;

            if ((int)PetManager.ActivePetType == 14)
                return SmnPets.Phoenix;

            if ((int)PetManager.ActivePetType == 23)
                return SmnPets.Carbuncle;

            if ((int)PetManager.ActivePetType == 24)
                return SmnPets.Ruby;

            if ((int)PetManager.ActivePetType == 25)
                return SmnPets.Topaz;

            if ((int)PetManager.ActivePetType == 26)
                return SmnPets.Emerald;

            if ((int)PetManager.ActivePetType == 27)
                return SmnPets.Ifrit;

            if ((int)PetManager.ActivePetType == 28)
                return SmnPets.Titan;

            if ((int)PetManager.ActivePetType == 29)
                return SmnPets.Garuda;

            if ((int)PetManager.ActivePetType == 30)
                return SmnPets.Ifrit;

            if ((int)PetManager.ActivePetType == 31)
                return SmnPets.Titan;

            if ((int)PetManager.ActivePetType == 32)
                return SmnPets.Garuda;

            if ((int)PetManager.ActivePetType == 46)
                return SmnPets.SolarBahamut;

            return GameObjectManager.PetObjectId != GameObjectManager.EmptyGameObject
                ? SmnPets.Carbuncle
                : SmnPets.None;
        }

    }
}

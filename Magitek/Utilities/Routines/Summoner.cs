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

        // The gauge byte RB returns as ElementalAttunement is packed on the current client: the
        // attuned gem in bits 0-1, the stacks left in bits 2-7 (FFXIVClientStructs SummonerGauge).
        // RB hands it back unmasked - two Ruby stacks read 9, one reads 5 - so read stacks here.
        public static int AttunementStacks => ActionResourceManager.Summoner.ElementalAttunement >> 2;

        // The gem attuned right now, from the same byte: 1 Ifrit, 2 Titan, 3 Garuda - the order of
        // RB's ActivePetType. RB's own ActivePet reads the demi cycle (which demi is out or comes
        // next), not the attuned gem.
        public static ActionResourceManager.Summoner.ActivePetType AttunedGem =>
            (ActionResourceManager.Summoner.ActivePetType)(ActionResourceManager.Summoner.ElementalAttunement & 3);

        /// <summary>
        /// Weave gate with a stall fallback (the Sage pattern). Bare WeaveWindow.CanWeave() is false
        /// whenever the GCD is ready, so when no GCD can be cast at all — forced movement in a
        /// hardcast-only state, or the GCD toggles switched off — every oGCD behind it is locked out
        /// for the duration. Once a Combat() pass has tried every GCD with the GCD ready and cast none,
        /// and the last action finished long enough ago, let oGCDs fire anyway.
        /// </summary>
        public static bool CanWeave()
        {
            if (GlobalCooldown.CanWeave())
                return true;

            // Idle GCD only: the age check alone also comes true in the tail of every
            // rolling recast (age passes 1750ms before a 2.5s GCD comes back), exactly
            // where CanWeave refuses because an oGCD would clip the next GCD. The
            // fallback exists for a rotation that has genuinely stopped casting.
            if (Spells.Ruin.Cooldown > System.TimeSpan.Zero || Core.Me.IsCasting)
                return false;

            // Ready is not stalled: after an instant GCD with nothing woven the age is already past
            // 1750ms on the pulse the GCD comes back, and an oGCD ordered ahead of the GCDs in
            // Combat() took that pulse and pushed a castable spell back by its animation lock.
            // Only a Combat() pass that tried every GCD and cast none proves the stall.
            if (!gcdStalledLastPass)
                return false;

            return Casting.LastSpellTimeFinishAge.ElapsedMilliseconds > 1750 + Models.Account.BaseSettings.Instance.UserLatencyOffset;
        }

        // Set by NoteGcdStall() at the end of a Combat() pass that cast nothing, and moved into
        // gcdStalledLastPass when the next pass begins: the fallback above reads the previous pass's
        // verdict at any tick rate, and a pass that casts or returns early leaves it shut.
        private static bool gcdStallPending;
        private static bool gcdStalledLastPass;

        public static void BeginCombatPass()
        {
            gcdStalledLastPass = gcdStallPending;
            gcdStallPending = false;
        }

        /// <summary>
        /// Called at the end of Combat() once every GCD has declined. Counts only with the GCD ready
        /// and the target inside spell range, so a rolling recast, or a target still being walked up
        /// to, never opens the stall fallback.
        /// </summary>
        public static void NoteGcdStall()
        {
            if (Spells.Ruin.Cooldown <= TimeSpan.Zero && Core.Me.CurrentTarget.WithinSpellRange(Spells.Ruin.Range))
                gcdStallPending = true;
        }

        // Stamps when Searing Light first found the demi summon ready-or-imminent and
        // began waiting for it; bounds the wait so gem phases cannot park the buff.
        public static long SearingLightHoldStartTick;

        private const int DemiImminentMs = 5000;

        // Milliseconds until the next demi summon is off recast, 0 once it is ready. The demis,
        // Dreadwyrm Trance and Aethercharge share one recast (60s, shortened by spell speed); the
        // longest recast among the known summons is read, in case one of them is tracked apart from
        // the one on the button.
        public static double DemiSummonCooldownMs =>
            new[] { Spells.SummonSolarBahamut, Spells.SummonBahamut, Spells.SummonPhoenix, Spells.DreadwyrmTrance, Spells.Aethercharge }
                .Where(s => s.IsKnown())
                .Select(s => s.Cooldown.TotalMilliseconds)
                .DefaultIfEmpty(0)
                .Max();

        /// <summary>
        /// Reports SMN burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the SMN rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: the demi phase (Solar Bahamut/Bahamut/Phoenix, 15s every
        /// 60s; Searing Light cast inside the even-minute one), read from the
        /// trance gauge timer since no self-aura exists for the summon.
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
            // Demi/trance phase via TranceTimer ONLY — never PetTimer, never a
            // Max() of the two. Log-proven (2026-08-30, full-session forensics):
            // Max(PetTimer, TranceTimer) opened a ~30.0s window on every gem
            // summon (Garuda/Titan/Ifrit II) and ~15.0s on demi summons, so
            // "burst" asserted through the gem attunement phases — roughly 2/3
            // of combat — and starved Surecast/Addle/Radiant Aegis.
            // Field mapping: TranceTimer reads gauge offset 0x08, the client's
            // SummonTimer (runs for trances AND demis); PetTimer reads 0x0A,
            // the AttunementTimer (gem phases). Consistent with the shipped
            // Dreadwyrm Trance gates (TranceTimer > 0 with Carbuncle out): if
            // TranceTimer were the attunement timer it would read 0 during a
            // trance and those gates could never have fired.
            // ASSUMPTIONS — per-field values were never sampled in combat, only
            // the Max was ever logged: that TranceTimer stays 0 through gem
            // phases and runs through 70+ demis both come from the mapping
            // above. A wrong mapping fails toward a MISSED report (a defensive
            // weaving during burst), never toward the false report this
            // replaces, because the pet conjunct below blocks the gem half
            // regardless: a gem phase keeps its egi out essentially phase-long
            // (field-observed, with rare ~1s Carbuncle/None blips), and the pet
            // id can lag a spawn by a pulse or two at any phase edge — again
            // only ever dropping a report.
            // The no-demi-pet clause exists ONLY for Dreadwyrm Trance (58-69),
            // the one band where a trance is legitimately petless. At 70+ it is
            // disabled (!SummonBahamut known): in a full field-validation run
            // (2026-08-30, 32 windows) every real window came from the demi-pet
            // clause or Searing Light, while the only two false asserts (~2s
            // micro-windows mid-gem-phase, mechanism unidentified) came from
            // this clause catching a transient pet blip. Below 58 nothing
            // reports; no burst there is worth starving a defensive for.
            var tranceMs = ActionResourceManager.Summoner.TranceTimer;
            var remaining = TimeSpan.Zero;

            if (tranceMs > 0)
            {
                var pet = Core.Me.SummonedPet();

                var demiPetOut = pet == SmnPets.Bahamut || pet == SmnPets.Phoenix || pet == SmnPets.SolarBahamut;

                var tranceWithoutDemiPet = (pet == SmnPets.Carbuncle || pet == SmnPets.None)
                    && Spells.DreadwyrmTrance.IsKnown()
                    && !Spells.SummonBahamut.IsKnown();

                if (demiPetOut || tranceWithoutDemiPet)
                    remaining = TimeSpan.FromMilliseconds(tranceMs);
            }

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

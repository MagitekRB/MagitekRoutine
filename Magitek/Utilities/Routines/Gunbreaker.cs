using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Gunbreaker

    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Gunbreaker, Spells.KeenEdge);

        // Track Gnashing Fang usage in current burst window (reset on No Mercy, increment on each GF cast)
        public static int GnashingFangUsesThisBurst = 0;

        // How close No Mercy's cooldown has to be before we report the burst as
        // imminent. Covers the choreographed pre-window GCDs (Burst Strike cartridge
        // dump, Bloodfest weave) where a slow foreign cast would delay the press and
        // drift the whole 60s loop.
        private const int NoMercyImminentMs = 5000;

        /// <summary>
        /// Reports GNB burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the GNB rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: No Mercy (20s, every 60s; Bloodfest weaved at window start).
        /// Since 7.4 Bloodfest is 60s, so every No Mercy window is internally
        /// identical — no 1-min/2-min distinction. Window contents: No Mercy,
        /// Bloodfest, Double Down, Sonic Break, Gnashing Fang, Blasting Zone,
        /// Bow Shock, Reign of Beasts/Lionheart combo.
        /// Sources: The Balance GNB basic guide, official job guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: the No Mercy self-buff — one contiguous 20s, no flicker,
            // no auxiliary auras. Self-only, but the own-cast filter matches the
            // MCH/BRD pattern and is harmless. Unlocks at Lv2, so the aura is its own
            // gate at every sync level — no level branching needed.
            var noMercy = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.NoMercy && x.CasterId == Core.Me.ObjectId);
            if (noMercy != null)
            {
                RoutineState.ReportBurstWindow(noMercy.TimespanLeft, "GNB No Mercy");
                return;
            }

            // No Mercy almost off cooldown: the pre-window GCDs are choreographed, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.NoMercy.IsKnown())
            {
                var cooldownMs = Spells.NoMercy.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= NoMercyImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "GNB No Mercy");
            }
        }

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.Rampart,
            Spells.Camouflage,
            Spells.Nebula,
            Spells.GreatNebula,
            Spells.HeartOfCorundum,
            Spells.HeartofStone
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.Camouflage,
            Auras.Nebula,
            Auras.Aurora,
            Auras.Superbolide,
            Auras.HeartofLight,
            Auras.HeartOfCorundum
        };

        public static SpellData HeartOfCorundum => Spells.HeartOfCorundum.IsKnown()
                                            ? Spells.HeartOfCorundum
                                            : Spells.HeartofStone;

        public static SpellData BlastingZone => Spells.BlastingZone.IsKnown()
                                            ? Spells.BlastingZone
                                            : Spells.DangerZone;

        // HARDCODED: Level 88 trait increases max cartridges and Bloodfest generation.
        public static int MaxCartridge => Core.Me.ClassLevel < 88 ? 2 : 3;
        // HARDCODED: Level 88 trait increases max cartridges and Bloodfest generation.
        public static int AmountCartridgeFromBloodfest => Core.Me.ClassLevel < 88 ? 2 : 3;
        public static int RequiredCartridgeForGnashingFang => 1;
        public static int RequiredCartridgeForDoubleDown => 2;
        public static int RequiredCartridgeForBurstStrike => 1;
        public static int RequiredCartridgeForFatedCircle => 1;

        public static bool IsAurasForComboActive()
        {
            return (Core.Me.HasAura(Auras.ReadytoRip)
                || Core.Me.HasAura(Auras.ReadytoTear)
                || Core.Me.HasAura(Auras.ReadytoGouge)
                || Core.Me.HasAura(Auras.ReadytoBlast)
                );
        }

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

    }
}

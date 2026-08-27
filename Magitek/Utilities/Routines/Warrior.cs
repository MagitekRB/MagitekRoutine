using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Collections.Generic;

namespace Magitek.Utilities.Routines
{
    internal static class Warrior
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Warrior, Spells.HeavySwing);

        // How close Inner Release's cooldown has to be before we report the burst as
        // imminent. Covers the pre-burst weave where IR is about to go out and an
        // interruption would delay the whole window.
        private const int InnerReleaseImminentMs = 5000;

        /// <summary>
        /// Reports WAR burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the WAR rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Inner Release (3 stacks/15s, every 60s), with the Primal Rend
        /// -> Primal Ruination -> Primal Wrath follow-up chain carried by their Ready
        /// auras, which overlap IR and each other into one contiguous window.
        /// Window contents: Inner Release (auto-crit Fell Cleaves), Inner Chaos,
        /// Primal Rend, Primal Ruination, Primal Wrath, Infuriate charges.
        /// Sensitivity is low-moderate — WAR has slack GCDs and generous Ready
        /// timers; the window is reported for consistency and item/phantom gating.
        /// Sources: The Balance WAR basic/intermediate guides, official job guide,
        /// live client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Window: union (max remaining) of Inner Release and the chain's Ready
            // auras. IR grants Primal Rend Ready (30s), Primal Rend grants Primal
            // Ruination Ready (20s), the 3rd IR Fell Cleave grants Wrathful (30s) —
            // the grant order makes the union one contiguous interval, no bridging
            // needed. Under level sync missing auras simply never appear (Primal Rend
            // Lv90, Wrathful Lv96, Ruination Lv100). The sub-70 Berserk aura (86) is
            // deliberately not covered — mild value, and its constant is not in
            // Auras.cs.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.InnerRelease && aura.Id != Auras.PrimalRendReady
                    && aura.Id != Auras.PrimalRuinationReady && aura.Id != Auras.Wrathful)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "WAR Inner Release");
                return;
            }

            // Inner Release almost off cooldown: the window is about to open, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.InnerRelease.IsKnown())
            {
                var cooldownMs = Spells.InnerRelease.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= InnerReleaseImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "WAR Inner Release");
            }
        }

        public static SpellData FellCleave => Spells.FellCleave.IsKnown()
                                            ? Spells.FellCleave
                                            : Spells.InnerBeast;

        public static SpellData Decimate => Spells.Decimate.IsKnown()
                                            ? Spells.Decimate
                                            : Spells.SteelCyclone;

        public static SpellData InnerRelease => Spells.InnerRelease.IsKnown()
                                            ? Spells.InnerRelease
                                            : Spells.Berserk;

        public static SpellData Bloodwhetting => Spells.Bloodwhetting.IsKnown()
                                            ? Spells.Bloodwhetting
                                            : Spells.RawIntuition;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.Rampart,
            Spells.Damnation,
            Spells.Vengeance,
            Spells.ThrillofBattle,
            Spells.Bloodwhetting,
            Spells.RawIntuition
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.RawIntuition,
            Auras.Bloodwhetting,
            Auras.Vengeance,
            Auras.Holmgang,
            Auras.ThrillOfBattle,
            Auras.Damnation
        };

        public static readonly List<uint> Heal = new List<uint>()
        {
            Auras.Equilibrium,
            Auras.ThrillOfBattle
        };
    }
}

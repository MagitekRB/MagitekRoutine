using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Machinist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Magitek.Utilities.Routines
{
    internal static class Machinist
    {
        // The Double Hypercharged Wildfire label promises "(requires late weave wildfire)".
        // Every consumer reads THIS so the promise is kept in one place: with Late Weave off,
        // no hold, guard, or alignment anywhere saves for a double window that cannot happen.
        public static bool DoubleHyperchargedWildfireActive =>
            Models.Machinist.MachinistSettings.Instance.DoubleHyperchargedWildfire
            && Models.Machinist.MachinistSettings.Instance.LateWeaveWildfire;

        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Machinist, Spells.SplitShot, new List<SpellData>() { Spells.Flamethrower });

        // How close Wildfire's cooldown has to be before we report the burst as
        // imminent. Matches the hold thresholds MCH's own logic already uses
        // (Cooldowns.cs holds Barrel Stabilizer and Hypercharge at 7s/15s out).
        private const int WildfireImminentMs = 7000;

        // At 100 the 2-minute burst is a double Hypercharge: two overheat windows
        // separated by exactly one GCD, during which both Overheated and Wildfire
        // have dropped. Bridge that gap for up to this long after Overheated ends.
        private const int OverheatBridgeMs = 3000;

        // Time since Overheated was last seen. Never started until we first see the
        // aura, so a fresh combat can't bridge off a stale timestamp — and once it
        // runs past OverheatBridgeMs it simply stops matching.
        private static readonly Stopwatch OverheatedDropTimer = new Stopwatch();

        /// <summary>
        /// Reports MCH burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the MCH rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Wildfire, entered via Hypercharge (weaved before Full Metal
        /// Field); at 100 the 2-minute burst is a double Hypercharge with one GCD
        /// between the overheat windows. Window contents: Wildfire, Hypercharge,
        /// Barrel Stabilizer, Reassemble, Drill, Air Anchor, Chain Saw/Excavator,
        /// Full Metal Field, Automaton Queen.
        /// Sources: The Balance MCH basic guide/FAQ/openers, official job guide,
        /// Icy Veins (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Overheat: Heat Blast every 1.5s for up to 10s. An interjected cast still
            // lands all five Blazing Shots (The Balance FAQ), but drifts the window and
            // costs a Wildfire weaponskill stack (~240 potency). Strongest signal,
            // checked first.
            var overheated = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.Overheated);
            if (overheated != null)
            {
                OverheatedDropTimer.Restart();
                RoutineState.ReportBurstWindow(overheated.TimespanLeft, "MCH Overheat");
                return;
            }

            // Wildfire ticking on the target: every GCD in its 10s counts toward the
            // detonation. Covers the moments around overheat inside the window.
            var wildfire = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.WildfireBuff && x.CasterId == Core.Me.ObjectId);
            if (wildfire != null)
            {
                RoutineState.ReportBurstWindow(wildfire.TimespanLeft, "MCH Wildfire");
                return;
            }

            // Double-Hypercharge gap: one GCD between overheat windows where both
            // auras above have dropped. Bridge it only while a second Hypercharge is
            // actually available (heat or Hypercharged), so an interjected cast can't
            // push the second window out of raid buffs.
            if (OverheatedDropTimer.IsRunning && OverheatedDropTimer.ElapsedMilliseconds <= OverheatBridgeMs
                && (ActionResourceManager.Machinist.Heat >= 50 || Core.Me.HasAura(Auras.Hypercharged, true)))
            {
                RoutineState.ReportBurstWindow(
                    TimeSpan.FromMilliseconds(OverheatBridgeMs - OverheatedDropTimer.ElapsedMilliseconds),
                    "MCH Overheat bridge");
                return;
            }

            // Wildfire almost off cooldown: the 2-minute window is about to open, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && MachinistSettings.Instance.UseWildfire && Spells.Wildfire.IsKnown())
            {
                var cooldownMs = Spells.Wildfire.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= WildfireImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "MCH Wildfire");
            }
        }

        public static SpellData HeatedSplitShot => Spells.HeatedSplitShot.IsKnown()
                                                    ? Spells.HeatedSplitShot
                                                    : Spells.SplitShot;
        public static SpellData HeatedSlugShot => Spells.HeatedSlugShot.IsKnown()
                                                    ? Spells.HeatedSlugShot
                                                    : Spells.SlugShot;

        public static SpellData HeatedCleanShot => Spells.HeatedCleanShot.IsKnown()
                                                    ? Spells.HeatedCleanShot
                                                    : Spells.CleanShot;

        public static SpellData HotAirAnchor => Spells.AirAnchor.IsKnown()
                                                    ? Spells.AirAnchor
                                                    : Spells.HotShot;

        public static SpellData RookQueenPet => Spells.AutomationQueen.IsKnown()
                                                    ? Spells.AutomationQueen
                                                    : Spells.RookAutoturret;

        public static SpellData RookQueenOverdrive => Spells.QueenOverdrive.IsKnown()
                                                    ? Spells.QueenOverdrive
                                                    : Spells.RookOverdrive;

        public static SpellData Scattergun => Spells.Scattergun.IsKnown()
                                                    ? Spells.Scattergun
                                                    : Spells.SpreadShot;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static bool CheckCurrentDamageIncrease(int neededDmgIncrease)
        {
            double dmgIncrease = 1;

            //From PLD
            //From DRK
            //From GNB
            //From WAR
            //From WHM
            //From BLM
            //From SAM
            //From MCH
            //From BRD
            if (Core.Me.HasAura(Auras.RadiantFinale))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAura(Auras.BattleVoice))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.TheWanderersMinuet))
                dmgIncrease *= 1.02;
            if (Core.Me.HasAura(Auras.MagesBallad))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.ArmysPaeon))
                dmgIncrease *= 1.01;

            //From DNC
            if (Core.Me.HasAura(Auras.Devilment))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.TechnicalFinish))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAura(Auras.StandardFinish))
                dmgIncrease *= 1.06;

            //From RDM
            if (Core.Me.HasAura(Auras.Embolden))
                dmgIncrease *= 1.05;

            //From SMN
            if (Core.Me.HasAura(Auras.SearingLight))
                dmgIncrease *= 1.03;

            //From MNK
            if (Core.Me.HasAura(Auras.Brotherhood))
                dmgIncrease *= 1.05;

            //From NIN
            if (Core.Me.CurrentTarget.HasAura(Auras.VulnerabilityUp))
                dmgIncrease *= 1.05;

            //From DRG
            if (Core.Me.HasAura(Auras.BattleLitany))
                dmgIncrease *= 1.01;

            //From RPR
            if (Core.Me.HasAura(Auras.ArcaneCircle))
                dmgIncrease *= 1.03;

            //From SCH
            if (Core.Me.CurrentTarget.HasAura(Auras.ChainStratagem))
                dmgIncrease *= 1.01;

            //From SGE

            //From AST
            if (Core.Me.HasAura(Auras.Divination))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAnyDpsCardAura())
                dmgIncrease *= 1.06;

            Logger.WriteInfo($@"Damage Increase: {dmgIncrease}");
            return dmgIncrease >= (1 + (double)neededDmgIncrease / 100);
        }
    }
}

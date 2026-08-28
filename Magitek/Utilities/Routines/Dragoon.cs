using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Collections.Generic;


namespace Magitek.Utilities.Routines
{
    internal static class Dragoon
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Dragoon, Spells.TrueThrust);

        // How close Lance Charge's cooldown has to be before we report the burst as
        // imminent. Geirskogul shares the 60s cadence and is deliberately pressed
        // after Lance Charge, so Lance Charge alone predicts the window.
        private const int LanceChargeImminentMs = 5000;

        /// <summary>
        /// Reports DRG burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the DRG rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Lance Charge (20s, every 60s; Battle Litany-aligned every
        /// 2 minutes), with Life of the Dragon extending the tail past Lance Charge.
        /// Window contents: Lance Charge, Battle Litany, Geirskogul -> Life of the
        /// Dragon (Nastrond, Stardiver, Starcross), High Jump/Mirage Dive,
        /// Dragonfire Dive, Wyrmwind Thrust, Life Surge x2.
        /// Sources: The Balance DRG basic guide/openers, official job guide, live
        /// client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: own-cast Lance Charge and Battle Litany — every Lance
            // Charge counts (odd minutes included), and the window ends when the last
            // buff drops, hence the max remaining. Battle Litany also lands from
            // another Dragoon, so only own-cast records count. Under level sync
            // missing buffs simply never appear — no level branching needed.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.LanceCharge && aura.Id != Auras.BattleLitany)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            // Life of the Dragon gauge: Geirskogul is pressed after Lance Charge, so
            // LotD outlives the buffs by a few seconds — the gauge timer covers that
            // tail, which the auras miss. Gauge read structurally verified against
            // the live client (Mode/Timer), but not yet observed mid-LotD in game.
            if (ActionResourceManager.Dragoon.Mode == ActionResourceManager.Dragoon.DragoonMode.Life
                && ActionResourceManager.Dragoon.Timer > remaining)
                remaining = ActionResourceManager.Dragoon.Timer;

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "DRG Lance Charge");
                return;
            }

            // Lance Charge almost off cooldown: the buff stack is about to go out, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.LanceCharge.IsKnown())
            {
                var cooldownMs = Spells.LanceCharge.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= LanceChargeImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "DRG Lance Charge");
            }
        }

        public static SpellData HighJump => Spells.HighJump.IsKnown()
                                            ? Spells.HighJump
                                            : Spells.Jump;

        public static SpellData HeavensThrust => Spells.HeavensThrust.IsKnown()
                                            ? Spells.HeavensThrust
                                            : Spells.FullThrust;

        public static SpellData ChaoticSpring => Spells.ChaoticSpring.IsKnown()
                                            ? Spells.ChaoticSpring
                                            : Spells.ChaosThrust;

        public static SpellData Disembowel => !Spells.SpiralBlow.IsKnown()
                                            ? Spells.Disembowel
                                            : Spells.SpiralBlow;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static List<SpellData> JumpsList = new List<SpellData>()
        {
            HighJump,
            Spells.DragonfireDive,
            Spells.MirageDive,
            Spells.Stardiver
        };

        public static List<SpellData> SingleWeaveJumpsList = new List<SpellData>()
        {
            HighJump,
            Spells.DragonfireDive,
            Spells.Stardiver
        };
    }
}

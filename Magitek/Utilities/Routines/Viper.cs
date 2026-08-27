using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using Magitek.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Viper
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Viper, Spells.SteelFangs);

        public static int EnemiesAroundPlayer5Yards;

        // How close Serpent's Ire's cooldown has to be before we report the burst as
        // imminent. The Balance intermediate guide: with around 10s left on Ire's
        // cooldown, stick to dual wield combos so resources are ready for the window.
        private const int SerpentIreImminentMs = 10000;

        // The 2-minute burst is a double Reawaken aligned to Serpent's Ire, with one
        // GCD between the two Reawakened states where gauge and aura both read zero.
        // Bridge that gap for up to this long after the state was last seen.
        private const int ReawakenBridgeMs = 4000;

        // Hasted GCD length inside Reawaken, used to estimate remaining time from the
        // Anguine Tribute count when neither the gauge timer nor the aura is readable.
        private const double ReawakenGcdSeconds = 1.7;

        // Time since the Reawakened state was last seen. Never started until the state
        // is first seen, so a fresh combat can't bridge off a stale timestamp — and
        // once it runs past ReawakenBridgeMs it simply stops matching.
        private static readonly Stopwatch ReawakenedDropTimer = new Stopwatch();

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesAroundPlayer5Yards = Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach);
        }

        /// <summary>
        /// Reports VPR burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the VPR rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: the Reawakened state (5 generations + Ouroboros, ~11s hasted),
        /// entered on 50 Serpent's Offerings or Ready to Reawaken; the 2-minute burst
        /// is a double Reawaken aligned to Serpent's Ire, bridged across its one-GCD
        /// gap. Inside Reawaken the GCD is ~1.7s with a mandatory Legacy weave per
        /// generation — zero spare weave slots; strictest interruption tier alongside
        /// MCH overheat. Window contents: Reawaken x2, Generations 1-4 + Legacies,
        /// Ouroboros x2, Serpent's Ire, Uncoiled Fury spends.
        /// Sources: The Balance VPR basic/intermediate guides, official job guide,
        /// live client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Below Reawaken (Lv90) VPR has no burst window to report.
            if (!Spells.Reawaken.IsKnown())
                return;

            // Reawakened state: Anguine Tribute is self-only by construction; the
            // own-cast aura covers any frame where the gauge reads late. ReawakenTimer
            // is verified structurally against the client but not yet observed
            // mid-Reawaken in game, so fall back to the aura, then to tribute times
            // the hasted GCD length. Do not use AdjustedCooldown for Reawaken-family
            // actions here — it disagrees with The Balance's sks-immunity claim.
            var tribute = ActionResourceManager.Viper.AnguineTribute;
            var reawakened = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.Reawakened && x.CasterId == Core.Me.ObjectId);
            if (tribute > 0 || reawakened != null)
            {
                var remaining = ActionResourceManager.Viper.ReawakenTimer;
                if (remaining <= TimeSpan.Zero)
                    remaining = reawakened?.TimespanLeft ?? TimeSpan.FromSeconds(tribute * ReawakenGcdSeconds);

                ReawakenedDropTimer.Restart();
                RoutineState.ReportBurstWindow(remaining, "VPR Reawaken");
                return;
            }

            // Double-Reawaken gap: one GCD between the two Reawakened states where
            // gauge and aura both read zero. Bridge it only while a second Reawaken
            // is actually available (Ready to Reawaken or 50 offerings), so a single
            // Reawaken ending can't stretch the window.
            if (ReawakenedDropTimer.IsRunning && ReawakenedDropTimer.ElapsedMilliseconds <= ReawakenBridgeMs
                && (Core.Me.HasAura(Auras.ReadytoReawaken, true) || ActionResourceManager.Viper.SerpentsOffering >= 50))
            {
                RoutineState.ReportBurstWindow(
                    TimeSpan.FromMilliseconds(ReawakenBridgeMs - ReawakenedDropTimer.ElapsedMilliseconds),
                    "VPR Reawaken bridge");
                return;
            }

            // Serpent's Ire almost off cooldown: the 2-minute window is about to open,
            // so nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.SerpentIre.IsKnown())
            {
                var cooldownMs = Spells.SerpentIre.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= SerpentIreImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "VPR Reawaken");
            }
        }
    }
}

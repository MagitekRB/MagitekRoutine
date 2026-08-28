using ff14bot;
using ff14bot.Enums;
using Magitek.Extensions;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Monk
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Monk, Spells.Bootshine);
        public static int EnemiesInCone;
        public static int AoeEnemies5Yards;
        public static int UseToast = 9;

        // How close Riddle of Fire's cooldown has to be before we report the burst
        // as imminent.
        private const int RiddleOfFireImminentMs = 5000;

        /// <summary>
        /// Reports MNK burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the MNK rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Riddle of Fire (20s, every 60s; Brotherhood-aligned every
        /// 2 minutes), entered via Perfect Balance prep in even windows. Window
        /// contents: Riddle of Fire, Brotherhood, Perfect Balance -> Masterful Blitz,
        /// Riddle of Wind, Fire's/Wind's Reply, the ~11 buffed GCDs.
        /// Sources: The Balance MNK basic guide, official job guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: own-cast Riddle of Fire (both the 1- and 2-minute uses —
            // +15% applies to everything cast under it) OR own-cast Perfect Balance,
            // which covers the choreographed prep in even windows where PB goes out
            // 2-4 GCDs before RoF. They overlap staggered, so the window is the union:
            // max remaining among whichever are up. PB only counts once Masterful
            // Blitz exists (Lv60) — at 50-59 PB is a standalone tool with no Blitz and
            // no RoF, and reporting it would be a false window. Brotherhood is
            // deliberately not part of the definition: it also lands from another
            // Monk's cast, and RoF already anchors every window.
            var countPerfectBalance = Spells.MasterfulBlitz.IsKnown();
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.RiddleOfFire && (aura.Id != Auras.PerfectBalance || !countPerfectBalance))
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "MNK Riddle of Fire");
                return;
            }

            // Riddle of Fire almost off cooldown: the burst is about to open, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.RiddleofFire.IsKnown())
            {
                var cooldownMs = Spells.RiddleofFire.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= RiddleOfFireImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "MNK Riddle of Fire");
            }
        }

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;


            EnemiesInCone = Core.Me.EnemiesInCone(40);
            AoeEnemies5Yards = Combat.Enemies.Count(x => x.WithinSpellRange(5) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
        }
    }
}

using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Reaper;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Reaper
    {
        public static int EnemiesAroundPlayer5Yards;
        public static int EnemiesIn8YardCone;
        public static ReaperComboStages CurrentComboStage = ReaperComboStages.Slice;
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Reaper, Spells.Slice);

        // How close Arcane Circle's cooldown has to be before we report the burst as
        // imminent. 10s, not 5s: the choreographed prep (Soul Slice pooling, Shadow of
        // Death refresh, Enshroud pressed ~2 GCDs early) keys off the Arcane Circle
        // cooldown clock, and the first shroud starts ~4-5s before it lands.
        private const int ArcaneCircleImminentMs = 10000;

        /// <summary>
        /// Reports RPR burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the RPR rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Arcane Circle (20s, every 2 minutes), with the double-Enshroud
        /// sequence starting ~2 GCDs before it; the Enshrouded gauge and Ideal Host
        /// carry the segments Arcane Circle misses (odd-minute shrouds, the
        /// between-shrouds seam). Window contents: Arcane Circle, Enshroud x2
        /// (Void/Cross Reaping at fixed 1.5s GCDs, Lemure's Slice, Sacrificium,
        /// Communio), Plentiful Harvest, Perfectio. Sources: The Balance RPR
        /// basic/intermediate guides, official job guide, live client via rb
        /// (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: own-cast Arcane Circle (party-wide — another Reaper's
            // lands with the same id, so only own-cast records count) and Ideal Host,
            // which bridges the seam between Communio ending shroud #1 and pressing
            // the free shroud #2. Under level sync missing buffs simply never appear —
            // no level branching needed.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.ArcaneCircle && aura.Id != Auras.IdealHost)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            // Enshrouded gauge: every Enshroud counts, odd-minute ones included —
            // Void/Cross Reaping's recast is a fixed 1.5s with strict single-weave
            // room, so any shroud is timing-sensitive. The gauge timer is self-only
            // by construction, so no caster filter is needed.
            if (ActionResourceManager.Reaper.EnshroudedTimeRemaining > remaining)
                remaining = ActionResourceManager.Reaper.EnshroudedTimeRemaining;

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "RPR Arcane Circle");
                return;
            }

            // Arcane Circle almost off cooldown: the prep sequence is already under
            // way, so nothing slow should start now. Only reported while it is
            // actually cooling down — "ready but held" is unbounded and would starve
            // consumers.
            if (Core.Me.InCombat && Spells.ArcaneCircle.IsKnown())
            {
                var cooldownMs = Spells.ArcaneCircle.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= ArcaneCircleImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "RPR Arcane Circle");
            }
        }

        public static bool CheckTTDIsEnemyDyingSoon()
        {
            return Common.CheckTTDIsEnemyDyingSoon(ReaperSettings.Instance);
        }

        // In-game tooltip potencies (7.55) for the Enshroud cone-versus-single choices: the cone attack replaces the
        // single-target action at the target count where it out-damages it. One table, so the cone and the
        // single-target methods agree on where the line is; when they disagreed (Lemure's Slice yielding at two,
        // Lemure's Scythe waiting for three) the shroud sat on four Void Shroud and did nothing until it expired.
        public const int ReapingPotency = 580;
        public const int EnhancedReapingPotency = 640;
        public const int GrimReapingPotency = 220;
        public const int LemuresSlicePotency = 280;
        public const int LemuresScythePotency = 100;

        // Soul Slice and Soul Scythe share two 30 s charges (the recast ignores skill speed). True when both are
        // up, or the second arrives within one weaponskill GCD - the point past which waiting loses a charge.
        // Below level 78 there is one charge and this reads "ready or within a GCD of ready", the single-charge
        // rule: cast it whatever the gauge reads.
        public static bool SoulSliceChargeAboutToCap =>
            Spells.SoulSlice.Charges >= Spells.SoulSlice.MaxCharges - Spells.Slice.AdjustedCooldown.TotalMilliseconds / Spells.SoulSlice.AdjustedCooldown.TotalMilliseconds;

        // Reaper uses an 8x8 square in front for its "cone". So it can hit something 90* to the side 8y away.
        public static int EnemiesInReaperCone(float maxdistance)
        {
            return Combat.Enemies.Count(r => r.Distance(Core.Me) <= maxdistance + r.CombatReach && r.InCustomRadiantCone(1.57079f));
        }

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesIn8YardCone = EnemiesInReaperCone(8);
            EnemiesAroundPlayer5Yards = Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach);
        }
    }
}
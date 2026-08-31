using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Magitek.Utilities
{
    internal static class Combat
    {
        public static readonly List<BattleCharacter> Enemies = new List<BattleCharacter>();
        public static readonly Stopwatch CombatTime = new Stopwatch();
        public static readonly Stopwatch OutOfCombatTime = new Stopwatch();
        public static readonly Stopwatch MovingInCombatTime = new Stopwatch();
        public static readonly Stopwatch NotMovingInCombatTime = new Stopwatch();
        public static int CombatTotalTimeLeft;
        // Wall-clock estimate of when the PULL ends: the longest single enemy time-to-die.
        // CombatTotalTimeLeft sums every enemy's estimate, which overstates the pull's real
        // duration whenever enemies die concurrently (four mobs at 3s each sum to 12s) — use
        // this for "is the fight about to end" checks and the sum for total-effort checks.
        public static int CombatWallClockTimeLeft;
        public static readonly Stopwatch DutyTime = new Stopwatch();
        public static double CurrentTargetCombatTimeLeft;

        public static bool IsBoss()
        {
            return Core.Me.CurrentTarget.IsBoss() || (Globals.InActiveDuty && Enemies.Count == 1);
        }

        public static bool IsMoving(GameObject target)
        {
            //if (!Tracking.EnemyInfos.Any(r => r.Unit == target && r.IsMoving)) return false;

            var movingTarget = Tracking.EnemyInfos.FirstOrDefault(r => r.Unit == target && r.IsMoving);

            return movingTarget != null && movingTarget.IsMovingChange.ElapsedMilliseconds > 2000;
        }

        public static void AdjustCombatTime()
        {
            //General Combat Status Check
            if (Core.Me.InCombat)
            {
                AdjustInCombatTimers();
                return;
            }

            //If Our Party Has Tagged Something We're Also InCombat
            if (Globals.InParty && Globals.PartyInCombat)
            {
                AdjustInCombatTimers();
                return;
            }

            AdjustOutOfCombatTimers();

            // Private methods
            void AdjustInCombatTimers()
            {
                if (!CombatTime.IsRunning)
                {
                    CombatTime.Start();
                }

                if (OutOfCombatTime.IsRunning)
                {
                    OutOfCombatTime.Reset();
                }

                if (MovementManager.IsMoving)
                {
                    if (!MovingInCombatTime.IsRunning)
                    {
                        MovingInCombatTime.Restart();
                    }

                    if (NotMovingInCombatTime.IsRunning)
                    {
                        NotMovingInCombatTime.Reset();
                    }
                }
                else
                {
                    if (MovingInCombatTime.IsRunning)
                    {
                        MovingInCombatTime.Reset();
                    }

                    if (!NotMovingInCombatTime.IsRunning)
                    {
                        NotMovingInCombatTime.Restart();
                    }
                }
            }

            void AdjustOutOfCombatTimers()
            {
                if (CombatTime.IsRunning)
                {
                    CombatTime.Reset();
                }

                if (!OutOfCombatTime.IsRunning)
                {
                    OutOfCombatTime.Start();
                }
            }
        }

        public static void AdjustDutyTime()
        {
            var inDuty = DutyManager.InInstance;

            if (inDuty && !DutyTime.IsRunning)
                DutyTime.Start();

            if (inDuty || !DutyTime.IsRunning)
                return;

            DutyTime.Reset();
            DutyTime.Stop();
        }

        public static BattleCharacter SmartAoeTarget(SpellData spell, bool smartAoeSetting = false)
        {
            if (!Core.Me.InCombat)
                return null;

            if (!smartAoeSetting)
                return Core.Me.CurrentTarget == null ? null : (BattleCharacter)Core.Me.CurrentTarget;

            // Combat.Enemies deliberately keeps damage-immune units (immunity gating lives at
            // offensive call sites, not in the collection), so the picker must check
            // CanBeDamagedByMe itself: the client accepts casts at damage-immune enemies and
            // nullifies the result. Immune units are excluded from the anchor AND from the
            // density scoring, so clusters of mostly-immune enemies stop attracting casts.
            var bestTarget = Enemies.Where(x => x.WithinSpellRange(spell.Range) && x.CanBeDamagedByMe())
                .OrderByDescending(x => x.EnemiesNearby(spell.Radius).Count(y => y.CanBeDamagedByMe()));

            return bestTarget?.FirstOrDefault();
        }
    }
}

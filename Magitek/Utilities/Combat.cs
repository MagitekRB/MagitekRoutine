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

        // Every live hostile, including ones we currently cannot damage. Enemies drops those, which
        // is right for targeting and wrong for reacting: a boss behind a damage-type immunity still
        // casts the mechanics we need to mitigate.
        public static readonly List<BattleCharacter> Threats = new List<BattleCharacter>();
        public static readonly Stopwatch CombatTime = new Stopwatch();
        public static readonly Stopwatch OutOfCombatTime = new Stopwatch();
        public static readonly Stopwatch MovingInCombatTime = new Stopwatch();
        public static readonly Stopwatch NotMovingInCombatTime = new Stopwatch();
        public static int CombatTotalTimeLeft;
        public static readonly Stopwatch DutyTime = new Stopwatch();
        public static double CurrentTargetCombatTimeLeft;

        public static bool IsBoss()
        {
            // Threats, not Enemies, for the single-enemy fallback: "alone with one enemy in a duty"
            // has to stay true while that enemy is temporarily immune to us.
            return Core.Me.CurrentTarget.IsBoss() || (Globals.InActiveDuty && Threats.Count == 1);
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

            var bestTarget = Enemies.Where(x => x.WithinSpellRange(spell.Range))
                .OrderByDescending(x => x.EnemiesNearby(spell.Radius).Count());

            return bestTarget?.FirstOrDefault();
        }
    }
}

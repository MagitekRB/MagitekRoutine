using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using Magitek.Extensions;
using System.Threading.Tasks;
using BaseSettings = Magitek.Models.Account.BaseSettings;

namespace Magitek.Utilities
{
    internal static class Movement
    {

        public static void NavigateToUnitLos(GameObject unit, float distance)
        {
            if (!BaseSettings.Instance.MagitekMovement)
                return;

            if (RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement))
                return;

            if (RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing) && MovementManager.IsMoving)
                return;

            if (unit == null)
                return;

            if (AvoidanceManager.IsRunningOutOfAvoid)
                return;

            // A movement-punishing mechanic (Acceleration Bomb, Pyretic) has parked navigation. This belongs
            // here rather than at the callers: six of the eight navigation sites are job rotations that never
            // consulted the latch, so stopping in the fight-logic handler was immediately undone by whichever
            // rotation navigated next in the same pulse.
            if (FightLogic.MovementHeld)
                return;

            //if (!MovementManager.IsMoving && !unit.InView() && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing))
            //   Core.Me.Face(Core.Me.CurrentTarget);

            if (unit.Distance(Core.Me) > distance)
            {
                Navigator.MoveTo(new MoveToParameters(unit.Location));
            }

            if (Core.Me.Distance(unit.Location) <= distance && unit.InView() && unit.InLineOfSight())
            {
                if (MovementManager.IsMoving)
                {
                    Navigator.PlayerMover.MoveStop();
                }
            }
            else
            {
                Navigator.MoveTo(new MoveToParameters(unit.Location));
            }
        }

        public static async Task<bool> Dismount()
        {
            if (!Core.Me.IsMounted)
                return false;

            while (Core.Me.IsMounted)
            {
                ActionManager.Dismount();
                await Coroutine.Yield();
            }

            return true;
        }

        /// <summary>
        /// Checks if gap closer abilities can be used based on CapabilityManager flags.
        /// Gap closers are disabled if either GapCloser or Movement flags are disallowed, or while a
        /// movement-punishing mechanic has movement parked.
        /// </summary>
        public static bool CanUseGapCloser()
        {
            // Acceleration Bomb only objects to movement, so the routine deliberately keeps acting through
            // it — but a gap closer IS movement wearing an action's clothing, and it moves the character
            // just as surely as the navigator would. Parked here rather than at the fourteen call sites.
            if (FightLogic.MovementHeld)
                return false;

            return !RoutineManager.IsAnyDisallowed(CapabilityFlags.GapCloser | CapabilityFlags.Movement);
        }

        /// <summary>
        /// Checks if general movement is allowed based on CapabilityManager flags.
        /// </summary>
        public static bool CanUseMovement()
        {
            return !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement);
        }
    }
}

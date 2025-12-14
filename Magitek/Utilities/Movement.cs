using Buddy.Coroutines;
using ff14bot;
using ff14bot.Behavior;
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

        public static async Task NavigateToUnitLos(GameObject unit, float distance)
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

            //if (!MovementManager.IsMoving && !unit.InView() && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing))
            //   Core.Me.Face(Core.Me.CurrentTarget);

            var me = Core.Me;

            //Care about line of sight first
            if (!unit.InLineOfSight() || unit.Distance(me) > distance)
            {
                await CommonTasks.MoveTo(unit.Location);
            }

            if (unit.InView())
            {
                if (MovementManager.IsMoving)
                {
                    Navigator.PlayerMover.MoveStop();
                }
            }
            else
            {
                unit.Face();
            }
        }
    }
}

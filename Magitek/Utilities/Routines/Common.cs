using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Roles;

namespace Magitek.Utilities.Routines
{
    internal class Common
    {
        public static bool CheckTTDIsEnemyDyingSoon<T>(T settings) where T : JobSettings
        {
            return settings.UseTTD
                && Combat.CurrentTargetCombatTimeLeft < settings.SaveIfEnemyDyingWithin
                && !Core.Me.CurrentTarget.IsBoss();
        }
    }
}

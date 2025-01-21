using ff14bot;
using ff14bot.Enums;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Viper
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Reaper, Spells.Slice);

        public static int EnemiesAroundPlayer5Yards;

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesAroundPlayer5Yards = Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach);
        }
    }
}
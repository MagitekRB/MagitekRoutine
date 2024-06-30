using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Utilities.Routines
{
    internal static class Viper
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Viper, Spells.SteelMaw, new List<SpellData>() { });

        public static int AoeEnemies5Yards;
        public static int AoeEnemies6Yards;

        public static int OpenerBurstAfterGCD = 2;


        public static DateTime oGCD = DateTime.Now;


        public static void RefreshVars()
        {

            //AoeEnemies5Yards = Core.Me.EnemiesNearby(5).Count();
            //AoeEnemies6Yards = Core.Me.CurrentTarget.EnemiesNearby(6).Count();

        }
    }
}

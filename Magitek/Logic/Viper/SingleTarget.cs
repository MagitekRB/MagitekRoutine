using System.Threading.Tasks;
using System.Linq;
using System;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Utilities;
using ViperRoutine = Magitek.Utilities.Routines.Viper;

namespace Magitek.Logic.Viper
{
    internal static class SingleTarget
    {

        #region Base Combo

        public static async Task<bool> SteelMaw()
        {
            if (!Spells.SteelMaw.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.SteelMaw.Cast(Core.Me.CurrentTarget);
        }


        #endregion

        
    }
}

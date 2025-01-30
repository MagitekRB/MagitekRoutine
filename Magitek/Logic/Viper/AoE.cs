using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Viper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using ViperRoutine = Magitek.Utilities.Routines.Viper;

namespace Magitek.Logic.Viper
{
    internal static class AoE
    {
        public static async Task<bool> SteelReavingMaw()
        {
            if (Core.Me.ClassLevel < Spells.SteelMaw.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseAoe)
                return false;

            if (ViperRoutine.EnemiesAroundPlayer5Yards < ViperSettings.Instance.AoeEnemies)
                return false;

            if (Core.Me.ClassLevel >= Spells.ReavingMaw.LevelAcquired && Core.Me.HasAura(Auras.HonedReavers, true))
                return await Spells.ReavingMaw.Cast(Core.Me);
            else
                return await Spells.SteelMaw.Cast(Core.Me);
        }

        public static async Task<bool> HunterOrSwiftSkinBite()
        {
            if (Core.Me.ClassLevel < Spells.HunterBite.LevelAcquired)
                return false;

            if (Core.Me.ClassLevel < Spells.SwiftskinBite.LevelAcquired)
                return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HindstungVenom, true) || Core.Me.HasAura(Auras.GrimskinVenom, true))
                return await Spells.SwiftskinBite.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.FlankstungVenom, true) || Core.Me.HasAura(Auras.GrimhunterVenom, true))
                return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

            return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> JaggedOrBloodiedMaw()
        {
            if (Core.Me.ClassLevel < Spells.JaggedMaw.LevelAcquired)
                return false;

            if (Core.Me.HasAura(Auras.GrimhunterVenom, true))
                return await Spells.JaggedMaw.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel >= Spells.BloodiedMaw.LevelAcquired && Core.Me.HasAura(Auras.GrimskinVenom, true))
                return await Spells.BloodiedMaw.Cast(Core.Me.CurrentTarget);

            else
                return await Spells.JaggedMaw.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Vicepit()
        {
            if (Core.Me.ClassLevel < Spells.Vicepit.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseAoe)
                return false;

            if (ViperRoutine.EnemiesAroundPlayer5Yards < ViperSettings.Instance.AoeEnemies)
                return false;

            if (!ViperSettings.Instance.UseVicepit)
                return false;

            return await Spells.Vicepit.Cast(Core.Me);
        }

        public static async Task<bool> HunterOrSwiftskinDen()
        {
            if (Core.Me.ClassLevel < Spells.HunterDen.LevelAcquired)
                return false;

            if (Spells.HunterDen.CanCast())
                return await Spells.HunterDen.Cast(Core.Me);
            else
                return await Spells.SwiftskinDen.Cast(Core.Me);
        }
    }
}
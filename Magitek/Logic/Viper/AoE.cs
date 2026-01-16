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
            if (!Spells.SteelMaw.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseAoe)
                return false;

            if (ViperRoutine.EnemiesAroundPlayer5Yards < ViperSettings.Instance.AoeEnemies)
                return false;

            if (Spells.ReavingMaw.IsKnown() && Core.Me.HasAura(Auras.HonedReavers, true))
                return await Spells.ReavingMaw.Cast(Core.Me);
            else
                return await Spells.SteelMaw.Cast(Core.Me);
        }

        public static async Task<bool> HunterOrSwiftSkinBite()
        {
            if (!Spells.HunterBite.IsKnown())
                return false;

            if (!Spells.SwiftskinBite.IsKnown())
                return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HindstungVenom, true) || Core.Me.HasAura(Auras.GrimskinVenom, true))
                return await Spells.SwiftskinBite.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.FlankstungVenom, true) || Core.Me.HasAura(Auras.GrimhunterVenom, true))
                return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

            return await Spells.HunterBite.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> JaggedOrBloodiedMaw()
        {
            if (!Spells.JaggedMaw.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.GrimhunterVenom, true))
                return await Spells.JaggedMaw.Cast(Core.Me.CurrentTarget);

            if (Spells.BloodiedMaw.IsKnown() && Core.Me.HasAura(Auras.GrimskinVenom, true))
                return await Spells.BloodiedMaw.Cast(Core.Me.CurrentTarget);

            else
                return await Spells.JaggedMaw.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Vicepit()
        {
            if (!Spells.Vicepit.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseAoe)
                return false;

            if (ViperRoutine.EnemiesAroundPlayer5Yards < ViperSettings.Instance.AoeEnemies)
                return false;

            if (!ViperSettings.Instance.UseVicepit)
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            return await Spells.Vicepit.Cast(Core.Me);
        }

        public static async Task<bool> HunterOrSwiftskinDen()
        {
            if (!Spells.HunterDen.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Spells.HunterDen.CanCast())
                return await Spells.HunterDen.Cast(Core.Me);
            else
                return await Spells.SwiftskinDen.Cast(Core.Me);
        }
    }
}
using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Viper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Viper
{
    internal static class Cooldown
    {

        public static async Task<bool> DeathRattle()
        {
            //Add level check so it doesn't hang here
            if (Core.Me.ClassLevel < Spells.DeathRattle.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseDeathRattle)
                return false;

            return await Spells.DeathRattle.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> LastLash()
        {
            //Add level check so it doesn't hang here
            if (Core.Me.ClassLevel < Spells.LastLash.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseLastLash)
                return false;

            if (!Spells.LastLash.CanCast())
                return false;

            return await Spells.LastLash.Cast(Core.Me);
        }

        public static async Task<bool> TwinBiteCombo()
        {
            if (Core.Me.ClassLevel >= Spells.TwinfangBite.LevelAcquired && Core.Me.HasAura(Auras.HunterVenom, true))
                return await Spells.TwinfangBite.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel >= Spells.TwinbloodBite.LevelAcquired && Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return await Spells.TwinbloodBite.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> TwinThreshCombo()
        {
            if (Core.Me.ClassLevel >= Spells.TwinfangThresh.LevelAcquired && Core.Me.HasAura(Auras.FellhunterVenom, true))
                return await Spells.TwinfangThresh.Cast(Core.Me);

            if (Core.Me.ClassLevel >= Spells.TwinbloodThresh.LevelAcquired && Core.Me.HasAura(Auras.FellskinVenom, true))
                return await Spells.TwinbloodThresh.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> UncoiledTwinCombo()
        {
            if (Core.Me.ClassLevel >= Spells.UncoiledTwinfang.LevelAcquired && Core.Me.HasAura(Auras.PoisedforTwinfang, true))
                return await Spells.UncoiledTwinfang.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel >= Spells.UncoiledTwinblood.LevelAcquired && Core.Me.HasAura(Auras.PoisedforTwinblood, true))
                return await Spells.UncoiledTwinblood.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> SerpentIre()
        {
            if (Core.Me.ClassLevel < Spells.SerpentIre.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseSerpentIre || ViperSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Core.Me.HasAura(Auras.Swiftscaled, true))
                return false;

            return await Spells.SerpentIre.Cast(Core.Me);

        }

        public static async Task<bool> FirstLegacy()
        {
            if (Core.Me.ClassLevel < Spells.FirstLegacy.LevelAcquired)
                return false;

            if (!Spells.FirstLegacy.CanCast())
                return false;

            return await Spells.FirstLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> SecondLegacy()
        {
            if (Core.Me.ClassLevel < Spells.SecondLegacy.LevelAcquired)
                return false;

            if (!Spells.SecondLegacy.CanCast())
                return false;

            return await Spells.SecondLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> ThirdLegacy()
        {
            if (Core.Me.ClassLevel < Spells.ThirdLegacy.LevelAcquired)
                return false;

            if (!Spells.ThirdLegacy.CanCast())
                return false;

            return await Spells.ThirdLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> FourthLegacy()
        {
            if (Core.Me.ClassLevel < Spells.FourthLegacy.LevelAcquired)
                return false;

            if (!Spells.FourthLegacy.CanCast())
                return false;

            return await Spells.FourthLegacy.Cast(Core.Me.CurrentTarget);

        }

    }
}
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
            if (!Spells.DeathRattle.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseDeathRattle)
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.DeathRattle.Range))
                return false;

            return await Spells.DeathRattle.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> LastLash()
        {
            //Add level check so it doesn't hang here
            if (!Spells.LastLash.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseLastLash)
                return false;

            if (!Spells.LastLash.CanCast())
                return false;

            return await Spells.LastLash.Cast(Core.Me);
        }

        public static async Task<bool> TwinBiteCombo()
        {
            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Spells.TwinfangBite.IsKnown() && Core.Me.HasAura(Auras.HunterVenom, true))
                return await Spells.TwinfangBite.Cast(Core.Me.CurrentTarget);

            if (Spells.TwinbloodBite.IsKnown() && Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return await Spells.TwinbloodBite.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> TwinThreshCombo()
        {
            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Spells.TwinfangThresh.IsKnown() && Core.Me.HasAura(Auras.FellhunterVenom, true))
                return await Spells.TwinfangThresh.Cast(Core.Me);

            if (Spells.TwinbloodThresh.IsKnown() && Core.Me.HasAura(Auras.FellskinVenom, true))
                return await Spells.TwinbloodThresh.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> UncoiledTwinCombo()
        {

            if (Spells.UncoiledTwinfang.IsKnown() && Core.Me.HasAura(Auras.PoisedforTwinfang, true))
                return await Spells.UncoiledTwinfang.Cast(Core.Me.CurrentTarget);

            if (Spells.UncoiledTwinblood.IsKnown() && Core.Me.HasAura(Auras.PoisedforTwinblood, true))
                return await Spells.UncoiledTwinblood.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> SerpentIre()
        {
            if (!Spells.SerpentIre.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseSerpentIre || ViperSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Core.Me.HasAura(Auras.Swiftscaled, true))
                return false;

            return await Spells.SerpentIre.Cast(Core.Me);

        }

        public static async Task<bool> FirstLegacy()
        {
            if (!Spells.FirstLegacy.IsKnown())
                return false;

            if (!Spells.FirstLegacy.CanCast())
                return false;

            return await Spells.FirstLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> SecondLegacy()
        {
            if (!Spells.SecondLegacy.IsKnown())
                return false;

            if (!Spells.SecondLegacy.CanCast())
                return false;

            return await Spells.SecondLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> ThirdLegacy()
        {
            if (!Spells.ThirdLegacy.IsKnown())
                return false;

            if (!Spells.ThirdLegacy.CanCast())
                return false;

            return await Spells.ThirdLegacy.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> FourthLegacy()
        {
            if (!Spells.FourthLegacy.IsKnown())
                return false;

            if (!Spells.FourthLegacy.CanCast())
                return false;

            return await Spells.FourthLegacy.Cast(Core.Me.CurrentTarget);

        }

    }
}
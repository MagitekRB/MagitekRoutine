using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Viper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Viper
{
    internal static class Pvp
    {
        public static async Task<bool> WorldGeneration()
        {
            if (!Core.Me.HasAura(Auras.PvpReawakened, true))
                return false;
            if (Spells.FirstGenerationPvp.CanCast() && await Spells.FirstGenerationPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.FirstGenerationPvp.HasCastRecently() && Spells.SecondGenerationPvp.CanCast() && await Spells.SecondGenerationPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.SecondGenerationPvp.HasCastRecently() && Spells.ThirdGenerationPvp.CanCast() && await Spells.ThirdGenerationPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.ThirdGenerationPvp.HasCastRecently() && Spells.FourthGenerationPvp.CanCast() && await Spells.FourthGenerationPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.FourthGenerationPvp.HasCastRecently() && Spells.OuroborosPvp.CanCast())
                return await Spells.OuroborosPvp.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> DualFang()
        {
            if (Core.Me.HasAura(Auras.PvpReawakened, true))
                return false;
            if (Spells.RavenousBitePvp.CanCast() && await Spells.RavenousBitePvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.SwiftskinsStingPvp.CanCast() && await Spells.SwiftskinsStingPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.PiercingFangsPvp.CanCast() && await Spells.PiercingFangsPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.BarbarousBitePvp.CanCast() && await Spells.BarbarousBitePvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.HuntersStringPvp.CanCast() && await Spells.HuntersStringPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;
            if (Spells.SteelFangsPvp.CanCast() && await Spells.SteelFangsPvp.CastPvpCombo(Spells.DualFangPvpCombo, Core.Me.CurrentTarget))
                return true;

            return false;
        }

        public static async Task<bool> SerpentsTail()
        {
            // masked action to cast before anything else
            var spell = Spells.SerpentsTailPvp.Masked();

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RattlingCoil()
        {
            // resets uncoiled fury && snake scales
            var uncoiledFury = Spells.UncoiledFuryPvp;

            if (Spells.OuroborosPvp.CanCast() && Core.Me.CurrentTarget.WithinSpellRange(Spells.OuroborosPvp.Range))
                return false;

            if (uncoiledFury.Cooldown.TotalMilliseconds <= 5000)
                return false;

            if (Core.Me.HasAura(Auras.PvpReawakened, true))
                return false;

            if (!Spells.RattlingCoilPvp.CanCast())
                return false;

            return await Spells.RattlingCoilPvp.Cast(Core.Me);
        }

        public static async Task<bool> UncoiledFury()
        {
            var spell = Spells.UncoiledFuryPvp;

            if (Core.Me.HasAura(Auras.PvpReawakened, true) && Core.Me.CurrentTarget.WithinSpellRange(Spells.UncoiledFuryPvp.Radius))
                return false;

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > ViperSettings.Instance.Pvp_UncoiledFuryHealthPercent)
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Bloodcoil()
        {
            var spell = Spells.BloodcoilPvp.Masked();

            if (spell.Charges < 1)
                return false;

            if (Core.Me.HasAura(Auras.PvpReawakened, true))
                return false;

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SnakeScales()
        {
            if (!ViperSettings.Instance.Pvp_SnakeScales)
                return false;

            var spell = Spells.SnakeScalesPvp.Masked();

            if (Core.Me.CurrentHealthPercent > ViperSettings.Instance.Pvp_SnakeScalesHealthPercent)
                return false;

            if (!spell.CanCast())
                return false;

            if (spell == Spells.SnakeScalesPvp)
                return await spell.Cast(Core.Me);
            //else
            // spell is now backlash
            //return await spell.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> WorldSwallower()
        {
            if (!ViperSettings.Instance.Pvp_WorldSwallower)
                return false;

            if (!Spells.WorldswallowerPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.WorldswallowerPvp.Range))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > ViperSettings.Instance.Pvp_WorldSwallowerHealthPercent)
                return false;

            return await Spells.WorldswallowerPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

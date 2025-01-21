using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Viper;
using Magitek.Utilities;
using System.Threading.Tasks;

namespace Magitek.Logic.Viper
{
    internal static class Utility
    {

        public static async Task<bool> TrueNorth()
        {
            if (ViperSettings.Instance.EnemyIsOmni || !ViperSettings.Instance.UseTrueNorth) return false;

            if (Core.Me.HasAura(Auras.TrueNorth))
                return false;

            if (Core.Me.CurrentTarget.IsBehind)
                return false;

            if (!Core.Me.HasAura(Auras.HindstungVenom) && !Core.Me.HasAura(Auras.HindsbaneVenom))
                return false;

            if ((Casting.LastSpell == Spells.HunterSting || Casting.LastSpell == Spells.SwiftskinSting))
                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);

            return false;

        }

        public static async Task<bool> UsePotion()
        {
            if (!Core.Me.HasAura(Auras.Swiftscaled, true))
                return false;

            return await PhysicalDps.UsePotion(ViperSettings.Instance);
        }

    }
}
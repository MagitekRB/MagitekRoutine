using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Scholar
{
    internal static class Aoe
    {
        public static async Task<bool> ArtOfWar()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!ScholarSettings.Instance.ArtOfWar)
                return false;

            if (Core.Me.EnemiesNearby(5).Count() < ScholarSettings.Instance.ArtOfWarEnemies)
                return false;

            return await Spells.ArtOfWar.Cast(Core.Me);
        }

        public static async Task<bool> BanefulImpaction()
        {
            if (!ScholarSettings.Instance.BanefulImpaction)
                return false;

            if (!Core.Me.HasAura(Auras.ImpactImminent))
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.CanAttack)
                return false;

            return await Spells.BanefulImpaction.Cast(target);
        }
    }
}

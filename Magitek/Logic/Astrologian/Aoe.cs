using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Astrologian
{
    internal static class Aoe
    {
        public static async Task<bool> Gravity()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!AstrologianSettings.Instance.Gravity)
                return false;

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            var target = Combat.SmartAoeTarget(Spells.Gravity, AstrologianSettings.Instance.SmartAoe);
            if (target == null)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Gravity.Radius) < AstrologianSettings.Instance.GravityEnemies)
                return false;

            return await Spells.Gravity.Cast(target);
        }

        public static async Task<bool> LordOfCrown()
        {
            //if (ActionResourceManager.Astrologian.CurrentDraw != ActionResourceManager.Astrologian.AstrologianDraw.Astral)
            //    return false;

            if (!AstrologianSettings.Instance.LordOfCrowns)
                return false;

            // Lord is a damage oGCD - the master damage switch applies to it like everything else.
            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            if (!Spells.LordofCrowns.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(20).Count() < AstrologianSettings.Instance.LordOfCrownsEnemies && AstrologianSettings.Instance.LordOfCrownsEnemies > 1)
                return false;

            return await Spells.LordofCrowns.Cast(Core.Me);

        }

        public static async Task<bool> Oracle()
        {
            if (!AstrologianSettings.Instance.Oracle)
                return false;

            // Oracle is a damage oGCD - the master damage switch applies to it like everything else.
            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            if (!Spells.Oracle.IsKnownAndReady())
                return false;

            // Snapshot the target rather than reading it repeatedly. The game can clear it at any point,
            // including partway through the Count below, and a null reaching Distance() inside the
            // predicate throws out of the whole rotation instead of just declining this one action.
            var target = Core.Me.CurrentTarget;

            if (target == null)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Oracle.Radius) < AstrologianSettings.Instance.OracleEnemies)
                return false;

            if (!Core.Me.HasAura(Auras.Divining, true))
                return false;

            return await Spells.Oracle.Cast(target);

        }

    }
}
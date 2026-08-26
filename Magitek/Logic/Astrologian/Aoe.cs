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

            // A damage oGCD has no business firing out of combat - but in a duty the
            // heal-oGCD block runs between pulls too (InActiveDuty stays true for the whole
            // instance), and Lord was landing 0.7s after out-of-combat raises: measured five
            // times across two days, once onto a still-idle pack.
            if (!Core.Me.InCombat)
                return false;

            if (!Spells.LordofCrowns.IsKnownAndReady())
                return false;

            // Lord is centred on the caster, so count around US rather than the current
            // target - and always require at least one enemy in range: the old guard skipped
            // counting entirely at the default setting, and dereferenced a target that
            // between pulls often does not exist. WithinSpellRange rather than EnemiesNearby:
            // called on ourselves the latter adds our own combat reach twice and never the
            // enemy's, so a big-hitbox target inside the radius could go uncounted.
            if (Combat.Enemies.Count(r => r.WithinSpellRange(20)) < System.Math.Max(1, AstrologianSettings.Instance.LordOfCrownsEnemies))
                return false;

            return await Spells.LordofCrowns.Cast(Core.Me);

        }

        public static async Task<bool> Oracle()
        {
            if (!AstrologianSettings.Instance.Oracle)
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
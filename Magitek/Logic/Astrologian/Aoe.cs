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

            // EnemiesNearby, matching the smart-AoE picker and the measured game geometry
            // (radius + the counted enemy's own hitbox): a bare centre-distance count missed
            // big-hitbox enemies inside the real blast and disagreed with the picker's
            // ranking, refusing targets it had just selected.
            if (target.EnemiesNearby(Spells.Gravity.Radius).Count() < AstrologianSettings.Instance.GravityEnemies)
                return false;

            return await Spells.Gravity.Cast(target);
        }

        public static async Task<bool> LordOfCrown()
        {
            //if (ActionResourceManager.Astrologian.CurrentDraw != ActionResourceManager.Astrologian.AstrologianDraw.Astral)
            //    return false;

            // Same three gates Gravity carries, in the same order: Lord is a self-centred
            // damage AoE, but it is dispatched from the heal/buff blocks, which run before
            // the DoDamage, StopDamageWhenMoreThanEnemies and mana gates in Combat() - so it
            // never reached any of them and fired with the global AoE toggle off.
            if (!AoeControl.Enabled)
                return false;

            if (!AstrologianSettings.Instance.LordOfCrowns)
                return false;

            // Lord is a damage oGCD - the master damage switch applies to it like everything else.
            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            // It has no business firing out of combat either - but in a duty the heal-oGCD
            // block runs between pulls too (InActiveDuty stays true for the whole instance),
            // and Lord was landing 0.7s after out-of-combat raises: measured five times
            // across two days, once onto a still-idle pack.
            if (!Core.Me.InCombat)
                return false;

            if (!Spells.LordofCrowns.IsKnownAndReady())
                return false;

            // Lord is centred on the caster, so count around US rather than the current
            // target, which between pulls often does not exist. EnemiesNearby is the right
            // geometry since it was reworked to add only the counted enemy's hitbox - exactly
            // how a self-centred blast resolves; WithinSpellRange is an edge-to-edge range
            // check and padded the count with our own reach.
            if (Core.Me.EnemiesNearby(Spells.LordofCrowns.Radius).Count() < AstrologianSettings.Instance.LordOfCrownsEnemies)
                return false;

            return await Spells.LordofCrowns.Cast(Core.Me);

        }

        public static async Task<bool> Oracle()
        {
            // Same exposure as Lord above: dispatched from CombatBuff(), which runs before
            // Combat()'s damage gates, so it needs its own. Divination is a party buff and is
            // not damage-gated, so the Divining requirement below does not stand in for these.
            if (!AoeControl.Enabled)
                return false;

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

            // Same geometry as Gravity above: the blast reaches radius + each enemy's hitbox.
            if (target.EnemiesNearby(Spells.Oracle.Radius).Count() < AstrologianSettings.Instance.OracleEnemies)
                return false;

            if (!Core.Me.HasAura(Auras.Divining, true))
                return false;

            return await Spells.Oracle.Cast(target);

        }

    }
}
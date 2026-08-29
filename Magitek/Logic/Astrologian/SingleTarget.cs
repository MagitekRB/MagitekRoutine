using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class SingleTarget
    {
        public static async Task<bool> Malefic()
        {
            if (!AstrologianSettings.Instance.Malefic)
                return false;

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            if (!Spells.Malefic.IsReady())
                return false;

            return await Spells.Malefic.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> CombustMultipleTargets()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!AstrologianSettings.Instance.Combust)
                return false;

            if (AstrologianSettings.Instance.CombustUpToEnemies < 2)
                return false;

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            // One positive dial instead of the old cap/blanket toggles: keep the dot rolling
            // on up to this many enemies, counting the ones already carrying it.
            if (Combat.Enemies.Count(e => e.HasAnyAura(CombustAuras, true)) >= AstrologianSettings.Instance.CombustUpToEnemies)
                return false;

            var combustTarget = Combat.Enemies.FirstOrDefault(NeedsCombust);

            if (combustTarget == null)
                return false;

            return await Spells.Combust.Cast(combustTarget);

            bool NeedsCombust(BattleCharacter unit)
            {
                if (!CanCombust(unit))
                    return false;

                // Same guard as the single-target path: at 25+ statuses the debuff silently
                // fails to apply, and a cast that lands no aura re-selects this enemy every
                // pulse - an unbounded loop of zero-damage GCDs.
                if (unit.CharacterAuras.Count() >= 25)
                    return false;

                return !unit.HasAnyAura(CombustAuras, true, msLeft: AstrologianSettings.Instance.CombustRefreshMSeconds);
            }

            bool CanCombust(GameObject unit)
            {
                if (!AstrologianSettings.Instance.UseTTDForCombust)
                    return true;

                // Same rule as the single-target path: bosses are always worth the dot - their
                // time-to-death estimate is unreliable and they exempt there too.
                if (unit.IsBoss())
                    return true;

                return unit.CombatTimeLeft() >= AstrologianSettings.Instance.DontCombustIfEnemyDyingWithin;
            }
        }

        public static async Task<bool> Combust()
        {
            if (!AstrologianSettings.Instance.Combust)
                return false;

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            if (!Spells.Combust.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.UseTTDForCombust)
            {
                if (Combat.CurrentTargetCombatTimeLeft
                    <= AstrologianSettings.Instance.DontCombustIfEnemyDyingWithin
                    && !Core.Me.CurrentTarget.IsBoss())
                {
                    return false;
                }
            }

            var target = Core.Me.CurrentTarget as Character;

            if (target == null)
                return false;

            if (target.CharacterAuras.Count() >= 25)
                return false;

            // The dial covers this path too: a fresh dot on the current target spends the
            // same budget the multi-target path counts, or target-swapping would ride the
            // primary cast past any cap. Refreshing a target already carrying it is free.
            if (!Core.Me.CurrentTarget.HasAnyAura(CombustAuras, true)
                && Combat.Enemies.Count(e => e.HasAnyAura(CombustAuras, true)) >= AstrologianSettings.Instance.CombustUpToEnemies)
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(CombustAuras, true, msLeft: AstrologianSettings.Instance.CombustRefreshMSeconds))
                return false;

            return await Spells.Combust.Cast(Core.Me.CurrentTarget);
        }

        private static readonly uint[] CombustAuras =
        {
            Auras.Combust,
            Auras.Combust2,
            Auras.Combust3
        };
    }
}

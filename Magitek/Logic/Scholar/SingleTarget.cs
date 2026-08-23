using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Scholar
{
    internal static class SingleTarget
    {
        public static async Task<bool> Broil()
        {
            if (!ScholarSettings.Instance.RuinBroil)
                return false;

            return await Spells.SchRuin.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Ruin2()
        {
            if (!ScholarSettings.Instance.Ruin2)
                return false;

            if (Core.Me.HasAura(Auras.Swiftcast))
                return false;

            if (!MovementManager.IsMoving)
                return false;

            return await Spells.Ruin2.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BioMultipleTargets()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!ScholarSettings.Instance.Bio)
                return false;

            if (!ScholarSettings.Instance.BioMultipleTargets)
                return false;

            if (Combat.Enemies.Count(HasMyBio) >= ScholarSettings.Instance.BioTargetLimit)
                return false;

            if (ScholarSettings.Instance.DontDotIfMoreEnemies
                && ScholarSettings.Instance.DontDotIfMoreEnemiesThan > 0
                && Combat.Enemies.Count > ScholarSettings.Instance.DontDotIfMoreEnemiesThan)
                return false;

            var bioTarget = Combat.Enemies.FirstOrDefault(NeedsBio);

            if (bioTarget == null)
                return false;

            return await Spells.Bio.Cast(bioTarget);

            bool HasMyBio(BattleCharacter unit)
            {
                if (unit == null) return false;

                return unit.HasAnyAura(BioAuras, true, ScholarSettings.Instance.BioRefreshSeconds * 1000);
            }

            bool NeedsBio(BattleCharacter unit)
            {
                if (!CanBio(unit))
                    return false;

                return !unit.HasAnyAura(BioAuras, true, ScholarSettings.Instance.BioRefreshSeconds * 1000);
            }


        }
        public static bool CanBio(GameObject unit)
        {
            if (!ScholarSettings.Instance.BioUseTimeTillDeath)
                return true;

            if (unit.IsBoss())
                return true;

            return unit.CombatTimeLeft() >= ScholarSettings.Instance.BioDontIfEnemyDyingWithinSeconds;
        }

        public static async Task<bool> Bio()
        {
            if (!ScholarSettings.Instance.Bio)
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(BioAuras, true, ScholarSettings.Instance.BioRefreshSeconds * 1000))
                return false;

            if (ScholarSettings.Instance.DontDotIfMoreEnemies
                && ScholarSettings.Instance.DontDotIfMoreEnemiesThan > 0
                && Combat.Enemies.Count > ScholarSettings.Instance.DontDotIfMoreEnemiesThan)
                return false;

            if (!CanBio(Core.Me.CurrentTarget))
                return false;

            return await Spells.Bio.Cast(Core.Me.CurrentTarget);
        }

        private static readonly uint[] BioAuras =
        {
            Auras.Bio,
            Auras.Bio2,
            Auras.Biolysis
        };

        public static async Task<bool> EnergyDrain2()
        {
            if (!ScholarSettings.Instance.EnergyDrain)
                return false;

            if (!Core.Me.HasAetherflow())
                return false;

            if (!Spells.EnergyDrain2.IsKnownAndReady())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.CanAttack)
                return false;

            // Balance Optimization: Dump Energy Drain if Aetherflow is coming off CD soon to prevent drifting,
            // or if the target has Chain Stratagem for burst damage, bypassing the restrictive MP checks.
            bool isBurstWindow = target.HasAura(Auras.ChainStratagem);
            bool aetherflowAlmostReady = Spells.Aetherflow.Cooldown.TotalMilliseconds <= 15000;
            bool needsMp = Core.Me.CurrentManaPercent <= ScholarSettings.Instance.EnergyDrainManaPercent;

            if (isBurstWindow || aetherflowAlmostReady || needsMp)
            {
                return await Spells.EnergyDrain2.Cast(target);
            }

            return false;
        }
    }
}

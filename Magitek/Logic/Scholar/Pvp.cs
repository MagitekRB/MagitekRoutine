using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Gambits.Conditions;
using Magitek.Models.Dancer;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using Magitek.Utilities.Managers;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using ScholarRoutine = Magitek.Utilities.Routines.Scholar;

namespace Magitek.Logic.Scholar
{
    internal static class Pvp
    {
        public static async Task<bool> BroilIVPvp()
        {

            if (!Spells.BroilIVPvp.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BroilIVPvp.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> BiolysisPvp()
        {

            if (!Spells.BiolysisPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_Biolysis)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BiolysisPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MummificationPvp()
        {

            if (!Spells.MummificationPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_Mummification)
                return false;

            if (ScholarRoutine.EnemiesInCone < 1)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 12)
            {
                var nearby = Combat.Enemies
                    .Where(e => e.Distance(Core.Me) < 7
                            && e.ValidAttackUnit()
                            && e.InLineOfSight())
                    .OrderBy(e => e.Distance(Core.Me));
                var nearbyTarget = nearby.FirstOrDefault();

                if (nearbyTarget != null)
                {
                    return await Spells.MummificationPvp.Cast(nearbyTarget);
                }

                return false;
            }

            return await Spells.MummificationPvp.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> AdloquiumPvp()
        {

            if (!Spells.AdloquiumPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_Adloquium)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (ScholarSettings.Instance.Pvp_HealSelfOnly)
            {
                if (Core.Me.HasAura(Auras.PvpCatalyze) || Core.Me.CurrentHealthPercent > ScholarSettings.Instance.Pvp_AdloquiumHealthPercent)
                    return false;

                return await Spells.AdloquiumPvp.Heal(Core.Me);
            }

            var Target = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= ScholarSettings.Instance.Pvp_AdloquiumHealthPercent && !r.HasAura(Auras.PvpCatalyze));

            if (Target == null)
                if (Core.Me.CurrentHealthPercent <= ScholarSettings.Instance.Pvp_AdloquiumHealthPercent && !Core.Me.HasAura(Auras.PvpCatalyze))
                    return await Spells.AdloquiumPvp.Heal(Core.Me);
                else
                    return false;

            return await Spells.AdloquiumPvp.Heal(Target);

        }

        public static async Task<bool> DeploymentTacticsPvp()
        {

            if (!Spells.DeploymentTacticsPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_DeploymentTacticsOnSelf)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpCatalyze))
                 return false;

            if (!Core.Me.HasAura(Auras.PvpCatalyze, true, 14000))
                return false;

            return await Spells.DeploymentTacticsPvp.Cast(Core.Me);
        }

        public static async Task<bool> DeploymentTacticsAlliesPvp()
        {

            if (!Spells.DeploymentTacticsPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_DeploymentTacticsOnAllies)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var deploymentTacticsTarget = Group.CastableAlliesWithin30.FirstOrDefault(r =>
                r.HasAura(Auras.PvpCatalyze, true)
                && Group.CastableAlliesWithin30.Count(x => x.Distance(r) <= 15 + x.CombatReach) >= 2);

            if (deploymentTacticsTarget == null)
                return false;

            if (!deploymentTacticsTarget.HasAura(Auras.PvpBiolytic, true, 14000))
                return false;

            return await Spells.DeploymentTacticsPvp.Cast(deploymentTacticsTarget);

        }

        public static async Task<bool> DeploymentTacticsEnemyPvp()
        {

            if (!Spells.DeploymentTacticsPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!ScholarSettings.Instance.Pvp_DeploymentTacticsOnEnemy)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpBiolytic))
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpBiolytic, true, 14000))
                return false;

            return await Spells.DeploymentTacticsPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ExpedientPvp()
        {

            if (!Spells.ExpedientPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_Expedient)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (ScholarSettings.Instance.Pvp_DeploymentTacticsOnEnemy 
                && Spells.BiolysisPvp.Cooldown.TotalMilliseconds >= 2500)
                return false;

            return await Spells.ExpedientPvp.Cast(Core.Me);
        }

        public static async Task<bool> SummonSeraphPvp()
        {
            if (!ScholarSettings.Instance.Pvp_SummonSeraph)
                return false;

            if (!Spells.SummonSeraphPvp.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Group.CastableAlliesWithin30.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SummonSeraphNearbyAllies)
                return false;

            if (Combat.Enemies.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SummonSeraphNearbyAllies)
                return false;

            return await Spells.SummonSeraphPvp.Cast(Core.Me);
        }

        public static async Task<bool> ConsolationPvp()
        {
            if (!Spells.ConsolationPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.ConsolationPvp.Cast(Core.Me);
        }
    }
}
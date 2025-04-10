using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using ScholarRoutine = Magitek.Utilities.Routines.Scholar;

namespace Magitek.Logic.Scholar
{
    internal static class Pvp
    {
        private static DateTime _lastGalvanizeDeploymentTime = DateTime.MinValue;
        private static DateTime _lastBiolysisDeploymentTime = DateTime.MinValue;
        private static readonly TimeSpan DeploymentCooldownWindow = TimeSpan.FromSeconds(6);

        public static async Task<bool> BroilIVPvp()
        {
            if (!Spells.BroilIVPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.BroilIVPvp.Masked();

            if (spell == Spells.BroilIVPvp && MovementManager.IsMoving)
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
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

        public static async Task<bool> ChainStratagemPvp()
        {
            if (!Spells.ChainStratagemPvp.CanCast())
                return false;

            if (!ScholarSettings.Instance.Pvp_ChainStratagem)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ChainStratagemPvp.Cast(Core.Me.CurrentTarget);
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

            if (!Core.Me.HasAura(Auras.PvpCatalyze, true, 10500))
                return false;

            if (DateTime.Now - _lastGalvanizeDeploymentTime < DeploymentCooldownWindow)
                return false;

            if (Group.CastableAlliesWithin15.Count(x => x.Distance(Core.Me) <= 15 + x.CombatReach) < 2)
                return false;

            _lastGalvanizeDeploymentTime = DateTime.Now;

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

            if (!deploymentTacticsTarget.HasAura(Auras.PvpGalvanize, true, 10500))
                return false;

            if (DateTime.Now - _lastGalvanizeDeploymentTime < DeploymentCooldownWindow)
                return false;

            _lastGalvanizeDeploymentTime = DateTime.Now;

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

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpBiolytic, true, 10500))
                return false;

            if (DateTime.Now - _lastBiolysisDeploymentTime < DeploymentCooldownWindow)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) <= 15 + x.CombatReach) < 2)
                return false;

            _lastBiolysisDeploymentTime = DateTime.Now;

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

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
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

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Group.CastableAlliesWithin20.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SummonSeraphNearbyAllies)
                return false;

            if (Combat.Enemies.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SummonSeraphNearbyAllies)
                return false;

            // Find the ally with the lowest HP percentage
            var lowestHpAlly = Group.CastableAlliesWithin30
                .Where(x => x.IsValid && x.IsAlive)
                .OrderBy(x => x.CurrentHealthPercent)
                .FirstOrDefault();

            // If no valid ally is found, cast on self
            if (lowestHpAlly == null)
                return await Spells.SummonSeraphPvp.Cast(Core.Me);

            return await Spells.SummonSeraphPvp.Cast(lowestHpAlly);
        }

        public static async Task<bool> SeraphismPvp()
        {
            if (!ScholarSettings.Instance.Pvp_Seraphism)
                return false;

            if (!Spells.SeraphismPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Group.CastableAlliesWithin30.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SeraphismNearbyAllies)
                return false;

            if (Combat.Enemies.Count(x => x.IsValid && x.IsAlive) < ScholarSettings.Instance.Pvp_SeraphismNearbyAllies)
                return false;

            return await Spells.SeraphismPvp.Cast(Core.Me);
        }
    }
}
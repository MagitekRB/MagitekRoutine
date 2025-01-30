using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Logic.Scholar;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using ScholarRoutine = Magitek.Utilities.Routines.Scholar;

namespace Magitek.Rotations
{
    public static class Scholar
    {
        public static async Task<bool> Rest()
        {
            if (ScholarSettings.Instance.PhysickOnRest)
            {
                if (Core.Me.CurrentHealthPercent > 70 || Core.Me.ClassLevel < 4)
                    return false;

                return await Spells.Physick.Heal(Core.Me);
            }

            return false;
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (await Buff.SummonPet())
                return true;

            return false;
        }

        public static async Task<bool> Pull()
        {
            if (Globals.InParty && Utilities.Combat.Enemies.Count > ScholarSettings.Instance.StopDamageWhenMoreThanEnemies)
                return false;

            if (!ScholarSettings.Instance.DoDamage)
                return false;

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            if (await Buff.SummonPet()) return true;

            //LimitBreak
            if (Logic.Scholar.Heal.ForceLimitBreak()) return true;

            if (await HealFightLogic.Aoe()) return true;
            if (await HealFightLogic.Tankbuster()) return true;

            if (await Logic.Scholar.Heal.Resurrection()) return true;

            // Scalebound Extreme Rathalos
            if (Core.Me.HasAura(1495))
            {
                return await Dispel.Execute();
            }

            #region Pre-Healing Stuff

            if (await Logic.Scholar.Heal.ForceDeployAdloWithRecitation()) return true;
            if (await Logic.Scholar.Heal.ForceEmergencySuccor()) return true;
            if (await Logic.Scholar.Heal.ForceSuccor()) return true;
            if (await Logic.Scholar.Heal.ForceWhispDawn()) return true;
            if (await Logic.Scholar.Heal.ForceAdlo()) return true;
            if (await Logic.Scholar.Heal.ForceIndom()) return true;
            if (await Logic.Scholar.Heal.ForceExcog()) return true;
            if (await Logic.Scholar.Heal.ForceSacredSoil()) return true;

            if (await Dispel.Execute()) return true;
            if (await Buff.Aetherflow()) return true;
            if (await Buff.LucidDreaming()) return true;

            if (Globals.InParty)
            {
                if (await Buff.DeploymentTactics()) return true;
                if (await Buff.Aetherpact()) return true;
                if (await Buff.BreakAetherpact()) return true;
            }

            if (await Buff.ChainStrategem()) return true;

            #endregion

            if (await Logic.Scholar.Heal.Accession()) return true;
            if (await Logic.Scholar.Heal.Manifestation()) return true;

            if (await Logic.Scholar.Heal.Excogitation()) return true;
            if (await Logic.Scholar.Heal.Lustrate()) return true;

            if (Core.Me.Pet != null && Core.Me.InCombat)
            {
                if (await Buff.Seraphism()) return true;
                if (await Logic.Scholar.Buff.SummonSeraph()) return true;
                if (await Logic.Scholar.Heal.Consolation()) return true;
                if (await Logic.Scholar.Heal.FeyIllumination()) return true;
                if (await Logic.Scholar.Heal.WhisperingDawn()) return true;
                if (await Logic.Scholar.Heal.FeyBlessing()) return true;
            }

            if (Globals.InParty)
            {
                if (await Logic.Scholar.Heal.Indomitability()) return true;
                if (await Logic.Scholar.Heal.EmergencyTacticsSuccor()) return true;
                if (await Logic.Scholar.Heal.Succor()) return true;
                if (await Logic.Scholar.Heal.SacredSoil()) return true;
            }

            if (await Buff.Expedient()) return true;
            if (await Buff.Protraction()) return true;
            if (await Logic.Scholar.Heal.EmergencyTacticsAdloquium()) return true;
            if (await Logic.Scholar.Heal.Adloquium()) return true;
            if (await Logic.Scholar.Heal.Physick()) return true;

            return await HealAlliance();
        }

        public static async Task<bool> HealAlliance()
        {
            if (Group.CastableAlliance.Count == 0)
                return false;

            Group.SwitchCastableToAlliance();
            var res = await DoHeal();
            Group.SwitchCastableToParty();
            return res;

            async Task<bool> DoHeal()
            {
                if (await Logic.Scholar.Heal.Resurrection()) return true;

                if (ScholarSettings.Instance.HealAllianceOnlyPhysick)
                {
                    if (await Logic.Scholar.Heal.Physick()) return true;
                    return false;
                }

                if (await Logic.Scholar.Heal.Lustrate()) return true;
                if (await Logic.Scholar.Heal.EmergencyTacticsAdloquium()) return true;
                if (await Logic.Scholar.Heal.Adloquium()) return true;
                if (await Logic.Scholar.Heal.Physick()) return true;

                return false;
            }
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (Core.Me.CurrentManaPercent <= ScholarSettings.Instance.MinimumManaPercent)
                return false;

            if (Utilities.Combat.Enemies.Count > ScholarSettings.Instance.StopDamageWhenMoreThanEnemies)
                return false;

            if (!ScholarSettings.Instance.DoDamage)
                return false;

            if (Globals.InParty)
            {
                if (Core.Me.CurrentManaPercent < ScholarSettings.Instance.MinimumManaPercent)
                    return false;

                if (Group.CastableAlliesWithin30.Any(c => c.IsAlive && c.CurrentHealthPercent < ScholarSettings.Instance.DamageOnlyIfAboveHealthPercent))
                    return false;
            }

            if (await Aoe.ArtOfWar()) return true;

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (await Aoe.BanefulImpaction()) return true;
            if (await SingleTarget.Bio()) return true;
            if (await SingleTarget.BioMultipleTargets()) return true;
            if (await SingleTarget.Ruin2()) return true;
            if (await SingleTarget.EnergyDrain2()) return true;
            return await SingleTarget.Broil();
        }

        public static async Task<bool> PvP()
        {
            ScholarRoutine.RefreshVars();

            if (await CommonPvp.CommonTasks(ScholarSettings.Instance)) return true;

            if (await Pvp.ConsolationPvp()) return true;
            if (await Pvp.SummonSeraphPvp()) return true;

            if (await Pvp.DeploymentTacticsEnemyPvp()) return true;
            if (await Pvp.DeploymentTacticsAlliesPvp()) return true;
            if (await Pvp.DeploymentTacticsPvp()) return true;
            if (await Pvp.AdloquiumPvp()) return true;
            if (await Pvp.ExpedientPvp()) return true;

            if (!CommonPvp.GuardCheck(ScholarSettings.Instance))
            {
                if (await Pvp.MummificationPvp()) return true;
                if (await Pvp.BiolysisPvp()) return true;
            }

            return (await Pvp.BroilIVPvp());
        }
    }
}

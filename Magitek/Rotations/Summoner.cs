using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Logic.Summoner;
using Magitek.Models.Summoner;
using Magitek.Utilities;
using System.Threading.Tasks;

namespace Magitek.Rotations
{
    public static class Summoner
    {
        public static async Task<bool> Rest()
        {
            if (Core.Me.CurrentHealthPercent > 70 || Core.Me.ClassLevel < 4)
                return false;

            if (WorldManager.InSanctuary)
                return false;

            return SummonerSettings.Instance.Physick ? await Spells.SmnPhysick.Heal(Core.Me) : false;
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await Pets.SummonCarbuncle();
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }
        public static async Task<bool> Heal()
        {
            if (await Logic.Summoner.Heal.Resurrection()) return true;
            #region Force Toggles
            if (await Logic.Summoner.Heal.ForceRaise()) return true;
            if (await Logic.Summoner.Heal.ForceHardRaise()) return true;
            #endregion

            if (await Logic.Summoner.Heal.LuxSolaris()) return true;
            if (await Logic.Summoner.Heal.RadiantAegis()) return true;
            return await Logic.Summoner.Heal.Physick();
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (Core.Me.CurrentTarget.HasAura(Auras.MagicResistance))
                return false;

            //Fix issue with level 1 SMN and/or PotD
            if (Core.Me.ClassLevel == 1)
            {
                return await Spells.Ruin.Cast(Core.Me.CurrentTarget);
            }

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (await MagicDps.FightLogic_Addle(SummonerSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_SelfShield(SummonerSettings.Instance.FightLogicRadiantAegis, Spells.RadiantAegis, true, Auras.RadiantAegis)) return true;

            if (await Aoe.CrimsonStrike()) return true;
            if (await Buff.LucidDreaming()) return true;
            if (await Pets.SummonCarbuncleOrEgi()) return true;
            if (await Aoe.EnergySiphon()) return true;
            if (await Aoe.SearingFlash()) return true;
            if (await SingleTarget.EnergyDrain()) return true;
            if (await SingleTarget.Enkindle()) return true;
            if (await Aoe.AstralFlow()) return true;
            if (await Aoe.Painflare()) return true;
            if (await SingleTarget.Fester()) return true;
            if (await Buff.Aethercharge()) return true;
            if (await Aoe.Ruin4()) return true;
            if (await Aoe.Outburst()) return true;
            return await SingleTarget.Ruin();
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(SummonerSettings.Instance)) return true;

            if (await Pvp.RadiantAegisPvp()) return true;
            if (await Pvp.SummonBahamutPvp()) return true;
            if (await Pvp.SummonPhoenixPvp()) return true;

            if (!CommonPvp.GuardCheck(SummonerSettings.Instance))
            {
                if (await Pvp.MountainBusterPvp()) return true;
                if (await Pvp.CrimsonStrikePvp()) return true;

                if (await Pvp.DeathflarePvp()) return true;
                if (await Pvp.BrandOfPurgatoryPvp()) return true;

                if (await Pvp.NecrotizePvp()) return true;
                if (await Pvp.CrimsonCyclonePvp()) return true;

                if (await Pvp.SlipstreamPvp()) return true;
            }

            return (await Pvp.RuinIIIPvp());
        }
    }
}

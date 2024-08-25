using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Bard;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Bard;
using Magitek.Utilities;
using BardRoutine = Magitek.Utilities.Routines.Bard;
using System.Threading.Tasks;

namespace Magitek.Rotations
{
    public static class Bard
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < BardSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await PhysicalDps.Peloton(BardSettings.Instance);
        }

        public static async Task<bool> Pull()
        {
            BardRoutine.RefreshVars();

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return false;
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            BardRoutine.RefreshVars();

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_PartyShield(BardSettings.Instance.FightLogicTroubadour, Spells.Troubadour, true, PhysicalDps.partyShieldAuras)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(BardSettings.Instance.FightLogicNaturesMinne, Spells.NaturesMinne, true, aura: Auras.NaturesMinne)) return true;

            if (BardRoutine.GlobalCooldown.CanWeave())
            {
                // Utility
                if (await Utility.RepellingShot()) return true;
                if (await Utility.WardensPaean()) return true;
                if (await Utility.NaturesMinne()) return true;
                if (await Utility.Troubadour()) return true;
                if (await PhysicalDps.ArmsLength(BardSettings.Instance)) return true;
                if (await PhysicalDps.SecondWind(BardSettings.Instance)) return true;
                if (await PhysicalDps.Interrupt(BardSettings.Instance)) return true;
                if (await Cooldowns.UsePotion()) return true;

                // Damage
                if (await SingleTarget.LastPossiblePitchPerfectDuringWM()) return true;
                if (await Songs.LetMeSingYouTheSongOfMyPeople()) return true;
                if (await Cooldowns.RagingStrikes()) return true;
                //if (await Cooldowns.RadiantFinale()) return true;
                if (await Cooldowns.BattleVoice()) return true;
                if (await Cooldowns.Barrage()) return true;
                if (await SingleTarget.PitchPerfect()) return true;
                if (await Aoe.RainOfDeathDuringMagesBallard()) return true;
                if (await SingleTarget.BloodletterInMagesBallard()) return true;
                if (await SingleTarget.EmpyrealArrow()) return true;
                if (await SingleTarget.Sidewinder()) return true;
                if (await Aoe.RainOfDeath()) return true;
                if (await SingleTarget.Bloodletter()) return true;
            }

            if (await DamageOverTime.IronJawsOnCurrentTarget()) return true;
            if (await DamageOverTime.SnapShotIronJawsOnCurrentTarget()) return true;
            if (await Aoe.BlastArrow()) return true;
            if (await SingleTarget.StraightShotAfterBarrage()) return true;
            if (await DamageOverTime.StormbiteOnCurrentTarget()) return true;
            if (await DamageOverTime.CausticBiteOnCurrentTarget()) return true;
            if (await Aoe.ApexArrow()) return true;
            if (await Aoe.ShadowBite()) return true;
            if (await DamageOverTime.IronJawsOnOffTarget()) return true;
            if (await DamageOverTime.StormbiteOnOffTarget()) return true;
            if (await DamageOverTime.CausticBiteOnOffTarget()) return true;
            if (await Aoe.LadonsBite()) return true;
            if (await SingleTarget.StraightShot()) return true;
            if (await SingleTarget.ResonantArrow()) return true;
            if (await SingleTarget.RadiantEncore()) return true;
            return (await SingleTarget.HeavyShot());

        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(BardSettings.Instance)) return true;

            if (await Pvp.FinalFantasiaPvp()) return true;

            if (!CommonPvp.GuardCheck(BardSettings.Instance))
            {
                if (await Pvp.SilentNocturnePvp()) return true;
                if (await Pvp.EmpyrealArrow()) return true;
                if (await Pvp.BlastArrow()) return true;
                if (await Pvp.ApexArrow()) return true;
            }

            return (await Pvp.PowerfulShot());
        }
    }
}



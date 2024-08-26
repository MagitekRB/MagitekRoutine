using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.BlackMage;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Rotations
{
    public static class BlackMage
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < BlackMageSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            //Try to keep stacks outside combat
            if (await Buff.UmbralSoul()) return true;
            //if (await Buff.Transpose()) return true;

            return false;
        }

        public static async Task<bool> Pull()
        {
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
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_SelfShield(BlackMageSettings.Instance.FightLogicManaward, Spells.Manaward, castTimeRemainingMs: 19000)) return true;
            if (await MagicDps.FightLogic_Addle(BlackMageSettings.Instance)) return true;

            //DON'T CHANGE THE ORDER OF THESE
            if (await Buff.Amplifier()) return true;
            if (await Buff.Triplecast()) return true;
            if (await Buff.Sharpcast()) return true;
            if (await Buff.ManaFont()) return true;
            if (await Buff.LeyLines()) return true;
            if (await Buff.UmbralSoul()) return true;
            if (await Buff.UseEther()) return true;
            //if (await Buff.Transpose()) return true;

            if (BlackMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
            {                
                //Umbral
                if (await Aoe.Freeze()) return true;
                if (await Aoe.Blizzard2()) return true;
                if (await Aoe.Foul()) return true;

                //Astral
                if (await Aoe.Fire2()) return true;
                if (await Aoe.Fire3()) return true;
                if (await Aoe.Fire4()) return true;
                if (await Aoe.Flare()) return true;
                
                //Either
                if (await Aoe.Thunder4()) return true;
            }

            if (await Aoe.FlareStar()) return true;
            if (await SingleTarget.ParadoxUmbral()) return true;
            if (await SingleTarget.Blizzard4()) return true;
            if (await SingleTarget.Fire()) return true;
            if (await SingleTarget.Thunder3()) return true;
            if (await SingleTarget.Xenoglossy()) return true;
            if (await SingleTarget.Fire4()) return true;
            if (await SingleTarget.Despair()) return true;
            if (await SingleTarget.Fire3()) return true;
            if (await SingleTarget.Blizzard()) return true;
            if (await SingleTarget.Blizzard3()) return true;

            if (Core.Me.ClassLevel < 80)
                return await Spells.Fire3.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> PvP()
        {
            BlackMageRoutine.RefreshVars();

            if (await CommonPvp.CommonTasks(BlackMageSettings.Instance)) return true;

            if (await Pvp.SoulResonancePvp()) return true;
            if (await Pvp.FoulPvp()) return true;

            if (!CommonPvp.GuardCheck(BlackMageSettings.Instance))
            {
                if (await Pvp.AetherialManipulation()) return true;

                if (await Pvp.Paradox()) return true;
                if (await Pvp.SuperFlare()) return true;
            }
            if (await Pvp.Burst()) return true;
            if (await Pvp.Blizzard()) return true;
            return (await Pvp.Fire());
        }
    }
}

using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Roles;
using Magitek.Toggles;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    internal static class MagicDps
    {
        public static async Task<bool> Interrupt(MagicDpsSettings settings)
        {
            if (!settings.UseStunOrInterrupt)
                return false;

            List<SpellData> stuns = new List<SpellData>();
            List<SpellData> interrupts = new List<SpellData>();

            if (Core.Me.IsBlueMage())
            {
                interrupts.Add(Spells.FlyingSardine);
            }

            return await InterruptAndStunLogic.StunOrInterrupt(stuns, interrupts, settings.Strategy);
        }

        public static bool ForceLimitBreak(SpellData limitBreak1Spell, SpellData limitBreak2Spell, SpellData limitBreak3Spell, SpellData gcd)
        {
            if (!BaseSettings.Instance.ForceLimitBreak)
                return false;

            //LB 3
            if (PartyManager.NumMembers == 8
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak3Spell)
                && gcd.Cooldown.TotalMilliseconds < 500)
            {
                ActionManager.DoActionLocation(limitBreak3Spell.Id, Core.Me.CurrentTarget.Location);
                BaseSettings.Instance.ForceLimitBreak = false;
                TogglesManager.ResetToggles();
                return true;
            }

            //LB 2 or LB 1
            if (PartyManager.NumMembers == 4
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak1Spell)
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak2Spell)
                && gcd.Cooldown.TotalMilliseconds < 500)
            {
                if (!ActionManager.DoActionLocation(limitBreak2Spell.Id, Core.Me.CurrentTarget.Location))
                    ActionManager.DoActionLocation(limitBreak1Spell.Id, Core.Me.CurrentTarget.Location); ;

                BaseSettings.Instance.ForceLimitBreak = false;
                TogglesManager.ResetToggles();
                return true;
            }
            return false;
        }

        public static async Task<bool> UsePotion<T>(T settings) where T : MagicDpsSettings
        {
            if (!settings.UsePotion)
                return false;

            if (Core.Me.HasAura(Auras.Medicated, true))
                return false;

            return await Potion.UsePotion((int)settings.PotionTypeAndGradeLevel);
        }

        public static async Task<bool> FightLogic_Addle(MagicDpsSettings settings)
        {
            return await CommonFightLogic.FightLogic_Debuff(settings.FightLogicAddle, Spells.Addle, true, Auras.Addle);
        }
    }
}

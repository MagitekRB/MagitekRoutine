using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.DarkKnight;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.DarkKnight;
using Magitek.Utilities;
using System.Threading.Tasks;
using DarkKnightRoutine = Magitek.Utilities.Routines.DarkKnight;

namespace Magitek.Rotations
{
    public static class DarkKnight
    {
        public static async Task<bool> PreCombatBuff()
        {
            return await Buff.Grit();
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }
        public static async Task<bool> Heal()
        {
            return false;
        }

        public static async Task<bool> Combat()
        {
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            //LimitBreak
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(DarkKnightSettings.Instance.FightLogicDefensives, DarkKnightRoutine.DefensiveSpells, DarkKnightRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(DarkKnightSettings.Instance.FightLogicPartyShield, Spells.DarkMissionary, true, aura: Auras.DarkMissionary)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(DarkKnightSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(DarkKnightSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            //Utility
            if (await Buff.Grit()) return true;
            if (await Tank.Interrupt(DarkKnightSettings.Instance)) return true;

            if (DarkKnightRoutine.GlobalCooldown.CanWeave())
            {
                //Potion
                if (await Buff.UsePotion()) return true;

                //Defensive Buff
                if (await Defensive.Execute()) return true;
                if (await Defensive.Oblation(true)) return true;
                if (await Defensive.Reprisal()) return true;
                if (await Tank.ArmsLength(DarkKnightSettings.Instance)) return true;

                if (await SingleTarget.CarveAndSpit()) return true;
                if (await Aoe.SaltedEarth()) return true;
                if (await Aoe.AbyssalDrain()) return true;
                if (await Aoe.FloodofDarknessShadow()) return true;
                if (await SingleTarget.EdgeofDarknessShadow()) return true;
                if (await Buff.Delirium()) return true;
                if (await Buff.BloodWeapon()) return true;
                if (await Buff.LivingShadow()) return true;
                if (await SingleTarget.Shadowbringer()) return true;
                if (await SingleTarget.Shadowstride()) return true;
            }

            //Pull or get back aggro with LightningShot
            if (await SingleTarget.UnmendToPullOrAggro()) return true;
            if (await SingleTarget.UnmendToDps()) return true;

            if (await SingleTarget.Disesteem()) return true;

            if (await Aoe.Quietus()) return true;
            if (await Aoe.StalwartSoul()) return true;
            if (await Aoe.Unleash()) return true;

            if (await SingleTarget.Bloodspiller()) return true;
            if (await SingleTarget.SoulEater()) return true;
            if (await SingleTarget.SyphonStrike()) return true;

            return await SingleTarget.HardSlash();
        }
        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(DarkKnightSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except basic combo
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.EventidePvp()) return true;
                if (await Pvp.SaltAndDarkness()) return true;
                if (await Pvp.BlackestNightPvp()) return true;
                if (await Pvp.SaltedEarthPvp()) return true;

                if (!CommonPvp.GuardCheck(DarkKnightSettings.Instance))
                {
                    if (await Pvp.PlungePvp()) return true;
                    if (await Pvp.ShadowbringerPvp()) return true;
                    if (await Pvp.ImpalementPvp()) return true;
                    if (await Pvp.DisesteemPvp()) return true;
                }
            }

            // Basic Combo (ungated)
            if (await Pvp.SouleaterPvp()) return true;
            if (await Pvp.SyphonStrikePvp()) return true;
            return (await Pvp.HardSlashPvp());
        }

        public static Task<bool> Rest() => Task.FromResult(false);
        public static Task<bool> CombatBuff() => Task.FromResult(false);
    }
}

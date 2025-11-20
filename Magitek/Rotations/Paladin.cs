using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Paladin;
using Magitek.Logic.Roles;
using Magitek.Models.Paladin;
using Magitek.Utilities;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using Healing = Magitek.Logic.Paladin.Heal;
using PaladinRoutine = Magitek.Utilities.Routines.Paladin;

namespace Magitek.Rotations
{
    public static class Paladin
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < PaladinSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await Buff.IronWill();
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return await Healing.Clemency();
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
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(PaladinSettings.Instance.FightLogicDefensives, PaladinRoutine.DefensiveFastSpells, PaladinRoutine.Defensives, castTimeRemainingMs: 3000)) return true;
            if (await CommonFightLogic.FightLogic_TankDefensive(PaladinSettings.Instance.FightLogicDefensives, PaladinRoutine.DefensiveSpells, PaladinRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(PaladinSettings.Instance.FightLogicPartyShield, Spells.DivineVeil, true, aura: Auras.DivineVeil)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(PaladinSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(PaladinSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            if (!Core.Me.HasAura(Auras.PassageOfArms))
            {
                //Utility
                if (await SingleTarget.Interrupt()) return true;
                if (await Buff.IronWill()) return true;

                if (PaladinRoutine.GlobalCooldown.CanWeave())
                {
                    //Potion
                    if (await Buff.UsePotion()) return true;

                    //Defensive Buff
                    if (await Defensive.HallowedGround()) return true;
                    if (await Defensive.Sentinel()) return true;
                    if (await Defensive.Rampart()) return true;
                    if (await Defensive.Reprisal()) return true;
                    if (await Defensive.Sheltron()) return true;
                    if (await Defensive.DivineVeil()) return true;
                    if (await Tank.ArmsLength(PaladinSettings.Instance)) return true;

                    //Cover
                    if (await Defensive.Intervention()) return true;
                    if (await Defensive.Cover()) return true;

                    //Damage Buff
                    if (await Buff.FightOrFlight()) return true;

                    //oGCDS
                    if (await SingleTarget.Requiescat()) return true;
                    if (await Aoe.CircleOfScorn()) return true;
                    if (await Aoe.Expiacion()) return true;
                    if (await SingleTarget.Intervene()) return true; //dash
                }

                //Combo AOE (Single Target or Multi Target)
                if (await Aoe.BladeOfHonor()) return true;
                if (await Aoe.BladeOfValor()) return true;
                if (await Aoe.BladeOfTruth()) return true;
                if (await Aoe.BladeOfFaith()) return true;
                if (await Aoe.Confiteor()) return true;

                if (await SingleTarget.ShieldLobOnLostAggro()) return true;
                if (await SingleTarget.GoringBlade()) return true;

                //Under Divine Might Aura to have no cast or stacks of Sword Oath
                if (await Aoe.HolyCircle()) return true;
                if (await SingleTarget.Atonement()) return true;
                if (await SingleTarget.HolySpirit()) return true;

                //Combo Action AOE
                if (await Aoe.Prominence()) return true;
                if (await Aoe.TotalEclipse()) return true;

                //Combo Action Single Target
                if (await SingleTarget.RoyalAuthority()) return true;
                if (await SingleTarget.RiotBlade()) return true;
                if (await SingleTarget.FastBlade()) return true;

                return await SingleTarget.ShieldLob();
            }
            else
            {
                return false;
            }
        }
        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(PaladinSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except FastBlade combo (the basic attack fallback)
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.PhalanxPvp()) return true;
                if (await Pvp.BladeofValorPvp()) return true;
                if (await Pvp.BladeofTruthPvp()) return true;
                if (await Pvp.BladeofFaithPvp()) return true;
                if (await Pvp.HolySheltronPvp()) return true;

                if (await Pvp.ShieldSmitePvp()) return true;
                if (await Pvp.HolySpiritPvp()) return true;

                if (!CommonPvp.GuardCheck(PaladinSettings.Instance))
                {
                    if (await Pvp.ImperatorPvp()) return true;
                    if (await Pvp.IntervenePvp()) return true;
                }
            }

            if (await Pvp.AtonementPvp()) return true;
            if (await Pvp.SupplicationPvp()) return true;
            if (await Pvp.SepulchrePvp()) return true;
            if (await Pvp.ConfiteorPvp()) return true;

            // Basic combo fallback (ONLY ungated abilities)
            if (await Pvp.RoyalAuthorityPvp()) return true;
            if (await Pvp.RiotBladePvp()) return true;
            return (await Pvp.FastBladePvp());
        }
    }
}

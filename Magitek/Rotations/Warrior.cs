using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Logic.Warrior;
using Magitek.Models.Warrior;
using Magitek.Utilities;
using System.Threading.Tasks;
using Healing = Magitek.Logic.Warrior.Heal;
using WarriorRoutine = Magitek.Utilities.Routines.Warrior;

namespace Magitek.Rotations
{
    public static class Warrior
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await Buff.Defiance();
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
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(WarriorSettings.Instance.FightLogicDefensives, WarriorRoutine.DefensiveSpells, WarriorRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(WarriorSettings.Instance.FightLogicPartyShield, Spells.ShakeItOff, true, aura: Auras.ShakeItOff)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(WarriorSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(WarriorSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            //Utility
            if (await Buff.Defiance()) return true;
            if (await Tank.Interrupt(WarriorSettings.Instance)) return true;

            if (WarriorRoutine.GlobalCooldown.CanWeave())
            {
                //Potion
                if (await Buff.UsePotion()) return true;

                //Defensive Buff
                if (await Defensive.Holmgang()) return true;
                if (await Healing.Equilibrium()) return true;
                if (await Healing.ThrillOfBattle()) return true;
                if (await Defensive.BloodWhetting()) return true;
                if (await Defensive.Reprisal()) return true;
                if (await Defensive.Rampart()) return true;
                if (await Defensive.VengeanceDamnation()) return true;
                if (await Defensive.ShakeItOff()) return true;
                if (await Buff.NascentFlash()) return true;
                if (await Tank.ArmsLength(WarriorSettings.Instance)) return true;

                //Cooldowns
                if (await Buff.InnerRelease()) return true;
                if (await Buff.Infuriate()) return true;

                //oGCD
                if (await Aoe.Orogeny()) return true;
                if (await SingleTarget.Upheaval()) return true;
                if (await SingleTarget.Onslaught()) return true;
            }

            //Spell to use with Nascent Chaos
            if (await Aoe.ChaoticCyclone()) return true;
            if (await SingleTarget.InnerChaos()) return true;

            //Spell to spam inside Inner Release
            if (await Aoe.PrimalRuination()) return true;
            if (await Aoe.PrimalRend()) return true;
            if (await Aoe.Decimate()) return true;
            if (await SingleTarget.FellCleave()) return true;

            //Use On CD
            if (await SingleTarget.TomahawkOnLostAggro()) return true;
            if (await Aoe.MythrilTempest()) return true;
            if (await Aoe.Overpower()) return true;

            //Storm Eye Combo + Filler
            if (await SingleTarget.StormsEye()) return true;
            if (await SingleTarget.StormsPath()) return true;
            if (await SingleTarget.Maim()) return true;
            if (await SingleTarget.HeavySwing()) return true;

            return await SingleTarget.Tomahawk();
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(WarriorSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except basic combo
            if (CommonPvp.ShouldUseBurst())
            {
                if (!CommonPvp.GuardCheck(WarriorSettings.Instance))
                {
                    // Limit Break
                    if (await Pvp.PrimalScreamPvp()) return true;
                    if (await Pvp.PrimalWrathPvp()) return true;

                    // High Priority Abilities
                    if (await Pvp.PrimalRendPvp()) return true;
                    if (await Pvp.PrimalRuinationPvp()) return true;
                    if (await Pvp.InnerChaosPvp()) return true;
                    if (await Pvp.ChaoticCyclonePvp()) return true;

                    // Gap Closers and Utility
                    if (await Pvp.OnslaughtPvp()) return true;
                    if (await Pvp.BlotaPvp()) return true;

                    // Defensive and Healing
                    if (await Pvp.BloodwhettingPvp()) return true;
                    if (await Pvp.OrogenyPvp()) return true;
                }
            }

            // Basic Combo (ungated)
            if (await Pvp.StormPathPvp()) return true;
            if (await Pvp.MaimPvp()) return true;
            if (await Pvp.HeavySwingPvp()) return true;

            return false;
        }
    }
}

using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Machinist;
using Magitek.Logic.Roles;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Threading.Tasks;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Rotations
{
    public static class Machinist
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < MachinistSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await PhysicalDps.Peloton(MachinistSettings.Instance);
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
            // Ahead of everything, defensives included: Flamethrower is a channel and ANY other action ends
            // it, so a reaction firing here throws the channel away. It reads only Core.Me, so it is safe
            // above the target guard. This is the order the rotation had before the defensives were hoisted.
            if (MachinistSettings.Instance.UseFlamethrower && Core.Me.HasAura(Auras.Flamethrower))
            {
                // First check movement otherwise Flamethrower can be executed whereas you are moving
                if (MovementManager.IsMoving)
                    return false;

                if (!MachinistSettings.Instance.UseFlamethrower)
                    return true;

                if (Core.Me.EnemiesInCone(8) >= MachinistSettings.Instance.FlamethrowerEnemyCount)
                    return true;
            }

            if (await CommonFightLogic.FightLogic_Debuff(MachinistSettings.Instance.FightLogicDismantle, Spells.Dismantle, true, Auras.Dismantled)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(MachinistSettings.Instance.FightLogicTactician, Spells.Tactician, true, PhysicalDps.partyShieldAuras)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(MachinistSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            var canAttack = Core.Me.HasTarget && Core.Me.CurrentTarget.ThoroughCanAttack();

            // Above the attack guard: an enemy our damage cannot reach can still be casting something
            // interruptible, and the interrupt scan reads Combat.Threats for exactly that reason. Weave
            // discipline still applies while attacking; with no attackable target the GCD is idle, so
            // the weave check alone would be false exactly when the interrupt is needed most.
            if ((!canAttack || MachinistRoutine.GlobalCooldown.CanWeave(1)) && await PhysicalDps.Interrupt(MachinistSettings.Instance)) return true;

            if (!canAttack)
                return false;

            //LimitBreak
            if (MultiTarget.ForceLimitBreak()) return true;


            if (ActionResourceManager.Machinist.OverheatRemaining != TimeSpan.Zero)
            {
                if (MachinistRoutine.GlobalCooldown.CanWeave())
                {
                    if (await Cooldowns.Wildfire()) return true;
                    if (await Cooldowns.Hypercharge()) return true; // for 10xHB
                }

                if (MachinistRoutine.GlobalCooldown.CanWeave(1))
                {
                    //Utility
                    if (await PhysicalDps.ArmsLength(MachinistSettings.Instance)) return true;

                    //Pets
                    if (await Pet.RookQueen()) return true;
                }

                if (MachinistRoutine.GlobalCooldown.CanWeave(1))
                {
                    //oGCDs
                    if (await SingleTarget.GaussRound()) return true;
                    if (await MultiTarget.Ricochet()) return true;
                }
            }
            else
            {
                if (MachinistRoutine.GlobalCooldown.CanWeave())
                {
                    //Utility
                    if (await PhysicalDps.ArmsLength(MachinistSettings.Instance)) return true;
                    if (await Utility.Tactician()) return true;
                    if (await Utility.Dismantle()) return true;
                    if (await PhysicalDps.SecondWind(MachinistSettings.Instance)) return true;
                    if (await Cooldowns.UsePotion()) return true;

                    //Pets
                    if (await Pet.RookQueen()) return true;
                    if (await Pet.RookQueenOverdrive()) return true;

                    //Cooldowns
                    if (await Cooldowns.Reassemble()) return true;
                    if (await Cooldowns.Wildfire()) return true;
                    if (await Cooldowns.Hypercharge()) return true;
                    if (await Cooldowns.BarrelStabilizer()) return true;
                }

                if (MachinistRoutine.GlobalCooldown.CanWeave(1))
                {
                    //oGCDs
                    if (await SingleTarget.GaussRound()) return true;
                    if (await MultiTarget.Ricochet()) return true;
                }
            }

            if (await MultiTarget.FullMetalField()) return true;

            //GCDs - Top Hypercharge Priority
            if (await MultiTarget.AutoCrossbow()) return true;
            if (await SingleTarget.HeatBlast()) return true;

            //Use On CD
            if (await MultiTarget.Excavator()) return true;
            if (await MultiTarget.ChainSaw()) return true;

            if (await MultiTarget.BioBlaster()) return true;

            if (await SingleTarget.HotAirAnchor()) return true;
            if (await SingleTarget.Drill()) return true;

            //AOE
            if (await MultiTarget.Flamethrower()) return true;
            if (await MultiTarget.Scattergun()) return true;

            //Default Combo
            if (await SingleTarget.HeatedCleanShot()) return true;
            if (await SingleTarget.HeatedSlugShot()) return true;

            return await SingleTarget.HeatedSplitShot();
        }
        public static async Task<bool> PvP()
        {
            // Utilities
            if (await CommonPvp.CommonTasks(MachinistSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except BlastedCharge (the basic attack fallback)
            if (CommonPvp.ShouldUseBurst())
            {
                // HIGH PRIORITY: Full Metal Field during Wildfire burst window
                // Cast BEFORE other abilities since FMF extends Overheated buff and adds 2 Wildfire stacks
                if (Core.Me.HasAura(Auras.PvpWildfireBuff))
                {
                    if (await Pvp.FullMetalField()) return true;
                }

                if (await Pvp.Analysis()) return true;
                if (await Pvp.Drill()) return true;
                if (await Pvp.BlazingShot()) return true;
                if (await Pvp.Scattergun()) return true;

                if (!CommonPvp.GuardCheck(MachinistSettings.Instance))
                {
                    //LB
                    if (await Pvp.MarksmansSpite()) return true;

                    if (await Pvp.Detonator()) return true;

                    // Buff
                    if (await Pvp.BishopAutoturret()) return true;
                    if (await Pvp.WildFire()) return true;

                    // Tools
                    if (await Pvp.ChainSaw()) return true;
                    if (await Pvp.AirAnchor()) return true;
                    if (await Pvp.BioBlaster()) return true;

                    // NORMAL PRIORITY: Full Metal Field when not in burst window
                    if (await Pvp.FullMetalField()) return true;
                }
            }

            // Main - Basic attack fallback (ONLY ungated ability)
            if (await Pvp.BlazingShot()) return true;
            return await Pvp.BlastedCharge();
        }
    }
}

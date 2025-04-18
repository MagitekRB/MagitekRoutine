﻿using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Dragoon;
using Magitek.Logic.Roles;
using Magitek.Models.Dragoon;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DragoonRoutine = Magitek.Utilities.Routines.Dragoon;

namespace Magitek.Rotations
{
    public static class Dragoon
    {
        private static readonly Stopwatch JumpGcdTimer = new Stopwatch();

        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < DragoonSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
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
            if (!Core.Me.HasTarget && !Core.Me.InCombat)
                return false;

            if (!Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            #region Off GCD debugging
            if (DragoonRoutine.JumpsList.Contains(Casting.LastSpell))
            {
                // Check to see if we're OFF GCD
                if (Spells.TrueThrust.Cooldown == TimeSpan.Zero)
                {
                    // Start the stopwatch if it isn't running
                    if (!JumpGcdTimer.IsRunning)
                        JumpGcdTimer.Restart();
                }
            }
            else
            {
                // We didn't use a Jump last, check to see if the stopwatch is running
                if (JumpGcdTimer.IsRunning)
                {
                    // We'll give a 50ms buffer for the time it takes to tick
                    if (JumpGcdTimer.ElapsedMilliseconds > 50)
                    {
                        Logger.WriteInfo($@"We wasted {JumpGcdTimer.ElapsedMilliseconds} ms off GCD");
                    }

                    // Stop the stopwatch
                    JumpGcdTimer.Stop();
                }
            }
            #endregion

            //LimitBreak
            if (SingleTarget.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_Debuff(DragoonSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(DragoonSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;
            //Utility
            if (await PhysicalDps.Interrupt(DragoonSettings.Instance)) return true;
            if (await PhysicalDps.SecondWind(DragoonSettings.Instance)) return true;
            if (await PhysicalDps.Bloodbath(DragoonSettings.Instance)) return true;

            if (DragoonRoutine.GlobalCooldown.CanWeave() && !DragoonRoutine.SingleWeaveJumpsList.Contains(Casting.LastSpell))
            {
                if (await Utility.TrueNorth()) return true;
                if (await Buff.UsePotion()) return true;

                //Buffs
                if (await Buff.LanceCharge()) return true;
                if (await Buff.BattleLitany()) return true;
                if (await Buff.LifeSurge()) return true;

                if (await Jumps.Starcross()) return true;
                if (await Jumps.RiseOfTheDragon()) return true;

                //oGCD AOE / SingleTarget
                if (await Aoe.WyrmwindThrust()) return true; //used in Single Target Rotation
                if (await Aoe.Geirskogul()) return true; //used in Single Target Rotation
                if (await Aoe.Nastrond()) return true; //used in Single Target Rotation

                //Jumps SingleWeave
                if (DragoonRoutine.GlobalCooldown.CanWeave(1))
                {
                    if (await Jumps.HighJump()) return true;  //SingleWeave
                    if (await Jumps.DragonfireDive()) return true; //SingleWeave
                    if (await Jumps.Stardiver()) return true; //SingleWeave
                }

                //Jump DoubleWeave
                if (await Jumps.MirageDive()) return true; //DoubleWeave
            }

            if (await Aoe.DraconianFury()) return true;
            if (await Aoe.CoerthanTorment()) return true;
            if (await Aoe.SonicThrust()) return true;
            if (await Aoe.DoomSpike()) return true;

            if (await SingleTarget.RaidenThrust()) return true;

            // Combo finisher
            if (await SingleTarget.Drakesbane()) return true;

            // Combo 2
            if (await SingleTarget.FangAndClaw()) return true;
            if (await SingleTarget.HeavensThrust()) return true;
            if (await SingleTarget.VorpalThrust()) return true;

            // Combo 1 + DOT
            if (await SingleTarget.WheelingThrust()) return true;
            if (await SingleTarget.ChaoticSpring()) return true;
            if (await SingleTarget.Disembowel()) return true;

            return await SingleTarget.TrueThrust();
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(DragoonSettings.Instance)) return true;

            if (await Pvp.StarcrossPvp()) return true;
            if (await Pvp.ChaoticSpringPvp()) return true;
            if (await Pvp.HeavensThrustPvp()) return true;
            if (await Pvp.WyrmwindThrustPvp()) return true;
            if (await Pvp.NastrondPvp()) return true;
            if (await Pvp.ElusiveJumpPvp()) return true;

            if (!CommonPvp.GuardCheck(DragoonSettings.Instance))
            {
                if (await Pvp.SkyHighPvp()) return true;
                if (await Pvp.GeirskogulPvp()) return true;

                if (await Pvp.HighJumpPvp()) return true;
                if (await Pvp.HorridRoarPvp()) return true;
            }

            if (await Pvp.DrakesbanePvp()) return true;
            if (await Pvp.WheelingThrustPvp()) return true;
            if (await Pvp.FangandClawPvp()) return true;

            return (await Pvp.RaidenThrustPvp());
        }

        public static void RegisterCombatMessages()
        {
            //Highest priority: Don't show anything if we're not in combat
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(100,
                                          "",
                                          () => !Core.Me.InCombat || !Core.Me.HasTarget));

            //Second priority: Don't show anything if positional requirements are Nulled
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(200,
                                          "",
                                          () => DragoonSettings.Instance.HidePositionalMessage || Core.Me.HasAura(Auras.TrueNorth) || DragoonSettings.Instance.EnemyIsOmni));

            //Third priority : Positional
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Chaotic Spring => BEHIND !!",
                                          "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => !Core.Me.CurrentTarget.IsBehind && (ActionManager.LastSpell == DragoonRoutine.Disembowel)));

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Wheeling Thrust => BEHIND !!",
                                          "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => !Core.Me.CurrentTarget.IsBehind && (ActionManager.LastSpell == DragoonRoutine.ChaoticSpring)));

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Fang & Claw => SIDE !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => !Core.Me.CurrentTarget.IsFlanking && (ActionManager.LastSpell == DragoonRoutine.HeavensThrust)));
        }
    }
}

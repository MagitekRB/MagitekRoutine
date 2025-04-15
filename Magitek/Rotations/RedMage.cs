using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.RedMage;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.RedMage;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using System.Linq;
using System.Threading.Tasks;
using static Magitek.Logic.RedMage.Utility;
using RedMageRoutine = Magitek.Utilities.Routines.RedMage;

namespace Magitek.Rotations
{

    public static class RedMage
    {
        static RedMage()
        {
            //StateMachineManager.RegisterStateMachine(RdmStateMachine.StateMachine);
        }

        public static Task<bool> Rest()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return false;
        }

        public static async Task<bool> Pull()
        {
            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    // attempt to move to melee if in combo and we got out of range somehow

                    if (Core.Me.ClassLevel < 2 || ShouldApproachForCombo())
                        Movement.NavigateToUnitLos(Core.Me.CurrentTarget, Core.Me.CombatReach + Core.Me.CurrentTarget.CombatReach);

                    else Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);
                }
            }
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            if (await Logic.RedMage.Heal.Verraise()) return true;
            return await Logic.RedMage.Heal.Vercure();
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (BotManager.Current.IsAutonomous)
            {

                if (BaseSettings.Instance.MagitekMovement)
                {
                    // attempt to move to melee if in combo and we got out of range somehow

                    if (Core.Me.ClassLevel < 2 || ShouldApproachForCombo())
                        Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 3 + Core.Me.CurrentTarget.CombatReach);

                    else Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);

                }
            }

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_PartyShield(RedMageSettings.Instance.FightLogicMagickBarrier, Spells.MagickBarrier, castTimeRemainingMs: 4500)) return true;
            if (await MagicDps.FightLogic_Addle(RedMageSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(RedMageSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            if (RedMageRoutine.GlobalCooldown.CanWeave())
            {
                //Buffs
                if (await Buff.MagickBarrier()) return true;
                if (await Buff.Acceleration()) return true;
                if (await Buff.Embolden()) return true;
                if (await Buff.Manafication()) return true;
                if (await Buff.Swiftcast()) return true;
                if (await Buff.LucidDreaming()) return true;

                //oGCD Abilities
                if (await SingleTarget.Prefulgence()) return true;
                if (await Aoe.ContreSixte()) return true;
                if (await SingleTarget.Fleche()) return true;

                //Movement Abilities
                if (await SingleTarget.Engagement()) return true;
                if (await SingleTarget.Displacement()) return true;
                if (await SingleTarget.CorpsACorps()) return true;

            }

            //Combo
            if (await SingleTarget.ScorchResolutionCombo()) return true;
            if (await SingleTarget.Verflare()) return true;
            if (await SingleTarget.Verholy()) return true;
            if (await Aoe.Moulinet()) return true;
            if (await SingleTarget.Reprise()) return true;
            if (await SingleTarget.Redoublement()) return true;
            if (await SingleTarget.Zwerchhau()) return true;
            if (await SingleTarget.Riposte()) return true;

            //Combo procs
            if (await SingleTarget.ViceofThorns()) return true;

            if (await Aoe.GrandImpact()) return true;

            //AoE
            if (RedMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(8).Count() >= RedMageSettings.Instance.AoeEnemies)
            {

                if (await Aoe.Impact()) return true;
                if (await Aoe.Scatter()) return true;
                if (await Aoe.Veraero2()) return true;
                if (await Aoe.Verthunder2()) return true;

            }
            //Single Target
            if (await SingleTarget.Verfire()) return true;
            if (await SingleTarget.Verstone()) return true;
            if (await SingleTarget.Verthunder()) return true;
            if (await SingleTarget.Veraero()) return true;
            if (await SingleTarget.Jolt()) return true;

            return false;
            //return await RdmStateMachine.StateMachine.Pulse();
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(RedMageSettings.Instance)) return true;

            // Movement Abilities
            if (await Pvp.DisplacementPvp()) return true;
            if (await Pvp.CorpsacorpsPvp()) return true;

            // Melee Combo
            if (await Pvp.EnchantedRipostePvp()) return true;
            if (await Pvp.EnchantedZwerchhauPvp()) return true;
            if (await Pvp.EnchantedRedoublementPvp()) return true;
            if (await Pvp.ScorchPvp()) return true;

            // Main Rotation
            if (!CommonPvp.GuardCheck(RedMageSettings.Instance))
            {
                // Limit Break
                if (await Pvp.SouthernCrossPvp()) return true;

                if (await Pvp.ResolutionPvp()) return true;

                if (await Pvp.EmboldenPvp()) return true;
                if (await Pvp.PrefulgencePvp()) return true;

                if (await Pvp.FortePvp()) return true;
                if (await Pvp.ViceOfThornsPvp()) return true;
            }

            // Basic Spells
            if (await Pvp.GrandImpactPvp()) return true;
            if (await Pvp.JoltIIIPvp()) return true;

            return false;
        }

        public static void RegisterCombatMessages()
        {
            if (!BaseSettings.Instance.ActivePvpCombatRoutine)
            {
                CombatMessageManager.RegisterMessageStrategy(
                    new CombatMessageStrategy(100,
                                          "",
                                          () => !Core.Me.InCombat || !Core.Me.HasTarget));

                CombatMessageManager.RegisterMessageStrategy(
                    new CombatMessageStrategy(200,
                                              "Melee Combo!", "/Magitek;component/Resources/Images/General/ArowNoneHighlighted.png",
                                              () => ShouldApproachForCombo()
                ));

                CombatMessageManager.RegisterMessageStrategy(
                    new CombatMessageStrategy(300,
                                              "Melee Combo Soon",
                                              () => RedMageRoutine.WithinManaOf(6, 50) || Spells.Manafication.IsKnownAndReady()
                ));
            }
        }
    }
}

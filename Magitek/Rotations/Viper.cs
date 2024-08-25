using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Viper;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Viper;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using ViperRoutine = Magitek.Utilities.Routines.Viper;
using System.Threading.Tasks;
using ff14bot.Enums;
using System.Linq;

namespace Magitek.Rotations
{
    public static class Viper
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < ViperSettings.Instance.RestHealthPercent;
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
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            ViperRoutine.RefreshVars();

            if (SingleTarget.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_Debuff(ViperSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;

            if (ViperRoutine.GlobalCooldown.CanWeave())
            {
                if (await PhysicalDps.Interrupt(ViperSettings.Instance)) return true;
                if (await PhysicalDps.SecondWind(ViperSettings.Instance)) return true;
                if (await PhysicalDps.Bloodbath(ViperSettings.Instance)) return true;

                if (await Utility.TrueNorth()) return true;
                if (await SingleTarget.Slither()) return true;

                if (await Cooldown.FourthLegacy()) return true;
                if (await Cooldown.ThirdLegacy()) return true;
                if (await Cooldown.SecondLegacy()) return true;
                if (await Cooldown.FirstLegacy()) return true;

                if (await Cooldown.SerpentIre()) return true;
                if (await Cooldown.LastLash()) return true;
            }

            if (await SingleTarget.Ouroboros()) return true;

            if (await SingleTarget.FourthGeneration()) return true;
            if (await SingleTarget.ThirdGeneration()) return true;
            if (await SingleTarget.SecondGeneration()) return true;
            if (await SingleTarget.FirstGeneration()) return true;

            if (await SingleTarget.Reawaken()) return true;

            if (await Cooldown.UncoiledTwinCombo()) return true;
            if (await Cooldown.TwinThreshCombo()) return true;
            if (await Cooldown.TwinBiteCombo()) return true;

            if (await AoE.HunterOrSwiftskinDen()) return true;
            if (await AoE.Vicepit()) return true;

            if (await SingleTarget.HunterOrSwiftskinCoil()) return true;
            if (await SingleTarget.UncoiledFury()) return true;
            if (await SingleTarget.Vicewinder()) return true;

            if (await AoE.JaggedOrBloodiedMaw()) return true;
            if (await AoE.HunterOrSwiftSkinBite()) return true;
            if (await AoE.SteelReavingMaw()) return true;

            if (await Cooldown.DeathRattle()) return true;
            if (await SingleTarget.FankstingOrFlankbane()) return true;
            if (await SingleTarget.HunterOrSwiftSkinSting()) return true;
            return await SingleTarget.SteelOrReavingFangs();


        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(ViperSettings.Instance)) return true;
            if (await Pvp.SnakeScales()) return true;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (await Pvp.SerpentsTail()) return true;
            if (await Pvp.WorldGeneration()) return true;
            if (await Pvp.WorldSwallower()) return true;

            if (!CommonPvp.GuardCheck(ViperSettings.Instance))
            {
                if (await Pvp.RattlingCoil()) return true;
                if (await Pvp.UncoiledFury()) return true;
                if (await Pvp.HuntersSnap()) return true;
            }

            return await Pvp.DualFang();
        }

        public static void RegisterCombatMessages()
        {
            //Highest priority: Don't show anything if we're not in combat
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(100,
                                          "",
                                          () => !Core.Me.InCombat || !Core.Me.HasTarget)
                );

            //Second priority: Don't show anything if positional requirements are Nulled
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(200,
                                          "",
                                          () => ViperSettings.Instance.HidePositionalMessage || Core.Me.HasAura(Auras.TrueNorth) || Core.Me.HasAura(Auras.Reawakened) || ViperSettings.Instance.EnemyIsOmni)
                );

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Fanksting Strike: Side of Enemy", "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => Core.Me.HasAura(Auras.FlankstungVenom) && (Casting.LastSpell == Spells.HunterSting || Casting.LastSpell == Spells.SwiftskinSting)
            ) );

            CombatMessageManager.RegisterMessageStrategy(
               new CombatMessageStrategy(300,
                                         "Fanksbane Fang: Side of Enemy", "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                         () => Core.Me.HasAura(Auras.FlanksbaneVenom) && (Casting.LastSpell == Spells.HunterSting || Casting.LastSpell == Spells.SwiftskinSting)
           ));

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Hindsting Strike: Get behind Enemy", "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => Core.Me.HasAura(Auras.HindstungVenom) && (Casting.LastSpell == Spells.HunterSting || Casting.LastSpell == Spells.SwiftskinSting)
            ));

            CombatMessageManager.RegisterMessageStrategy(
               new CombatMessageStrategy(300,
                                         "Hindsbane Fang: Get behind Enemy", "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                         () => Core.Me.HasAura(Auras.HindsbaneVenom) && (Casting.LastSpell == Spells.HunterSting || Casting.LastSpell == Spells.SwiftskinSting)
           ));
            
        }
    }
}



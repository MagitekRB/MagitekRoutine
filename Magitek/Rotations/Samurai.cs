using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Logic.Samurai;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using System.Threading.Tasks;
using SamuraiRoutine = Magitek.Utilities.Routines.Samurai;

namespace Magitek.Rotations
{

    public static class Samurai
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(Core.Me.CurrentHealthPercent < 75 || Core.Me.CurrentManaPercent < 50);
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

            SamuraiRoutine.RefreshVars();

            //LimitBreak
            if (SingleTarget.ForceLimitBreak()) return true;

            //Buff for opener
            if (await Buff.MeikyoShisuiNotInCombat()) return true;

            if (await CommonFightLogic.FightLogic_SelfShield(SamuraiSettings.Instance.FightLogicTengentsu, Spells.Tengentsu.IsKnown() ? Spells.Tengentsu : Spells.ThirdEye, castTimeRemainingMs: 2000)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(SamuraiSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(SamuraiSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;
            //Utility
            if (await PhysicalDps.Interrupt(SamuraiSettings.Instance)) return true;
            if (await PhysicalDps.SecondWind(SamuraiSettings.Instance)) return true;
            if (await PhysicalDps.Bloodbath(SamuraiSettings.Instance)) return true;

            if (SamuraiRoutine.GlobalCooldown.CanWeave())
            {
                //Utility
                if (await Utility.TrueNorth()) return true;
                if (await Utility.Hagakure()) return true;
                if (await Buff.UsePotion()) return true;

                //Buffs
                if (await Buff.MeikyoShisui()) return true;
                if (await Buff.Ikishoten()) return true;

                //oGCD Meditation
                if (await Aoe.ShohaII()) return true;
                if (await SingleTarget.Shoha()) return true;

                //oGCD Kenki - AOE
                if (await Aoe.Zanshin()) return true;
                if (await Aoe.HissatsuGuren()) return true; //share recast time with Senei
                if (await Aoe.HissatsuKyuten()) return true;

                //oGCD Kenki - SingleTarget
                if (await SingleTarget.HissatsuShinten()) return true;
                if (await SingleTarget.HissatsuSenei()) return true; //share recast time with Guren
                if (await SingleTarget.HissatsuGyoten()) return true; //dash forward
                //if (await SingleTarget.HissatsuYaten()) return true; //dash backward
            }

            //Namikiri
            if (await Aoe.OgiNamikiri()) return true;
            if (await Aoe.KaeshiNamikiri()) return true;

            //Tsubame Gaeshi
            if (await SingleTarget.KaeshiSetsugekka()) return true;
            if (await Aoe.KaeshiGoken()) return true;

            //Iaijutsu
            if (await SingleTarget.MidareSetsugekka()) return true;
            if (await Aoe.TenkaGoken()) return true;
            if (await SingleTarget.Higanbana()) return true;

            //Combo AOE
            if (await Aoe.Mangetsu()) return true;
            if (await Aoe.Oka()) return true;
            if (await Aoe.Fuko()) return true;

            //3 Combos Single Target
            if (await SingleTarget.Gekko()) return true;
            if (await SingleTarget.Kasha()) return true;
            if (await SingleTarget.Yukikaze()) return true;
            if (await SingleTarget.Jinpu()) return true;
            if (await SingleTarget.Shifu()) return true;
            if (await SingleTarget.Hakaze()) return true;

            return await SingleTarget.Enpi();
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
                                          () => SamuraiSettings.Instance.HidePositionalMessage || Core.Me.HasAura(Auras.TrueNorth) || SamuraiSettings.Instance.EnemyIsOmni));

            //Third priority : Positional
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Gekko => BEHIND !!",
                                          "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => !Core.Me.CurrentTarget.IsBehind && (ActionManager.LastSpell == Spells.Jinpu
                                                    || (Core.Me.HasAura(Auras.MeikyoShisui) && Casting.LastSpell == Spells.MeikyoShisui))));

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Kasha => SIDE !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => !Core.Me.CurrentTarget.IsFlanking && (ActionManager.LastSpell == Spells.Shifu
                                                    || (Core.Me.HasAura(Auras.MeikyoShisui) && ActionManager.LastSpell == Spells.Gekko))));
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(SamuraiSettings.Instance)) return true;

            if (!CommonPvp.GuardCheck(SamuraiSettings.Instance))
            {
                if (await Pvp.ZantetsukenPvp()) return true;
                if (await Pvp.MineuchiPvp()) return true;
            }

            if (await Pvp.KaeshiNamikiriPvp()) return true;
            if (await Pvp.ZanshinPvp()) return true;

            if (await Pvp.TendoKaeshiSetsugekkaPvp()) return true;
            if (await Pvp.TendoSetsugekkaPvp()) return true;

            if (!CommonPvp.GuardCheck(SamuraiSettings.Instance))
            {
                if (await Pvp.HissatsuChitenPvp()) return true;
                if (await Pvp.HissatsuSotenPvp()) return true;

                if (await Pvp.OgiNamikiriPvp()) return true;
                if (await Pvp.MeikyoShisuiPvp()) return true;
            }

            if (await Pvp.OkaPvp()) return true;
            if (await Pvp.MangetsuPvp()) return true;
            if (await Pvp.HyosetsuPvp()) return true;

            if (await Pvp.KashaPvp()) return true;
            if (await Pvp.GekkoPvp()) return true;
            return (await Pvp.YukikazePvp());
        }
    }
}

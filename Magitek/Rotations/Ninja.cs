using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Ninja;
using Magitek.Logic.Roles;
using Magitek.Models.Ninja;
using Magitek.Utilities;
using System;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;

namespace Magitek.Rotations
{
    public static class Ninja
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> PreCombatBuff()
        {
            Utilities.Routines.Ninja.RefreshVars();

            if (await Utility.PrePullHide()) return true;

            if (await Ninjutsu.PrePullSuitonRamp()) return true;
            if (await Ninjutsu.PrePullSuitonUse()) return true;

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

            Utilities.Routines.Ninja.RefreshVars();

            if (await CommonFightLogic.FightLogic_SelfShield(NinjaSettings.Instance.FightLogicShadeShift, Spells.ShadeShift, castTimeRemainingMs: 19000)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(NinjaSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(NinjaSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            if (NinjaRoutine.GlobalCooldown.CountOGCDs() < 2 && Spells.SpinningEdge.Cooldown.TotalMilliseconds >= 770
                && DateTime.Now >= NinjaRoutine.oGCD)
            {

                bool usedOGCD = false;

                if (!usedOGCD && await Buff.Kassatsu()) usedOGCD = true;
                if (!usedOGCD && await Cooldown.Mug()) usedOGCD = true;
                if (!usedOGCD && await Cooldown.TrickAttack()) usedOGCD = true;
                if (!usedOGCD && await Ninjutsu.TenChiJin()) usedOGCD = true;
                if (!usedOGCD && await Cooldown.DreamWithinaDream()) usedOGCD = true;
                if (!usedOGCD && await Buff.Meisui()) usedOGCD = true;
                if (!usedOGCD && await SingleTarget.Bhavacakra()) usedOGCD = true;
                if (!usedOGCD && await Aoe.HellfrogMedium()) usedOGCD = true;
                if (!usedOGCD && await Cooldown.ZeshoMeppo()) usedOGCD = true;
                if (!usedOGCD && await Cooldown.TenriJindo()) usedOGCD = true;
                if (!usedOGCD && await Buff.Bunshin()) usedOGCD = true;

                if (usedOGCD)
                {

                    NinjaRoutine.oGCD = DateTime.Now.AddMilliseconds(620);
                    return true;

                }
            }

            if (await Ninjutsu.TenChiJin_FumaShuriken()) return true;
            if (await Ninjutsu.TenChiJin_Raiton()) return true;
            if (await Ninjutsu.TenChiJin_Suiton()) return true;

            if (await Ninjutsu.Doton()) return true;
            if (await Ninjutsu.GokaMekkyaku()) return true;
            if (await Ninjutsu.HyoshoRanryu()) return true;
            if (await Ninjutsu.Huton()) return true;
            if (await Ninjutsu.Suiton()) return true;
            if (await Ninjutsu.Katon()) return true;
            if (await Ninjutsu.Raiton()) return true;
            if (await Ninjutsu.FumaShuriken()) return true;

            if (await SingleTarget.FleetingRaiju()) return true;
            if (await SingleTarget.ForkedRaiju()) return true;

            if (await Aoe.PhantomKamaitachi()) return true;

            if (await Aoe.HakkeMujinsatsu()) return true;
            if (await Aoe.DeathBlossom()) return true;

            if (await SingleTarget.ArmorCrush()) return true;
            if (await SingleTarget.AeolianEdge()) return true;
            if (await SingleTarget.GustSlash()) return true;
            if (await SingleTarget.SpinningEdge()) return true;

            return false;

        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(NinjaSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except basic combo
            if (CommonPvp.ShouldUseBurst())
            {
                if (!CommonPvp.GuardCheck(NinjaSettings.Instance))
                {
                    if (await Pvp.BunshinPvp()) return true;
                    if (await Pvp.ShukuchiPvp()) return true;
                    if (await Pvp.AssassinatePvp()) return true;

                    if (await Pvp.SeitonTenchuPvp()) return true;
                    if (await Pvp.FleetingRaijuPvp()) return true;
                    if (await Pvp.DokumoriPvp()) return true;
                    if (await Pvp.ZeshoMeppoPvp()) return true;

                    if (await Pvp.FumaShurikenPvp()) return true;

                    if (await Pvp.HutonPvp()) return true;
                    if (await Pvp.MeisuiPvp()) return true;

                    if (await Pvp.DotonPvp()) return true;
                    if (await Pvp.GokaMekkyakuPvp()) return true;

                    if (await Pvp.HyoshoRanryuPvp()) return true;
                    if (await Pvp.ForkedRaijuPvp()) return true;
                    if (await Pvp.ThreeMudraPvp()) return true;
                }
            }

            // Basic Combo (ungated)
            if (await Pvp.AeolianEdgePvp()) return true;
            if (await Pvp.GustSlashPvp()) return true;

            return (await Pvp.SpinningEdgePvp());

        }
    }
}

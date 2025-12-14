using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Ninja;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using GameObject = ff14bot.Objects.GameObject;

namespace Magitek.Logic.Ninja
{
    internal static class Pvp
    {
        public static async Task<bool> SpinningEdgePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.SpinningEdgePvp.CanCast())
                return false;

            return await Spells.SpinningEdgePvp.CastPvpCombo(Spells.AeolianEdgePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> GustSlashPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.GustSlashPvp.CanCast())
                return false;

            return await Spells.GustSlashPvp.CastPvpCombo(Spells.AeolianEdgePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> AeolianEdgePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.AeolianEdgePvp.CanCast())
                return false;

            return await Spells.AeolianEdgePvp.CastPvpCombo(Spells.AeolianEdgePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> AssassinatePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.AssassinatePvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_Assassinate)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.AssassinatePvp.Range))
                return false;

            return await Spells.AssassinatePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BunshinPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.BunshinPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_Bunshin)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.BunshinPvp.Range))
                return false;

            return await Spells.BunshinPvp.Cast(Core.Me);
        }

        public static async Task<bool> FumaShurikenPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Spells.FumaShurikenPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (Spells.FumaShurikenPvp.Charges < 1)
                return false;

            if (!NinjaSettings.Instance.Pvp_FumaShuriken)
                return false;

            if (NinjaSettings.Instance.Pvp_FumaShurikenOnlyWithBunshin && !Core.Me.HasAura(Auras.PvpBunshin))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.FumaShurikenPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.FumaShurikenPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ShukuchiPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Spells.ShukuchiPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!NinjaSettings.Instance.Pvp_Shukuchi)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ShukuchiPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ShukuchiPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SeitonTenchuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!NinjaSettings.Instance.Pvp_SeitonTenchu)
                return false;

            if (!Spells.SeitonTenchuPvp.CanCast())
                return false;

            // Seiton Tenchu: 12,000 potency, incapacitates foes below 50% HP, ignores Guard
            const double potency = 12000;
            const float range = 20f;

            // Seiton Tenchu incapacitates below 50% HP, so we consider it a "kill" for that purpose
            Func<GameObject, double> potencyCalculator = (target) =>
            {
                // If target is below 50% HP, treat as killable (incapacitates)
                if (target.CurrentHealthPercent <= 50)
                    return target.CurrentHealth; // Treat as killable
                return potency;
            };

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                NinjaSettings.Instance,
                potency,
                range,
                ignoreGuard: true,
                checkGuard: false, // Seiton Tenchu ignores Guard
                searchAllTargets: NinjaSettings.Instance.Pvp_UseSeitonTenchuAnyTarget,
                potencyCalculator: potencyCalculator);

            if (killableTarget != null)
            {
                return await Spells.SeitonTenchuPvp.Cast(killableTarget);
            }

            // Fallback: cast normally if not kill-only mode
            if (!NinjaSettings.Instance.Pvp_SeitonTenchuForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(20f))
                    return false;

                // Health check - Seiton Tenchu incapacitates below 50% HP
                if (Core.Me.CurrentTarget.CurrentHealthPercent > NinjaSettings.Instance.Pvp_SeitonTenchuHealthPercent)
                    return false;

                // Check Guard if enabled (though Seiton Tenchu ignores Guard, we still respect the setting)
                if (CommonPvp.GuardCheck(NinjaSettings.Instance, Core.Me.CurrentTarget, checkGuard: false))
                    return false;

                if (!Spells.SeitonTenchuPvp.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await Spells.SeitonTenchuPvp.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        #region Three Mudra

        public static async Task<bool> ThreeMudraPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden) || Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.ThreeMudraPvp.CanCast())
                return false;

            if (NinjaSettings.Instance.Pvp_DoNotUseThreeMudra)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ThreeMudraPvp.Range))
                return false;

            return await Spells.ThreeMudraPvp.Cast(Core.Me);
        }

        public static async Task<bool> HyoshoRanryuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.HyoshoRanryuPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_HyoshoRanryu)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.HyoshoRanryuPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.HyoshoRanryuPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GokaMekkyakuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.GokaMekkyakuPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_GokaMekkyaku)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.GokaMekkyakuPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) <= 5 + x.CombatReach) < NinjaSettings.Instance.Pvp_GokaMekkyakuMinEnemies)
                return false;

            return await Spells.GokaMekkyakuPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DotonPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.DotonPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_Doton)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(5)) < NinjaSettings.Instance.Pvp_DotonMinEnemies)
                return false;

            return await Spells.DotonPvp.Cast(Core.Me);
        }

        public static async Task<bool> HutonPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.HutonPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_Huton)
                return false;

            if (Core.Me.CurrentHealthPercent >= NinjaSettings.Instance.Pvp_HutonHealthPercent)
                return false;

            return await Spells.HutonPvp.Cast(Core.Me);
        }

        public static async Task<bool> MeisuiPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.MeisuiPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_Meisui)
                return false;

            if (Core.Me.CurrentHealthPercent >= NinjaSettings.Instance.Pvp_MeisuiHealthPercent)
                return false;

            return await Spells.MeisuiPvp.Cast(Core.Me);
        }

        public static async Task<bool> ForkedRaijuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!Spells.ForkedRaijuPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_ForkedRaiju)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ForkedRaijuPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ForkedRaijuPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FleetingRaijuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpFleetingRaijuReady))
                return false;

            if (!Spells.FleetingRaijuPvp.CanCast())
                return false;

            if (!NinjaSettings.Instance.Pvp_FleetingRaiju)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.FleetingRaijuPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.FleetingRaijuPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DokumoriPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Spells.DokumoriPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpThreeMudra))
                return false;

            if (!NinjaSettings.Instance.Pvp_Dokumori)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.DokumoriPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.DokumoriPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ZeshoMeppoPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard) || Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (!Core.Me.HasAura(Auras.PvpZeshoMeppoReady))
                return false;

            if (!Spells.ZeshoMeppoPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ZeshoMeppoPvp.Range))
                return false;

            return await Spells.ZeshoMeppoPvp.Cast(Core.Me.CurrentTarget);
        }

        #endregion

    }
}

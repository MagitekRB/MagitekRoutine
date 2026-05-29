using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Magitek.Logic.Roles;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Samurai
{
    internal static class Pvp
    {
        public static async Task<bool> YukikazePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.YukikazePvp.CanCast())
                return false;

            return await Spells.YukikazePvp.CastPvpCombo(Spells.KashaPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> GekkoPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.GekkoPvp.CanCast())
                return false;

            return await Spells.GekkoPvp.CastPvpCombo(Spells.KashaPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> KashaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.KashaPvp.CanCast())
                return false;

            return await Spells.KashaPvp.CastPvpCombo(Spells.KashaPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> HyosetsuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HyosetsuPvp.CanCast())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.HyosetsuPvp.Radius)) < 1)
                return false;

            return await Spells.HyosetsuPvp.Cast(Core.Me);
        }

        public static async Task<bool> MangetsuPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.MangetsuPvp.CanCast())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.MangetsuPvp.Radius)) < 1)
                return false;

            return await Spells.MangetsuPvp.Cast(Core.Me);
        }

        public static async Task<bool> OkaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.OkaPvp.CanCast())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.OkaPvp.Radius)) < 1)
                return false;

            return await Spells.OkaPvp.Cast(Core.Me);
        }

        public static async Task<bool> HissatsuSotenPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HissatsuSotenPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_HissatsuSoten)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.HissatsuSotenPvp.Range))
                return false;

            return await Spells.HissatsuSotenPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HissatsuChitenPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HissatsuChitenPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_HissatsuChiten)
                return false;

            // Intentionally check for Zanshin range here
            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ZanshinPvp.Range)) < 1)
                return false;

            return await Spells.HissatsuChitenPvp.Cast(Core.Me);
        }

        public static async Task<bool> ZanshinPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ZanshinPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_Zanshin)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ZanshinPvp.Range)) < 1)
                return false;

            return await Spells.ZanshinPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MineuchiPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.MineuchiPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_Mineuchi)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.MineuchiPvp.Range))
                return false;

            return await Spells.MineuchiPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> OgiNamikiriPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (!Spells.OgiNamikiriPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_OgiNamikiri)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.OgiNamikiriPvp.Range))
                return false;

            return await Spells.OgiNamikiriPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> KaeshiNamikiriPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.KaeshiNamikiriPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.KaeshiNamikiriPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.KaeshiNamikiriPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MeikyoShisuiPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.MeikyoShisuiPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_MeikyoShisui)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.TendoSetsugekkaPvp.Range)) < 1)
                return false;

            return await Spells.MeikyoShisuiPvp.Cast(Core.Me);
        }

        public static async Task<bool> TendoSetsugekkaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (!Spells.TendoSetsugekkaPvp.CanCast())
                return false;

            if (!SamuraiSettings.Instance.Pvp_TendoSetsugekka)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.TendoSetsugekkaPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.TendoSetsugekkaPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TendoKaeshiSetsugekkaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.TendoKaeshiSetsugekkaPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.TendoKaeshiSetsugekkaPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.TendoKaeshiSetsugekkaPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ZantetsukenPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!SamuraiSettings.Instance.Pvp_Zantetsuken)
                return false;

            if (!Spells.ZantetsukenPvp.CanCast())
                return false;

            // Zantetsuken deals 24,000 potency, or 100% of the target's max HP when OUR Kuzushi is on them.
            // Mitigation and ally damage buffs still scale it, so run the real number per-target through the potency
            // calculator (same pattern as MCH Marksman's Spite / role Smite). Zantetsuken ignores Guard.
            //
            // When "only with Kuzushi" is enabled, returning 0 potency for non-Kuzushi targets makes them count as
            // unkillable, so FindKillableTargetInRange skips them during the all-target search.
            Func<GameObject, double> zantetsukenPotency = target =>
            {
                bool hasKuzushi = target.HasAura(Auras.PvpKuzushi, true);

                if (SamuraiSettings.Instance.Pvp_ZantetsukenWithKuzushi && !hasKuzushi)
                    return 0d;

                return hasKuzushi ? target.MaxHealth : 24000d;
            };

            // Prefer a confirmed kill. Searches every enemy in range only when the user opted in; otherwise just the
            // current target. Honors the ally-targeting cap the same way MCH does. Zantetsuken ignores Guard, so we
            // skip the Guard filter on candidates.
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                SamuraiSettings.Instance,
                24000d, // base potency, overridden per-target by the calculator below
                (float)Spells.ZantetsukenPvp.Range,
                ignoreGuard: true,
                checkGuard: false,
                searchAllTargets: SamuraiSettings.Instance.Pvp_ZantetsukenAnyTarget,
                potencyCalculator: zantetsukenPotency,
                maxAlliesTargetingLimit: SamuraiSettings.Instance.Pvp_MaxAlliesTargetingLimit);

            if (killableTarget != null)
                return await Spells.ZantetsukenPvp.Cast(killableTarget);

            // No confirmed kill found. Kills-only mode stops here.
            if (SamuraiSettings.Instance.Pvp_ZantetsukenForKillsOnly)
                return false;

            // Fallback: spend it as limit-break pressure on the current target sitting below the configured HP
            // threshold.
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ZantetsukenPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // "Only with Kuzushi" gate applies to the HP-threshold fallback too.
            if (SamuraiSettings.Instance.Pvp_ZantetsukenWithKuzushi && !Core.Me.CurrentTarget.HasAura(Auras.PvpKuzushi, true))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > SamuraiSettings.Instance.Pvp_ZantetsukenHealthPercent)
                return false;

            // Check if too many allies are targeting the current target
            if (CommonPvp.TooManyAlliesTargeting(SamuraiSettings.Instance))
                return false;

            return await Spells.ZantetsukenPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

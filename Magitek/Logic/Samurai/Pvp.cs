﻿using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Magitek.Logic.Roles;

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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ZantetsukenPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > SamuraiSettings.Instance.Pvp_ZantetsukenHealthPercent && !SamuraiSettings.Instance.Pvp_ZantetsukenWithKuzushi)
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpKuzushi) && SamuraiSettings.Instance.Pvp_ZantetsukenWithKuzushi)
                return false;

            // Check if too many allies are targeting the current target
            if (CommonPvp.TooManyAlliesTargeting(SamuraiSettings.Instance))
                return false;

            return await Spells.ZantetsukenPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

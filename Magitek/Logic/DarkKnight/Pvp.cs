using ff14bot;
using Magitek.Extensions;
using Magitek.Models.DarkKnight;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.DarkKnight
{
    internal static class Pvp
    {
        public static async Task<bool> HardSlashPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.HardSlashPvp.Masked();

            if (!spell.CanCast())
                return false;

            return await spell.CastPvpCombo(Spells.SouleaterPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> SyphonStrikePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.SyphonStrikePvp.Masked();

            if (!spell.CanCast())
                return false;

            return await spell.CastPvpCombo(Spells.SouleaterPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> SouleaterPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.SouleaterPvp.Masked();

            if (!spell.CanCast())
                return false;

            return await spell.CastPvpCombo(Spells.SouleaterPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> PlungePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PlungePvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_Plunge)
                return false;

            if (Core.Me.HasAura(Auras.PvpNoMercy))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.PlungePvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (DarkKnightSettings.Instance.Pvp_SafePlunge && Core.Me.CurrentTarget.WithinSpellRange(3))
                return false;

            return await Spells.PlungePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BlackestNightPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BlackestNightPvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_BlackestNight)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(5)) < 1)
                return false;

            return await Spells.BlackestNightPvp.Cast(Core.Me);
        }

        public static async Task<bool> SaltedEarthPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SaltedEarthPvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_SaltedEarth)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.SaltedEarthPvp.Range)) < 1)
                return false;

            return await Spells.SaltedEarthPvp.Cast(Core.Me);
        }

        public static async Task<bool> ShadowbringerPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ShadowbringerPvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_Shadowbringer)
                return false;

            if (Core.Me.HasAura(Auras.PvpBlackblood))
                return false;

            if (Core.Me.HasAura(Auras.PvpDarkArts))
            {
                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ShadowbringerPvp.Range))
                    return false;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent < DarkKnightSettings.Instance.Pvp_ShadowbringerHealthPercent && !Core.Me.HasAura(Auras.PvpDarkArts))
                    return false;

                if (Combat.Enemies.Count(x => x.WithinSpellRange(5)) < 1)
                    return false;
            }

            return await Spells.ShadowbringerPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EventidePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.EventidePvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_Eventide)
                return false;

            if (Core.Me.CurrentHealthPercent > DarkKnightSettings.Instance.Pvp_EventideHealthPercent)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(20)) < 1)
                return false;

            if (Core.Me.CurrentTarget.WithinSpellRange(20))
                return false;

            return await Spells.EventidePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SaltAndDarkness()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!DarkKnightSettings.Instance.Pvp_SaltAndDarkness)
                return false;

            if (!Spells.SaltAndDarknessPvp.CanCast())
                return false;

            return await Spells.SaltAndDarknessPvp.Cast(Core.Me);
        }

        public static async Task<bool> ImpalementPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ImpalementPvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_Impalement)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ImpalementPvp.Radius)) < 1)
                return false;

            return await Spells.ImpalementPvp.Cast(Core.Me);
        }

        public static async Task<bool> DisesteemPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DisesteemPvp.CanCast())
                return false;

            if (!DarkKnightSettings.Instance.Pvp_Disesteem)
                return false;

            if (!Core.Me.HasAura(Auras.PvpScorn))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.DisesteemPvp.Radius)) < 1)
                return false;

            return await Spells.DisesteemPvp.Cast(Core.Me.CurrentTarget);
        }

        // public static async Task<bool> ScarletDeliriumPvp()
        // {
        //     if (Core.Me.HasAura(Auras.PvpGuard))
        //         return false;

        //     if (!Spells.ScarletDeliriumPvp.CanCast())
        //         return false;

        //     if (!DarkKnightSettings.Instance.Pvp_ScarletDelirium)
        //         return false;

        //     if (!Core.Me.HasAura(Auras.PvpBlackblood))
        //         return false;

        //     return await Spells.ScarletDeliriumPvp.Cast(Core.Me.CurrentTarget);
        // }

        // public static async Task<bool> ComeuppancePvp()
        // {
        //     if (Core.Me.HasAura(Auras.PvpGuard))
        //         return false;

        //     if (!Spells.ComeuppancePvp.CanCast())
        //         return false;

        //     if (!DarkKnightSettings.Instance.Pvp_Comeuppance)
        //         return false;

        //     if (!Core.Me.HasAura(Auras.PvpBlackblood) || !Core.Me.HasAura(Auras.PvpComeuppanceReady))
        //         return false;

        //     return await Spells.ComeuppancePvp.Cast(Core.Me.CurrentTarget);
        // }

        // public static async Task<bool> TorcleaverPvp()
        // {
        //     if (Core.Me.HasAura(Auras.PvpGuard))
        //         return false;

        //     if (!Spells.TorcleaverPvp.CanCast())
        //         return false;

        //     if (!DarkKnightSettings.Instance.Pvp_Torcleaver)
        //         return false;

        //     if (!Core.Me.HasAura(Auras.PvpBlackblood) || !Core.Me.HasAura(Auras.PvpTorcleaverReady))
        //         return false;

        //     return await Spells.TorcleaverPvp.Cast(Core.Me.CurrentTarget);
        // }
    }
}

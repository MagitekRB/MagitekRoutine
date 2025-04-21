using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Dragoon;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Dragoon
{
    internal static class Pvp
    {
        public static async Task<bool> RaidenThrustPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RaidenThrustPvp.CanCast())
                return false;

            return await Spells.RaidenThrustPvp.CastPvpCombo(Spells.WheelingThrustPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> FangandClawPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.FangandClawPvp.CanCast())
                return false;

            return await Spells.FangandClawPvp.CastPvpCombo(Spells.WheelingThrustPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> WheelingThrustPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.WheelingThrustPvp.CanCast())
                return false;

            return await Spells.WheelingThrustPvp.CastPvpCombo(Spells.WheelingThrustPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> DrakesbanePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DrakesbanePvp.CanCast())
                return false;

            return await Spells.DrakesbanePvp.CastPvpCombo(Spells.WheelingThrustPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> HeavensThrustPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HeavensThrustPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.HeavensThrustPvp.Range))
                return false;

            return await Spells.HeavensThrustPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ChaoticSpringPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ChaoticSpringPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_ChaoticSpring)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ChaoticSpringPvp.Range))
                return false;

            return await Spells.ChaoticSpringPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GeirskogulPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.GeirskogulPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_Geirskogul)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.GeirskogulPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.GeirskogulPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> NastrondPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.NastrondPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_Geirskogul)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.NastrondPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.NastrondPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HighJumpPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HighJumpPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_HighJump)
                return false;

            if (DragoonSettings.Instance.Pvp_ElusiveJump && Spells.ElusiveJumpPvp.IsKnownAndReady(9000))
                return false;

            if (DragoonSettings.Instance.Pvp_ElusiveJump && Spells.WyrmwindThrustPvp.IsKnownAndReady())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.HighJumpPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.HighJumpPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ElusiveJumpPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ElusiveJumpPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_ElusiveJump)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ElusiveJumpPvp.Range))
                return false;

            return await Spells.ElusiveJumpPvp.Cast(Core.Me);
        }

        public static async Task<bool> WyrmwindThrustPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.WyrmwindThrustPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.WyrmwindThrustPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.WyrmwindThrustPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HorridRoarPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HorridRoarPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_HorridRoar)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.HorridRoarPvp.Radius)) < 1)
                return false;

            return await Spells.HorridRoarPvp.Cast(Core.Me);
        }

        public static async Task<bool> SkyHighPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SkyHighPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_SkyHigh)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            return await Spells.SkyHighPvp.Cast(Core.Me);
        }


        public static async Task<bool> StarcrossPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.StarcrossPvp.CanCast())
                return false;

            if (!DragoonSettings.Instance.Pvp_Starcross)
                return false;

            if (Core.Me.CurrentTarget.WithinSpellRange(Spells.StarcrossPvp.Range))
                return false;

            return await Spells.StarcrossPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SkyShatterPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SkyShatterPvp.CanCast())
                return false;

            return await Spells.SkyShatterPvp.Cast(Core.Me);
        }
    }
}

using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Paladin;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Paladin
{
    internal static class Pvp
    {

        public static async Task<bool> FastBladePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.FastBladePvp.Range)
                return false;

            if (!Spells.FastBladePvp.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.FastBladePvp.CastPvpCombo(Spells.RoyalAuthorityPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> RiotBladePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;
                                
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.RiotBladePvp.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.RiotBladePvp.CastPvpCombo(Spells.RoyalAuthorityPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> RoyalAuthorityPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.RoyalAuthorityPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.RoyalAuthorityPvp.CastPvpCombo(Spells.RoyalAuthorityPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> ConfiteorPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpConfiteorReady))
                return false;

            if (!Core.Me.HasTarget)
                return false;
                
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.ConfiteorPvp.Range)
                return false;

            if (!Spells.ConfiteorPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Confiteor)
                return false;

            return await Spells.ConfiteorPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AtonementPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpAtonementReady))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.AtonementPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Atonement)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.AtonementPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SupplicationPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpSupplicationReady))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.SupplicationPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Supplication)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.SupplicationPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SepulchrePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpSepulchreReady))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.SepulchrePvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Sepulchre)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.SepulchrePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ShieldSmitePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ShieldSmitePvp.CanCast())
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.ShieldSmitePvp.Range)
                return false;

            if (!PaladinSettings.Instance.Pvp_ShieldSmite)
                return false;

            if (PaladinSettings.Instance.Pvp_ShieldSmiteOnlyOnGuard && !Core.Me.CurrentTarget.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.ShieldSmitePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HolySpiritPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.HolySpiritPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_HolySpirit)
                return false;

            if (Core.Me.CurrentHealthPercent > PaladinSettings.Instance.Pvp_HolySpiritHpThreshold)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.HolySpiritPvp.Range)
                return false;

            return await Spells.HolySpiritPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ImperatorPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.ImperatorPvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Imperator)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.ImperatorPvp.Range)
                return false;

            return await Spells.ImperatorPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HolySheltronPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HolySheltronPvp.CanCast())
                return false;

            if (!PaladinSettings.Instance.Pvp_HolySheltron)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 5 + x.CombatReach) < 1)
                return false;

            return await Spells.HolySheltronPvp.Cast(Core.Me);
        }

        public static async Task<bool> IntervenePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Spells.IntervenePvp.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!PaladinSettings.Instance.Pvp_Intervene)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 20)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (PaladinSettings.Instance.Pvp_SafeIntervene && Core.Me.CurrentTarget.Distance(Core.Me) > 3)
                return false;

            return await Spells.IntervenePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PhalanxPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PhalanxPvp.CanCast())
                return false;

            if (!PaladinSettings.Instance.Pvp_Phalanx)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 5 + x.CombatReach) < 1)
                return false;

            return await Spells.PhalanxPvp.Cast(Core.Me);
        }

        public static async Task<bool> BladeofFaithPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BladeofFaithPvp.CanCast())
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BladeofFaithPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeofTruthPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BladeofTruthPvp.CanCast())
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BladeofTruthPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeofValorPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BladeofValorPvp.CanCast())
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BladeofValorPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}
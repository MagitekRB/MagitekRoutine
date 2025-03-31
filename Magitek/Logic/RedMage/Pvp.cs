using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.RedMage
{
    internal static class Pvp
    {
        public static bool OutsideComboRange => (Core.Me.CurrentTarget == null || Core.Me.CurrentTarget == Core.Me) ? false : Core.Me.Distance(Core.Me.CurrentTarget) > 3.4 + Core.Me.CombatReach + Core.Me.CurrentTarget.CombatReach;

        public static async Task<bool> JoltIIIPvp()
        {
            if (!Spells.JoltIIIPvp.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.JoltIIIPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GrandImpactPvp()
        {
            if (!Spells.GrandImpactPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseGrandImpact)
                return false;

            if (!Core.Me.HasAura(Auras.PvpDualcast))
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.GrandImpactPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnchantedRipostePvp()
        {
            if (!Spells.EnchantedRipostePvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseEnchantedRiposte)
                return false;

            if (OutsideComboRange)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.EnchantedRipostePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnchantedZwerchhauPvp()
        {
            if (!Spells.EnchantedZwerchhauPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseEnchantedZwerchhau)
                return false;

            if (OutsideComboRange)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.EnchantedZwerchhauPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnchantedRedoublementPvp()
        {
            if (!Spells.EnchantedRedoublementPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseEnchantedRedoublement)
                return false;

            if (OutsideComboRange)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.EnchantedRedoublementPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ScorchPvp()
        {
            if (!Spells.ScorchPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseScorch)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ScorchPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrefulgencePvp()
        {
            if (!Spells.PrefulgencePvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UsePrefulgence)
                return false;

            if (!Core.Me.HasAura(Auras.PvpPrefulgenceReady))
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.PrefulgencePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ViceOfThornsPvp()
        {
            if (!Spells.ViceOfThornsPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseViceOfThorns)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ViceOfThornsPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FortePvp()
        {
            if (!Spells.FortePvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseForte)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > RedMageSettings.Instance.Pvp_ForteHealthPercent)
                return false;

            return await Spells.FortePvp.Cast(Core.Me);
        }

        public static async Task<bool> EmboldenPvp()
        {
            if (!Spells.EmboldenPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseEmbolden)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentTarget == null || Core.Me.CurrentTarget.CurrentHealthPercent > RedMageSettings.Instance.Pvp_EmboldenTargetHealthPercent)
                return false;

            return await Spells.EmboldenPvp.Cast(Core.Me);
        }

        public static async Task<bool> CorpsacorpsPvp()
        {
            if (!Spells.CorpsacorpsPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseCorpsACorps)
                return false;

            if (OutsideComboRange)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement))
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.CorpsacorpsPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DisplacementPvp()
        {
            if (!Spells.DisplacementPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseDisplacement)
                return false;

            if (RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement))
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > RedMageSettings.Instance.Pvp_DisplacementHealthPercent)
                return false;

            return await Spells.DisplacementPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ResolutionPvp()
        {
            if (!Spells.ResolutionPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseResolution)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > RedMageSettings.Instance.Pvp_ResolutionTargetHealthPercent)
                return false;

            return await Spells.ResolutionPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SouthernCrossPvp()
        {
            if (!Spells.SouthernCrossPvp.CanCast())
                return false;

            if (!RedMageSettings.Instance.Pvp_UseSouthernCross)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.SouthernCrossPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

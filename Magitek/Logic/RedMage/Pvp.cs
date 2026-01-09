using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using Magitek.Logic.Roles;

namespace Magitek.Logic.RedMage
{
    internal static class Pvp
    {
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.EnchantedRipostePvp.Range))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.EnchantedZwerchhauPvp.Range))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.EnchantedRedoublementPvp.Range))
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

            if (!Movement.CanUseGapCloser())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.CorpsacorpsPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
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

            if (!Movement.CanUseGapCloser())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > RedMageSettings.Instance.Pvp_DisplacementHealthPercent)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.DisplacementPvp.Range))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ResolutionPvp.Range))
                return false;

            return await Spells.ResolutionPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SouthernCrossPvp()
        {
            if (!RedMageSettings.Instance.Pvp_UseSouthernCross)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SouthernCrossPvp.CanCast())
                return false;

            // Southern Cross: 12,000 damage potency (also 12,000 cure potency for party members)
            const double potency = 12000;

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                RedMageSettings.Instance,
                potency,
                (float)Spells.SouthernCrossPvp.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: RedMageSettings.Instance.Pvp_SouthernCrossAnyTarget);

            if (killableTarget != null)
            {
                return await Spells.SouthernCrossPvp.Cast(killableTarget);
            }

            // Fallback to HP threshold if WouldKill is disabled or target not killable
            if (!RedMageSettings.Instance.Pvp_SouthernCrossForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.SouthernCrossPvp.Range))
                    return false;

                if (Core.Me.CurrentTarget.CurrentHealthPercent > RedMageSettings.Instance.Pvp_SouthernCrossTargetHealthPercent)
                    return false;

                if (!Spells.SouthernCrossPvp.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await Spells.SouthernCrossPvp.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }
    }
}

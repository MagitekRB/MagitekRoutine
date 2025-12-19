using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.BlackMage
{
    internal static class Pvp
    {
        public static async Task<bool> Fire()
        {
            var fireSpell = Spells.FirePvp.Masked();

            if (!fireSpell.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await fireSpell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard()
        {
            var blizzardSpell = Spells.BlizzardPvp.Masked();

            if (!blizzardSpell.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await blizzardSpell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ElementalWeave()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!BlackMageSettings.Instance.Pvp_UseElementalWeave)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var maskedSpell = Spells.ElementalWeavePvp.Masked();

            if (maskedSpell == Spells.WreathofFirePvp)
            {
                if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                    return false;

                return await Spells.WreathofFirePvp.Cast(Core.Me);
            }

            if (maskedSpell == Spells.WreathofIcePvp)
            {
                if (Core.Me.CurrentHealthPercent > 80)
                    return false;

                return await Spells.WreathofIcePvp.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> Burst()
        {
            if (!Spells.BurstPvp.CanCast())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.BurstPvp.Radius) && !x.HasAura(Auras.PvpGuard)) < 1)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.BurstPvp.Cast(Core.Me);
        }

        public static async Task<bool> Paradox()
        {
            if (!Spells.ParadoxPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.HasAura(Auras.PvpParadox))
                return false;

            return await Spells.ParadoxPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Xenoglossy()
        {
            if (!Spells.XenoglossyPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!BlackMageSettings.Instance.Pvp_UseXenoglossy)
                return false;

            // Determine potency based on HP
            // Xenoglossy does 12,000 potency normally, or 8,000 potency + 16,000 heal if under 50% HP
            double potency = Core.Me.CurrentHealthPercent < 50 ? 8000 : 12000;

            // 1. Cast if we can kill the enemy with it (always prioritize kills)
            if (CommonPvp.WouldKillWithPotency(potency, Core.Me.CurrentTarget))
                return await Spells.XenoglossyPvp.Cast(Core.Me.CurrentTarget);

            // If "Save for Kills" is enabled, skip overcap protection (would prevent us from ever using charges for movement/healing)
            if (!BlackMageSettings.Instance.Pvp_SaveXenoglossyForKills)
            {
                // Calculate overcap threshold: GCD is 2.5s, charge time is 16s
                // We need to cast if we'll get a charge within the next GCD window
                const double PvpGcdSeconds = 2.5;
                const double ChargeTimeSeconds = 16.0;
                double overcapThreshold = Spells.XenoglossyPvp.MaxCharges - (PvpGcdSeconds / ChargeTimeSeconds);

                // 2. Cast if about to overcap charges (prevent waste)
                if (Spells.XenoglossyPvp.Charges >= overcapThreshold)
                    return await Spells.XenoglossyPvp.Cast(Core.Me.CurrentTarget);
            }

            // If "Save for Kills" is enabled, don't use the saved charge for anything except kills
            if (BlackMageSettings.Instance.Pvp_SaveXenoglossyForKills)
            {
                if (Spells.XenoglossyPvp.Charges <= 1)
                    return false;
            }

            // 3. Cast while moving (prefer over ice spells)
            if (MovementManager.IsMoving)
                return await Spells.XenoglossyPvp.Cast(Core.Me.CurrentTarget);

            // Otherwise, save charges
            return false;
        }

        public static async Task<bool> Lethargy()
        {
            if (!Spells.LethargyPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!BlackMageSettings.Instance.Pvp_UseLethargy)
                return false;

            if (!Core.Me.BeingTargetedBy(Core.Me.CurrentTarget) && !Core.Me.CurrentTarget.IsFacing(Core.Me))
                return false;

            return await Spells.LethargyPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SoulResonancePvp()
        {
            if (!BlackMageSettings.Instance.Pvp_SoulResonance)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.SoulResonancePvp.Masked();

            if (spell == Spells.FlareStarPvp && !MovementManager.IsMoving)
                return await Spells.FlareStarPvp.Cast(Core.Me.CurrentTarget);

            if (spell == Spells.FrostStarPvp && MovementManager.IsMoving)
                return await Spells.FrostStarPvp.Cast(Core.Me.CurrentTarget);

            if (!Spells.SoulResonancePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > BlackMageSettings.Instance.Pvp_SoulResonanceHealthPercent)
                return false;

            return await Spells.SoulResonancePvp.Cast(Core.Me);
        }
    }
}

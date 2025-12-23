using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using ff14bot.Helpers;
using GameObject = ff14bot.Objects.GameObject;

namespace Magitek.Logic.Gunbreaker
{
    internal static class Pvp
    {
        public static async Task<bool> KeenEdgePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.KeenEdgePvp.CanCast())
                return false;

            return await Spells.KeenEdgePvp.CastPvpCombo(Spells.SolidBarrelPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> BrutalShelPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BrutalShelPvp.CanCast())
                return false;

            return await Spells.BrutalShelPvp.CastPvpCombo(Spells.SolidBarrelPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> SolidBarrelPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SolidBarrelPvp.CanCast())
                return false;

            return await Spells.SolidBarrelPvp.CastPvpCombo(Spells.SolidBarrelPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> BurstStrikePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BurstStrikePvp.CanCast())
                return false;

            return await Spells.BurstStrikePvp.CastPvpCombo(Spells.SolidBarrelPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> GnashingFangPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.GnashingFangPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_GnashingFangCombo)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.GnashingFangPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SavageClawPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SavageClawPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_GnashingFangCombo)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.SavageClawPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> WickedTalonPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.WickedTalonPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_GnashingFangCombo)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.WickedTalonPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ContinuationPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!GunbreakerSettings.Instance.Pvp_Continuation)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            var spell = Spells.ContinuationPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (spell == Spells.FatedBrandPvp)
            {
                if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.FatedBrandPvp.Radius)) < 1)
                    return false;

                return await spell.Cast(Core.Me);
            }

            if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RoughDividePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RoughDividePvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_RoughDivide)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit())
                return false;

            if (Core.Me.HasAura(Auras.PvpNoMercy))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(20))
                return false;

            if (GunbreakerSettings.Instance.Pvp_SafeRoughDivide && !Core.Me.CurrentTarget.WithinSpellRange(3))
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.RoughDividePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BlastingZonePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BlastingZonePvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_BlastingZone)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            // Blasting Zone: 5,000 potency at 100% HP, scales linearly up to 10,000 when target HP <= 50%
            const double basePotency = 5000;
            const double maxPotency = 10000;

            // Potency calculator: linear scaling from 5,000 (100% HP) to 10,000 (50% HP or less)
            Func<GameObject, double> potencyCalculator = (target) =>
            {
                if (target.CurrentHealthPercent <= 50)
                {
                    return maxPotency; // Max potency at 50% HP or less
                }
                else
                {
                    // Linear scaling from 100% HP (5000) to 50% HP (10000)
                    double hpPercent = target.CurrentHealthPercent;
                    double scaleFactor = (100 - hpPercent) / 50.0; // 0 at 100% HP, 1 at 50% HP
                    return basePotency + (scaleFactor * (maxPotency - basePotency)); // Scale from 5000 to 10000
                }
            };

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                GunbreakerSettings.Instance,
                basePotency,
                (float)Spells.BlastingZonePvp.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: GunbreakerSettings.Instance.Pvp_BlastingZoneAnyTarget,
                potencyCalculator: potencyCalculator);

            if (killableTarget != null)
            {
                return await Spells.BlastingZonePvp.Cast(killableTarget);
            }

            // Fallback to HP threshold if WouldKill is disabled or target not killable
            if (!GunbreakerSettings.Instance.Pvp_BlastingZoneForKillsOnly)
            {
                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.BlastingZonePvp.Range))
                    return false;

                if (Core.Me.CurrentTarget.CurrentHealthPercent > 50)
                    return false;

                return await Spells.BlastingZonePvp.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> NebulaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.NebulaPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_Nebula)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.NebulaPvp.Cast(Core.Me);
        }

        public static async Task<bool> AuroraPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.AuroraPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_Aurora)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            return await Spells.AuroraPvp.Cast(Core.Me);
        }

        public static async Task<bool> HeartOfCorundumPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HeartOfCorundumPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_HeartOfCorundum)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            if (Core.Me.CurrentHealthPercent > 60)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.HeartOfCorundumPvp.Cast(Core.Me);
        }

        public static async Task<bool> FatedCirclePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.FatedCirclePvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_FatedCircle)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.FatedCirclePvp.Radius)) < 2)
                return false;

            return await Spells.FatedCirclePvp.Cast(Core.Me);
        }

        public static async Task<bool> RelentlessRushPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RelentlessRushPvp.CanCast())
                return false;

            if (!GunbreakerSettings.Instance.Pvp_RelentlessRush)
                return false;

            if (Core.Me.HasAura(Auras.PvpRelentlessRush))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.RelentlessRushPvp.Radius)) < GunbreakerSettings.Instance.Pvp_RelentlessRushEnemyCount)
                return false;

            return await Spells.RelentlessRushPvp.Cast(Core.Me);
        }
    }
}

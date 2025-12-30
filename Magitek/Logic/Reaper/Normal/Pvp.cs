using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Reaper
{
    internal static class Pvp
    {
        public static async Task<bool> SlicePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.SlicePvp.CanCast())
                return false;

            return await Spells.SlicePvp.CastPvpCombo(Spells.InfernalSlicePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> WaxingSlicePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.WaxingSlicePvp.CanCast())
                return false;

            return await Spells.WaxingSlicePvp.CastPvpCombo(Spells.InfernalSlicePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> InfernalSlicePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.InfernalSlicePvp.CanCast())
                return false;

            return await Spells.InfernalSlicePvp.CastPvpCombo(Spells.InfernalSlicePvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> GrimSwathePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.GrimSwathePvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_GrimSwathe)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(8))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.GrimSwathePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> LemureSlicePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.LemureSlicePvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_LemureSlice)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(8))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.LemureSlicePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ArcaneCrestPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ArcaneCrestPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_ArcaneCrest)
                return false;

            if (Group.CastableAlliesWithin15.Count(x => x.IsValid && x.IsAlive) < ReaperSettings.Instance.Pvp_ArcaneCrestNumberOfAllies)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(10)) < 1)
                return false;

            if (Core.Me.CurrentHealthPercent > 80)
                return false;

            return await Spells.ArcaneCrestPvp.Cast(Core.Me);
        }

        public static async Task<bool> DeathWarrantPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DeathWarrantPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_DeathWarrant)
                return false;

            if (Core.Me.CurrentTarget.WithinSpellRange(7))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent < 15)
                return false;

            if (Spells.DeathWarrantPvp.Masked() != Spells.DeathWarrantPvp)
                return false;

            return await Spells.DeathWarrantPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HarvestMoonPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HarvestMoonPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_HarvestMoon)
                return false;

            // Harvest Moon: 8,000 base potency, scales up to 12,000 at 50% HP or less
            // Potency calculator function for scaling based on target HP
            Func<GameObject, double> potencyCalculator = (target) =>
            {
                if (target.CurrentHealthPercent <= 50)
                {
                    return 12000; // Max potency at 50% HP or less
                }
                else
                {
                    // Linear scaling from 100% HP (8000) to 50% HP (12000)
                    double hpPercent = target.CurrentHealthPercent;
                    double scaleFactor = (100 - hpPercent) / 50.0; // 0 at 100% HP, 1 at 50% HP
                    return 8000 + (scaleFactor * 4000); // Scale from 8000 to 12000
                }
            };

            // Always prioritize "would kill" scenarios - use any available charge for kills
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                ReaperSettings.Instance,
                8000, // Base potency (will be calculated per target)
                25f, // Range: 25y
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: ReaperSettings.Instance.Pvp_HarvestMoonAnyTarget,
                potencyCalculator: potencyCalculator);

            if (killableTarget != null)
            {
                return await Spells.HarvestMoonPvp.Cast(killableTarget);
            }

            // HP threshold usage
            // If "For Kills Only" is enabled: only use HP threshold if we have more than 1 charge
            // This allows using 1 charge for HP threshold while keeping 1 charge reserved for kills
            if (ReaperSettings.Instance.Pvp_HarvestMoonForKillsOnly && Spells.HarvestMoonPvp.Charges < 2.0f)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > ReaperSettings.Instance.Pvp_HarvestMoonTargetHealthPercent)
                return false;

            // Don't use if target is above 50% HP and we have Soulsow with 2s+ remaining
            if (Core.Me.CurrentTarget.CurrentHealthPercent > 50)
                return false;

            return await Spells.HarvestMoonPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PlentifulHarvestPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PlentifulHarvestPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_PlentifulHarvest)
                return false;

            if (!Core.Me.HasAura(Auras.PvpImmortalSacrifice))
                return false;

            if (Core.Me.HasAura(Auras.PvpEnshrouded))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(15))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.PlentifulHarvestPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GuillotinePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.GuillotinePvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_Guillotine)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(8))
                return false;

            return await Spells.GuillotinePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> VoidReapingPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.VoidReapingPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_VoidReapingNCrossReaping)
                return false;

            if (!Core.Me.HasAura(Auras.PvpEnshrouded) || !Core.Me.HasAura(Auras.PvpEnshrouded, true, 3000))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (EnshroudedCount == 1)
                return false;

            if (await Spells.VoidReapingPvp.Cast(Core.Me.CurrentTarget))
            {
                EnshroudedCount -= 1;
                return true;
            }

            return false;
        }

        public static async Task<bool> CrossReapingePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.CrossReapingePvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_VoidReapingNCrossReaping)
                return false;

            if (!Core.Me.HasAura(Auras.PvpEnshrouded) || !Core.Me.HasAura(Auras.PvpEnshrouded, true, 3000))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (EnshroudedCount == 1)
                return false;

            if (await Spells.CrossReapingePvp.Cast(Core.Me.CurrentTarget))
            {
                EnshroudedCount -= 1;
                return true;
            }

            return false;
        }

        public static async Task<bool> CommunioPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.CommunioPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_Communio)
                return false;

            if (!Core.Me.HasAura(Auras.PvpEnshrouded))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (EnshroudedCount != 1 && Core.Me.HasAura(Auras.PvpEnshrouded, true, 3000))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PerfectioPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PerfectioPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_Communio)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.PerfectioPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TenebraeLemurumPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.TenebraeLemurumPvp.CanCast())
                return false;

            if (!ReaperSettings.Instance.Pvp_TenebraeLemurum)
                return false;

            if (Core.Me.HasAura(Auras.PvpEnshrouded))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(5))
                return false;

            if (await Spells.TenebraeLemurumPvp.Cast(Core.Me.CurrentTarget))
            {
                EnshroudedCount = 5;
                return true;
            }

            return false;
        }

        public static int EnshroudedCount = 5;
    }
}

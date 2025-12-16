using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Pictomancer;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Pictomancer
{
    internal static class Pvp
    {
        private static readonly uint[] sketches =
        {
            Auras.PvpPomSketch,
            Auras.PvpWingSketch,
            Auras.PvpClawSketch,
            Auras.PvpMawSketch
        };

        private static readonly uint[] motifs =
        {
            Auras.PvpPomMotif,
            Auras.PvpWingMotif,
            Auras.PvpClawMotif,
            Auras.PvpMawMotif
        };

        private static readonly uint[] hodlMotifs =
        {
            Auras.PvpWingMotif,
            Auras.PvpMawMotif
        };

        private static readonly uint[] mogs =
        {
            Auras.PvpMooglePortrait,
            Auras.PvpMadeenPortrait
        };

        public static async Task<bool> TemperaCoat()
        {
            if (!PictomancerSettings.Instance.Pvp_UseTemperaCoat)
                return false;

            if (!Spells.TemperaCoatPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > PictomancerSettings.Instance.Pvp_TemperaCoatHealthPercent)
                return false;

            return await Spells.TemperaCoatPvp.Cast(Core.Me);
        }

        public static async Task<bool> TemperaGrassa()
        {
            if (!PictomancerSettings.Instance.Pvp_UseTemperaGrassa)
                return false;

            if (!Spells.TemperaGrassaPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpTemperaCoat))
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= PictomancerSettings.Instance.Pvp_TemperaCoatHealthPercent) < 3)
                return false;

            return await Spells.TemperaGrassaPvp.Cast(Core.Me);
        }

        public static async Task<bool> PaintRGB()
        {
            if (!PictomancerSettings.Instance.Pvp_UsePaintRGB)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.PaintRGBPvp.Masked();

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PaintW()
        {
            if (!PictomancerSettings.Instance.Pvp_UsePaintWhite)
                return false;

            if (Core.Me.HasAura(Auras.PvpSubtractivePalette))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (PictomancerSettings.Instance.Pvp_UsePaintWhiteOnlyToHeal && Core.Me.CurrentHealthPercent > PictomancerSettings.Instance.Pvp_UsePaintWhiteOnlyToHealHealth)
                return false;

            var spell = Spells.PaintWBPvp.Masked();

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PaintB()
        {
            if (!PictomancerSettings.Instance.Pvp_UsePaintBlack)
                return false;

            if (!Core.Me.HasAura(Auras.PvpSubtractivePalette))
                return false;

            var spell = Spells.PaintWBPvp.Masked();

            if (!spell.CanCast())
                return false;

            // PaintB is Comet in Black: 12,000 potency to target and all enemies nearby it
            const double potency = 12000;

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                PictomancerSettings.Instance,
                potency,
                (float)spell.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: PictomancerSettings.Instance.Pvp_CometInBlackAnyTarget);

            if (killableTarget != null)
            {
                return await spell.Cast(killableTarget);
            }

            // Fallback: cast normally if not kill-only mode
            if (!PictomancerSettings.Instance.Pvp_CometInBlackForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                    return false;

                if (!spell.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await spell.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> CreatureMotif()
        {
            if (!PictomancerSettings.Instance.Pvp_UseMotif)
                return false;

            if (!Core.Me.HasAnyAura(sketches))
                return false;

            var spell = Spells.CreatureMotifPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (Spells.LivingMusePvp.Masked().Charges < 1
                && Core.Me.HasTarget
                && !Core.Me.HasAura(Auras.PvpQuickSketch))
                return false;

            if (WorldManager.ZoneId == 250 && !Core.Me.HasTarget)
                return false;

            return await spell.Cast(Core.Me);
        }

        public static async Task<bool> LivingMuse()
        {
            if (!PictomancerSettings.Instance.Pvp_UseMuse)
                return false;

            if (!Core.Me.HasAnyAura(motifs))
                return false;

            if (Core.Me.HasAnyAura(hodlMotifs) && Core.Me.HasAnyAura(mogs))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.LivingMusePvp.Masked();

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MogoftheAges()
        {
            if (!PictomancerSettings.Instance.Pvp_UseMog)
                return false;

            var spell = Spells.MogOfTheAgesPvp.Masked();

            if (!spell.CanCast())
                return false;

            // Check if masked spell is Retribution of Madeen (ID 39783) or Mog of the Ages (ID 39782)
            // Mog of the Ages: 14,000 potency, Retribution of Madeen: 16,000 potency
            double potency = (spell.Id == Spells.RetributionOfMadeenPvp.Id) ? 16000 : 14000;

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                PictomancerSettings.Instance,
                potency,
                (float)spell.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: PictomancerSettings.Instance.Pvp_MogOfTheAgesAnyTarget);

            if (killableTarget != null)
            {
                return await spell.Cast(killableTarget);
            }

            // Fallback: cast normally if not kill-only mode
            if (!PictomancerSettings.Instance.Pvp_MogOfTheAgesForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                    return false;

                if (!spell.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await spell.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> SubtractivePalette()
        {
            if (!PictomancerSettings.Instance.Pvp_UseSubtractivePalette)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // if moving and can use subtractive palette to do more damage with black paint and don't need to heal
            if (MovementManager.IsMoving
                && PictomancerSettings.Instance.Pvp_SwapToBlackWhileMoving
                && !Core.Me.HasAura(Auras.PvpSubtractivePalette)
                && Spells.PaintWBPvp.Masked().Charges >= 1
                && Spells.SubtractivePalettePvp.CanCast()
                && (!PictomancerSettings.Instance.Pvp_UsePaintWhiteOnlyToHeal ||
                    (PictomancerSettings.Instance.Pvp_UsePaintWhiteOnlyToHeal && Core.Me.CurrentHealthPercent > PictomancerSettings.Instance.Pvp_UsePaintWhiteOnlyToHealHealth)))
                return await Spells.SubtractivePalettePvp.Cast(Core.Me);

            if (MovementManager.IsMoving
                && Core.Me.HasAura(Auras.PvpSubtractivePalette)
                && Spells.ReleaseSubtractivePalettePvp.CanCast()
                && Spells.PaintWBPvp.Masked().Charges < 1)
                return await Spells.ReleaseSubtractivePalettePvp.Cast(Core.Me);

            if (!MovementManager.IsMoving
                && !Core.Me.HasAura(Auras.PvpSubtractivePalette)
                && Spells.SubtractivePalettePvp.CanCast())
                return await Spells.SubtractivePalettePvp.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> Starstruck()
        {
            if (!PictomancerSettings.Instance.Pvp_UseStarstruck)
                return false;

            if (!Core.Me.HasAura(Auras.PvpStarstruck))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.AdventofChocobastionPvp.Masked();

            if (!spell.CanCast(Core.Me.CurrentTarget))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AdventofChocobastion()
        {
            if (!PictomancerSettings.Instance.Pvp_UseAdventofChocobastion)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.HasAura(Auras.PvpStarstruck))
                return false;

            var spell = Spells.AdventofChocobastionPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= PictomancerSettings.Instance.Pvp_AdventofChocobastionYalms + x.CombatReach) < PictomancerSettings.Instance.Pvp_AdventofChocobastionCount)
                return false;

            return await spell.Cast(Core.Me);
        }
    }
}

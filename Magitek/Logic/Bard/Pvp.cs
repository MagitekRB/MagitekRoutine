using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Bard;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Bard
{
    internal static class Pvp
    {
        public static async Task<bool> PowerfulShot()
        {
            if (!BardSettings.Instance.Pvp_PowerfulShot)
                return false;

            if (!Spells.PowerfulShotPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.PowerfulShotPvp.Range))
                return false;

            return await Spells.PowerfulShotPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ApexArrow()
        {
            if (!BardSettings.Instance.Pvp_ApexArrow)
                return false;

            if (!Spells.ApexArrowPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ApexArrowPvp.Range))
                return false;

            return await Spells.ApexArrowPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BlastArrow()
        {
            if (!BardSettings.Instance.Pvp_BlastArrow)
                return false;

            if (!Spells.BlastArrowPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.BlastArrowPvp.Range))
                return false;

            return await Spells.BlastArrowPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SilentNocturnePvp()
        {
            if (!BardSettings.Instance.Pvp_SilentNocturne)
                return false;

            if (!Spells.SilentNocturnePvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.SilentNocturnePvp.Range))
                return false;

            // Don't use Silent Nocturne on mounted targets in Warmachina
            if (CommonPvp.IsPvpMounted(Core.Me.CurrentTarget))
                return false;

            return await Spells.SilentNocturnePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RepellingShot()
        {
            if (!BardSettings.Instance.Pvp_RepellingShot)
                return false;

            if (!Spells.RepellingShotPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RepellingShotPvp.Range))
                return false;

            // Don't use Repelling Shot on mounted targets in Warmachina
            if (CommonPvp.IsPvpMounted(Core.Me.CurrentTarget))
                return false;

            return await Spells.RepellingShotPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> WardensPaean()
        {
            if (!BardSettings.Instance.Pvp_WardensPaean)
                return false;

            if (!Spells.TheWardenPaeanPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura("Stun") && !Core.Me.HasAura("Heavy") && !Core.Me.HasAura("Bind") && !Core.Me.HasAura("Silence") && !Core.Me.HasAura("Half-asleep") && !Core.Me.HasAura("Sleep") && !Core.Me.HasAura("Deep Freeze"))
                return false;

            return await Spells.TheWardenPaeanPvp.Cast(Core.Me);
        }

        public static async Task<bool> HarmonicArrow()
        {
            if (!BardSettings.Instance.Pvp_HarmonicArrow)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.HarmonicArrowPvp.Masked();

            if (!spell.CanCast())
                return false;

            var currentCharges = spell.Charges;
            if (currentCharges < 1)
                return false;

            // Calculate total potency based on charge count
            // 2 Charge: 6,000 per strike (12,000 total)
            // 3 Charge: 5,000 per strike (15,000 total)
            // 4 Charge: 4,500 per strike (18,000 total)
            // Base: 9,000 (1 charge)
            double totalPotency = 0;
            if (currentCharges >= 4)
            {
                totalPotency = 4500 * 4; // 18,000
            }
            else if (currentCharges >= 3)
            {
                totalPotency = 5000 * 3; // 15,000
            }
            else if (currentCharges >= 2)
            {
                totalPotency = 6000 * 2; // 12,000
            }
            else
            {
                totalPotency = 9000; // Base 9,000
            }

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                BardSettings.Instance,
                totalPotency,
                (float)spell.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: BardSettings.Instance.Pvp_HarmonicArrowAnyTarget);

            if (killableTarget != null)
            {
                return await spell.Cast(killableTarget);
            }

            // Fallback to HP threshold if WouldKill is disabled or target not killable
            if (!BardSettings.Instance.Pvp_HarmonicArrowForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                    return false;

                if (Core.Me.CurrentTarget.CurrentHealthPercent > BardSettings.Instance.Pvp_HarmonicArrowHealthPercent)
                    return false;

                // Only use if we have 4 charges (max damage)
                if (currentCharges < 4)
                    return false;

                if (!spell.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await spell.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> FinalFantasiaPvp()
        {
            if (!BardSettings.Instance.Pvp_UseFinalFantasia)
                return false;

            if (!Spells.FinalFantasiaPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // FinalFantasy doesn't have a range check, so we use EncoreOfLight's range
            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.EncoreOfLightPvp.Range))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > BardSettings.Instance.Pvp_FinalFantasiaHealthPercent)
                return false;

            return await Spells.FinalFantasiaPvp.Cast(Core.Me);
        }

        public static async Task<bool> EncoreOfLight()
        {
            if (!BardSettings.Instance.Pvp_EncoreOfLight)
                return false;

            if (!Spells.EncoreOfLightPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.EncoreOfLightPvp.Range))
                return false;

            return await Spells.EncoreOfLightPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PitchPerfect()
        {
            if (!BardSettings.Instance.Pvp_PitchPerfect)
                return false;

            if (!Spells.PitchPerfectPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.PitchPerfectPvp.Range))
                return false;

            return await Spells.PitchPerfectPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

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

            var spell = Spells.HarmonicArrowPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                return false;

            // Calculate damage based on current charges
            const double PvpDamageConversionFactor = 0.8;
            var currentCharges = spell.Charges;
            var estimatedDamage = 0.0;

            if (currentCharges < 1)
                return false;

            // Calculate damage based on charge count
            if (currentCharges >= 4)
            {
                estimatedDamage = 4250 * 4 * PvpDamageConversionFactor;
            }
            else if (currentCharges >= 3)
            {
                estimatedDamage = 5000 * 3 * PvpDamageConversionFactor;
            }
            else if (currentCharges >= 2)
            {
                estimatedDamage = 6000 * 2 * PvpDamageConversionFactor;
            }
            else if (currentCharges >= 1)
            {
                estimatedDamage = 8000 * 1 * PvpDamageConversionFactor;
            }

            // If target is a tank, reduce expected damage
            if (Core.Me.CurrentTarget.IsTank())
            {
                estimatedDamage *= 0.8;
            }

            var targetCurrentHp = Core.Me.CurrentTarget.CurrentHealth;
            var wouldKill = targetCurrentHp <= estimatedDamage;

            if (wouldKill)
            {
                return await spell.Cast(Core.Me.CurrentTarget);
            }

            if (Core.Me.CurrentTarget.CurrentHealthPercent > BardSettings.Instance.Pvp_HarmonicArrowHealthPercent)
                return false;

            if (currentCharges < 4)
                return false;
            return await spell.Cast(Core.Me.CurrentTarget);
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

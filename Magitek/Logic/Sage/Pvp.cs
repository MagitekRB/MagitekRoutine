using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Sage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using SageRoutine = Magitek.Utilities.Routines.Sage;

namespace Magitek.Logic.Sage
{
    internal static class Pvp
    {
        public static async Task<bool> DosisIIIPvp()
        {

            if (!Spells.DosisIIIPvp.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.DosisIIIPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PhlegmaIIIPvp()
        {
            if (!Spells.PhlegmaIIIPvp.CanCast())
                return false;

            if (!SageSettings.Instance.Pvp_PhlegmaIII)
                return false;

            var spell = Spells.PhlegmaIIIPvp.Masked();

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // If spell is masked (different from original), use current target
            if (spell != Spells.PhlegmaIIIPvp)
            {
                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                return await Spells.PhlegmaIIIPvp.Cast(Core.Me.CurrentTarget);
            }

            // Check if current target is valid and in range
            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.PhlegmaIIIPvp.Range))
            {
                // If current target isn't in range, find any valid nearby enemy
                var nearby = Combat.Enemies
                    .Where(e => e.WithinSpellRange(Spells.PhlegmaIIIPvp.Range)
                            && e.ValidAttackUnit()
                            && e.InLineOfSight())
                    .OrderBy(e => e.Distance(Core.Me));

                var nearbyTarget = nearby.FirstOrDefault();

                if (nearbyTarget != null)
                {
                    return await Spells.PhlegmaIIIPvp.Cast(nearbyTarget);
                }

                return false;
            }

            return await Spells.PhlegmaIIIPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ToxikonPvp()
        {
            if (!Spells.ToxikonPvp.CanCast())
                return false;

            if (!SageSettings.Instance.Pvp_Toxikon)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ToxikonPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PneumaPvp()
        {
            if (!Spells.PneumaPvp.CanCast())
                return false;

            if (!SageSettings.Instance.Pvp_Pneuma)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.PneumaPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EukrasiaPvp()
        {
            if (!Spells.EukrasiaPvp.CanCast())
                return false;

            if (!SageSettings.Instance.Pvp_Eukrasia)
                return false;

            if (Core.Me.HasAura(Auras.PvpEukrasias))
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.EukrasiaPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> KardiaPvp()
        {
            if (!Spells.KardiaPvp.CanCast())
                return false;

            if (!SageSettings.Instance.Pvp_Kardia)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAura(Auras.PvpKardia, true))
                return false;

            var currentKardiaTarget = Group.CastableAlliesWithin30.Where(a => a.HasAura(Auras.PvpKardion, true)).FirstOrDefault();

            // Only switch targets if current target is above 80% HP or if we don't have a target
            if (currentKardiaTarget != null && currentKardiaTarget.CurrentHealthPercent > 80 && currentKardiaTarget.WithinSpellRange(30))
            {
                var kardiaTarget = Group.CastableAlliesWithin30
                    .Where(CanKardia)
                    .OrderByDescending(x => x.CurrentHealthPercent)
                    .FirstOrDefault();

                if (kardiaTarget == null)
                    kardiaTarget = Core.Me;

                if (kardiaTarget == currentKardiaTarget)
                    return false;

                return await Spells.KardiaPvp.CastAura(kardiaTarget, Auras.PvpKardion);
            }
            // Initial Kardia application
            else if (currentKardiaTarget == null)
            {
                var kardiaTarget = Group.CastableAlliesWithin30
                    .Where(CanKardia)
                    .OrderByDescending(KardiaPriority)
                    .ThenBy(x => x.CurrentHealthPercent)
                    .FirstOrDefault();

                if (kardiaTarget == null)
                    kardiaTarget = Core.Me;

                return await Spells.KardiaPvp.CastAura(kardiaTarget, Auras.PvpKardion);
            }

            return false;

            bool CanKardia(Character unit)
            {
                if (unit == null)
                    return false;

                if (!unit.IsAlive)
                    return false;

                if (!unit.WithinSpellRange(30))
                    return false;

                return true;
            }

            int KardiaPriority(Character unit)
            {
                if (unit.IsTank())
                    return 100;
                if (unit.IsMeleeDps())
                    return 90;
                if (unit.IsRangedPhysicalDps())
                    return 80;
                if (unit.IsHealer())
                    return 70;
                return 0;
            }
        }

        public static async Task<bool> MesotesPvp()
        {
            if (!Spells.MesotesPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpMesotes))
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // Check for enemies nearby (only cast in combat)
            if (Combat.Enemies.Count(x => x.IsValid && x.IsAlive && x.WithinSpellRange(25)) < 1)
                return false;

            // Check if any allies need healing
            var alliesNeedingHealing = Group.CastableAlliesWithin15.Count(x =>
                x.IsValid &&
                x.IsAlive &&
                x.CurrentHealthPercent <= SageSettings.Instance.Pvp_MesotesHealthPercent);

            // Include self in the count if below health threshold
            if (Core.Me.CurrentHealthPercent <= SageSettings.Instance.Pvp_MesotesHealthPercent)
                alliesNeedingHealing++;

            if (alliesNeedingHealing < SageSettings.Instance.Pvp_MesoteNearbyAllies)
                return false;

            return await Spells.MesotesPvp.Cast(Core.Me);
        }
    }
}

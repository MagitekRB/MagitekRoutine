using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Summoner;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Summoner
{
    internal static class Pvp
    {
        public static async Task<bool> RuinIIIPvp()
        {
            if (!Spells.RuinIIIPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            var spell = Spells.RuinIIIPvp.Masked();


            if (spell == Spells.RuinIIIPvp || spell == Spells.AstralImpulsePvp)
            {
                if (MovementManager.IsMoving)
                    return false;
            }

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SlipstreamPvp()
        {
            if (!Spells.SlipstreamPvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedSlipstream)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.SlipstreamPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MountainBusterPvp()
        {
            if (!Spells.MountainBusterPvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedMountainBuster)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // Check if current target is in range
            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.MountainBusterPvp.Range))
            {
                // If current target isn't in range, find any valid nearby enemy
                var nearby = Combat.Enemies
                    .Where(e => e.WithinSpellRange(Spells.MountainBusterPvp.Range)
                            && e.ValidAttackUnit()
                            && e.InLineOfSight())
                    .OrderBy(e => e.Distance(Core.Me));

                var nearbyTarget = nearby.FirstOrDefault();

                if (nearbyTarget != null)
                {
                    return await Spells.MountainBusterPvp.Cast(nearbyTarget);
                }

                return false;
            }

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.MountainBusterPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RadiantAegisPvp()
        {
            if (!Spells.RadiantAegisPvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedRadiantAegis)
                return false;

            if (Core.Me.CurrentHealthPercent > SummonerSettings.Instance.Pvp_UseRadiantAegisHealthPercent)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            return await Spells.RadiantAegisPvp.Cast(Core.Me);
        }

        public static async Task<bool> CrimsonCyclonePvp()
        {
            if (!Spells.CrimsonCyclonePvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedCrimsonCyclone)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.CrimsonCyclonePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> CrimsonStrikePvp()
        {
            if (!Spells.CrimsonStrikePvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedCrimsonStrike)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.CrimsonStrikePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> NecrotizePvp()
        {
            if (!Spells.NecrotizePvp.CanCast())
                return false;

            if (!SummonerSettings.Instance.Pvp_UsedNecrotize)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAura(Auras.PvpFurtherRuin))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.NecrotizePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DeathflarePvp()
        {
            if (!Spells.DeathflarePvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.DeathflarePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BrandOfPurgatoryPvp()
        {
            if (!Spells.BrandOfPurgatoryPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BrandOfPurgatoryPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SummonBahamutPvp()
        {
            if (!Spells.SummonBahamutPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!SummonerSettings.Instance.Pvp_Summon)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if auto-summon is enabled
            if (SummonerSettings.Instance.Pvp_SummonAuto)
            {
                // Check if enemy has less than specified health percentage for Bahamut
                if (Core.Me.CurrentTarget.CurrentHealthPercent > SummonerSettings.Instance.Pvp_SummonBahamutEnemyHealthPercent)
                    return false;
            }
            else if (!SummonerSettings.Instance.Pvp_SummonBahamut)
            {
                // If not auto and Bahamut is not selected, don't summon
                return false;
            }

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 30)
                return false;

            if (Spells.SummonBahamutPvp.Masked() != Spells.SummonBahamutPvp)
                return false;

            return await Spells.SummonBahamutPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SummonPhoenixPvp()
        {
            if (!Spells.SummonPhoenixPvp.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!SummonerSettings.Instance.Pvp_Summon)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if auto-summon is enabled
            if (SummonerSettings.Instance.Pvp_SummonAuto)
            {
                // Check if player has less than specified health percentage for Phoenix
                if (Core.Me.CurrentHealthPercent > SummonerSettings.Instance.Pvp_SummonPhoenixHealthPercent)
                    return false;
            }
            else if (!SummonerSettings.Instance.Pvp_SummonPhoenix)
            {
                // If not auto and Phoenix is not selected, don't summon
                return false;
            }

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 30)
                return false;

            if (Spells.SummonPhoenixPvp.Masked() != Spells.SummonPhoenixPvp)
                return false;

            return await Spells.SummonPhoenixPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

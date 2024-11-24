using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using Auras = Magitek.Utilities.Auras;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Machinist
{
    internal static class Pvp
    {
        public static async Task<bool> BlastedCharge()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BlastChargePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            return await Spells.BlastChargePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BlazingShot()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BlazingShotPvp.CanCast())
                return false;

            if (!Core.Me.HasAura(Auras.PvpOverheated))
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BlazingShotPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> WildFire()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_Wildfire)
                return false;

            if (!Spells.WildfirePvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.WildfirePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Detonator()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_Wildfire)
                return false;

            if (!Spells.DetonatorPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.WildfirePvp))
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) < 27)
                return false;

            return await Spells.DetonatorPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FullMetalField()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_FullMetalField)
                return false;

            if (!Spells.FullMetalFieldPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.FullMetalFieldPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Scattergun()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_Scattergun)
                return false;

            if (!Spells.ScattergunPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 12)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) < 12) < 1)
                return false;

            return await Spells.ScattergunPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Analysis()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.AnalysisPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.HasAura(Auras.PvpAnalysis))
                return false;

            if (!MachinistSettings.Instance.Pvp_UsedAnalysisOnDrill && Core.Me.HasAura(Auras.PvpDrillPrimed))
                return false;

            if (!MachinistSettings.Instance.Pvp_UsedAnalysisOnBio && Core.Me.HasAura(Auras.PvpBioPrimed))
                return false;

            if (!MachinistSettings.Instance.Pvp_UsedAnalysisOnAA && Core.Me.HasAura(Auras.PvpAirAnchorPrimed))
                return false;

            if (!MachinistSettings.Instance.Pvp_UsedAnalysisOnChainSaw && Core.Me.HasAura(Auras.PvpChainSawPrimed))
                return false;

            return await Spells.AnalysisPvp.Cast(Core.Me);
        }

        public static async Task<bool> Drill()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DrillPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.DrillPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BioBlaster()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BioblasterPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 12)
            {
                var nearby = Combat.Enemies
                    .Where(e => e.Distance(Core.Me) < 10 
                            && e.ValidAttackUnit() 
                            && e.InLineOfSight())
                    .OrderBy(e => e.Distance(Core.Me));
                var nearbyTarget = nearby.FirstOrDefault();

                if (nearbyTarget != null)
                {
                    return await Spells.BioblasterPvp.Cast(nearbyTarget);
                }

                return false;
            }

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.BioblasterPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AirAnchor()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.AirAnchorPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.AirAnchorPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ChainSaw()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ChainSawPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.ChainSawPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BishopAutoturret()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_BishopAutoturret)
                return false;

            if (!Spells.BishopAutoturretPvp.CanCast())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < 5 ) < MachinistSettings.Instance.Pvp_BishopAutoturretNumberOfEnemy)
                return false;

            return await Spells.BishopAutoturretPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MarksmansSpite()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MachinistSettings.Instance.Pvp_UseMarksmansSpite)
                return false;

            if (!Spells.MarksmansSpitePvp.CanCast())
                return false;

            if(Core.Me.CurrentTarget.CurrentHealthPercent > MachinistSettings.Instance.Pvp_UseMarksmansSpiteHealthPercent)
            {
                if (MachinistSettings.Instance.Pvp_UseMarksmansSpiteAnyTarget)
                {
                    var nearby = Combat.Enemies
                        .Where(e => e.Distance(Core.Me) <= 50
                                && e.ValidAttackUnit()
                                && e.InLineOfSight()
                                && e.CurrentHealthPercent <= MachinistSettings.Instance.Pvp_UseMarksmansSpiteHealthPercent)
                        .OrderBy(e => e.Distance(Core.Me));

                    var nearbyTarget = nearby.FirstOrDefault();

                    if (nearbyTarget != null)
                        return await Spells.MarksmansSpitePvp.Cast(nearbyTarget);
                }

                return false;
            }

            return await Spells.MarksmansSpitePvp.Cast(Core.Me.CurrentTarget);
        }        
    }
}

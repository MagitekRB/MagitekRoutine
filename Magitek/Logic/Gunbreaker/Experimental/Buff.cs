using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker.Experimental
{
    internal static class Buff
    {
        public static async Task<bool> RoyalGuard() //Tank stance
        {
            if (GunbreakerSettings.Instance.ManuallyControlTankStance)
                return false;

            switch (GunbreakerSettings.Instance.UseRoyalGuard)
            {
                case true:
                    if (!Core.Me.HasAura(Auras.RoyalGuard))
                        return await Spells.RoyalGuard.CastAura(Core.Me, Auras.RoyalGuard);
                    break;

                case false:
                    if (Core.Me.HasAura(Auras.RoyalGuard))
                        return await Spells.RoyalGuard.Cast(Core.Me);
                    break;
            }
            return false;
        }

        public static async Task<bool> NoMercy() // Damage Buff +20%
        {
            if (!GunbreakerSettings.Instance.UseNoMercy || !Spells.NoMercy.IsKnownAndReady())
                return false;

            // force delay CD
            // 2.50 GCD: Early weave (3 o'clock) acceptable
            // 2.45-2.47 GCD: Early weave without Bloodfest, late weave (9 o'clock) with Bloodfest
            // 2.40-2.44 GCD: Late weave (9 o'clock)
            double gcdSpeed = GunbreakerRoutine.GlobalCooldown.AdjustedCooldown.TotalSeconds;
            if (gcdSpeed < 2.45)
            {
                if (!GunbreakerRoutine.GlobalCooldown.IsLateWeaveWindow())
                    return false;
            }
            else if (gcdSpeed >= 2.45 && gcdSpeed <= 2.47)
            {
                if (Spells.Bloodfest.IsKnownAndReady(20000))
                {
                    if (!GunbreakerRoutine.GlobalCooldown.IsLateWeaveWindow())
                        return false;
                }
                else
                {
                    if (!GunbreakerRoutine.GlobalCooldown.CanWeave())
                        return false;
                }
            }
            else
            {
                if (!GunbreakerRoutine.GlobalCooldown.CanWeave())
                    return false;
            }

            // if (Spells.KeenEdge.Cooldown.TotalMilliseconds > Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset + 100)
            // {
            //     Logger.WriteInfo("[NoMercy] CHECK FAIL: Animation lock/CDTooHigh.");
            //     return false;
            // }

            // Force Delay when pulling
            if (Casting.LastSpell == Spells.LightningShot)
                return false;

            if (Casting.LastSpell == Spells.Bloodfest)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldNoMercy
                && !Spells.GnashingFang.IsKnownAndReady(3000)
                && !Spells.DoubleDown.IsKnownAndReady(5000))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.KeenEdge.Range))
                return false;

            // Special Condition for opener when UseNoMercyMaxCartridge 
            if (Core.Me.ClassLevel >= Spells.Bloodfest.LevelAcquired)
            {
                if (GunbreakerSettings.Instance.UseNoMercyMaxCartridge
                    && Spells.GnashingFang.IsKnownAndReady()
                    && Spells.DoubleDown.IsKnownAndReady()
                    && Spells.Bloodfest.IsKnownAndReady()
                    && Cartridge > 0)
                    return await Spells.NoMercy.Cast(Core.Me);
            }

            // solid barrel into No Mercy
            if (Cartridge < GunbreakerRoutine.MaxCartridge
            && ActionManager.LastSpell.Id == Spells.BrutalShell.Id
            && ActionManager.LastSpell.Id != Spells.BurstStrike.Id)
                return false;

            if (Cartridge == 0)
                return false;
            if (Cartridge < GunbreakerRoutine.MaxCartridge && GunbreakerSettings.Instance.UseNoMercyMaxCartridge)
                return false;

            return await Spells.NoMercy.CastAura(Core.Me, Auras.NoMercy);
        }

        public static async Task<bool> Bloodfest() // +2 or +3 cartrige
        {
            if (!GunbreakerSettings.Instance.UseBloodfest || !Spells.Bloodfest.IsKnownAndReady())
                return false;

            // Allow sacrifice 1 cartridge for alignment if needed, but only if No Mercy has less GCDs remaining
            if (Cartridge > 1 + GunbreakerRoutine.MaxCartridge - GunbreakerRoutine.AmountCartridgeFromBloodfest)
            {
                // Calculate minimum time for GCDs using GlobalCooldown
                int minTimeForGcds = (int)(GunbreakerRoutine.GlobalCooldown.AdjustedCooldownMs * 3.5);

                // Only allow sacrificing if No Mercy has less than the threshold GCDs remaining
                if (Core.Me.HasAura(Auras.NoMercy, false, minTimeForGcds))
                    return false; // Has enough GCDs remaining, don't sacrifice
            }
            // Otherwise, ensure we won't overcap at all (0 cartridges left after Bloodfest)
            else if (Cartridge > GunbreakerRoutine.MaxCartridge - GunbreakerRoutine.AmountCartridgeFromBloodfest)
            {
                return false;
            }

            // Don't cast immediately after No Mercy in the same pulse
            if (Casting.LastSpell == Spells.NoMercy)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (Spells.NoMercy.IsKnownAndReady(8000))
                return false;

            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.NoMercy.IsKnown() && !Spells.NoMercy.IsReady(3000))
                return false;

            return await Tank.UsePotion(GunbreakerSettings.Instance);
        }
    }
}

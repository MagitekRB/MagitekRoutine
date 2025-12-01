using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker.Latest
{
    internal static class Buff
    {
        public static async Task<bool> RoyalGuard() //Tank stance
        {
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
            if (!GunbreakerSettings.Instance.UseNoMercy)
                return false;

            // Don't try to cast if not ready or already active
            if (!Spells.NoMercy.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldNoMercy && !Spells.GnashingFang.IsKnownAndReady(3000) && !Spells.DoubleDown.IsKnownAndReady(5000))
                return false;

            //Force Delay when pulling
            if (Casting.LastSpell == Spells.LightningShot)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == GunbreakerStrategy.OptimizedBurst)
            {
                // Optimized No Mercy timing following The Balance guide
                // Uses late weave window for faster GCD speeds to fit 9 GCDs inside No Mercy

                // Cartridge requirement depends on AoE vs single-target and available abilities
                // Note: DoubleDown (90) requires GnashingFang (60), so if we have DoubleDown we have both
                int enemyCount = Combat.Enemies.Count(r => r.WithinSpellRange(5));
                bool hasDoubleDown = Core.Me.ClassLevel >= Spells.DoubleDown.LevelAcquired;
                bool hasGnashingFang = Core.Me.ClassLevel >= Spells.GnashingFang.LevelAcquired;

                int requiredCartridges = 0;

                // Calculate required cartridges based on available abilities
                if (enemyCount >= GunbreakerSettings.Instance.PrioritizeFatedCircleOverGnashingFangEnemies)
                {
                    // AoE: Skip Gnashing Fang, only need Double Down if available
                    if (hasDoubleDown)
                        requiredCartridges = 1;
                    // If no DoubleDown, no cartridges needed for AoE burst
                }
                else
                {
                    // Single-target: Need cartridges for available burst abilities
                    if (hasDoubleDown)
                        requiredCartridges = 2; // Have both Gnashing Fang and Double Down
                    else if (hasGnashingFang)
                        requiredCartridges = 1; // Only Gnashing Fang available
                    // If neither available, no cartridges needed (will use Burst Strike or other fillers)
                }

                if (Cartridge < requiredCartridges)
                    return false;

                // Don't cast right after Bloodfest (let GCD roll)
                if (Casting.LastSpell == Spells.Bloodfest)
                    return false;

                // Get GCD speed to determine weave timing
                double gcdSpeed = Spells.KeenEdge.AdjustedCooldown.TotalSeconds;

                // For GCD speeds < 2.50, use late weave to fit 9th GCD inside No Mercy
                // At 2.50 GCD, can use early weave (any time during weave window)
                if (gcdSpeed < 2.48)
                {
                    // Only cast No Mercy in the late weave window (9 o'clock position)
                    if (!GunbreakerRoutine.GlobalCooldown.IsLateWeaveWindow())
                        return false;
                }
                else
                {
                    // At 2.50 GCD, just need to be in any weave window
                    if (!GunbreakerRoutine.GlobalCooldown.CanWeave())
                        return false;
                }

                return await Spells.NoMercy.CastAura(Core.Me, Auras.NoMercy);
            }
            else
            {
                // Legacy No Mercy timing (FastGCD/SlowGCD strategies)
                if (GunbreakerSettings.Instance.GunbreakerStrategy == GunbreakerStrategy.SlowGCD && (Cartridge == 3 || Casting.LastSpell == Spells.Bloodfest) && Spells.NoMercy.IsKnownAndReady(1000))
                {
                    if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) >= GunbreakerSettings.Instance.UseAoeEnemies)
                    {
                        if (!await UseFatedCircle())
                            return false;
                    }
                    else
                    {
                        if (!await UseBurstStrike())
                            return false;
                    }

                    return await Spells.NoMercy.CastAura(Core.Me, Auras.NoMercy);
                }

                if (Casting.LastSpell == Spells.Bloodfest)
                    return false;

                if (Cartridge < 2)
                    return false;

                return await Spells.NoMercy.CastAura(Core.Me, Auras.NoMercy);
            }
        }

        public static async Task<bool> Bloodfest() // +2 or +3 cartrige
        {
            if (!GunbreakerSettings.Instance.UseBloodfest)
                return false;

            // Don't try to cast if not ready
            if (!Spells.Bloodfest.IsKnownAndReady())
                return false;

            // Don't cast immediately after No Mercy in the same pulse
            if (Casting.LastSpell == Spells.NoMercy)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // Smart Bloodfest logic for OptimizedBurst strategy
                // Bloodfest: 120s CD, No Mercy: 60s CD → Bloodfest every OTHER burst window
                // Priority: Use Bloodfest ASAP when available > Perfect ammo usage

                // Allow up to 1 cartridge before Bloodfest (sacrifice 1 for alignment if needed)
                if (Cartridge > 1)
                    return false;

                // Case 1: No Mercy buff is ACTIVE → Use Bloodfest immediately (in the burst window)
                if (Core.Me.HasAura(Auras.NoMercy))
                    return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);

                // Case 2: No Mercy is coming up very soon → Use Bloodfest to prep burst
                if (Spells.NoMercy.IsKnownAndReady(5000)) // Within 5s of No Mercy
                    return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);

                // Case 3: No Mercy was used recently (on cooldown with significant time remaining)
                // Use Bloodfest ASAP to avoid drift
                double noMercyCooldown = Spells.NoMercy.Cooldown.TotalSeconds;
                if (noMercyCooldown > 40) // No Mercy was used within last ~20s (>40s remaining on 60s CD)
                {
                    // Use Bloodfest immediately at 0-1 cartridges to catch the burst window
                    return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);
                }

                // Case 4: Normal situation - prefer 0 cartridges
                if (Cartridge == 0)
                    return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);

                return false;
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                // Must have room for all cartridges from Bloodfest (stricter)
                if (Cartridge > GunbreakerRoutine.MaxCartridge - GunbreakerRoutine.AmountCartridgeFromBloodfest)
                    return false;

                return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);
            }
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.NoMercy.IsKnown() && !Spells.NoMercy.IsReady(3000))
                return false;

            return await Tank.UsePotion(GunbreakerSettings.Instance);
        }

        private static async Task<bool> UseBurstStrike()
        {
            if (Core.Me.ClassLevel < Spells.BurstStrike.LevelAcquired)
                return false;

            if (!await Spells.BurstStrike.Cast(Core.Me.CurrentTarget))
                return false;

            return await Coroutine.Wait(1000, Spells.Hypervelocity.CanCast);
        }

        private static async Task<bool> UseFatedCircle()
        {
            if (Core.Me.ClassLevel < Spells.FatedCircle.LevelAcquired)
                return false;

            if (!await Spells.FatedCircle.Cast(Core.Me.CurrentTarget))
                return false;

            return await Coroutine.Wait(1000, Spells.FatedBrand.CanCast);
        }
    }
}
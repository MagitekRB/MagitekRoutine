using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using Auras = Magitek.Utilities.Auras;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker.Latest
{
    internal static class SingleTarget
    {

        /********************************************************************************
         *                               Pull - GCD
         *******************************************************************************/
        public static async Task<bool> LightningShotToDps()
        {
            if (!GunbreakerSettings.Instance.LightningShotToDps)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit()
                        || !Core.Me.CurrentTarget.NotInvulnerable()
                        || Core.Me.CurrentTarget.Distance(Core.Me) < Core.Me.CombatReach + Core.Me.CurrentTarget.CombatReach + GunbreakerSettings.Instance.LightningShotMinDistance
                        || !Core.Me.CurrentTarget.WithinSpellRange(20)
                        || (Core.Me.CurrentTarget as BattleCharacter).TargetGameObject == null)
                return false;

            if (!await Spells.LightningShot.Cast(Core.Me.CurrentTarget))
                return false;

            if (Core.Me.HasAura(Auras.NoMercy))
                return false;

            Logger.WriteInfo($@"Lightning Shot On {Core.Me.CurrentTarget.Name} To DPS");
            return true;
        }

        public static async Task<bool> LightningShotToPullOrAggro()
        {
            if (!GunbreakerSettings.Instance.LightningShotToPullAggro)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (!Core.Me.HasAura(Auras.RoyalGuard))
                return false;

            //find target already pulled on which I lose aggro
            var lightningShotTarget = Combat.Enemies.FirstOrDefault(r => r.ValidAttackUnit()
                                                                    && r.NotInvulnerable()
                                                                    && r.Distance(Core.Me) >= Core.Me.CombatReach + r.CombatReach + GunbreakerSettings.Instance.LightningShotMinDistance
                                                                    && r.WithinSpellRange(20)
                                                                    && r.TargetGameObject != Core.Me);

            //if no target found, then check if current target is not pulled yet
            if (lightningShotTarget == null)
            {
                lightningShotTarget = (BattleCharacter)Core.Me.CurrentTarget;

                if (!lightningShotTarget.ValidAttackUnit()
                    || !lightningShotTarget.NotInvulnerable()
                    || lightningShotTarget.Distance(Core.Me) < Core.Me.CombatReach + lightningShotTarget.CombatReach + GunbreakerSettings.Instance.LightningShotMinDistance
                    || !lightningShotTarget.WithinSpellRange(20)
                    || lightningShotTarget.TargetGameObject != null)
                    return false;
            }

            if (!await Spells.LightningShot.Cast(lightningShotTarget))
                return false;

            Logger.WriteInfo($@"Lightning Shot On {lightningShotTarget.Name} to pull or get back aggro");
            return true;
        }

        /********************************************************************************
         *                            Primary combo
         *******************************************************************************/
        public static async Task<bool> KeenEdge()
        {
            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Cartridge >= 2 && CanNoMercy())
            {
                // Check if No Mercy is ready but we're not in a weave window
                // If so, allow casting this GCD to create the weave window for No Mercy
                if (Spells.NoMercy.IsKnownAndReady() && !GunbreakerRoutine.GlobalCooldown.CanWeave())
                {
                    // Allow casting to create weave window - this breaks the deadlock
                    return await Spells.KeenEdge.Cast(Core.Me.CurrentTarget);
                }
                return false;
            }

            return await Spells.KeenEdge.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BrutalShell()
        {
            if (!GunbreakerRoutine.CanContinueComboAfter(Spells.KeenEdge))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Cartridge >= 2 && CanNoMercy() && Casting.LastSpell != Spells.Bloodfest)
            {
                // Check if No Mercy is ready but we're not in a weave window
                // If so, allow casting this GCD to create the weave window for No Mercy
                if (Spells.NoMercy.IsKnownAndReady() && !GunbreakerRoutine.GlobalCooldown.CanWeave())
                {
                    // Allow casting to create weave window - this breaks the deadlock
                    return await Spells.BrutalShell.Cast(Core.Me.CurrentTarget);
                }
                return false;
            }

            return await Spells.BrutalShell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SolidBarrel()
        {
            if (!GunbreakerRoutine.CanContinueComboAfter(Spells.BrutalShell))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // For OptimizedBurst: Prevent cartridge overcapping
                // Let rotation priority handle burst strike when at max cartridges

                // Don't finish combo if at max cartridges - let Burst Strike spend first
                if (Cartridge >= GunbreakerRoutine.MaxCartridge)
                    return false;

                // Hold combo completion if we're about to use No Mercy (avoid cartridge overcap)
                // BUT: Allow casting if No Mercy is ready but not in weave window - we need to cast a GCD to create the weave window
                if (Cartridge >= 2 && CanNoMercy())
                {
                    // Check if No Mercy is ready but we're not in a weave window
                    // If so, allow casting this GCD to create the weave window for No Mercy
                    if (Spells.NoMercy.IsKnownAndReady() && !GunbreakerRoutine.GlobalCooldown.CanWeave())
                    {
                        // Allow casting to create weave window - this breaks the deadlock
                        return await Spells.SolidBarrel.Cast(Core.Me.CurrentTarget);
                    }
                    return false;
                }

                return await Spells.SolidBarrel.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                if (Cartridge >= 2 && CanNoMercy())
                    return false;

                if (Cartridge == GunbreakerRoutine.MaxCartridge)
                {
                    if (!await UseBurstStrike())
                        return false;

                    if (CanNoMercy())
                        return await Spells.NoMercy.Cast(Core.Me);
                }

                return await Spells.SolidBarrel.Cast(Core.Me.CurrentTarget);
            }
        }

        private static async Task<bool> UseBurstStrike()
        {
            if (Core.Me.ClassLevel < Spells.BurstStrike.LevelAcquired)
                return false;

            if (!await Spells.BurstStrike.Cast(Core.Me.CurrentTarget))
                return false;

            return await Coroutine.Wait(1000, Spells.Hypervelocity.CanCast);
        }

        private static bool CanNoMercy()
        {
            if (!GunbreakerSettings.Instance.UseNoMercy)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldNoMercy && !Spells.GnashingFang.IsKnownAndReady(3000) && !Spells.DoubleDown.IsKnownAndReady(5000))
                return false;

            if (!Spells.NoMercy.IsKnownAndReady())
                return false;

            return true;
        }


        /********************************************************************************
         *                            Secondary combo
         *******************************************************************************/
        public static async Task<bool> GnashingFang()
        {
            if (!GunbreakerSettings.Instance.UseAmmoCombo)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Cartridge < GunbreakerRoutine.RequiredCartridgeForGnashingFang)
                return false;

            // AoE check: At 4+ enemies, prefer Fated Circle over Gnashing Fang combo
            // BUT: If Fated Circle is not available, allow Gnashing Fang in AoE
            int enemyCount = Combat.Enemies.Count(r => r.WithinSpellRange(5));
            bool hasFatedCircle = Core.Me.ClassLevel >= Spells.FatedCircle.LevelAcquired;
            if (enemyCount >= GunbreakerSettings.Instance.PrioritizeFatedCircleOverGnashingFangEnemies && hasFatedCircle)
                return false;

            if (Spells.NoMercy.IsKnownAndReady())
                return false;

            // Smart Gnashing Fang hold logic for OptimizedBurst strategy
            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                double noMercyCooldown = Spells.NoMercy.Cooldown.TotalSeconds;

                // Get Gnashing Fang's cooldown and subtract 5s buffer for safety
                double gnashingFangCooldown = Spells.GnashingFang.AdjustedCooldown.TotalSeconds;
                double holdThreshold = gnashingFangCooldown - 5.0; // e.g., 28.89s - 5s = 23.89s

                // Only use Gnashing Fang if it will be ready again before No Mercy comes off CD
                // Hold if No Mercy CD < (Gnashing Fang CD - 5s buffer)
                if (noMercyCooldown > 0 && noMercyCooldown < holdThreshold)
                {
                    // Hold - using Gnashing Fang now would make it unavailable for No Mercy
                    return false;
                }
            }
            else
            {
                // Legacy hold logic for FastGCD/SlowGCD strategies
                if (GunbreakerSettings.Instance.HoldAmmoCombo && Spells.NoMercy.IsKnownAndReady(GunbreakerSettings.Instance.HoldAmmoComboSeconds * 1000))
                    return false;
            }

            return await Spells.GnashingFang.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SavageClaw()
        {
            if (SecondaryComboStage != 1)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            return await Spells.SavageClaw.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> WickedTalon()
        {
            if (SecondaryComboStage != 2)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            return await Spells.WickedTalon.Cast(Core.Me.CurrentTarget);
        }


        /********************************************************************************
         *                           Secondary combo oGCD 
         *******************************************************************************/
        public static async Task<bool> JugularRip()
        {
            if (!Core.Me.HasAura(Auras.ReadytoRip))
                return false;

            return await Spells.JugularRip.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AbdomenTear()
        {
            if (!Core.Me.HasAura(Auras.ReadytoTear))
                return false;

            return await Spells.AbdomenTear.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EyeGouge()
        {
            if (!Core.Me.HasAura(Auras.ReadytoGouge))
                return false;

            return await Spells.EyeGouge.Cast(Core.Me.CurrentTarget);
        }


        /********************************************************************************
         *                              Third combo GCD  
         *******************************************************************************/

        public static async Task<bool> BurstStrike()
        {
            if (!GunbreakerSettings.Instance.UseBurstStrike)
                return false;

            if (Cartridge < GunbreakerRoutine.RequiredCartridgeForBurstStrike)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            // AoE check: At 2+ enemies, prefer Fated Circle over Burst Strike
            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) >= GunbreakerSettings.Instance.PrioritizeFatedCircleOverBurstStrikeEnemies)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // Get GCD time for calculations
                double gcdTime = Spells.KeenEdge.AdjustedCooldown.TotalMilliseconds;

                // Priority 1: The Balance - "Burst Strike into No Mercy when you will also have Bloodfest"
                // Prevent cartridge overcap - ALWAYS dump at max cartridges
                if (Cartridge >= GunbreakerRoutine.MaxCartridge)
                {
                    // Don't dump if Reign combo is active AND No Mercy is active (executing in burst)
                    // This is the ONLY case where we skip - Reign combo should execute in No Mercy
                    if (Core.Me.HasAura(Auras.ReadyToReign) && Core.Me.HasAura(Auras.NoMercy))
                        return false; // Let Reign execute inside No Mercy buff

                    // In ALL other cases at max cartridges: DUMP
                    // This prevents: Bloodfest → Keen Edge (breaks combo)
                    // This allows: Bloodfest → Burst Strike → filler GCD → No Mercy
                    return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
                }

                // Priority 2: Force cartridge spending when Bloodfest is coming soon
                // Start dumping ~3 GCDs before Bloodfest comes off cooldown
                int bloodfestPreDumpWindow = (int)(gcdTime * 3); // 3 GCDs worth of time

                // If Bloodfest will be ready within 3 GCDs and we have cartridges, start dumping
                if (Spells.Bloodfest.IsKnownAndReady(bloodfestPreDumpWindow) && Cartridge > 0)
                {
                    // Don't spend during important combos, but dump otherwise
                    if (!Spells.GnashingFang.IsKnownAndReady() &&
                        !Spells.DoubleDown.IsKnownAndReady() &&
                        !Core.Me.HasAura(Auras.ReadyToReign))
                    {
                        return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
                    }
                }

                // Priority 3: Normal burst strike logic (inside No Mercy)
                if (!Core.Me.HasAura(Auras.NoMercy))
                    return false;

                if (Spells.DoubleDown.IsKnownAndReady() || Spells.GnashingFang.IsKnownAndReady() || Spells.SonicBreak.IsKnownAndReadyAndCastable())
                    return false;

                return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                // Normal burst strike logic (inside No Mercy)
                if (!Core.Me.HasAura(Auras.NoMercy))
                    return false;

                if (Spells.DoubleDown.IsKnownAndReady() || Spells.GnashingFang.IsKnownAndReady() || Spells.SonicBreak.IsKnownAndReadyAndCastable())
                    return false;

                return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
            }
        }

        /********************************************************************************
         *                              Third combo oGCD  
         *******************************************************************************/
        public static async Task<bool> Hypervelocity()
        {
            if (!Core.Me.HasAura(Auras.ReadytoBlast))
                return false;

            return await Spells.Hypervelocity.Cast(Core.Me.CurrentTarget);
        }

        /********************************************************************************
        *                              Four combo oGCD  
        *******************************************************************************/

        public static async Task<bool> ReignOfBeasts()
        {

            if (Core.Me.ClassLevel < 100)
                return false;

            if (!GunbreakerSettings.Instance.UseLionHeartCombo)
                return false;

            if (!Core.Me.HasAura(Auras.ReadyToReign))
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // For OptimizedBurst: ALWAYS save Lion Heart combo for No Mercy
                // Lion Heart is ~1000 potency - must be buffed by No Mercy!
                if (!Core.Me.HasAura(Auras.NoMercy))
                    return false;

                if (GunbreakerRoutine.IsAurasForComboActive())
                    return false;

                return await Spells.ReignOfBeasts.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                //if (Spells.GnashingFang.IsKnownAndReady(1000) && Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                //  return false;

                if (Spells.DoubleDown.IsKnownAndReady(1000) && !GunbreakerSettings.Instance.BurstLogicHoldBurst)
                    return false;

                if (GunbreakerRoutine.IsAurasForComboActive())
                    return false;

                return await Spells.ReignOfBeasts.Cast(Core.Me.CurrentTarget);
            }
        }

        public static async Task<bool> NobleBlood()
        {

            if (Core.Me.ClassLevel < 100)
                return false;

            if (!GunbreakerSettings.Instance.UseLionHeartCombo)
                return false;

            return await Spells.NobleBlood.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> LionHeart()
        {
            if (Core.Me.ClassLevel < 100)
                return false;

            if (!GunbreakerSettings.Instance.UseLionHeartCombo)
                return false;

            return await Spells.LionHeart.Cast(Core.Me.CurrentTarget);
        }

        /********************************************************************************
         *                                    oGCD 
         *******************************************************************************/
        public static async Task<bool> BlastingZone()
        {
            if (!GunbreakerSettings.Instance.UseBlastingZone)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // Use Blasting Zone on cooldown (30s CD)
                // This gives 2 uses per No Mercy window (60s / 30s = 2)

                // Hold if No Mercy is coming up very soon - save it for inside the buff window
                if (Spells.NoMercy.IsKnownAndReady(5000))
                    return false;

                return await GunbreakerRoutine.BlastingZone.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                if (Spells.NoMercy.IsKnownAndReady())
                    return false;

                if (GunbreakerSettings.Instance.HoldBlastingZone && Spells.NoMercy.IsKnownAndReady(GunbreakerSettings.Instance.HoldAmmoComboSeconds * 1000))
                    return false;

                if (Core.Me.HasAura(Auras.NoMercy) && Spells.DoubleDown.IsKnownAndReady() || Spells.GnashingFang.IsKnownAndReady())
                    return false;

                return await GunbreakerRoutine.BlastingZone.Cast(Core.Me.CurrentTarget);
            }
        }


        /********************************************************************************
         *                                    Gap Closer
         *******************************************************************************/
        public static async Task<bool> Trajectory()
        {
            if (!GunbreakerSettings.Instance.UseTrajectory)
                return false;

            if (!Spells.Trajectory.IsKnown())
                return false;

            if (Casting.LastSpell == Spells.Trajectory)
                return false;

            if (GunbreakerSettings.Instance.TrajectoryOnlyInMelee && !Core.Me.CurrentTarget.WithinSpellRange(Spells.KeenEdge.Range))
                return false;

            if (Spells.Trajectory.Charges <= GunbreakerSettings.Instance.SaveTrajectoryCharges + 1)
                return false;

            if (!GunbreakerRoutine.GlobalCooldown.CanWeave(1))
                return false;

            return await Spells.Trajectory.Cast(Core.Me.CurrentTarget);
        }

        /********************************************************************************
         *                                    GCD 
         *******************************************************************************/
        public static async Task<bool> SonicBreak()
        {
            if (!Spells.SonicBreak.IsKnownAndReadyAndCastable())
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // For OptimizedBurst: Sonic Break is lowest priority GCD filler
                // Only use when higher priority GCDs are not available

                // Don't use if any higher priority GCD is ready
                if (Spells.GnashingFang.IsKnownAndReady() ||
                    Spells.DoubleDown.IsKnownAndReady() ||
                    Core.Me.HasAura(Auras.ReadyToReign) ||
                    Spells.NobleBlood.IsKnownAndReadyAndCastable() ||
                    Spells.LionHeart.IsKnownAndReadyAndCastable())
                    return false;

                return await Spells.SonicBreak.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                if (Spells.GnashingFang.IsKnownAndReady() || Spells.DoubleDown.IsKnownAndReady() || Spells.Bloodfest.IsKnownAndReady() || Spells.Bloodfest.Cooldown.TotalMilliseconds >= 118000)
                    return false;

                if (Core.Me.HasAura(Auras.ReadyToReign) || Spells.NobleBlood.IsKnownAndReadyAndCastable() || Spells.LionHeart.IsKnownAndReadyAndCastable())
                    return false;

                return await Spells.SonicBreak.Cast(Core.Me.CurrentTarget);
            }
        }
    }
}
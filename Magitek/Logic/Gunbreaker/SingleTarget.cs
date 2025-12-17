using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using Auras = Magitek.Utilities.Auras;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker
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
                        || !Core.Me.WithinSpellRange(20)
                        || (Core.Me.CurrentTarget as BattleCharacter).TargetGameObject == null)
                return false;

            if (!await Spells.LightningShot.Cast(Core.Me.CurrentTarget))
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

            return await Spells.KeenEdge.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BrutalShell()
        {
            if (!GunbreakerRoutine.CanContinueComboAfter(Spells.KeenEdge))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            return await Spells.BrutalShell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SolidBarrel()
        {
            if (!GunbreakerRoutine.CanContinueComboAfter(Spells.BrutalShell))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            return await Spells.SolidBarrel.Cast(Core.Me.CurrentTarget);
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

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) >= GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            if (Spells.NoMercy.IsKnownAndReady(GunbreakerSettings.Instance.HoldAmmoComboSeconds * 1000))
                return false;

            if (Spells.DoubleDown.IsKnownAndReady(2000) && Cartridge == GunbreakerRoutine.RequiredCartridgeForDoubleDown)
                return false;

            // Handle 2-charge Gnashing Fang logic for optimal No Mercy window usage (7.4+)
            if (Core.Me.HasAura(Auras.NoMercy))
            {
                int gnashingFangUses = GunbreakerRoutine.GnashingFangUsesThisBurst;
                Logger.WriteInfo($@"[GnashingFang] In NoMercy window - GnashingFangUsesThisBurst: {gnashingFangUses}, Charges: {Spells.GnashingFang.Charges:F2}");

                // First use (early in burst window)
                if (gnashingFangUses == 0)
                {
                    Logger.WriteInfo($@"[GnashingFang] Attempting first use (early in burst)");
                    return await Spells.GnashingFang.Cast(Core.Me.CurrentTarget, callback: async () =>
                    {
                        GunbreakerRoutine.GnashingFangUsesThisBurst++;
                        Logger.WriteInfo($@"[GnashingFang] First use successful - counter now: {GunbreakerRoutine.GnashingFangUsesThisBurst}");
                        await Task.CompletedTask;
                    });
                }

                // Second use (after Lion Heart combo completes)
                if (gnashingFangUses == 1)
                {
                    // Check if Bloodfest has been used (60s cooldown as of 7.4)
                    bool bloodfestUsed = Spells.Bloodfest.Cooldown.TotalMilliseconds > 0;

                    // Check if Lion Heart combo is complete
                    bool lionHeartComboComplete = !Core.Me.HasAura(Auras.ReadyToReign) &&
                                                  !Spells.ReignOfBeasts.CanCast() &&
                                                  !Spells.LionHeart.CanCast() &&
                                                  !Spells.NobleBlood.CanCast();

                    Logger.WriteInfo($@"[GnashingFang] Second use check - BloodfestUsed: {bloodfestUsed}, LionHeartComboComplete: {lionHeartComboComplete}");

                    // Only use second charge after Bloodfest and Lion Heart combo
                    if (bloodfestUsed && lionHeartComboComplete)
                    {
                        Logger.WriteInfo($@"[GnashingFang] Attempting second use (after Lion Heart combo)");
                        return await Spells.GnashingFang.Cast(Core.Me.CurrentTarget, callback: async () =>
                        {
                            GunbreakerRoutine.GnashingFangUsesThisBurst++;
                            Logger.WriteInfo($@"[GnashingFang] Second use successful - counter now: {GunbreakerRoutine.GnashingFangUsesThisBurst}");
                            await Task.CompletedTask;
                        });
                    }

                    // Not ready for second charge yet - hold
                    Logger.WriteInfo($@"[GnashingFang] SKIP: Second use not ready yet (holding for conditions)");
                    return false;
                }

                // Already used both charges this burst - don't use more
                Logger.WriteInfo($@"[GnashingFang] SKIP: Already used {gnashingFangUses} charges this burst");
                return false;
            }

            // Outside No Mercy window - use normally (don't track, counter resets on next No Mercy)
            Logger.WriteInfo($@"[GnashingFang] Outside NoMercy window - using normally");
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

            if (Spells.FatedCircle.IsKnown() && Combat.Enemies.Count(r => r.WithinSpellRange(5)) >= GunbreakerSettings.Instance.PrioritizeFatedCircleOverBurstStrikeEnemies)
                return false;

            if (Core.Me.HasAura(Auras.ReadyToReign))
                return false;

            if (Spells.NoMercy.IsKnownAndReady(2000) && Cartridge < GunbreakerRoutine.MaxCartridge)
                return false;

            //Cast BurstStrike in NoMercy Window
            if (Core.Me.HasAura(Auras.NoMercy) && Cartridge > 0)
            {
                if (Cartridge < GunbreakerRoutine.MaxCartridge && (Spells.DoubleDown.IsKnownAndReady(10000) || Spells.GnashingFang.IsKnownAndReady(10000)))
                    return false;

                return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
            }

            //Cast BurstStrike after No Mercy window
            if (Core.Me.ClassLevel >= Spells.Bloodfest.LevelAcquired)
            {
                if (!Core.Me.HasAura(Auras.NoMercy)
                && Cartridge >= 1
                && Spells.NoMercy.Cooldown.TotalMilliseconds >= 35000
                && !Spells.GnashingFang.IsKnownAndReady(6500)
                && Spells.Bloodfest.Cooldown.TotalMilliseconds >= 30000)
                    return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
            }

            if (Cartridge >= GunbreakerRoutine.MaxCartridge
                && (ActionManager.LastSpell.Id != Spells.BrutalShell.Id))
                return false;

            if (Cartridge < GunbreakerRoutine.MaxCartridge)
                return false;

            return await Spells.BurstStrike.Cast(Core.Me.CurrentTarget);
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
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: Level {Core.Me.ClassLevel} < 100");
                return false;
            }

            if (!GunbreakerSettings.Instance.UseLionHeartCombo)
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: UseLionHeartCombo disabled");
                return false;
            }

            if (!Core.Me.HasAura(Auras.ReadyToReign))
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: No ReadyToReign aura");
                return false;
            }

            int enemyCount = Combat.Enemies.Count(r => r.WithinSpellRange(5));
            bool gnashingFangReady = Spells.GnashingFang.IsKnownAndReady(1000);
            int gnashingFangUses = GunbreakerRoutine.GnashingFangUsesThisBurst;

            if (gnashingFangReady
            && enemyCount < GunbreakerSettings.Instance.UseAoeEnemies
            && gnashingFangUses < 1)
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: GnashingFang ready (charges: {Spells.GnashingFang.Charges:F2}), enemyCount: {enemyCount}, GnashingFangUsesThisBurst: {gnashingFangUses} < 1");
                return false;
            }

            if (Spells.DoubleDown.IsKnownAndReady(1000))
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: DoubleDown ready");
                return false;
            }

            if (GunbreakerRoutine.IsAurasForComboActive())
            {
                Logger.WriteInfo($@"[ReignOfBeasts] SKIP: Combo continuation auras active");
                return false;
            }

            Logger.WriteInfo($@"[ReignOfBeasts] CASTING - GnashingFangUsesThisBurst: {gnashingFangUses}, EnemyCount: {enemyCount}");
            return await Spells.ReignOfBeasts.Cast(Core.Me.CurrentTarget);
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

            if (GunbreakerSettings.Instance.HoldBlastingZone && Spells.NoMercy.IsKnownAndReady(GunbreakerSettings.Instance.HoldBlastingZoneSeconds * 1000))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Core.Me.HasAura(Auras.NoMercy) && Spells.DoubleDown.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.NoMercy) && Spells.GnashingFang.IsKnownAndReady() && GunbreakerRoutine.GnashingFangUsesThisBurst < 1)
                return false;

            return await GunbreakerRoutine.BlastingZone.Cast(Core.Me.CurrentTarget);
        }


        /********************************************************************************
         *                                    GCD 
         *******************************************************************************/
        public static async Task<bool> SonicBreak()
        {
            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            // Delay Sonic Break until after Gnashing Fang and Bloodfest are used (7.4: 60s Bloodfest)
            if ((Spells.GnashingFang.IsKnownAndReady() && GunbreakerRoutine.GnashingFangUsesThisBurst < 1)
                || Spells.Bloodfest.IsKnownAndReady()
                || Spells.Bloodfest.Cooldown.TotalMilliseconds >= 58800)
                return false;

            return await Spells.SonicBreak.Cast(Core.Me.CurrentTarget);
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
    }
}

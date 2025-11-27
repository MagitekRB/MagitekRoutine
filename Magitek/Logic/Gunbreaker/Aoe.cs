using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker
{
    internal static class Aoe
    {

        /*************************************************************************************
         *                                    Combo
         * ***********************************************************************************/
        public static async Task<bool> DemonSlice()
        {
            if (!GunbreakerSettings.Instance.UseAoe)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            return await Spells.DemonSlice.Cast(Core.Me);
        }

        public static async Task<bool> DemonSlaughter()
        {
            if (!GunbreakerSettings.Instance.UseAoe)
                return false;

            if (!GunbreakerRoutine.CanContinueComboAfter(Spells.DemonSlice))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy == Enumerations.GunbreakerStrategy.OptimizedBurst)
            {
                // For OptimizedBurst: Prevent cartridge overcapping
                // Let rotation priority handle Fated Circle when at max cartridges

                // Don't finish combo if at max cartridges - let Fated Circle spend first
                if (Cartridge >= GunbreakerRoutine.MaxCartridge)
                    return false;

                // Hold combo completion if we're about to use No Mercy (avoid cartridge overcap)
                if (Cartridge >= 2 && CanNoMercy())
                    return false;

                return await Spells.DemonSlaughter.Cast(Core.Me);
            }
            else
            {
                // Legacy logic for FastGCD/SlowGCD strategies
                if (Cartridge >= 2 && CanNoMercy())
                    return false;

                if (Cartridge == GunbreakerRoutine.MaxCartridge)
                {
                    if (!await UseFatedCircle())
                        return false;

                    if (CanNoMercy())
                        return await Spells.NoMercy.Cast(Core.Me);
                }

                return await Spells.DemonSlaughter.Cast(Core.Me);
            }
        }

        private static async Task<bool> UseFatedCircle()
        {
            if (Core.Me.ClassLevel < Spells.FatedCircle.LevelAcquired)
                return false;

            if (!await Spells.FatedCircle.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, Spells.FatedBrand.CanCast);
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

        /*************************************************************************************
         *                                    GCD
         * ***********************************************************************************/
        public static async Task<bool> FatedCircle()
        {
            if (!GunbreakerSettings.Instance.UseAoe)
                return false;

            if (!GunbreakerSettings.Instance.UseFatedCircle)
                return false;

            if (Cartridge < GunbreakerRoutine.RequiredCartridgeForFatedCircle)
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (Spells.DoubleDown.IsKnownAndReady() || Spells.GnashingFang.IsKnownAndReady())
                return false;

            return await Spells.FatedCircle.Cast(Core.Me);
        }

        /*************************************************************************************
         *                                    oGCD
         * ***********************************************************************************/
        public static async Task<bool> BowShock()
        {
            if (!GunbreakerSettings.Instance.UseBowShock)
                return false;

            //if (GunbreakerRoutine.IsAurasForComboActive())
            //    return false;

            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.BowShockEnemies)
                return false;

            if (Core.Me.HasAura(Auras.NoMercy) && Spells.DoubleDown.IsKnownAndReady() || Spells.GnashingFang.IsKnownAndReady())
                return false;

            return await Spells.BowShock.Cast(Core.Me);
        }

        public static async Task<bool> FatedBrand()
        {
            if (Core.Me.ClassLevel < Spells.FatedBrand.LevelAcquired)
                return false;

            if (!GunbreakerSettings.Instance.UseAoe)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            return await Spells.FatedBrand.Cast(Core.Me);
        }

        public static async Task<bool> DoubleDown()
        {
            if (!GunbreakerSettings.Instance.UseDoubleDown)
                return false;

            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (Cartridge < GunbreakerRoutine.RequiredCartridgeForDoubleDown)
                return false;

            if (Spells.GnashingFang.IsKnownAndReady(1000) && Cartridge >= 1)
                return false;

            return await Spells.DoubleDown.Cast(Core.Me);
        }
    }
}
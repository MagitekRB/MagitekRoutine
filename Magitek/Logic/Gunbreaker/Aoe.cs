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

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.UseAoeEnemies)
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

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.UseAoeEnemies)
                return false;

            // Block overcapping when FatedCircle is known (the aoe cartridge dump)
            if (Cartridge == GunbreakerRoutine.MaxCartridge && Spells.FatedCircle.IsKnown())
                return false;

            return await Spells.DemonSlaughter.Cast(Core.Me);
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

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.PrioritizeFatedCircleOverBurstStrikeEnemies)
                return false;

            if (Core.Me.HasAura(Auras.NoMercy) && Cartridge > 0)
            {
                if (Spells.DoubleDown.IsKnownAndReady())
                    return false;

                return await Spells.FatedCircle.Cast(Core.Me);
            }

            if (Cartridge == GunbreakerRoutine.MaxCartridge
            && !GunbreakerRoutine.CanContinueComboAfter(Spells.DemonSlice)
            && !GunbreakerRoutine.CanContinueComboAfter(Spells.BrutalShell))
                return false;

            if (Cartridge < GunbreakerRoutine.MaxCartridge)
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

            if (!Core.Me.HasAura(Auras.NoMercy))
                return false;

            if (GunbreakerRoutine.IsAurasForComboActive())
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.BowShockEnemies)
                return false;

            if (Core.Me.HasAura(Auras.NoMercy) && Spells.DoubleDown.IsKnownAndReady())
                return false;

            return await Spells.BowShock.Cast(Core.Me);
        }

        public static async Task<bool> FatedBrand()
        {
            if (Core.Me.ClassLevel < Spells.FatedBrand.LevelAcquired)
                return false;

            if (!GunbreakerSettings.Instance.UseAoe)
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.PrioritizeFatedCircleOverBurstStrikeEnemies)
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

            if (Spells.GnashingFang.IsKnownAndReady(1000) && Combat.Enemies.Count(r => r.WithinSpellRange(5)) < GunbreakerSettings.Instance.UseAoeEnemies && Cartridge == GunbreakerRoutine.MaxCartridge)
                return false;

            return await Spells.DoubleDown.Cast(Core.Me);
        }
    }
}

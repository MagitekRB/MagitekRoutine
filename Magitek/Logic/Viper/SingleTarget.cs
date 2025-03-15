using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Viper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Viper
{
    internal static class SingleTarget
    {

        public static async Task<bool> SteelOrReavingFangs()
        {
            if (Core.Me.ClassLevel < Spells.SteelFangs.LevelAcquired)
                return false;

            if (Core.Me.HasAura(Auras.HunterVenom, true) || Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (Core.Me.HasAura(Auras.PoisedforTwinfang, true) || Core.Me.HasAura(Auras.PoisedforTwinblood, true))
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Core.Me.ClassLevel >= Spells.ReavingFangs.LevelAcquired && Core.Me.HasAura(Auras.HonedReavers, true))
                return await Spells.ReavingFangs.Cast(Core.Me.CurrentTarget);

            return await Spells.SteelFangs.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HunterOrSwiftSkinSting()
        {
            if (Core.Me.ClassLevel < Spells.HunterSting.LevelAcquired)
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Core.Me.ClassLevel < Spells.SwiftskinSting.LevelAcquired)
                return await Spells.HunterSting.Cast(Core.Me.CurrentTarget);

            if (!Core.Me.HasAura(Auras.Swiftscaled, true))
                return await Spells.SwiftskinSting.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HindstungVenom, true) || Core.Me.HasAura(Auras.HindsbaneVenom, true))
                return await Spells.SwiftskinSting.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.FlankstungVenom, true) || Core.Me.HasAura(Auras.FlanksbaneVenom, true))
                return await Spells.HunterSting.Cast(Core.Me.CurrentTarget);

            return await Spells.HunterSting.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> FankstingOrFlankbane()
        {

            if (Core.Me.ClassLevel < Spells.HindsbaneFang.LevelAcquired || Core.Me.ClassLevel < Spells.FankstingStrike.LevelAcquired)
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Core.Me.HasAura(Auras.FlankstungVenom, true))
                return await Spells.FankstingStrike.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.FlanksbaneVenom, true))
                return await Spells.FanksbaneFang.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HindstungVenom, true))
                return await Spells.HindstingStrike.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HindsbaneVenom, true))
                return await Spells.HindsbaneFang.Cast(Core.Me.CurrentTarget);

            else
                return await Spells.FankstingStrike.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Vicewinder()
        {
            if (Core.Me.ClassLevel < Spells.Vicewinder.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseVicewinder)
                return false;

            if (!Core.Me.HasAura(Auras.Swiftscaled, true))
                return false;

            if (Core.Me.HasAura(Auras.HunterVenom, true) || Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (Core.Me.HasAura(Auras.PoisedforTwinfang, true) || Core.Me.HasAura(Auras.PoisedforTwinblood, true))
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            return await Spells.Vicewinder.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HunterOrSwiftskinCoil()
        {
            if (Core.Me.ClassLevel < Spells.SwiftskinCoil.LevelAcquired)
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Core.Me.ClassLevel >= Spells.HunterCoil.LevelAcquired && Spells.HunterCoil.CanCast())
                return await Spells.HunterCoil.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (!Spells.SwiftskinCoil.CanCast())
                return false;

            return await Spells.SwiftskinCoil.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> UncoiledFury()
        {
            if (Core.Me.ClassLevel < Spells.UncoiledFury.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseUncoiledFury)
                return false;

            if (!Spells.UncoiledFury.CanCast() || ActionResourceManager.Viper.RattlingCoils <= ViperSettings.Instance.UncoiledFurySaveChrages)
                return false;

            if (Core.Me.CurrentTarget.Distance() >= 5)
                return await Spells.UncoiledFury.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.HunterVenom, true) || Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (Spells.Vicewinder.IsKnownAndReadyAndCastable() || Spells.Reawaken.IsKnownAndReadyAndCastable() || Spells.HunterCoil.IsKnownAndReadyAndCastable() || Spells.SwiftskinCoil.IsKnownAndReadyAndCastable())
                return false;

            return await Spells.UncoiledFury.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> Reawaken()
        {
            if (Core.Me.ClassLevel < Spells.Reawaken.LevelAcquired)
                return false;

            if (!ViperSettings.Instance.UseReawaken || ViperSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Spells.Reawaken.CanCast())
                return false;

            if (!Core.Me.HasAura(Auras.Swiftscaled, true) || !Core.Me.HasAura(Auras.HunterInstinct, true))
                return false;

            if (Spells.HunterCoil.IsKnownAndReadyAndCastable() || Spells.SwiftskinCoil.IsKnownAndReadyAndCastable())
                return false;

            if(Spells.SerpentIre.IsKnownAndReady(30000))
                return false;

            if (!CanReawaken(Core.Me.CurrentTarget))
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.FirstGeneration.Range)
                return false;

            return await Spells.Reawaken.Cast(Core.Me.CurrentTarget);

        }

        public static bool CanReawaken(GameObject unit)
        {
            if (unit.IsBoss())
                return true;

            return unit.CombatTimeLeft() >= ViperSettings.Instance.DontReawakenIfEnemyDyingWithinSeconds;
        }

        public static async Task<bool> Ouroboros()
        {
            if (Core.Me.ClassLevel < Spells.Ouroboros.LevelAcquired)
                return false;

            if (!Spells.Ouroboros.CanCast())
                return false;

            if(Core.Me.CurrentTarget.Distance(Core.Me) > Spells.Ouroboros.Range + Core.Me.CombatReach)
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(1))
                return await Spells.Ouroboros.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> FirstGeneration()
        {
            if (Core.Me.ClassLevel < Spells.FirstGeneration.LevelAcquired)
                return false;

            if (!Spells.FirstGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.FirstGeneration.Range + Core.Me.CombatReach)
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(5))
                return await Spells.FirstGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> SecondGeneration()
        {
            if (Core.Me.ClassLevel < Spells.SecondGeneration.LevelAcquired)
                return false;

            if (!Spells.SecondGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.SecondGeneration.Range + Core.Me.CombatReach)
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(4))
                return await Spells.SecondGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> ThirdGeneration()
        {
            if (Core.Me.ClassLevel < Spells.ThirdGeneration.LevelAcquired)
                return false;

            if (!Spells.ThirdGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.ThirdGeneration.Range + Core.Me.CombatReach)
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(3))
                return await Spells.ThirdGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> FourthGeneration()
        {
            if (Core.Me.ClassLevel < Spells.FourthGeneration.LevelAcquired)
                return false;

            if (!Spells.FourthGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.FourthGeneration.Range + Core.Me.CombatReach)
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(2))
                return await Spells.FourthGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> Slither()
        {
            if (Core.Me.ClassLevel < Spells.Slither.LevelAcquired)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (!ViperSettings.Instance.UseSlither)
                return false;

            return await Spells.Slither.Cast(Core.Me.CurrentTarget);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            if (!Core.Me.HasTarget)
                return false;

            return PhysicalDps.ForceLimitBreak(Spells.Braver, Spells.Bladedance, Spells.TheEnd, Spells.Slice);
        }

    }
}

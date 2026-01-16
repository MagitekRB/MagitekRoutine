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
using ViperRoutine = Magitek.Utilities.Routines.Viper;

namespace Magitek.Logic.Viper
{
    internal static class SingleTarget
    {

        public static async Task<bool> SteelOrReavingFangs()
        {
            if (!Spells.SteelFangs.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.HunterVenom, true) || Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (Core.Me.HasAura(Auras.PoisedforTwinfang, true) || Core.Me.HasAura(Auras.PoisedforTwinblood, true))
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Spells.ReavingFangs.IsKnown() && Core.Me.HasAura(Auras.HonedReavers, true))
                return await Spells.ReavingFangs.Cast(Core.Me.CurrentTarget);

            return await Spells.SteelFangs.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HunterOrSwiftSkinSting()
        {
            if (!Spells.HunterSting.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (!Spells.SwiftskinSting.IsKnown())
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

            if (!Spells.HindsbaneFang.IsKnown() || !Spells.FankstingStrike.IsKnown())
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
            if (!Spells.Vicewinder.IsKnown())
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
            if (!Spells.SwiftskinCoil.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reawakened, true))
                return false;

            if (Spells.HunterCoil.IsKnown() && Spells.HunterCoil.CanCast())
                return await Spells.HunterCoil.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (!Spells.SwiftskinCoil.CanCast())
                return false;

            return await Spells.SwiftskinCoil.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> UncoiledFury()
        {
            if (!Spells.UncoiledFury.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseUncoiledFury)
                return false;

            if (Core.Me.HasAura(Auras.HunterVenom, true) || Core.Me.HasAura(Auras.SwiftskinVenom, true))
                return false;

            if (ViperRoutine.EnemiesAroundPlayer5Yards == 0)
                return await Spells.UncoiledFury.Cast(Core.Me.CurrentTarget);

            if (!Spells.UncoiledFury.CanCast() || ActionResourceManager.Viper.RattlingCoils <= ViperSettings.Instance.UncoiledFurySaveChrages)
                return false;


            if (Spells.Vicewinder.IsKnownAndReadyAndCastable() || Spells.Reawaken.IsKnownAndReadyAndCastable() || Spells.HunterCoil.IsKnownAndReadyAndCastable() || Spells.SwiftskinCoil.IsKnownAndReadyAndCastable())
                return false;

            return await Spells.UncoiledFury.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> Reawaken()
        {
            if (!Spells.Reawaken.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseReawaken || ViperSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Spells.Reawaken.CanCast())
                return false;

            if (!Core.Me.HasAura(Auras.Swiftscaled, true) || !Core.Me.HasAura(Auras.HunterInstinct, true))
                return false;

            if (Spells.HunterCoil.IsKnownAndReadyAndCastable() || Spells.SwiftskinCoil.IsKnownAndReadyAndCastable())
                return false;

            if (Spells.SerpentIre.IsKnownAndReady(30000))
                return false;

            if (!CanReawaken(Core.Me.CurrentTarget))
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
            if (!Spells.Ouroboros.IsKnown())
                return false;

            if (!Spells.Ouroboros.CanCast())
                return false;

            if (ActionResourceManager.Viper.AnguineTribute.Equals(1))
                return await Spells.Ouroboros.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> FirstGeneration()
        {
            if (!Spells.FirstGeneration.IsKnown())
                return false;

            if (!Spells.FirstGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (!Spells.FirstGeneration.CanCast())
                return false;

            // HARDCODED: Level 96 unlocks the Anguine Tribute trait that increases stack thresholds.
            var hasAnguineTributeTrait = Core.Me.ClassLevel >= 96;
            if (hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(5))
                return await Spells.FirstGeneration.Cast(Core.Me.CurrentTarget);

            if (!hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(4))
                return await Spells.FirstGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> SecondGeneration()
        {
            if (!Spells.SecondGeneration.IsKnown())
                return false;

            if (!Spells.SecondGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (!Spells.SecondGeneration.CanCast())
                return false;

            // HARDCODED: Level 96 unlocks the Anguine Tribute trait that increases stack thresholds.
            var hasAnguineTributeTrait = Core.Me.ClassLevel >= 96;
            if (hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(4))
                return await Spells.SecondGeneration.Cast(Core.Me.CurrentTarget);

            if (!hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(3))
                return await Spells.SecondGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> ThirdGeneration()
        {
            if (!Spells.ThirdGeneration.IsKnown())
                return false;

            if (!Spells.ThirdGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (!Spells.ThirdGeneration.CanCast())
                return false;

            // HARDCODED: Level 96 unlocks the Anguine Tribute trait that increases stack thresholds.
            var hasAnguineTributeTrait = Core.Me.ClassLevel >= 96;
            if (hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(3))
                return await Spells.ThirdGeneration.Cast(Core.Me.CurrentTarget);

            if (!hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(2))
                return await Spells.ThirdGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> FourthGeneration()
        {
            if (!Spells.FourthGeneration.IsKnown())
                return false;

            if (!Spells.FourthGeneration.IsKnownAndReadyAndCastable())
                return false;

            if (!Spells.FourthGeneration.CanCast())
                return false;

            // HARDCODED: Level 96 unlocks the Anguine Tribute trait that increases stack thresholds.
            var hasAnguineTributeTrait = Core.Me.ClassLevel >= 96;
            if (hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(2))
                return await Spells.FourthGeneration.Cast(Core.Me.CurrentTarget);

            if (!hasAnguineTributeTrait && ActionResourceManager.Viper.AnguineTribute.Equals(1))
                return await Spells.FourthGeneration.Cast(Core.Me.CurrentTarget);

            return false;

        }

        public static async Task<bool> Slither()
        {
            if (!Spells.Slither.IsKnown())
                return false;

            if (!ViperSettings.Instance.UseSlither)
                return false;

            if (!Movement.CanUseGapCloser())
                return false;

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.Slither.Range))
                return false;

            // Only use Slither when out of melee range (opposite of dash logic)
            if (Core.Me.CurrentTarget.WithinSpellRange(Spells.SteelFangs.Range))
                return false;

            if (MovementManager.IsMoving)
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

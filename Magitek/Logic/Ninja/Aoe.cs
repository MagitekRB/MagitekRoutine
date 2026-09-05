using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Ninja;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;

namespace Magitek.Logic.Ninja
{
    internal static class Aoe
    {
        // Death Blossom and Hakke Mujinsatsu only out-damage the single-target combo from four targets
        // (100 and 120 per target against a 400 average), or from three once Doton adds Hollow Nozuchi.
        // The setting is the user's count; Doton lowers it by one, never below three.
        private static int AoeComboEnemies =>
            Core.Me.HasAura(Auras.Doton) ? Math.Max(3, NinjaSettings.Instance.AoeEnemies - 1) : NinjaSettings.Instance.AoeEnemies;

        public static async Task<bool> DeathBlossom()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!Spells.DeathBlossom.IsKnown())
                return false;

            if (!Spells.DeathBlossom.CanCast(Core.Me))
                return false;

            if (NinjaRoutine.AoeEnemies5Yards < AoeComboEnemies)
                return false;

            return await Spells.DeathBlossom.Cast(Core.Me);
        }

        public static async Task<bool> HakkeMujinsatsu()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!Spells.HakkeMujinsatsu.IsKnown())
                return false;

            if (ActionManager.LastSpell != Spells.DeathBlossom)
                return false;

            if (!Spells.HakkeMujinsatsu.CanCast(Core.Me))
                return false;

            if (NinjaRoutine.AoeEnemies5Yards < AoeComboEnemies)
                return false;

            return await Spells.HakkeMujinsatsu.Cast(Core.Me);
        }

        public static async Task<bool> HellfrogMedium()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!NinjaSettings.Instance.UseHellfrogMedium)
                return false;

            if (!Spells.HellfrogMedium.IsKnown())
                return false;

            if (NinjaRoutine.AoeEnemies6Yards < NinjaRoutine.NinkiAoeEnemies)
                return false;

            if (ActionResourceManager.Ninja.NinkiGauge < 50)
                return false;

            // Kunai's Bane's bonus is single-target, so at three or more targets the gauge is spent as it
            // comes; at two it pools for the window the way Bhavacakra does.
            if (NinjaRoutine.AoeEnemies6Yards < 3 && ActionResourceManager.Ninja.NinkiGauge < 90
                && Spells.TrickAttack.Cooldown < new TimeSpan(0, 0, 45))
                return false;

            //Smart Target Logic needs to be addded
            return await Spells.HellfrogMedium.Cast(Core.Me.CurrentTarget);
        }

    }
}

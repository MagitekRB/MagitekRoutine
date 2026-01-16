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

        public static async Task<bool> DeathBlossom()
        {
            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!Spells.DeathBlossom.IsKnown())
                return false;

            if (!Spells.DeathBlossom.CanCast(Core.Me))
                return false;

            if (NinjaRoutine.AoeEnemies5Yards < 3)
                return false;

            return await Spells.DeathBlossom.Cast(Core.Me);
        }

        public static async Task<bool> HakkeMujinsatsu()
        {
            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!Spells.HakkeMujinsatsu.IsKnown())
                return false;

            if (ActionManager.LastSpell != Spells.DeathBlossom)
                return false;

            if (!Spells.HakkeMujinsatsu.CanCast(Core.Me))
                return false;

            if (NinjaRoutine.AoeEnemies5Yards < 3)
                return false;

            return await Spells.HakkeMujinsatsu.Cast(Core.Me);
        }

        public static async Task<bool> HellfrogMedium()
        {
            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!NinjaSettings.Instance.UseHellfrogMedium)
                return false;

            if (!Spells.HellfrogMedium.IsKnown())
                return false;

            //dumping ninki before mug is missing - Changed to ActionResourceManager from MagitekActionResourceManager
            if (ActionResourceManager.Ninja.NinkiGauge < 90)
                return false;

            if (Core.Me.HasAura(Auras.Higi))
            {
                if (NinjaRoutine.AoeEnemies6Yards < 2)
                    return false;
            }
            else if (NinjaRoutine.AoeEnemies6Yards < 3)
                return false;

            //Smart Target Logic needs to be addded
            return await Spells.HellfrogMedium.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PhantomKamaitachi()
        {
            if (!NinjaSettings.Instance.UseAoe)
                return false;

            if (!NinjaSettings.Instance.UsePhantomKamaitachi)
                return false;

            if (!Spells.PhantomKamaitachi.IsKnown())
                return false;

            if (!Core.Me.HasMyAura(Auras.PhantomKamaitachiReady) && Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell != Spells.Bunshin)
                return false;

            return await Spells.PhantomKamaitachi.Cast(Core.Me.CurrentTarget);

        }

    }
}

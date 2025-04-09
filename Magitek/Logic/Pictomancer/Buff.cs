using Buddy.Coroutines;
using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Pictomancer;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Pictomancer
{
    internal static class Buff
    {
        public static async Task<bool> Swiftcast()
        {
            if (await Spells.Swiftcast.CastAura(Core.Me, Auras.Swiftcast))
            {
                return await Coroutine.Wait(15000, () => Core.Me.HasAura(Auras.Swiftcast, true, 7000));
            }

            return false;
        }

        public static async Task<bool> SubtractivePalette()
        {
            if (!PictomancerSettings.Instance.UseSubtractivePalette)
                return false;

            if (!Spells.SubtractivePalette.CanCast() || !Spells.SubtractivePalette.IsKnownAndReady())
                return false;

            if (Utilities.Routines.Pictomancer.HasBlackPaint())
                return false;

            if (Core.Me.HasAura(Auras.SubtractivePalette))
                return false;

            if (!Spells.SubtractivePalette.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.MonochromeTones))
                return false;

            return await Spells.SubtractivePalette.CastAura(Core.Me, Auras.SubtractivePalette);
        }

        public static async Task<bool> FightLogic_TemperaCoat()
        {
            // intentionally don't wait for the grassa aura, it either happens as a automatic follow-on or it doesn't. 
            // tempera grassa has a 1.5 second cooldown before it can be cast, so we can't cast it immediately after coat.
            return await CommonFightLogic.FightLogic_PartyShield(
                PictomancerSettings.Instance.FightLogicTemperaCoat,
                Spells.TemperaCoat,
                true,
                new uint[] { Auras.TemperaCoat, Auras.TemperaGrassa }
                );
        }

        public static async Task<bool> FightLogic_TemperaGrassa()
        {
            if (!PictomancerSettings.Instance.FightLogicTemperaCoat)
                return false;

            if (!Spells.TemperaGrassa.IsKnownAndReady())
                return false;

            if (!Core.Me.HasAura(Auras.TemperaCoat))
                return false;

            if (!Globals.InParty)
                return false;

            // intentionally don't wait for the grassa aura, it either happens or it doesn't. 
            return await Spells.TemperaGrassa.Cast(Core.Me);
        }

        public static async Task<bool> UsePotion()
        {
            if (!Core.Me.HasAura(Auras.StarryMuse, true))
                return false;

            if (Core.Me.ClassLevel < 100)
            {
                return false;
            }

            return await MagicDps.UsePotion(PictomancerSettings.Instance);
        }
    }
}

using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.DarkKnight;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.DarkKnight
{
    internal static class Buff
    {
        public static async Task<bool> Grit()
        {
            if (!DarkKnightSettings.Instance.Grit)
            {
                if (Core.Me.HasAura(Auras.Grit))
                {
                    return await Spells.Grit.Cast(Core.Me);
                }

                return false;
            }

            if (Core.Me.HasAura(Auras.Grit))
                return false;

            return await Spells.Grit.Cast(Core.Me);
        }

        public static async Task<bool> LivingShadow()
        {
            if (!DarkKnightSettings.Instance.LivingShadow)
                return false;

            return await Spells.LivingShadow.Cast(Core.Me);
        }

        public static async Task<bool> BloodWeapon()
        {
            if (!DarkKnightSettings.Instance.BloodWeapon)
                return false;

            return await Spells.BloodWeapon.Cast(Core.Me);
        }

        public static async Task<bool> Delirium()
        {
            if (!DarkKnightSettings.Instance.Delirium)
                return false;

            if (Spells.HardSlash.Cooldown.TotalMilliseconds > 800)
                return false;

            // Only use Delirium when in melee range to avoid wasting it on Unmend spam
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.WithinSpellRange(Spells.ScarletDelirium.Range))
                return false;

            return await Spells.Delirium.Cast(Core.Me);
        }

        public static async Task<bool> ShadowedVigil()
        {
            if (!DarkKnightSettings.Instance.ShadowedVigil)
                return false;

            return await Spells.ShadowedVigil.Cast(Core.Me);
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.LivingShadow.IsKnown() && !Spells.LivingShadow.IsReady(10000))
                return false;

            return await Tank.UsePotion(DarkKnightSettings.Instance);
        }
    }
}
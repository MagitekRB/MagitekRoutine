using ff14bot;
using Magitek.Extensions;
using Magitek.Utilities;
using Magitek.Utilities.GamelogManager;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Ninja
{
    internal static class Utility
    {

        public static async Task<bool> PrePullHide()
        {

            if (!GamelogManagerCountdown.IsCountdownRunning())
                return false;

            if (GamelogManagerCountdown.GetCurrentCooldown() <= 6)
                return false;

            if ((uint)Spells.Ten.Charges == Spells.Ten.MaxCharges)
                return false;

            return await Spells.Hide.Cast(Core.Me);

        }

    }
}

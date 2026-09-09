using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Threading.Tasks;
using BeastMasterRoutine = Magitek.Utilities.Routines.BeastMaster;

namespace Magitek.Logic.BeastMaster
{
    /// <summary>
    /// Filling the Master's Bestiary. Which beasts can be captured and which are already befriended both come from
    /// the client (see BeastMasterRoutine.CaptureWanted), so Capture goes out on a wanted beast once it is under the
    /// health threshold (the odds rise as health falls, the mark lasts two minutes). Capture cannot kill; the attacks
    /// that follow do, and the pact is forged on the kill.
    /// </summary>
    internal static class Capture
    {
        public static async Task<bool> Mark()
        {
            var target = Core.Me.CurrentTarget as BattleCharacter;
            if (!BeastMasterRoutine.CaptureWanted(target))
                return false;

            // The pact's odds rise as the target's health falls; the setting is where that trades against the
            // beast dying before the mark is on it.
            if (target.CurrentHealthPercent > BeastMasterSettings.Instance.CaptureHealthPercent)
                return false;

            return await Spells.Capture.Cast(target);
        }
    }
}

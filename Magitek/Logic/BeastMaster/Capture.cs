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
        // A beast the tracker expects dead within this many seconds is marked now, whatever its health.
        private const int CaptureNowSeconds = 8;

        public static async Task<bool> Mark()
        {
            var target = Core.Me.CurrentTarget as BattleCharacter;
            if (!BeastMasterRoutine.CaptureWanted(target))
                return false;

            // The pact's odds rise as the target's health falls; the setting is where that trades against the
            // beast dying before the mark is on it. Two cases skip the wait: a beast well below your level dies
            // first and the level gap already lifts the odds, and a beast the tracker expects dead within moments
            // (a party is on it) gets the mark now, since worse odds beat no pact at all.
            var settings = BeastMasterSettings.Instance;
            var farBelow = Core.Me.ClassLevel - target.ClassLevel >= settings.CaptureAtOnceLevelGap;
            var timeLeft = target.CombatTimeLeft();
            var dyingSoon = timeLeft > 0 && timeLeft <= CaptureNowSeconds;
            if (!farBelow && !dyingSoon && target.CurrentHealthPercent > settings.CaptureHealthPercent)
                return false;

            return await Spells.Capture.Cast(target);
        }
    }
}

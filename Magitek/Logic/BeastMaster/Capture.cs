using ff14bot;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using BeastMasterRoutine = Magitek.Utilities.Routines.BeastMaster;

namespace Magitek.Logic.BeastMaster
{
    /// <summary>
    /// Filling the Master's Bestiary. Gauge asks the game about each new kind of beast (an ability, one weave), the
    /// chat answer is remembered, and Capture goes out on a beast that can be captured, is not yet befriended, is not
    /// above our level and is under the health threshold (the odds rise as health falls, the mark lasts two minutes).
    /// Capture cannot kill; the attacks that follow do, and the pact is forged on the kill.
    /// </summary>
    internal static class Capture
    {
        public static async Task<bool> Gauge()
        {
            if (!BeastMasterSettings.Instance.UseCapture || !Spells.Gauge.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget as ff14bot.Objects.BattleCharacter;
            if (target == null || !target.IsNpc)
                return false;

            if (BeastMasterBestiary.Verdict(target.NpcId) != null || BeastMasterBestiary.AwaitingAnswer(target.NpcId))
                return false;

            if (!await Spells.Gauge.Cast(target))
                return false;

            BeastMasterBestiary.Expect(target.NpcId, target.EnglishName);
            return true;
        }

        public static async Task<bool> Mark()
        {
            if (!BeastMasterSettings.Instance.UseCapture || !Spells.Capture.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget as ff14bot.Objects.BattleCharacter;
            if (target == null || !target.IsNpc || target.HasAura(Auras.InterestCaptured))
                return false;

            if (!BeastMasterRoutine.CaptureWanted(target))
            {
                // Without Gauge, Capture is its own question: the same answers come back. Anything else the game
                // has answered, or that is above our level, is not for capturing.
                if (Spells.Gauge.IsKnown() || BeastMasterBestiary.Verdict(target.NpcId) != null || target.ClassLevel > Core.Me.ClassLevel)
                    return false;
            }

            // The pact's odds rise as the target's health falls; the setting is where that trades against the
            // beast dying before the mark is on it.
            if (target.CurrentHealthPercent > BeastMasterSettings.Instance.CaptureHealthPercent)
                return false;

            if (!await Spells.Capture.Cast(target))
                return false;

            BeastMasterBestiary.Expect(target.NpcId, target.EnglishName);
            BeastMasterBestiary.Marked(target.NpcId, target.EnglishName);
            return true;
        }
    }
}

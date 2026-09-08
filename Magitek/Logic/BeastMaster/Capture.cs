using ff14bot;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.BeastMaster
{
    /// <summary>
    /// Filling the Master's Bestiary. Gauge asks the game about each new kind of beast (an ability, one weave), the
    /// chat answer is remembered, and Capture goes out on a beast that can be captured, is not yet befriended, is not
    /// above our level. The mark lasts two minutes and things die fast, so it goes out as soon as the answer allows;
    /// Capture cannot kill, the attacks that follow do, and the pact is forged on the kill.
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

            var verdict = BeastMasterBestiary.Verdict(target.NpcId);
            if (verdict == null)
            {
                // Without Gauge, Capture is its own question: the same answers come back.
                if (Spells.Gauge.IsKnown())
                    return false;
                verdict = BeastMasterBestiary.LowestOdds;
            }

            if (verdict < BeastMasterBestiary.LowestOdds)
                return false;

            // Setting 1..5 against Gauge's five odds.
            if (verdict - BeastMasterBestiary.LowestOdds + 1 < BeastMasterSettings.Instance.CaptureMinimumOdds)
                return false;

            // Capture is ineffective on targets above our level.
            if (target.ClassLevel > Core.Me.ClassLevel)
                return false;

            if (!await Spells.Capture.Cast(target))
                return false;

            BeastMasterBestiary.Expect(target.NpcId, target.EnglishName);
            BeastMasterBestiary.Marked(target.NpcId, target.EnglishName);
            return true;
        }
    }
}

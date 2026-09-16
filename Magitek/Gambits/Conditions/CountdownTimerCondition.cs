using ff14bot;
using ff14bot.Objects;
using Magitek.Utilities.GamelogManager;

namespace Magitek.Gambits.Conditions
{
    public class CountdownTimerCondition : GambitCondition
    {
        public CountdownTimerCondition() : base(GambitConditionTypes.CountdownTimer)
        {
        }

        public int CountdownTimerInSeconds { get; set; }

        public override bool Check(GameObject gameObject = null)
        {
            //Logger.WriteInfo($@"[Opener] Current Countdown = {GamelogManagerCountdown.GetCurrentCooldown()} | Step Timer Config = {CountdownTimerInSeconds}");
            // Zero is the pull: once combat is on we are past it, countdown or not.
            if (CountdownTimerInSeconds == 0 && Core.Me.InCombat)
                return true;

            if (GamelogManagerCountdown.GetCurrentCooldown() != CountdownTimerInSeconds)
                return false;

            return true;
        }
    }
}

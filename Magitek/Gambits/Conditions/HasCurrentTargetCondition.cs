using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;

namespace Magitek.Gambits.Conditions
{
    public class HasCurrentTargetCondition : GambitCondition
    {
        public HasCurrentTargetCondition() : base(GambitConditionTypes.HasCurrentTarget)
        {
        }

        public bool MustBeEnemy { get; set; }
        public bool MustBeAlly { get; set; }

        public override bool Check(GameObject gameObject = null)
        {
            // Check if we have a target at all
            if (Core.Me.CurrentTarget == null)
                return false;

            // If no specific type is required, just return true since we have a target
            if (!MustBeEnemy && !MustBeAlly)
                return true;

            // If we need an enemy target
            if (MustBeEnemy && Core.Me.CurrentTarget.Type == GameObjectType.BattleNpc)
                return true;

            // If we need an ally target
            if (MustBeAlly && Core.Me.CurrentTarget.Type == GameObjectType.Pc)
                return true;

            return false;
        }
    }
}
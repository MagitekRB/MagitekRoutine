﻿using ff14bot.Managers;
using ff14bot.Objects;

namespace Magitek.Gambits.Conditions
{
    public class NinjaHutonCondition : GambitCondition
    {
        public NinjaHutonCondition() : base(GambitConditionTypes.NinjaHuton)
        {
        }

        public int HutonMinimumTimeLeft { get; set; }
        public int HutonMaximumTimeLeft { get; set; }

        public override bool Check(GameObject gameObject = null)
        {
            return true;
        }
    }
}

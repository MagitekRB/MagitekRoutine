using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Utilities;
using System;
using System.Linq;

namespace Magitek.Gambits.Conditions
{
    public class SpellOffCooldownCondition : GambitCondition
    {
        public SpellOffCooldownCondition() : base(GambitConditionTypes.SpellOffCooldown) { }

        public string SpellName { get; set; }
        public bool IsPvpSpell { get; set; }

        public override bool Check(GameObject gameObject = null)
        {
            var spell = ActionManager.CurrentActions.Values.FirstOrDefault(SpellDataCheck);

            if (spell == null)
            {
                Logger.Write($@"[Magitek] {SpellName} is not found, not starting opener.");
                return false;
            }

            return spell.Cooldown <= TimeSpan.Zero;
        }

        private bool SpellDataCheck(SpellData spell)
        {
            // If looking for PvP spell, only match PvP spells
            if (IsPvpSpell && !spell.IsPvP)
                return false;

            // If NOT looking for PvP spell, exclude PvP spells to avoid matching wrong spell type
            if (!IsPvpSpell && spell.IsPvP)
                return false;

            return string.Equals(spell.Name, SpellName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

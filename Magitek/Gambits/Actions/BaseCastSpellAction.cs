using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Magitek.Gambits.Actions
{
    public abstract class BaseCastSpellAction : GambitAction
    {
        protected BaseCastSpellAction(GambitActionTypes type) : base(type) { }

        public string SpellName { get; set; }
        public bool IsPvpSpell { get; set; }

        [JsonIgnore]
        public SpellData SpellData
        {
            get
            {
                if (SpellName == null || string.IsNullOrEmpty(SpellName) || string.IsNullOrWhiteSpace(SpellName))
                    return null;

                return ActionManager.CurrentActions.Values.FirstOrDefault(SpellDataCheck).Masked();
            }
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
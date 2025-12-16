using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using System;
using System.Linq;
using static ff14bot.Managers.ActionResourceManager.RedMage;

namespace Magitek.Utilities.Routines
{
    internal static class RedMage
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.RedMage, Spells.Jolt);

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static bool WithinManaOf(int distance, int target) => WhiteMana >= target - distance && BlackMana >= target - distance;
    }
}

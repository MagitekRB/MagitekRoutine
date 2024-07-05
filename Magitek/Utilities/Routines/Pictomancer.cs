using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Paladin;
using Magitek.Models.Pictomancer;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Pictomancer
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Pictomancer, Spells.FireinRed);

        public static bool StarryOffCooldownSoon()
        {
            if (!Spells.StarryMuse.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.StarryMuse))
                return false;

            if (Spells.StarryMuse.Cooldown == TimeSpan.Zero && !Core.Me.HasAura(Auras.StarryMuse))
                return true;

            if (Spells.StarryMuse.Cooldown > TimeSpan.Zero && 
                Spells.StarryMuse.Cooldown.TotalSeconds <= PictomancerSettings.Instance.SaveForStarryMSeconds)
                return true;

            return false;
        }

        public static bool HasBlackPaint()
        {
            return ActionResourceManager.Pictomancer.Paint >= 1 && Core.Me.HasAura(Auras.MonochromeTones);
        }

        public static bool CheckTTDIsEnemyDyingSoon()
        {
            return PictomancerSettings.Instance.UseTTD
                && Combat.CurrentTargetCombatTimeLeft < PictomancerSettings.Instance.SaveIfEnemyDyingWithin
                && !Core.Me.CurrentTarget.IsBoss();
        }
    }
}
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Pictomancer;
using System;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Pictomancer
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Pictomancer, Spells.FireinRed);

        public static void DetectSmudge()
        {
            // Places smudge in the spell cast history if detected manually
            // this prevents incorrectly triple/quad weaving with manual smudge usages.
            if (Core.Me.HasAura(Auras.Smudge, true))
            {
                if (!Casting.SpellCastHistory.Any(s => s.Spell == Spells.Smudge))
                {
                    Casting.SpellCastHistory.Insert(0, new SpellCastHistoryItem
                    {
                        Spell = Spells.Smudge,
                        SpellTarget = Core.Me,
                        TimeCastUtc = DateTime.UtcNow,
                        TimeStartedUtc = DateTime.UtcNow,
                        DelayMs = 0
                    });
                }
            }
        }

        public static bool StarryOffCooldownSoon(int msLeft)
        {
            if (!Spells.StarryMuse.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.StarryMuse, true))
                return false;

            if (Spells.StarryMuse.Cooldown == TimeSpan.Zero)
                return true;

            if (Spells.StarryMuse.Cooldown > TimeSpan.Zero &&
                Spells.StarryMuse.Cooldown.TotalMilliseconds <= msLeft)
                return true;

            return false;
        }

        public static double StarryCooldownRemaining()
        {
            if (!Spells.StarryMuse.IsKnown())
                return 0;

            if (Core.Me.HasAura(Auras.StarryMuse, true))
                return 0;

            if (Spells.StarryMuse.Cooldown == TimeSpan.Zero)
                return 0;

            if (Spells.StarryMuse.Cooldown > TimeSpan.Zero)
                return Spells.StarryMuse.Cooldown.TotalMilliseconds;

            return 0;
        }

        public static bool HasBlackPaint()
        {
            return ActionResourceManager.Pictomancer.Paint >= 1 && Core.Me.HasAura(Auras.MonochromeTones);
        }

        public static bool CheckTTDIsEnemyDyingSoon()
        {
            return Common.CheckTTDIsEnemyDyingSoon(PictomancerSettings.Instance);
        }
    }
}
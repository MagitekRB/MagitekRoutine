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

        public static bool UseSimplifiedRotation =>
            PictomancerSettings.Instance.UseSimplifiedRotation &&
            (!PictomancerSettings.Instance.UseSimplifiedRotationBelow100 || Core.Me.ClassLevel < 100);

        // How close Starry Muse's cooldown has to be before we report the burst
        // as imminent.
        private const int StarryMuseImminentMs = 5000;

        /// <summary>
        /// Reports PCT burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the PCT rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Starry Muse (20s, every 2 minutes), with Starstruck carrying
        /// the Star Prism tail. Window contents: Starry Muse, subtractive combo,
        /// Hammer Time GCDs, Comet in Black, Hyperphantasia -> Rainbow Drip, Star
        /// Prism. Note the window has a hard expiry cliff — Star Prism pushed past
        /// Starstruck is dropped entirely, and the Hyperphantasia phase is hardcasts
        /// that cannot absorb an animation lock.
        /// Sources: The Balance PCT basic guide, official job guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: own-cast Starry Muse OR Starstruck, granted by the same
            // press. Starry Muse is party-wide, so only own-cast records count;
            // Starstruck covers the tail where the buff icon has dropped but Star
            // Prism has not fired yet, hence the max remaining across whichever are
            // up. Hyperphantasia is deliberately excluded (consumed mid-window, would
            // flicker) and so is Rainbow Bright (30s, trails 10s past the buff).
            // Under level sync missing auras simply never appear — no level branching
            // needed.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.StarryMuse && aura.Id != Auras.Starstruck)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "PCT Starry Muse");
                return;
            }

            // Starry Muse almost off cooldown: the burst is about to open, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.StarryMuse.IsKnown())
            {
                var cooldownMs = Spells.StarryMuse.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= StarryMuseImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "PCT Starry Muse");
            }
        }

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
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Utilities;
using System;
using System.Linq;
using static ff14bot.Managers.ActionResourceManager.BlackMage;



namespace Magitek.Utilities.Routines
{

    internal static class BlackMage
    {
        public static int AoeEnemies5Yards;
        public static int AoeEnemies30Yards;
        public static void RefreshVars()
        {
            AoeEnemies5Yards = Combat.Enemies.Count(x => x.WithinSpellRange(5) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
            AoeEnemies30Yards = Combat.Enemies.Count(x => x.WithinSpellRange(30) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
        }
        public static bool NeedToInterruptCast()
        {
            if (Casting.SpellTarget?.CurrentHealth == 0)
            {
                {
                    Logger.Error($"Stopped {Casting.CastingSpell.LocalizedName}: because HE'S DEAD, JIM!");
                }
                return true;
            }
            return false;
        }

        public static int MaxPolyglotCount
        {
            get
            {
                if (Core.Me.ClassLevel >= 98)
                    return 3;
                if (Core.Me.ClassLevel >= 80)
                    return 2;
                if (Core.Me.ClassLevel >= 70)
                    return 1;
                return 0;
            }
        }

        public static bool WillOvercapPolyglot()
        {
            // Check if Polyglot timer will expire within the threshold
            // We only overcap if we're at max polyglots AND the timer will expire soon
            // The timer counts down to 0 and when it hits 0 we get a new polyglot
            var gcdDuration = Spells.Fire.AdjustedCooldown.TotalMilliseconds;
            var polyglotTimer = ActionResourceManager.BlackMage.PolyglotTimer;

            // Calculate how many GCDs worth of time we need to check (buffer for movement)
            var gcdsToCheck = 1.5;
            var timeThreshold = gcdDuration * gcdsToCheck;

            // We're at max polyglots and timer will expire soon - need to spend one now
            // This puts us at max-1, then timer expires giving us max again
            return PolyglotCount == MaxPolyglotCount && polyglotTimer > TimeSpan.Zero && polyglotTimer.TotalMilliseconds <= timeThreshold;
        }

        public static readonly uint Ether = 4555;
        public static readonly uint HiEther = 4556;
        public static readonly uint XEther = 4558;
        public static readonly uint MegaEther = 13638;
        public static readonly uint SuperEther = 23168;
    }
}

using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
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

        /// <summary>
        /// Reports BLM burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the BLM rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Ley Lines (own, fixed 20s, 2 charges/120s) — BLM has no
        /// raid-buff burst window; Ley Lines is its only bounded state where an
        /// interjection costs extra (hasted 2.125s GCD, ~18% more per 2s lock, on top
        /// of BLM's already-high flat cost of ~0.8 GCD per interjection anywhere) and
        /// where the player is spatially committed. Circle of Power is deliberately
        /// not used (flickers when stepping out). No imminent trigger: Ley Lines has
        /// no ramp and its press is not cooldown-predictable. Sources: The Balance
        /// BLM basic guide, official job guide, consolegameswiki, live client via rb
        /// (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: the Ley Lines self-buff (fixed 20s). Not Circle of Power —
            // that aura drops and reapplies every time the player steps out of the
            // lines to dodge, which would flicker the window. Unlocks at Lv52; below
            // sync the aura simply never appears — no level branching needed.
            var leyLines = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.LeyLines && x.CasterId == Core.Me.ObjectId);
            if (leyLines != null)
                RoutineState.ReportBurstWindow(leyLines.TimespanLeft, "BLM Ley Lines");

            // No imminent branch: Ley Lines is an instant oGCD with zero ramp, and its
            // press is not cooldown-predictable (charges are deliberately held).
        }

        public static int MaxPolyglotCount
        {
            get
            {
                // HARDCODED: These levels correspond to trait unlocks that increase max Polyglot count
                // Level 70: First Polyglot trait
                // Level 80: Second Polyglot trait  
                // Level 98: Third Polyglot trait
                // These are trait checks, not spell availability checks
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

        // The AoE rotation's entry condition, shared so single-target logic can defer to it.
        public static bool InAoeRotation => BlackMageSettings.Instance.UseAoe
            && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies;
    }
}

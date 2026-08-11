using ff14bot.Objects;
using Magitek.Logic.Roles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities
{
    /// <summary>
    /// Concepts of Operation
    ///
    /// Learns which enemies are immune to a soft-CC debuff, so a routine stops burning casts on
    /// targets that will never take it. Same idea as <see cref="StunTracker"/>, with two
    /// differences that matter:
    ///
    ///   1: It is keyed by debuff, not by a single hardcoded Stun aura, because Occult Crescent
    ///      has several (Occult Toad, Slow) and more phantom jobs are still to come.
    ///
    ///   2: The debuffs here have CAST TIMES. StunTracker can conclude "no Stun aura 1.5s after
    ///      the attempt means immune" because stuns are instant oGCDs. Occult Toad is a 1.5s
    ///      cast and Cast() returns as soon as casting STARTS, so the aura cannot possibly have
    ///      landed yet when the attempt is recorded. Every deadline here is therefore
    ///      cast time + a grace period, not a flat constant.
    ///
    /// Evidence is recorded per debuff, but queried per CATEGORY. Occult Toad and Slow appear to
    /// share one immunity set in game, so a mob that resists Toad is assumed to resist Slow and
    /// vice versa - that halves the time to learn a zone. Storing the evidence separately anyway
    /// means that if the two ever turn out to differ, splitting them is a change to
    /// <see cref="Categories"/> alone, with no loss of what was already learned.
    ///
    /// Routines use this by calling IsWorthAttempting() before casting and RecordAttempt() after.
    /// </summary>
    internal static class OccultDebuffImmunityTracker
    {
        private enum LogLevel
        {
            None = 0,
            Useful = 1,
            Debug = 2
        }
        private const LogLevel mLogLevelToShow = LogLevel.Useful;

        /// <summary>
        /// How long after a cast is expected to finish we keep waiting for the aura to show up.
        /// Covers server round trip and the gap between "casting started" and "effect applied".
        /// </summary>
        private const double AuraAppearanceGraceMs = 1500;

        #region Known debuffs

        /// <summary>
        /// Aura id -> the name used as the key in the persisted and shipped JSON. Keeping the
        /// files keyed by name rather than by a bare number is what makes them hand-editable.
        /// Ids live in OCAuras; this only names them.
        /// </summary>
        private static readonly Dictionary<uint, string> DebuffNames = new Dictionary<uint, string>
        {
            { OCAuras.OccultToad, "OccultToad" },
            // Keyed by the spell that applies it, not by the generic Slow aura it happens to
            // use - in this routine Slow only ever comes from Occult Slowga, and the file is
            // maintained by hand.
            { OCAuras.Slow, "OccultSlowga" }
        };

        /// <summary>
        /// Debuffs believed to share a single immunity set in game. A lookup for any member
        /// consults the evidence gathered for every member. Occult Toad and Slow are grouped on
        /// the assumption that the game uses one resistance flag for both - if a mob ever turns
        /// up that takes one but not the other, split this array and nothing already learned is
        /// lost, because the evidence itself is stored per debuff.
        /// </summary>
        private static readonly uint[][] Categories =
        {
            new uint[] { OCAuras.OccultToad, OCAuras.Slow }
        };

        #endregion

        private static bool mDataModified = false;
        private static OccultDebuffImmunityPersistence.OccultDebuffImmunityData mData;

        /// <summary>Casts we have made and are still waiting on a verdict for.</summary>
        private static readonly List<PendingAttempt> mPendingAttempts = new List<PendingAttempt>();

        static OccultDebuffImmunityTracker()
        {
            mData = OccultDebuffImmunityPersistence.LoadData();
            Log(LogLevel.Useful, $"Loaded immunity data for {mData.Immune.Count} debuff(s): " +
                                 string.Join(", ", mData.Immune.Select(e => $"{e.Key}={e.Value.Count}")));
        }

        public static void Save()
        {
            if (!mDataModified)
                return;

            OccultDebuffImmunityPersistence.SaveData(mData);
            mDataModified = false;
        }

        /// <summary>
        /// Should we bother casting this debuff on this enemy? Optimistic for enemies we know
        /// nothing about - an unknown enemy is worth one attempt, which is how anything is ever
        /// learned. Returns false once the enemy is known immune, or already has the debuff.
        /// </summary>
        public static bool IsWorthAttempting(BattleCharacter enemy, uint auraId)
        {
            if (enemy == null || !enemy.IsValid)
                return false;

            // Already debuffed - nothing to gain, and recasting would poison the evidence.
            if (enemy.HasAura(auraId))
                return false;

            // An attempt is already in flight; wait for its verdict rather than double casting.
            if (mPendingAttempts.Any(a => a.Enemy == enemy && a.AuraId == auraId))
                return false;

            return !IsKnownImmune(enemy.NpcId, auraId);
        }

        /// <summary>
        /// Report that we just cast <paramref name="auraId"/> at <paramref name="enemy"/>.
        /// <paramref name="castTime"/> is the spell's AdjustedCastTime - the verdict deadline is
        /// measured from when the cast should finish, not from now.
        /// </summary>
        public static void RecordAttempt(BattleCharacter enemy, uint auraId, TimeSpan castTime)
        {
            if (enemy == null || !enemy.IsValid)
                return;

            if (mPendingAttempts.Any(a => a.Enemy == enemy && a.AuraId == auraId))
                return;

            var deadline = DateTime.UtcNow
                .Add(castTime)
                .AddMilliseconds(AuraAppearanceGraceMs);

            mPendingAttempts.Add(new PendingAttempt(enemy, auraId, deadline));
            Log(LogLevel.Debug, $"Recording attempted {NameOf(auraId)}: {enemy.EnglishName}");
        }

        /// <summary>
        /// Must be called often so we do not miss short debuffs. Driven from Tracking.cs
        /// alongside StunTracker.Update().
        ///
        /// Takes no enemy list, unlike StunTracker: that one scans every enemy because it
        /// passively discovers stuns landed by anyone, whereas this only ever adjudicates casts
        /// it recorded itself, and each pending attempt holds its own target reference.
        /// </summary>
        public static void Update()
        {
            if (mPendingAttempts.Count == 0)
                return;

            for (int i = mPendingAttempts.Count - 1; i >= 0; i--)
            {
                var attempt = mPendingAttempts[i];

                // The enemy died or despawned before the debuff could land. That is not evidence
                // of immunity, so throw the attempt away rather than counting it as a failure.
                if (attempt.Enemy == null || !attempt.Enemy.IsValid || !attempt.Enemy.IsAlive)
                {
                    Log(LogLevel.Debug, "Discarded attempt on a no-longer-valid enemy");
                    mPendingAttempts.RemoveAt(i);
                    continue;
                }

                // The debuff landed.
                if (attempt.Enemy.HasAura(attempt.AuraId))
                {
                    MarkSusceptible(attempt.Enemy, attempt.AuraId);
                    mPendingAttempts.RemoveAt(i);
                    continue;
                }

                // Still within the window the aura could reasonably appear in.
                if (DateTime.UtcNow < attempt.Deadline)
                    continue;

                mPendingAttempts.RemoveAt(i);
                RecordFailure(attempt.Enemy, attempt.AuraId);
            }
        }

        private static void RecordFailure(BattleCharacter enemy, uint auraId)
        {
            // An enemy we have already seen take this debuff is not immune; that cast was
            // interrupted, resisted or otherwise lost. Do not hold it against them.
            if (IsRecordedSusceptible(enemy.NpcId, auraId))
            {
                Log(LogLevel.Debug, $"{enemy.EnglishName} failed to take {NameOf(auraId)} but is known susceptible - ignoring");
                return;
            }

            // One miss is enough, the same call StunTracker makes. Enemies that died or
            // despawned mid-cast never reach here, so what is left is a genuine refusal.
            MarkImmune(enemy, auraId);
        }

        private static void MarkImmune(BattleCharacter enemy, uint auraId)
        {
            var bucket = BucketFor(mData.Immune, NameOf(auraId));

            if (bucket.ContainsKey(enemy.NpcId))
                return;

            bucket[enemy.NpcId] = enemy.EnglishName;
            mDataModified = true;

            Log(LogLevel.Useful, $"{enemy.EnglishName.ToUpper()} IS IMMUNE TO {NameOf(auraId).ToUpper()}");
        }

        private static void MarkSusceptible(BattleCharacter enemy, uint auraId)
        {
            var name = NameOf(auraId);

            // A landed debuff overrules anything we previously concluded, including a bad
            // observation that had written this enemy off.
            if (mData.Immune.TryGetValue(name, out var immune) && immune.Remove(enemy.NpcId))
            {
                Log(LogLevel.Useful, $"{enemy.EnglishName.ToUpper()} TOOK {name.ToUpper()} AFTER ALL - clearing immunity");
                mDataModified = true;
            }

            var bucket = BucketFor(mData.Susceptible, name);
            if (bucket.ContainsKey(enemy.NpcId))
                return;

            bucket[enemy.NpcId] = enemy.EnglishName;
            mDataModified = true;
            Log(LogLevel.Debug, $"{enemy.EnglishName} takes {name}");
        }

        /// <summary>
        /// Immune to this debuff, or to anything sharing its category. Evidence for one member of
        /// a category counts for all of them.
        /// </summary>
        private static bool IsKnownImmune(uint npcId, uint auraId)
        {
            // Having actually seen this debuff land on this enemy beats an immunity assumption
            // inherited from a sibling debuff in the same category.
            if (IsRecordedSusceptible(npcId, auraId))
                return false;

            return CategoryOf(auraId).Any(related => Lookup(mData.Immune, NameOf(related)).ContainsKey(npcId));
        }

        private static bool IsRecordedSusceptible(uint npcId, uint auraId)
        {
            return Lookup(mData.Susceptible, NameOf(auraId)).ContainsKey(npcId);
        }

        private static IEnumerable<uint> CategoryOf(uint auraId)
        {
            var category = Categories.FirstOrDefault(c => c.Contains(auraId));
            return category ?? new[] { auraId };
        }

        private static string NameOf(uint auraId)
        {
            return DebuffNames.TryGetValue(auraId, out var name) ? name : auraId.ToString();
        }

        /// <summary>Read-only view of a bucket. Never adds an empty one just to answer a query.</summary>
        private static Dictionary<uint, string> Lookup(Dictionary<string, Dictionary<uint, string>> store, string debuffName)
        {
            return store.TryGetValue(debuffName, out var bucket) ? bucket : EmptyBucket;
        }

        /// <summary>Bucket to write into, created on demand.</summary>
        private static Dictionary<uint, string> BucketFor(Dictionary<string, Dictionary<uint, string>> store, string debuffName)
        {
            if (!store.TryGetValue(debuffName, out var bucket))
            {
                bucket = new Dictionary<uint, string>();
                store[debuffName] = bucket;
            }

            return bucket;
        }

        private static readonly Dictionary<uint, string> EmptyBucket = new Dictionary<uint, string>();

        private static void Log(LogLevel logLevel, string strToLog)
        {
            if (logLevel <= mLogLevelToShow)
            {
                Logger.WriteInfo($"[OccultDebuffImmunity] {strToLog}");
            }
        }

        private class PendingAttempt
        {
            public BattleCharacter Enemy { get; }
            public uint AuraId { get; }
            public DateTime Deadline { get; }

            public PendingAttempt(BattleCharacter enemy, uint auraId, DateTime deadline)
            {
                Enemy = enemy;
                AuraId = auraId;
                Deadline = deadline;
            }
        }
    }
}

using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Debug = Magitek.ViewModels.Debug;
using DebugSettings = Magitek.Models.Account.BaseSettings;

namespace Magitek.Utilities
{
    public static class FightLogic
    {
        private static readonly Stopwatch FlStopwatch = new Stopwatch();

        private static readonly Stopwatch GetEnemyLogicAndEnemyCacheAge = new Stopwatch();

        private static HashSet<uint> FlHandledCastingSpellId = new HashSet<uint>();

        // Global list of common AoeLockOn IDs that are used across multiple dungeons
        // These are visual indicators that seem to always represent AoE attacks regardless of dungeon
        private static readonly HashSet<uint> CommonAoeLockOns = new HashSet<uint>
        {
            100,  // Common AoE lockon
            62,   // Common AoE lockon
            79,   // Common AoE lockon
            96,   // Common AoE lockon
            101,  // Common AoE lockon
            139,  // Very common AoE lockon (used in many dungeons)
            161,  // Common AoE lockon
            311,  // Common AoE lockon
            315,  // Common AoE lockon
            316,  // Common AoE lockon
            376,  // Common AoE lockon
            466,  // Common AoE lockon
            542,  // Common AoE lockon
            543,  // Common AoE lockon
            558,  // Common AoE lockon
        };

        /// <summary>
        /// Returns a comma-separated string of common AoeLockOn IDs for display in UI
        /// </summary>
        public static string CommonAoeLockOnsDisplay => string.Join(", ", CommonAoeLockOns.OrderBy(x => x));

        /// <summary>
        /// The ally (or ourselves when solo) carrying a heal-to-full Doom, or null. This Doom
        /// recurs across content - dungeon bosses apply it as well as the Occult Crescent
        /// Necromancer's self-Doom - and it kills at expiry unless the target reaches full HP
        /// first, so healers treat the carrier as the top-priority heal target. Deliberately
        /// not zone-gated, unlike the enemy-cast catalogues.
        /// </summary>
        public static Character DoomedHealTarget()
        {
            // The below-full check matters: full HP is what removes the aura, so a carrier
            // sitting at full is already cleansing (removal latency) and healing them again
            // only wastes the GCD.
            if (Globals.InParty)
                return Group.CastableAlliesWithin30.FirstOrDefault(r => r.HasAura(Auras.Doom) && r.CurrentHealth < r.MaxHealth);

            return Core.Me.HasAura(Auras.Doom) && Core.Me.CurrentHealth < Core.Me.MaxHealth ? Core.Me : null;
        }



        private static TimeSpan FlCooldown
        {
            get
            {
                if (!FlStopwatch.IsRunning) return TimeSpan.Zero;

                var timeRemaining = new TimeSpan(0, 0, 0, 5).Subtract(FlStopwatch.Elapsed);

                if (timeRemaining > TimeSpan.Zero) return timeRemaining;

                FlStopwatch.Reset();

                return TimeSpan.Zero;
            }
        }

        public static bool IsFlReady => FlCooldown == TimeSpan.Zero;

        private static (Encounter, Enemy, BattleCharacter) GetEnemyLogicAndEnemyCached { get; set; }

        private static uint _layeredCastId;
        private static bool _layeredResponseUsed;

        /// <summary>
        /// Reserves the one extra response a single mechanic is allowed.
        /// <para>
        /// Normally reacting to a cast marks it handled and blocks anything further, so one mechanic gets
        /// one answer. That is wrong for a heavy raidwide: instant mitigation is off the global cooldown
        /// while barriers are on it, so a healer should lay one down and still raise the other. Callers
        /// pass the result to <see cref="DoAndBuffer(Task{bool}, bool)"/> as <c>layered</c>.
        /// </para>
        /// <para>
        /// Strictly one per cast. Without that cap a job with several instant mitigations — White Mage has
        /// five — would answer one raidwide with all of them over successive pulses, which is the priority
        /// dump the throttle exists to prevent.
        /// </para>
        /// </summary>
        private static bool TryConsumeLayeredResponse(uint? castId)
        {
            if (!castId.HasValue || castId.Value == 0)
                return false; // lock-on reactions have no cast to budget against

            if (_layeredCastId != castId.Value)
            {
                _layeredCastId = castId.Value;
                _layeredResponseUsed = false;
            }

            if (_layeredResponseUsed)
                return false;

            _layeredResponseUsed = true;
            return true;
        }

        /// <summary>
        /// Marks the mechanic being reacted to as handled without casting anything — for reactions
        /// dispatched through the spell queue, which execute over the following pulses. Marking at
        /// queue time keeps every other branch from answering the same cast while the queue runs,
        /// and stops the queueing branch itself re-firing every pulse until the queue engine starts.
        /// </summary>
        public static void BufferQueuedResponse()
        {
            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemy != null && enemy.IsValid && enemy.CastingSpellId != 0)
            {
                FlHandledCastingSpellId.Add(enemy.CastingSpellId);
                LatchSameNameSiblingCasts(encounter, enemy.CastingSpellId, enemy.SpellCastInfo?.Name);
            }

            // A lock-on-triggered response has no cast id — consume the marker id the detector
            // stashed instead, so a marker that outlives its mechanic cannot re-fire the reaction.
            LatchPendingLockOn();

            FlStopwatch.Start();
        }

        public static async Task<bool> DoAndBuffer(Task<bool> task, bool layered = false)
        {
            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            // Snapshot the cast we're reacting to BEFORE awaiting. The caller passes Spell.Cast(...),
            // which eagerly starts the cast; awaiting it burns the ~0.6s animation lock, during which the
            // enemy can move on to its NEXT catalogued cast. Reading enemy.CastingSpellId after the await
            // would latch THAT next cast as "handled" and suppress its own mitigation.
            var handledCastId = enemy?.CastingSpellId;
            var handledCastName = enemy != null && enemy.IsCasting ? enemy.SpellCastInfo?.Name : null;

            // Bailing on a null enemy here would NOT stop the action (the cast is already started) — it
            // would only skip the FL throttle, letting a lock-on reaction (enemy == null) re-fire every
            // pulse and dump the whole mitigation priority list. Await the cast, then start the 5s
            // throttle on success regardless; only record the handled cast id when we had a casting enemy.
            if (!await task) return false;

            // A layered response deliberately leaves the mechanic open so the next pulse can follow up with
            // the other half of the mitigation, neither marking the cast handled nor starting the throttle.
            // The budget is only spent once the cast actually landed, and only one is granted per mechanic,
            // so a second layered attempt falls through and closes the reaction as normal.
            if (layered && TryConsumeLayeredResponse(handledCastId))
                return true;

            // Never latch cast id 0: a lock-on reaction binds an IDLE boss, whose CastingSpellId is
            // 0 — and a latched 0 makes the cast-branch detector return false on Contains(0) before
            // the lock-on branch ever runs, eating every later marker wave until the set clears.
            if (handledCastId.HasValue && handledCastId.Value != 0)
            {
                FlHandledCastingSpellId.Add(handledCastId.Value);
                LatchSameNameSiblingCasts(encounter, handledCastId, handledCastName);
            }

            // Same for lock-on-triggered responses: consume the stashed marker id. Sits below the
            // layered early-return on purpose — a layered response leaves the mechanic open for its
            // second half, and the fall-through response is the one that closes it.
            LatchPendingLockOn();

            FlStopwatch.Start();
            return true;
        }

        public static Character EnemyIsCastingTankBuster()
        {
            if (!IsFlReady)
                return null;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic?.TankBusters == null || enemy == null || encounter == null)
                return EnemyIsCastingSharedTankBuster();

            if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                return null;
            FlHandledCastingSpellId.Clear();

            var output = enemyLogic.TankBusters.Contains(enemy.CastingSpellId)
                ? Group.CastableTanks.FirstOrDefault(x => x == enemy.TargetCharacter)
                : null;

            // Solo: no party tank is the buster's target, but it's aimed at us so we eat it — react on
            // ourselves. Scoped to out-of-party so an in-party aggro swap doesn't trigger a self-reaction.
            if (output == null
                && !Globals.InParty
                && enemyLogic.TankBusters.Contains(enemy.CastingSpellId)
                && enemy.TargetCharacter?.ObjectId == Core.Me.ObjectId)
                output = Core.Me;

            if (output != null && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo(
                    $"[TankBuster Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name} on {output.CurrentJob}{(Globals.InParty ? " in our party." : " (solo).")}");

            return output;
        }

        public static Character EnemyIsCastingSharedTankBuster()
        {
            if (!IsFlReady)
                return null;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic?.SharedTankBusters == null || enemy == null || encounter == null)
                return null;

            if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                return null;
            FlHandledCastingSpellId.Clear();

            // Every tank stacks for a shared buster, so every tank takes a share and every tank wants
            // covering. Healers pass this result straight to a cast, so it has to name a tank they can
            // actually reach — CastableTanks is our own party, not the alliance.
            //
            // Prefer the tank being aimed at when they are in our party. In an alliance raid the target is
            // usually in a different party, and the tank we can reach is ours, stacking in to share the
            // damage — so fall back to them rather than returning nothing. Returning the target alone left
            // a healer in another party doing nothing while their tank ate a share; returning a non-target
            // alone shielded the co-tank while the tank being hit went uncovered.
            //
            // CastableTanks is built before Group applies any distance filter, so it can name a tank well
            // outside heal range. Naming one satisfies the preferred-target branch, skips the fallback and
            // then fails the caller's own CanCast — no mitigation at all. Restrict both branches to tanks
            // inside standard heal range so the fallback can still find the co-tank we can reach.
            //
            // WithinSpellRange, not the CastableAlliesWithin30 list: that list is built from raw
            // centre-to-centre distance, so a large tank whose edge is well inside 30y can fall out of it
            // and be passed over for a co-tank while the game would have allowed the cast.
            var reachableTanks = Group.CastableTanks.Where(x => x.WithinSpellRange(30)).ToList();

            var output = enemyLogic.SharedTankBusters.Contains(enemy.CastingSpellId)
                ? reachableTanks.FirstOrDefault(x => x == enemy.TargetCharacter)
                  ?? reachableTanks.FirstOrDefault()
                : null;

            // Solo: no co-tank to share with, so if it's aimed at us we eat the whole thing — self-mitigate.
            if (output == null
                && !Globals.InParty
                && enemyLogic.SharedTankBusters.Contains(enemy.CastingSpellId)
                && enemy.TargetCharacter?.ObjectId == Core.Me.ObjectId)
                output = Core.Me;

            if (output != null && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo(
                    $"[Shared TankBuster Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}. Handling for {output.CurrentJob}{(Globals.InParty ? " in our party." : " (solo)")}.");

            return output;

        }

        // Lock-on responses have no cast id to latch as handled, so before this only the 5s
        // FlStopwatch separated re-fires — and client-side markers provably outlive their mechanic
        // by 3-10s. One Banishga IV marker produced two full responses 5.8s apart, the second
        // landing 4.4s AFTER the mechanic resolved.
        //
        // State is PER MARKER ID, tracked by presence transitions each pulse: a new id (or a rise
        // in how many players carry it — an overlapping second wave) starts a fresh application
        // with its own 5s actionable window (marker-to-hit measured 4.9-5.0s); the response
        // success paths mark the application handled; the id's state drops the moment the marker
        // leaves every VfxContainer, so the next wave is answerable immediately. Per-id clocks,
        // not a shared one: a brand-new marker must never inherit an older marker's age. Residual
        // blind spot, accepted: two same-id applications swapping between players in the same
        // pulse with no count change are indistinguishable from one continuous application.
        private sealed class FlLockOnState
        {
            public long FirstSeenTick;
            public int LastCount;
            public bool Handled;
        }

        private static readonly Dictionary<uint, FlLockOnState> FlLockOnStates = new Dictionary<uint, FlLockOnState>();
        private static uint? _pendingLockOnId;
        private const int LockOnActionableMs = 5000;

        // Called from the out-of-combat pulse alongside the spell-queue expiry: a wipe can end
        // combat with marker state still held, and a retry whose markers land before the first
        // in-combat upkeep sample would inherit a stale Handled flag and eat the wave unanswered.
        public static void ResetLockOnStates()
        {
            FlLockOnStates.Clear();
            _pendingLockOnId = null;
        }

        private static void LatchPendingLockOn()
        {
            if (!_pendingLockOnId.HasValue)
                return;

            if (FlLockOnStates.TryGetValue(_pendingLockOnId.Value, out var state))
                state.Handled = true;

            _pendingLockOnId = null;
        }

        // How many of each relevant marker id are on the player + party right now. Relevance is
        // the UNION of every catalogued enemy's AoeLockOns for the encounter, not the currently
        // bound enemy's: the bind swings between catalogued enemies as they cast (1s cache), and
        // scoping relevance to it made a bind flip drop a live marker's state - releasing on
        // DESELECTION when the design says release on DESPAWN. Multi-boss encounters (Jeuno's Ark
        // Angels, San d'Oria's Omega+Ultima) hit that constantly.
        private static Dictionary<uint, int> PresentLockOnCounts(Encounter encounter)
        {
            var counts = new Dictionary<uint, int>();
            var relevant = new HashSet<uint>();

            if (encounter?.Enemies != null)
                foreach (var catalogued in encounter.Enemies)
                    if (catalogued.AoeLockOns != null)
                        foreach (var id in catalogued.AoeLockOns)
                            relevant.Add(id);

            if (DebugSettings.Instance.FightLogicIncludeCommonAoeLockOnsTest)
                foreach (var id in CommonAoeLockOns)
                    relevant.Add(id);

            if (relevant.Count == 0)
                return counts;

            void Scan(Character unit)
            {
                if (unit == null || !unit.IsValid)
                    return;

                foreach (var lockOn in unit.VfxContainer.LockOns)
                {
                    if (!relevant.Contains(lockOn.Id))
                        continue;

                    counts.TryGetValue(lockOn.Id, out var n);
                    counts[lockOn.Id] = n + 1;
                }
            }

            // Explicit self-scan for solo play; skip self in the party loop or a self-carried
            // marker counts twice and the carrier-count wave detection reads a phantom rise.
            Scan(Core.Me);
            foreach (var partyMember in Group.CastableAlliesWithin50)
            {
                if (partyMember == Core.Me)
                    continue;

                Scan(partyMember);
            }

            return counts;
        }

        // Per-pulse upkeep. Runs ABOVE the IsFlReady throttle on purpose: a marker-free gap (or a
        // new wave) that falls entirely inside a throttle window must still be observed, or wave 2
        // of a chained mechanic inherits wave 1's handled flag and takes zero mitigation.
        private static void UpdateLockOnStates(Encounter encounter)
        {
            // The pending id never outlives the pulse that stashed it: detection and its response
            // run inside one pulse, so anything still pending here is stale and would let an
            // UNRELATED later response (a tankbuster, say) latch a live marker as handled. This
            // also removes the silent dependency on LockOnActionableMs equalling the 5s throttle.
            _pendingLockOnId = null;

            var counts = PresentLockOnCounts(encounter);

            if (FlLockOnStates.Count == 0 && counts.Count == 0)
                return;

            foreach (var pair in counts)
            {
                if (!FlLockOnStates.TryGetValue(pair.Key, out var state))
                {
                    FlLockOnStates[pair.Key] = new FlLockOnState
                    {
                        FirstSeenTick = Environment.TickCount64,
                        LastCount = pair.Value,
                    };
                    continue;
                }

                // More carriers than before = an overlapping new application of the same id.
                if (pair.Value > state.LastCount)
                {
                    state.FirstSeenTick = Environment.TickCount64;
                    state.Handled = false;
                }

                state.LastCount = pair.Value;
            }

            // Departed ids: state drops so the next application starts fresh.
            var gone = FlLockOnStates.Keys.Where(id => !counts.ContainsKey(id)).ToList();
            foreach (var id in gone)
            {
                FlLockOnStates.Remove(id);

                if (_pendingLockOnId == id)
                    _pendingLockOnId = null;
            }
        }

        /// <summary>
        /// Checks if any of the specified AoeLockOn IDs are present on the player or party members
        /// </summary>
        private static (bool found, uint? lockOnId) CheckAoeLockOns(IEnumerable<uint> lockOnIds)
        {
            if (lockOnIds == null)
                return (false, null);

            // Check player first
            var detectedLockOn = Core.Me.VfxContainer.LockOns.FirstOrDefault(lockOn => lockOnIds.Contains(lockOn.Id));
            if (detectedLockOn != null)
                return (true, detectedLockOn.Id);

            // Check party members
            foreach (var partyMember in Group.CastableAlliesWithin50)
            {
                if (partyMember == null || !partyMember.IsValid)
                    continue;

                detectedLockOn = partyMember.VfxContainer.LockOns.FirstOrDefault(lockOn => lockOnIds.Contains(lockOn.Id));
                if (detectedLockOn != null)
                    return (true, detectedLockOn.Id);
            }

            return (false, null);
        }

        public static bool EnemyIsCastingAoe()
        {
            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            // Marker lifecycle upkeep, ABOVE the throttle: a wave transition that happens entirely
            // inside a throttle window must still be observed, or the next wave inherits the last
            // wave's handled state. GetEnemyLogicAndEnemy is 1s-cached, so the reorder is cheap.
            UpdateLockOnStates(encounter);

            if (!IsFlReady)
                return false;

            // Check for AoE spell casting (requires enemy to be present and casting)
            if (enemy != null && enemyLogic?.Aoes != null && encounter != null)
            {
                if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                    return false;
                FlHandledCastingSpellId.Clear();

                var output = enemyLogic.Aoes.Contains(enemy.CastingSpellId);

                if (output && DebugSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}");

                if (output)
                    return true;
            }

            // Check for encounter-specific AoeLockOns (doesn't require enemy to be present - lockons are on player/party)
            if (enemyLogic?.AoeLockOns != null)
            {
                var (found, lockOnId) = CheckAoeLockOns(enemyLogic.AoeLockOns);
                if (found && LockOnIsActionable(lockOnId))
                {
                    if (DebugSettings.Instance.DebugFightLogic)
                    {
                        var encounterName = encounter?.Name ?? "Unknown Encounter";
                        var enemyName = enemy?.Name ?? "Unknown Enemy";
                        Logger.WriteInfo($"[AOE Lock On Detected] {encounterName} {enemyName} lockon {lockOnId}");
                    }
                    return true;
                }
            }

            // Also check common AoeLockOns if enabled (works even when boss isn't in encounter definition)
            if (DebugSettings.Instance.FightLogicIncludeCommonAoeLockOnsTest)
            {
                var (found, lockOnId) = CheckAoeLockOns(CommonAoeLockOns);
                if (found && LockOnIsActionable(lockOnId))
                {
                    if (DebugSettings.Instance.DebugFightLogic)
                        Logger.WriteInfo($"[AOE Lock On Detected] Common lockon {lockOnId}");
                    return true;
                }
            }

            return false;
        }

        // Shared gate for both lock-on branches above: refuses an application already answered,
        // refuses markers older than the mechanic they announce, stashes the id for the response
        // paths to consume on success. All lifecycle bookkeeping lives in UpdateLockOnStates.
        private static bool LockOnIsActionable(uint? lockOnId)
        {
            if (!lockOnId.HasValue)
                return false;

            if (!FlLockOnStates.TryGetValue(lockOnId.Value, out var state))
                return false;

            if (state.Handled)
                return false;

            if (Environment.TickCount64 - state.FirstSeenTick > LockOnActionableMs)
                return false;

            _pendingLockOnId = lockOnId;
            return true;
        }

        public static bool EnemyIsCastingBigAoe()
        {
            if (!IsFlReady)
                return false;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic == null || enemy == null || encounter == null)
                return false;

            if (enemyLogic.BigAoes == null)
                return EnemyIsCastingAoe();

            if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                return false;
            FlHandledCastingSpellId.Clear();

            var output = enemyLogic.BigAoes.Contains(enemy.CastingSpellId);

            if (output && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo(
                    $"[BIG AOE Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}");

            return output;
        }

        public static bool EnemyIsCastingKnockback()
        {
            if (!IsFlReady)
                return false;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic?.Knockbacks == null || enemy == null || encounter == null)
                return false;

            if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                return false;
            FlHandledCastingSpellId.Clear();

            var output = enemyLogic.Knockbacks.Contains(enemy.CastingSpellId);

            if (output && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[Knockback Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}");

            return output;
        }

        // Field Operations — Eureka, Bozja/Zadnor and the Occult Crescent — are instanced content whose
        // bosses are worth mitigating exactly like duty bosses, but none of them register as a standard
        // InActiveDuty (there's no duty-in-progress state), so the gates below would keep FightLogic
        // dormant there. Admitting these zones is safe: reactions only ever fire on catalogued cast ids,
        // so they cannot misfire on unmapped field trash. Delubrum Reginae is deliberately absent — it's
        // a real instanced raid inside Bozja and already satisfies InActiveDuty.
        private static readonly HashSet<ushort> FieldOperationZoneIds = new HashSet<ushort>
        {
            732,  // The Forbidden Land, Eureka Anemos
            763,  // The Forbidden Land, Eureka Pagos
            795,  // The Forbidden Land, Eureka Pyros
            827,  // The Forbidden Land, Eureka Hydatos
            920,  // The Bozjan Southern Front
            975,  // Zadnor
        };

        // The Occult Crescent is deliberately absent from the list above: it owns its own zone detection
        // (which also matches new Horns by name), so keeping it in one place stops the two drifting apart.
        public static bool InFieldOperation()
        {
            return FieldOperationZoneIds.Contains(WorldManager.ZoneId)
                || global::Magitek.Logic.Roles.OccultCrescent.IsInOccultCrescent();
        }

        public static bool ZoneHasFightLogic()
        {
            if (!DebugSettings.Instance.UseFightLogic)
                return false;

            // Admit field operations alongside duties so catalogued reactions (incl. the healer
            // HealFightLogic paths that gate on this) fire there. Mirrors GetEnemyLogicAndEnemy().
            if (!Globals.InActiveDuty && !InFieldOperation())
                return false;

            if (!Core.Me.InCombat)
                return false;

            // If common AoeLockOns are enabled, all dungeons technically have fightlogic
            if (DebugSettings.Instance.FightLogicIncludeCommonAoeLockOnsTest)
                return true;

            return FightLogicEncounters.Encounters.Any(x => x.ZoneId == WorldManager.RawZoneId);
        }

        public static bool EnemyHasAnyTankbusterLogic()
        {
            if (ZoneHasFightLogic())
            {
                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                return (enemyLogic?.TankBusters != null || enemyLogic?.SharedTankBusters != null);
            }

            return false;
        }

        public static bool EnemyHasAnyAoeLogic()
        {
            if (ZoneHasFightLogic())
            {
                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                // If common AoeLockOns are enabled, we always have AoE logic available
                if (DebugSettings.Instance.FightLogicIncludeCommonAoeLockOnsTest)
                    return true;

                return (enemyLogic?.Aoes != null || enemyLogic?.BigAoes != null || enemyLogic?.AoeLockOns != null);
            }

            return false;
        }

        /// <summary>
        /// The catalogued enemy whose cast the detectors matched, while it is still casting — or null.
        /// <para>
        /// Reactions that debuff the CASTER (Feint, Addle, Dismantle, Reprisal) need this rather than
        /// Core.Me.CurrentTarget: the two are frequently different enemies, and a mitigation debuff applied
        /// to whatever we happen to be hitting does nothing about the mechanic we are reacting to.
        /// </para>
        /// <para>
        /// The cast id has to be one this enemy is catalogued for, in a category the debuff reactions
        /// actually answer. An AoE can also be detected from a lock-on on the party, with no cast
        /// involved at all — and this enemy may still be mid-cast on something unrelated. Returning it
        /// then would aim the debuff using a cast no detector matched, so the id check keeps those
        /// reactions on the current-target fallback where they belong.
        /// </para>
        /// <para>
        /// Knockbacks are deliberately NOT counted. FightLogic_Debuff never calls
        /// EnemyIsCastingKnockback, so a knockback cast cannot be what it is reacting to — and several
        /// catalogued encounters carry both AoeLockOns and Knockbacks, where counting them would let a
        /// lock-on detection hand back an enemy that is merely mid-knockback.
        /// </para>
        /// </summary>
        public static BattleCharacter DetectedCaster()
        {
            var (_, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic == null || enemy == null || !enemy.IsValid || !enemy.IsCasting)
                return null;

            var castId = enemy.CastingSpellId;

            var matched = (enemyLogic.TankBusters?.Contains(castId) ?? false)
                          || (enemyLogic.SharedTankBusters?.Contains(castId) ?? false)
                          || (enemyLogic.Aoes?.Contains(castId) ?? false)
                          || (enemyLogic.BigAoes?.Contains(castId) ?? false);

            return matched ? enemy : null;
        }

        public static bool EnemyHasAnyKnockbackLogic()
        {
            if (ZoneHasFightLogic())
            {
                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                return (enemyLogic?.Knockbacks != null);
            }

            return false;
        }


        public static bool HodlCastTimeRemaining(int hodlTillCastInMs = 0, double hodlTillDurationInPct = 0.0)
        {
            if (hodlTillCastInMs == 0 && hodlTillDurationInPct == 0)
                return true;

            if (ZoneHasFightLogic())
            {
                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemy == null)
                    return true;

                if (enemy.IsCasting)
                {
                    if (hodlTillCastInMs > 0)
                        return enemy.SpellCastInfo.RemainingCastTime.TotalMilliseconds <= hodlTillCastInMs;
                    else if (hodlTillDurationInPct > 0)
                    {
                        double currentCastTime = enemy.SpellCastInfo.CurrentCastTime.TotalMilliseconds;
                        double totalCastTime = enemy.SpellCastInfo.CastTime.TotalMilliseconds;
                        double castProgress = (currentCastTime / totalCastTime) * 100;

                        return castProgress >= hodlTillDurationInPct;
                    }
                }
                else
                {
                    return true;
                }
            }

            return true;
        }

        // First entry cataloguing this cast id, across the whole encounter. Every id shared by
        // multiple entries today is same-category in all of them, so first-match cannot change the
        // response — if an id is ever catalogued under DIFFERENT categories for different enemies,
        // first-match decides and this comment is the warning.
        private static Enemy EnemyOwningCastId(Encounter encounter, uint castId)
        {
            return encounter?.Enemies?.FirstOrDefault(x =>
                (x.TankBusters != null && x.TankBusters.Contains(castId))
                || (x.SharedTankBusters != null && x.SharedTankBusters.Contains(castId))
                || (x.Aoes != null && x.Aoes.Contains(castId))
                || (x.BigAoes != null && x.BigAoes.Contains(castId))
                || (x.Knockbacks != null && x.Knockbacks.Contains(castId)));
        }

        // A cast id's response families across the whole encounter: tankbuster-family (strict +
        // shared), AoE-family (Aoes + BigAoes), knockback-family. Used to keep the sibling latch
        // from crossing families.
        private static (bool tb, bool aoe, bool kb) CastIdFamilies(Encounter encounter, uint castId)
        {
            bool tb = false, aoe = false, kb = false;

            foreach (var e in encounter.Enemies)
            {
                tb |= (e.TankBusters?.Contains(castId) ?? false) || (e.SharedTankBusters?.Contains(castId) ?? false);
                aoe |= (e.Aoes?.Contains(castId) ?? false) || (e.BigAoes?.Contains(castId) ?? false);
                kb |= e.Knockbacks?.Contains(castId) ?? false;
            }

            return (tb, aoe, kb);
        }

        // One mechanic often runs through several shells at once — Mega Holy and Cosmic Breath are
        // pairs of castbars with different ids, Starflare a whole ring of them. A response records
        // the one id it answered; when the 5s throttle lapses mid-cast, a sibling id re-detects the
        // same, already-mitigated mechanic and burns a second response on it. Latch every catalogued
        // id currently being cast under the SAME NAME — same-name is the sibling test, so a genuinely
        // different mechanic casting concurrently (a tankbuster during a raidwide) is never touched.
        private static void LatchSameNameSiblingCasts(Encounter encounter, uint? handledCastId, string handledCastName)
        {
            if (encounter?.Enemies == null || !handledCastId.HasValue || string.IsNullOrEmpty(handledCastName))
                return;

            foreach (var unit in GameObjectManager.GetObjectsOfType<BattleCharacter>())
            {
                if (unit == null || !unit.IsValid || !unit.IsNpc || !unit.IsCasting)
                    continue;

                var castId = unit.CastingSpellId;
                if (castId == handledCastId.Value || FlHandledCastingSpellId.Contains(castId))
                    continue;

                if (unit.SpellCastInfo?.Name != handledCastName)
                    continue;

                if (EnemyOwningCastId(encounter, castId) == null)
                    continue;

                // Same NAME is necessary but not sufficient: the catalogue holds same-name pairs in
                // DIFFERENT categories that need different responses (Blown Blessing 42124 is an Aoe,
                // 42123 a Knockback — latching the knockback while answering the AoE kills Surecast).
                // A sibling only shares the handled cast's latch when it shares its response family.
                if (CastIdFamilies(encounter, castId) != CastIdFamilies(encounter, handledCastId.Value))
                    continue;

                FlHandledCastingSpellId.Add(castId);
            }
        }

        private static (Encounter, Enemy, BattleCharacter) GetEnemyLogicAndEnemy()
        {
            if (GetEnemyLogicAndEnemyCacheAge.IsRunning && GetEnemyLogicAndEnemyCacheAge.ElapsedMilliseconds < 1000)
            {
                // The enemy can despawn while it sits in this cache, and every selector reads game
                // memory off it (CastingSpellId etc.) — on a freed object that read throws and kills
                // the whole combat pulse. A dead entry falls through and recomputes instead.
                if (GetEnemyLogicAndEnemyCached.Item3 == null || GetEnemyLogicAndEnemyCached.Item3.IsValid)
                    return GetEnemyLogicAndEnemyCached;
            }

            Encounter encounter = null;
            Enemy enemyLogic = null;
            BattleCharacter enemy = null;

            if (!DebugSettings.Instance.UseFightLogic)
                return SetAndReturn();

            // Admit field operations alongside duties (see FieldOperationZoneIds) so the catalogue load
            // below runs there rather than leaving FightLogic dormant.
            if (!Globals.InActiveDuty && !InFieldOperation())
                return SetAndReturn();

            if (!Core.Me.InCombat)
                return SetAndReturn();

            encounter = FightLogicEncounters.Encounters.FirstOrDefault(x => x.ZoneId == WorldManager.RawZoneId);

            if (encounter == null)
                return SetAndReturn();

            // Prefer a catalogued enemy that is CASTING one of its own catalogued mechanics right now.
            // With several catalogued enemies alive at once (Jeuno's Ark Angel phase), binding the first
            // list entry made every other boss's mechanics invisible for the whole fight.
            foreach (var candidateLogic in encounter.Enemies)
            {
                foreach (var threat in Combat.Enemies)
                {
                    if (candidateLogic.Id != threat.NpcId && candidateLogic.Name != threat.EnglishName)
                        continue;

                    if (!threat.IsCasting || FlHandledCastingSpellId.Contains(threat.CastingSpellId))
                        continue;

                    var castId = threat.CastingSpellId;
                    if ((candidateLogic.TankBusters != null && candidateLogic.TankBusters.Contains(castId))
                        || (candidateLogic.SharedTankBusters != null && candidateLogic.SharedTankBusters.Contains(castId))
                        || (candidateLogic.Aoes != null && candidateLogic.Aoes.Contains(castId))
                        || (candidateLogic.BigAoes != null && candidateLogic.BigAoes.Contains(castId))
                        || (candidateLogic.Knockbacks != null && candidateLogic.Knockbacks.Contains(castId)))
                    {
                        enemyLogic = candidateLogic;
                        enemy = threat;
                        break;
                    }
                }

                if (enemy != null)
                    break;
            }

            // Untargetable-caster fallback. Windurst proved bosses run whole kits through level-1,
            // HP-44 helper copies sharing the boss's name and NpcId — untargetable, attacking nobody,
            // so they never enter Combat.Enemies and six catalogued casts were structurally invisible
            // in one zone. Scan the object table for any hostile NPC mid-cast on a catalogued id.
            // Deliberately NO targetability/CanAttack gate: the whole point is that these casters fail
            // ValidAttackUnit (the Shockwave caster took zero player hits all night). The handled-id
            // skip is mandatory — main and helper start different catalogued sibling ids 0.09–0.22s
            // apart, and without it the second sibling re-fires the reaction the first already spent.
            // Same workaround shape the gaze scan below already uses for the same reason.
            if (enemy == null)
            {
                foreach (var unit in GameObjectManager.GetObjectsOfType<BattleCharacter>())
                {
                    if (unit == null || !unit.IsValid || !unit.IsNpc || !unit.IsCasting)
                        continue;

                    if (FlHandledCastingSpellId.Contains(unit.CastingSpellId))
                        continue;

                    if (Core.Me.Distance(unit) > 50)
                        continue;

                    // Key on the CAST ID, not the caster's identity. Hollow King re-uses Shinryu's
                    // helper actors in place — same object ids, re-identified 21s before casting —
                    // and the routine's view of Name/NpcId lagged on the recycled actors, so an
                    // identity-first match silently skipped eight catalogued Celestial Trail casts
                    // while the identical scan bound fresh-spawned helpers all night. IsNpc replaces
                    // the identity gate as the population filter: the object table includes players
                    // and friendly battle NPCs (Prishe casts mid-fight in this very zone).
                    var owner = EnemyOwningCastId(encounter, unit.CastingSpellId);
                    if (owner == null)
                        continue;

                    // Identity mismatch on a catalogued cast is the recycled-actor signature; log it
                    // so the stale-identity mechanism can be confirmed from a live run.
                    if (DebugSettings.Instance.DebugFightLogic
                        && owner.Id != unit.NpcId && owner.Name != unit.EnglishName)
                        Logger.WriteInfo(
                            $"[FightLogic] Caster identity mismatch: {unit.EnglishName}/{unit.NpcId} casting {unit.CastingSpellId} catalogued under {owner.Name}/{owner.Id} — recycled actor?");

                    enemyLogic = owner;
                    enemy = unit;
                    break;
                }
            }

            // Nothing catalogued is being cast right now: fall back to the original selection.
            if (enemy == null)
            {
                enemyLogic = encounter.Enemies.FirstOrDefault(x => Combat.Enemies.Any(y => x.Id == y.NpcId || x.Name == y.EnglishName), encounter.Enemies.FirstOrDefault());

                enemy = Combat.Enemies.FirstOrDefault(y => enemyLogic.Id == y.NpcId || enemyLogic.Name == y.EnglishName, Combat.Enemies.FirstOrDefault());
            }

            if (enemy != null && enemy.IsCasting && !FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                FlHandledCastingSpellId.Clear();

            return SetAndReturn();

            (Encounter, Enemy, BattleCharacter) SetAndReturn()
            {
                if (DebugSettings.Instance.DebugFightLogicFound)
                {
                    Debug.Instance.FightLogicData =
                        $"\nYou are currently in {WorldManager.CurrentZoneName} ({WorldManager.RawZoneId})";
                    var currentTarget = Core.Me.CurrentTarget == null ? "No Target" : Core.Me.CurrentTarget.Name;
                    var npcId = Core.Me.CurrentTarget?.NpcId == null ? 0 : Core.Me.CurrentTarget.NpcId;
                    Debug.Instance.FightLogicData += $"\nCurrent Target: {currentTarget} ({npcId})\n";

                    // Live cast discovery: every nearby enemy currently casting, with the raw action id.
                    // This is how you capture uncatalogued mechanic ids (gazes, etc.) to add to the catalogue —
                    // read the id off the boss's cast bar here, then it can be catalogued by enemy name.
                    var castingNow = Combat.Enemies.Where(y => y.IsCasting).ToList();
                    Debug.Instance.FightLogicData += "\nCasting now:\n";
                    if (castingNow.Count == 0)
                        Debug.Instance.FightLogicData += "\t(nothing casting)\n";
                    else
                        castingNow.ForEach(y => Debug.Instance.FightLogicData +=
                            $"\t{y.EnglishName} ({y.NpcId}): {y.SpellCastInfo.Name} ({y.CastingSpellId})\n");
                    Debug.Instance.FightLogicData += "\n\n";

                    if (encounter == null && enemyLogic == null && enemy == null)
                        Debug.Instance.FightLogicData += $"There is no Fight Logic for this zone - {WorldManager.CurrentZoneName} ({WorldManager.RawZoneId}). \n";
                    else
                    {
                        Debug.Instance.FightLogicData +=
                            $"Fight Logic Recognized for {encounter.Name} from ({encounter.Expansion})\n" +
                            $"There is Logic for {encounter.Enemies.Count()} enemies.\n\n";

                        encounter.Enemies.ForEach(element =>
                        {
                            Debug.Instance.FightLogicData += $"Enemy: {element.Name} ({element.Id}):\n";

                            if (element.TankBusters != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tTankbusters:\n{string.Join("", element.TankBusters.Select(tb => $"\t\t{DataManager.GetSpellData(tb).Name} ({tb})\n"))}";

                            if (element.SharedTankBusters != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tShared Tankbusters:\n{string.Join("", element.SharedTankBusters.Select(stb => $"\t\t{DataManager.GetSpellData(stb).Name} ({stb})\n"))}";

                            if (element.Aoes != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tAoes:\n{string.Join("", element.Aoes.Select(aoe => $"\t\t{DataManager.GetSpellData(aoe).Name} ({aoe})\n"))}";

                            if (element.BigAoes != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tBig Aoes:\n{string.Join("", element.BigAoes.Select(baoe => $"\t\t{DataManager.GetSpellData(baoe).Name} ({baoe})\n"))}";

                            if (element.AoeLockOns != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tAoe Lock Ons:\n{string.Join("", element.AoeLockOns.Select(aoeLockOn => $"\t\t({aoeLockOn})\n"))}";

                            if (element.Knockbacks != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tKnockbacks:\n{string.Join("", element.Knockbacks.Select(kb => $"\t\t{DataManager.GetSpellData(kb).Name} ({kb})\n"))}";


                            Debug.Instance.FightLogicData += $"\n";
                        });
                    }
                }

                GetEnemyLogicAndEnemyCached = (encounter, enemyLogic, enemy);
                if (!GetEnemyLogicAndEnemyCacheAge.IsRunning)
                    GetEnemyLogicAndEnemyCacheAge.Start();
                else GetEnemyLogicAndEnemyCacheAge.Restart();
                return GetEnemyLogicAndEnemyCached;
            }
        }
    }

    internal class Enemy
    {
        internal uint Id { get; set; }
        internal string Name { get; set; }
        internal List<uint> TankBusters { get; set; }
        internal List<uint> SharedTankBusters { get; set; }
        internal List<uint> Aoes { get; set; }
        internal List<uint> BigAoes { get; set; }
        internal List<uint> Knockbacks { get; set; }
        internal List<uint> AoeLockOns { get; set; }
    }

    internal class Encounter
    {
        internal ushort ZoneId { get; set; }
        internal string Name { get; set; }
        internal FfxivExpansion Expansion { get; set; }
        internal List<Enemy> Enemies { get; set; }
    }
}
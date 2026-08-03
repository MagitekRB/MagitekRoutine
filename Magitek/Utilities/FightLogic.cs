using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
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

        #region Action-punishing player debuffs (Pyretic / Acceleration Bomb)

        // Some mechanics apply a debuff that detonates if you ACT or MOVE while it's on you. There's no
        // enemy cast to react to, and they recur across many zones, so this is a flat watch on the
        // player's own auras — deliberately NOT zone-gated like the enemy-cast catalogues. Several status
        // IDs map to the same mechanic because each encounter defines its own copy; seed the ones we know
        // and expand the set as the debug logger surfaces more.
        public class ActionPunishMechanic
        {
            public string Name;
            public bool PunishesActions;   // casting / weaponskills / abilities set it off (e.g. Pyretic)
            public bool PunishesMovement;  // moving sets it off (e.g. Acceleration Bomb)
            public bool ChecksOnExpiry;    // snapshots our state as it falls off rather than punishing all along
        }

        // Pyretic is the continuous kind: acting or moving at any point while it ticks hurts.
        private static readonly ActionPunishMechanic Pyretic =
            new ActionPunishMechanic { Name = "Pyretic", PunishesActions = true, PunishesMovement = true };

        // Detonates on movement as it expires, so there's no reason to stand frozen for its whole duration.
        private static readonly ActionPunishMechanic AccelerationBomb =
            new ActionPunishMechanic { Name = "Acceleration Bomb", PunishesActions = false, PunishesMovement = true, ChecksOnExpiry = true };

        // Occult Crescent, Trade Tortoise: "Ill-gotten Goods" (41518) hands the whole raid an 8s Buyer's
        // Remorse and the state check lands when it falls off, not while it ticks — combat logs show players
        // attacking straight through the full 8s at untouched HP. Only the Pyretic-flavoured variant (4342)
        // is listed. The other two waves are deliberately left out because neither is answerable by holding
        // still: 4343 is frost, which wants the opposite (keep moving), and 4344 is a forced forward march
        // that's purely about where you're standing. Both are positioning the player owns, not the routine.
        private static readonly ActionPunishMechanic BuyersRemorse =
            new ActionPunishMechanic { Name = "Buyer's Remorse", PunishesActions = true, PunishesMovement = true, ChecksOnExpiry = true };

        // The Clyteum: "Under motion-sensing surveillance." Continuous rather than snapshot-on-expiry —
        // being seen at any point during it is what counts — so we sit still and quiet for the duration.
        private static readonly ActionPunishMechanic MotionTracker =
            new ActionPunishMechanic { Name = "Motion Tracker", PunishesActions = true, PunishesMovement = true };

        private static readonly Dictionary<uint, ActionPunishMechanic> ActionPunishAuras = new Dictionary<uint, ActionPunishMechanic>
        {
            // Every id below was taken from the game's own Status sheet by name, not from encounters we
            // happened to meet — each Pyretic row reads "Fire-aspected damage is taken with every action" and
            // each Acceleration Bomb row "any movement when effect wears off will result in detonation", so
            // the whole set is the same two mechanics. Waiting for the debug logger to surface them would
            // have left the newer copies — which is most current content — silently unhandled.
            { 639, Pyretic }, { 960, Pyretic }, { 1049, Pyretic }, { 1133, Pyretic },
            { 1599, Pyretic }, { 3522, Pyretic },
            { 1072, AccelerationBomb }, { 1384, AccelerationBomb }, { 2657, AccelerationBomb },
            { 3793, AccelerationBomb }, { 3802, AccelerationBomb }, { 4144, AccelerationBomb },
            { 5546, AccelerationBomb },
            { 4342, BuyersRemorse },
            { 5191, MotionTracker },
        };

        // The action-punishing mechanic currently on the player (plus how long it has left), or null if none.
        // Cheap: one scan of the player's own auras. Intentionally NOT throttled by IsFlReady — we want to
        // keep suppressing every pulse the debuff is present, not react a single time and move on.
        public static ActionPunishMechanic PlayerActionPunishAura(out double msRemaining)
        {
            msRemaining = 0;

            var me = Core.Me;

            if (me == null)
                return null;

            foreach (var aura in me.CharacterAuras)
            {
                if (!ActionPunishAuras.TryGetValue(aura.Id, out var mechanic))
                    continue;

                msRemaining = aura.TimespanLeft.TotalMilliseconds;
                return mechanic;
            }

            return null;
        }

        #endregion

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

        public static async Task<bool> DoAndBuffer(Task<bool> task, bool layered = false)
        {
            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            // Snapshot the cast we're reacting to BEFORE awaiting. The caller passes Spell.Cast(...),
            // which eagerly starts the cast; awaiting it burns the ~0.6s animation lock, during which the
            // enemy can move on to its NEXT catalogued cast. Reading enemy.CastingSpellId after the await
            // would latch THAT next cast as "handled" and suppress its own mitigation.
            var handledCastId = enemy?.CastingSpellId;

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

            if (handledCastId.HasValue)
                FlHandledCastingSpellId.Add(handledCastId.Value);
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

            var output = enemyLogic.SharedTankBusters.Contains(enemy.CastingSpellId)
                ? Group.CastableTanks.FirstOrDefault(x => x != enemy.TargetCharacter)
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
            if (!IsFlReady)
                return false;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

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
                if (found)
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
                if (found)
                {
                    if (DebugSettings.Instance.DebugFightLogic)
                        Logger.WriteInfo($"[AOE Lock On Detected] Common lockon {lockOnId}");
                    return true;
                }
            }

            return false;
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
            1252, // Occult Crescent: South Horn
            1346, // Occult Crescent: North Horn
        };

        public static bool InFieldOperation()
        {
            return FieldOperationZoneIds.Contains(WorldManager.ZoneId);
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

        public static bool EnemyHasAnyKnockbackLogic()
        {
            if (ZoneHasFightLogic())
            {
                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                return (enemyLogic?.Knockbacks != null);
            }

            return false;
        }

        #region Gaze mechanics (look away / look toward)

        public enum GazeDirection { None, Away, Toward }

        private static GameObject _gazeSource;
        private static GazeDirection _gazeDirection;
        private static DateTime _gazeHoldUntil = DateTime.MinValue;

        /// <summary>
        /// True while a gaze reaction owns our facing. Cast paths check this before force-facing the
        /// current target: that auto-face only runs while stationary, which is exactly why turning away
        /// used to work on the move and fail standing still — the next cast snapped us back at the boss.
        /// </summary>
        public static bool GazeHoldActive => DateTime.Now < _gazeHoldUntil;

        /// <summary>
        /// The heading the active latch is holding, or None once it lapses. Lets a caller tell an overlapping
        /// gaze that agrees with the current hold from one that wants the opposite way round.
        /// </summary>
        public static GazeDirection GazeHoldDirection => GazeHoldActive ? _gazeDirection : GazeDirection.None;

        /// <summary>
        /// Latch a gaze hold. Kept alive for a short grace after the gaze stops being detected, because the
        /// snapshot lands as the cast completes — releasing on the exact frame it ends lets a queued GCD fire
        /// and re-face us into the gaze before it resolves.
        /// </summary>
        public static void LatchGazeHold(GazeDirection direction, GameObject source, int graceMs)
        {
            _gazeDirection = direction;
            _gazeSource = source;
            _gazeHoldUntil = DateTime.Now.AddMilliseconds(graceMs);
        }

        /// <summary>
        /// Re-assert the latched facing during the post-gaze grace window. False once it lapses.
        /// </summary>
        public static bool ReassertGazeHold()
        {
            if (!GazeHoldActive || _gazeSource == null || !_gazeSource.IsValid)
                return false;

            FaceForGaze(_gazeDirection, _gazeSource);
            return true;
        }

        // The current zone's encounter, but only if something in it actually has catalogued gaze data.
        // Returning null lets the caller skip the object-manager scan entirely in every other zone.
        private static Encounter GazeEncounter()
        {
            if (!ZoneHasFightLogic())
                return null;

            var encounter = FightLogicEncounters.Encounters.FirstOrDefault(x => x.ZoneId == WorldManager.RawZoneId);

            if (encounter?.Enemies == null)
                return null;

            return encounter.Enemies.Any(x => x.LookAwayGazes != null || x.LookTowardGazes != null
                    || x.LookAwayLockOns != null || x.LookTowardLockOns != null || x.LookAwayFromMarkedLockOns != null)
                ? encounter
                : null;
        }

        // Units a gaze can come from, refreshed once per frame. Four rotation entry points reach the gaze
        // checks every pulse, and each was walking the whole object table on its own.
        //
        // This cannot reuse Group's cached collections or Combat.Enemies: those keep only what we could
        // attack, and gaze emitters are routinely things we cannot — the Occult Crescent's Accursed Orbs
        // and O8N/O8S's Graven Image statues are untargetable, and they are exactly what we need to find.
        // So it filters on validity and range only.
        private static readonly FrameCachedObject<IEnumerable<BattleCharacter>> _gazeCandidates =
            new(() => GameObjectManager.GetObjectsOfType<BattleCharacter>()
                .Where(x => x != null && x.IsValid && Core.Me.Distance(x) <= 50)
                .ToList());

        // The live object matching a catalogued enemy, used to orient against when the gaze is signalled
        // by a head marker rather than by that enemy's own cast.
        private static BattleCharacter FindCataloguedEnemy(Enemy logic)
        {
            return _gazeCandidates.Value
                .FirstOrDefault(x => logic.Id == x.NpcId || logic.Name == x.EnglishName);
        }

        // Gazes flagged by a head marker instead of a cast (e.g. Shinryu's Cataclysmic Vortex). RB surfaces
        // these through VfxContainer.LockOns — the same source the AoE lock-on checks read. There's no cast
        // bar to time against here, so the caller simply holds for as long as the marker is up.
        // Some mechanics punish movement but not casting. Those must not consume the pulse — that
        // would throw away every action in the window — but the rotation re-issues navigation later
        // in the same pulse, which would undo the MoveStop. This latch lets the rotation keep casting
        // while navigation stays parked. Short-lived and refreshed each pulse, so it cannot stick on.
        private static DateTime _movementHeldUntil = DateTime.MinValue;

        public static bool MovementHeld => DateTime.Now < _movementHeldUntil;

        public static void HoldMovement(int ms) => _movementHeldUntil = DateTime.Now.AddMilliseconds(ms);

        /// <summary>
        /// Drop the hold immediately. The latch is re-armed for a full second on every pulse the mechanic is
        /// up, so when it finally ends there is up to a second of stale hold left over — long enough to keep
        /// navigation and gap closers parked after the thing that justified them has gone.
        /// </summary>
        public static void ReleaseMovementHold() => _movementHeldUntil = DateTime.MinValue;

        private static uint _seenMarkerId;
        private static DateTime _seenMarkerSince = DateTime.MinValue;
        private static DateTime _seenMarkerLastPolled = DateTime.MinValue;

        // A marker is polled every pulse while it is up, so a gap means it went away and came back.
        private const double MarkerGoneAfterMs = 1000;

        /// <summary>
        /// How long the gaze marker we are currently reacting to has been on us. Markers carry no visible
        /// timer, unlike a cast bar, so this is the only way to turn late rather than surrendering the
        /// marker's entire lifetime. Resets whenever a different marker appears.
        /// </summary>
        public static double GazeMarkerAgeMs(uint markerId)
        {
            var now = DateTime.Now;

            // Watching the id alone is not enough. The same gaze comes round again every cycle, and
            // without noticing the gap between one and the next the second would be timed from the first,
            // count as long overdue, and turn instantly for the marker's whole life instead of waiting.
            if (_seenMarkerId != markerId
                || (now - _seenMarkerLastPolled).TotalMilliseconds > MarkerGoneAfterMs)
            {
                _seenMarkerId = markerId;
                _seenMarkerSince = now;
            }

            _seenMarkerLastPolled = now;

            return (now - _seenMarkerSince).TotalMilliseconds;
        }

        public static GazeDirection GazeLockOnActive(out GameObject source, out uint markerId)
        {
            source = null;
            markerId = 0;

            var encounter = GazeEncounter();
            var me = Core.Me;

            if (encounter == null || me == null)
                return GazeDirection.None;

            foreach (var logic in encounter.Enemies)
            {
                var away = logic.LookAwayLockOns == null
                    ? null
                    : me.VfxContainer.LockOns.FirstOrDefault(l => logic.LookAwayLockOns.Contains(l.Id));

                if (away != null)
                {
                    source = FindCataloguedEnemy(logic);
                    if (source != null)
                    {
                        markerId = away.Id;
                        return GazeDirection.Away;
                    }
                }

                var toward = logic.LookTowardLockOns == null
                    ? null
                    : me.VfxContainer.LockOns.FirstOrDefault(l => logic.LookTowardLockOns.Contains(l.Id));

                if (toward != null)
                {
                    source = FindCataloguedEnemy(logic);
                    if (source != null)
                    {
                        markerId = toward.Id;
                        return GazeDirection.Toward;
                    }
                }

                // Marker sits on somebody else and the rest of the party looks away from THEM. If we're the
                // marked one there's nothing to turn away from, so we leave our facing alone.
                if (logic.LookAwayFromMarkedLockOns == null)
                    continue;

                foreach (var ally in Group.CastableAlliesWithin50)
                {
                    if (ally == null || !ally.IsValid || ally.ObjectId == me.ObjectId)
                        continue;

                    var onAlly = ally.VfxContainer.LockOns.FirstOrDefault(l => logic.LookAwayFromMarkedLockOns.Contains(l.Id));

                    if (onAlly == null)
                        continue;

                    source = ally;
                    markerId = onAlly.Id;
                    return GazeDirection.Away;
                }
            }

            return GazeDirection.None;
        }

        public static bool EnemyHasAnyGazeLogic() => GazeEncounter() != null;

        // Which gaze is being cast right now, and by whom.
        //
        // Deliberately scans GameObjectManager instead of Combat.Enemies: the directional gaze is cast by
        // untargetable mechanic actors (the Gilded Headstone "eye" copies), and Combat.Enemies filters
        // those out via ValidAttackUnit/IsTargetable — so the cast would never be seen there. Also NOT
        // throttled by IsFlReady/FlHandledCastingSpellId: facing has to be re-asserted every pulse for the
        // whole cast rather than reacted to once.
        public static GazeDirection EnemyIsCastingGaze(out BattleCharacter source)
        {
            source = null;

            var encounter = GazeEncounter();

            if (encounter == null)
                return GazeDirection.None;

            var direction = GazeDirection.None;
            var soonest = double.MaxValue;

            foreach (var unit in _gazeCandidates.Value)
            {
                if (!unit.IsCasting)
                    continue;

                var logic = encounter.Enemies.FirstOrDefault(x => x.Id == unit.NpcId || x.Name == unit.EnglishName);

                if (logic == null)
                    continue;

                GazeDirection casting;

                if (logic.LookAwayGazes != null && logic.LookAwayGazes.Contains(unit.CastingSpellId))
                    casting = GazeDirection.Away;
                else if (logic.LookTowardGazes != null && logic.LookTowardGazes.Contains(unit.CastingSpellId))
                    casting = GazeDirection.Toward;
                else
                    continue;

                // These come in overlapping waves — copies start ~2s apart with ~4.7s casts, so an avert
                // and a face gaze are routinely in flight together demanding opposite headings. Obey the
                // one resolving first; once it lands the next becomes soonest and we flip for that.
                var remaining = unit.SpellCastInfo.RemainingCastTime.TotalMilliseconds;

                if (remaining >= soonest)
                    continue;

                soonest = remaining;
                direction = casting;
                source = unit;
            }

            return direction;
        }

        // Orient relative to the gaze source. Mind the codebase convention: MathHelper.CalculateHeading
        // returns the heading pointing AWAY from the destination — facing it is that value + PI (see the
        // InView/RadiansFromPlayerHeading math in GameObjectExtensions). So Away uses the raw heading and
        // Toward adds PI. SetFacing is instantaneous, so even a late turn lands; it only holds while
        // stationary, though — if a botbase is driving movement, movement direction wins.
        public static void FaceForGaze(GazeDirection direction, GameObject source)
        {
            if (source == null || !source.IsValid || direction == GazeDirection.None || Core.Me == null)
                return;

            var heading = MathHelper.CalculateHeading(Core.Me.Location, source.Location);

            if (direction == GazeDirection.Toward)
                heading = MathEx.NormalizeRadian(heading + (float)Math.PI);

            Core.Me.SetFacing(heading);
        }

        #endregion

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

        private static (Encounter, Enemy, BattleCharacter) GetEnemyLogicAndEnemy()
        {
            if (GetEnemyLogicAndEnemyCacheAge.IsRunning && GetEnemyLogicAndEnemyCacheAge.ElapsedMilliseconds < 1000)
                return GetEnemyLogicAndEnemyCached;

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

            enemyLogic = encounter.Enemies.FirstOrDefault(x => Combat.Enemies.Any(y => x.Id == y.NpcId || x.Name == y.EnglishName), encounter.Enemies.FirstOrDefault());

            enemy = Combat.Enemies.FirstOrDefault(y => enemyLogic.Id == y.NpcId || enemyLogic.Name == y.EnglishName, Combat.Enemies.FirstOrDefault());

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
                    //
                    // Deliberately the same source the gaze detector uses, NOT Combat.Enemies. That list keeps
                    // only what we can target and damage, and the casters most worth discovering are neither:
                    // the Occult Crescent's Accursed Orbs and O8N/O8S's Graven Image statues are untargetable
                    // emitters. Reading from Combat.Enemies made this blind to exactly the enemies it exists
                    // to catalogue.
                    var castingNow = _gazeCandidates.Value.Where(y => y.IsCasting).ToList();
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

                            if (element.LookAwayGazes != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tLook-Away Gazes:\n{string.Join("", element.LookAwayGazes.Select(g => $"\t\t{DataManager.GetSpellData(g).Name} ({g})\n"))}";

                            if (element.LookTowardGazes != null)
                                Debug.Instance.FightLogicData +=
                                    $"\tLook-Toward Gazes:\n{string.Join("", element.LookTowardGazes.Select(g => $"\t\t{DataManager.GetSpellData(g).Name} ({g})\n"))}";

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
        internal List<uint> LookAwayGazes { get; set; }
        internal List<uint> LookTowardGazes { get; set; }
        // Some fights signal the gaze with a head marker on players instead of an enemy cast. The first
        // two are markers on US and orient against the enemy; the third marks somebody ELSE and everyone
        // looks away from that player instead of from the boss.
        internal List<uint> LookAwayLockOns { get; set; }
        internal List<uint> LookTowardLockOns { get; set; }
        internal List<uint> LookAwayFromMarkedLockOns { get; set; }
    }

    internal class Encounter
    {
        internal ushort ZoneId { get; set; }
        internal string Name { get; set; }
        internal FfxivExpansion Expansion { get; set; }
        internal List<Enemy> Enemies { get; set; }
    }
}
using Clio.Utilities;
using ff14bot;
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

        public static async Task<bool> DoAndBuffer(Task<bool> task)
        {
            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemy == null)
                return false;

            if (!await task) return false;

            FlHandledCastingSpellId.Add(enemy.CastingSpellId);
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

            var output = MatchTankBuster(enemyLogic, enemy);

            if (output != null && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo(
                    $"[TankBuster Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name} on {output.CurrentJob} in our party.");

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

            var output = MatchSharedTankBuster(enemyLogic, enemy);

            if (output != null && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo(
                    $"[Shared TankBuster Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}. Handling for {output.CurrentJob} in our party.");

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

                var output = MatchAoe(enemyLogic, enemy);

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

            var output = MatchBigAoe(enemyLogic, enemy);

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

            var output = MatchKnockback(enemyLogic, enemy);

            if (output && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[Knockback Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}");

            return output;
        }

        // Single source of truth for what each detection category matches. The responder methods
        // above layer their gates (IsFlReady, the answered-once ledger, debug logging) on top of
        // these, and Peek below exposes them without any of that — keep the matching itself here
        // so the two surfaces can never drift apart.

        private static Character MatchTankBuster(Enemy enemyLogic, BattleCharacter enemy)
        {
            // The victim is whoever the cast is aimed at - usually a tank, but not always: a dead
            // main tank retargets the buster onto whoever has aggro, and some catalogued busters
            // (Io Ousia's Barreling Smash) pick an arbitrary player. Match any castable ally so the
            // response follows the hit; filtering to tanks went silent in exactly those cases.
            return enemyLogic.TankBusters.Contains(enemy.CastingSpellId)
                ? Group.CastableAlliesWithin30.FirstOrDefault(x => x == enemy.TargetCharacter)
                : null;
        }

        private static Character MatchSharedTankBuster(Enemy enemyLogic, BattleCharacter enemy)
        {
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

            return enemyLogic.SharedTankBusters.Contains(enemy.CastingSpellId)
                ? reachableTanks.FirstOrDefault(x => x == enemy.TargetCharacter)
                  ?? reachableTanks.FirstOrDefault()
                : null;
        }

        private static bool MatchAoe(Enemy enemyLogic, BattleCharacter enemy)
        {
            return enemyLogic.Aoes.Contains(enemy.CastingSpellId);
        }

        private static bool MatchBigAoe(Enemy enemyLogic, BattleCharacter enemy)
        {
            return enemyLogic.BigAoes.Contains(enemy.CastingSpellId);
        }

        private static bool MatchKnockback(Enemy enemyLogic, BattleCharacter enemy)
        {
            return enemyLogic.Knockbacks.Contains(enemy.CastingSpellId);
        }

        /// <summary>
        /// Read-only queries over the same detection matching the responder methods above use. A peek
        /// answers "is a mechanic incoming that it is time to react to", not "is a cast bar up".
        /// <para>
        /// Every peek waits out the configured fight logic response delay before answering, exactly as
        /// the responder call sites do through <see cref="HodlCastTimeRemaining"/> — so nothing acting
        /// on a peek can react to a mechanic faster than a real response would. The delay is baked in
        /// here rather than left to the call site precisely so a caller cannot forget it.
        /// </para>
        /// <para>
        /// Nothing in here touches the answered-once ledger, gates on <see cref="IsFlReady"/>, or
        /// starts the response stopwatch — asking a question never changes whether a responder still
        /// answers. A peek also keeps reporting a mechanic the responders have already answered, on
        /// purpose: peek callers are ADDITIVE, riding alongside the real response (aiming a card at
        /// the tank about to be hit while mitigation also fires), so going blind the moment a
        /// responder answers — which is what honoring <see cref="IsFlReady"/> would do — would defeat
        /// them exactly when they matter.
        /// </para>
        /// <para>
        /// Use these from job logic for targeting and priority hints only — choosing where to aim
        /// something that is being cast anyway. Anything cast BECAUSE a mechanic is incoming is a
        /// response and belongs in the responder methods via <see cref="DoAndBuffer"/>, so the
        /// answer-once bookkeeping and response pacing still apply to it.
        /// </para>
        /// </summary>
        public static class Peek
        {
            /// <summary>
            /// The same human-plausibility pacing every responder call site applies before acting.
            /// Measuring the CACHED enemy's cast progress matches responder behaviour for lock-on
            /// detections too: an unrelated cast early in its bar holds those back at the responder
            /// call sites, so it holds the peek back the same way.
            /// </summary>
            private static bool ResponseDelayElapsed()
            {
                return HodlCastTimeRemaining(hodlTillDurationInPct: DebugSettings.Instance.FightLogicResponseDelay);
            }

            /// <summary>The tank about to be hit by a catalogued tankbuster (falling back to shared busters), or null.</summary>
            public static Character EnemyIsCastingTankBuster()
            {
                if (!ResponseDelayElapsed())
                    return null;

                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemyLogic?.TankBusters == null || enemy == null || encounter == null)
                    return EnemyIsCastingSharedTankBuster();

                return MatchTankBuster(enemyLogic, enemy);
            }

            /// <summary>The reachable tank taking a catalogued shared tankbuster, or null.</summary>
            public static Character EnemyIsCastingSharedTankBuster()
            {
                if (!ResponseDelayElapsed())
                    return null;

                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemyLogic?.SharedTankBusters == null || enemy == null || encounter == null)
                    return null;

                return MatchSharedTankBuster(enemyLogic, enemy);
            }

            /// <summary>Whether a catalogued AoE is incoming — a matching cast, an encounter AoE lock-on, or (when enabled) a common lock-on.</summary>
            public static bool EnemyIsCastingAoe()
            {
                if (!ResponseDelayElapsed())
                    return false;

                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemy != null && enemyLogic?.Aoes != null && encounter != null && MatchAoe(enemyLogic, enemy))
                    return true;

                if (enemyLogic?.AoeLockOns != null && CheckAoeLockOns(enemyLogic.AoeLockOns).found)
                    return true;

                if (DebugSettings.Instance.FightLogicIncludeCommonAoeLockOnsTest && CheckAoeLockOns(CommonAoeLockOns).found)
                    return true;

                return false;
            }

            /// <summary>Whether a catalogued big AoE is incoming (falling back to <see cref="EnemyIsCastingAoe"/> for enemies with no big-AoE list).</summary>
            public static bool EnemyIsCastingBigAoe()
            {
                if (!ResponseDelayElapsed())
                    return false;

                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemyLogic == null || enemy == null || encounter == null)
                    return false;

                if (enemyLogic.BigAoes == null)
                    return EnemyIsCastingAoe();

                return MatchBigAoe(enemyLogic, enemy);
            }

            /// <summary>Whether a catalogued knockback is incoming.</summary>
            public static bool EnemyIsCastingKnockback()
            {
                if (!ResponseDelayElapsed())
                    return false;

                var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

                if (enemyLogic?.Knockbacks == null || enemy == null || encounter == null)
                    return false;

                return MatchKnockback(enemyLogic, enemy);
            }
        }

        public static bool ZoneHasFightLogic()
        {
            if (!DebugSettings.Instance.UseFightLogic)
                return false;

            if (!Globals.InActiveDuty)
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

            if (!Globals.InActiveDuty)
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
                    Debug.Instance.FightLogicData += $"\nCurrent Target: {currentTarget} ({npcId})\n\n\n";

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
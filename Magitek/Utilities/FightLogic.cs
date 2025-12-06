using Clio.Utilities;
using ff14bot;
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

            var output = enemyLogic.TankBusters.Contains(enemy.CastingSpellId)
                ? Group.CastableTanks.FirstOrDefault(x => x == enemy.TargetCharacter)
                : null;

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

            var output = enemyLogic.SharedTankBusters.Contains(enemy.CastingSpellId)
                ? Group.CastableTanks.FirstOrDefault(x => x != enemy.TargetCharacter)
                : null;

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
                return GetEnemyLogicAndEnemyCached;

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
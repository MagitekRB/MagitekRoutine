using Clio.Utilities;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Utilities.Collections;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Debug = Magitek.ViewModels.Debug;
using DebugSettings = Magitek.Models.Account.BaseSettings;
using Generic = System.Collections.Generic;

namespace Magitek.Utilities
{
    public static class FightLogic
    {
        private static readonly Stopwatch FlStopwatch = new Stopwatch();

        private static readonly Stopwatch GetEnemyLogicAndEnemyCacheAge = new Stopwatch();

        private static Generic.HashSet<uint> FlHandledCastingSpellId = new Generic.HashSet<uint>();

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

        public static bool EnemyIsCastingAoe()
        {
            if (!IsFlReady)
                return false;

            var (encounter, enemyLogic, enemy) = GetEnemyLogicAndEnemy();

            if (enemyLogic?.Aoes == null || enemy == null || encounter == null)
                return false;

            if (FlHandledCastingSpellId.Contains(enemy.CastingSpellId))
                return false;
            FlHandledCastingSpellId.Clear();

            var output = enemyLogic.Aoes.Contains(enemy.CastingSpellId);

            if (output && DebugSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[AOE Detected] {encounter.Name} {enemy.Name} casting {enemy.SpellCastInfo.Name}");

            if (!output && enemyLogic.AoeLockOns != null)
            {
                var detectedLockOn = Core.Me.VfxContainer.LockOns.FirstOrDefault(lockOn => enemyLogic.AoeLockOns.Contains(lockOn.Id));
                output = detectedLockOn != null;

                if (!output)
                {
                    foreach (var partyMember in Group.CastableAlliesWithin30)
                    {
                        detectedLockOn = partyMember.VfxContainer.LockOns.FirstOrDefault(lockOn => enemyLogic.AoeLockOns.Contains(lockOn.Id));
                        output = detectedLockOn != null;
                        if (output)
                            break;
                    }
                }

                if (output && DebugSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Lock On Detected] {encounter.Name} {enemy.Name} lockon {detectedLockOn.Id}");
            }

            return output;
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

        public static bool ZoneHasFightLogic()
        {
            if (!DebugSettings.Instance.UseFightLogic)
                return false;

            if (!Globals.InActiveDuty)
                return false;

            if (!Core.Me.InCombat)
                return false;

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

                return (enemyLogic?.Aoes != null || enemyLogic?.BigAoes != null || enemyLogic?.AoeLockOns != null);
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

            if (enemyLogic == null)
                return SetAndReturn();

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
}
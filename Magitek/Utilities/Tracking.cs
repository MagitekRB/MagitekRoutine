using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Debugging;
using Magitek.Utilities.Routines;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BaseSettings = Magitek.Models.Account.BaseSettings;
using Debug = Magitek.ViewModels.Debug;

namespace Magitek.Utilities
{
    internal static class Tracking
    {
        public static readonly List<EnemyInfo> EnemyInfos = new List<EnemyInfo>();
        private static List<BattleCharacter> _enemyCache = new List<BattleCharacter>();

        private static bool mWasInCombat;

        // HashSet for quick lookups of known spells and lockons
        private static HashSet<Tuple<uint, uint>> KnownFightLogicTBs = new HashSet<Tuple<uint, uint>>();
        private static HashSet<Tuple<uint, uint>> KnownFightLogicAOEs = new HashSet<Tuple<uint, uint>>();
        private static HashSet<Tuple<uint, uint>> KnownFightLogicLockOns = new HashSet<Tuple<uint, uint>>();
        private static HashSet<Tuple<uint, uint>> KnownFightLogicKnockbacks = new HashSet<Tuple<uint, uint>>();
        private static bool IsInitialized = false;

        private static void InitializeKnownData()
        {
            if (IsInitialized)
                return;

            foreach (var encounter in FightLogicEncounters.Encounters)
            {
                foreach (var enemy in encounter.Enemies)
                {
                    // Add tank busters
                    if (enemy.TankBusters != null)
                    {
                        foreach (var spellId in enemy.TankBusters)
                        {
                            KnownFightLogicTBs.Add(new Tuple<uint, uint>(spellId, enemy.Id));
                        }
                    }

                    // Add shared tank busters
                    if (enemy.SharedTankBusters != null)
                    {
                        foreach (var spellId in enemy.SharedTankBusters)
                        {
                            KnownFightLogicTBs.Add(new Tuple<uint, uint>(spellId, enemy.Id));
                        }
                    }

                    // Add AOEs
                    if (enemy.Aoes != null)
                    {
                        foreach (var spellId in enemy.Aoes)
                        {
                            KnownFightLogicAOEs.Add(new Tuple<uint, uint>(spellId, enemy.Id));
                        }
                    }

                    // Add big AOEs
                    if (enemy.BigAoes != null)
                    {
                        foreach (var spellId in enemy.BigAoes)
                        {
                            KnownFightLogicAOEs.Add(new Tuple<uint, uint>(spellId, enemy.Id));
                        }
                    }

                    // Add knockbacks
                    if (enemy.Knockbacks != null)
                    {
                        foreach (var spellId in enemy.Knockbacks)
                        {
                            KnownFightLogicKnockbacks.Add(new Tuple<uint, uint>(spellId, enemy.Id));
                        }
                    }

                    // Add lockons
                    if (enemy.AoeLockOns != null)
                    {
                        foreach (var lockOnId in enemy.AoeLockOns)
                        {
                            KnownFightLogicLockOns.Add(new Tuple<uint, uint>(lockOnId, enemy.Id));
                        }
                    }
                }
            }

            IsInitialized = true;
        }

        public static void Update()
        {
            UpdateCurrentPosition();

            if (Globals.InActiveDuty)
            {
                if (BotManager.Current.IsAutonomous)
                {
                    //In an instance, autonomous
                    //We should be a little less aggressive than if we're human-controlled, so just track attackers
                    _enemyCache = GameObjectManager.Attackers.ToList();
                }
                else
                {
                    //In an instance, not autonomous
                    //Track every tagged or aggroed enemy in the vicinity
                    _enemyCache = GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(r => (r.TaggerType > 0
                                                                                                    || r.HasTarget
                                                                                                    || (r.IsBoss() && Core.Me.InCombat))
                                                                                                   && Core.Me.Distance(r) < 50)
                                                                                       .ToList();
                }
            }
            else
            {
                // Track the attacker list combined with every aggro'd Fate Mob
                var _fateEnemyCache = GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(r =>
                    r.HasTarget
                    && r.IsFate
                    && Core.Me.Distance(r) < 50
                    && !GameObjectManager.GetObjectsOfType<BattleCharacter>().Any(x => x.IsNpc && x.ObjectId == r.TaggerObjectId)
                );
                _enemyCache = GameObjectManager.Attackers.Union(_fateEnemyCache).ToList();
            }

            Combat.Enemies.Clear();

            foreach (var unit in _enemyCache)
            {
                if (!unit.ValidAttackUnit())
                    continue;

                if (!unit.NotInvulnerable())
                    continue;

                if (!unit.IsTargetable)
                    continue;

                if (BaseSettings.Instance.EnemySpellCastHistory)
                {
                    UpdateEnemyCastedSpells(unit);
                }

                if (BaseSettings.Instance.EnemyAuraHistory)
                {
                    UpdateEnemyAuraHistory(unit);
                }

                if (BaseSettings.Instance.EnemyTargetHistory)
                {
                    UpdateEnemyTargetHistory(unit);
                }

                Combat.Enemies.Add(unit);

                #region Dps Infos
                if (EnemyInfos.All(r => r.Unit != unit))
                {
                    var info = new EnemyInfo()
                    {
                        Unit = unit,
                        CombatStart = DateTime.Now,
                        StartHealth = unit.CurrentHealth,
                        Location = unit.Location,
                        IsMoving = false,
                        IsMovingChange = new Stopwatch()
                    };

                    info.IsMovingChange.Start();

                    EnemyInfos.Add(info);

                    if (BaseSettings.Instance.DebugEnemyInfo)
                    {
                        Logger.WriteInfo($@"[Debug] Adding {info.Unit} To Enemy Infos");
                    }
                }
                else
                {
                    var info = EnemyInfos.FirstOrDefault(r => r.Unit == unit);

                    if (info == null)
                        continue;

                    if (info.Unit == null)
                        continue;

                    info.CurrentDps = (info.StartHealth - unit.CurrentHealth) / (DateTime.Now - info.CombatStart).TotalSeconds;

                    var unitLocation = unit.Location;
                    var isNowMoving = info.Location != unitLocation;
                    if (isNowMoving != info.IsMoving) info.IsMovingChange.Restart();
                    info.IsMoving = isNowMoving;
                    info.Location = unitLocation;

                    if (info.Unit.CurrentHealthPercent > info.LastTickHealth + 2f)
                    {
                        info.CombatStart = DateTime.Now;
                        info.StartHealth = info.Unit.CurrentHealth;
                    }

                    info.CombatTimeLeft = new TimeSpan(0, 0, (int)(unit.CurrentHealth / info.CurrentDps)).TotalSeconds;
                    info.LastTickHealth = info.Unit.CurrentHealthPercent;
                }
                #endregion
            }

            // Update LockOn history
            UpdateLockOnHistory();

            if (Core.Me.HasTarget && Core.Me.CurrentTarget.ValidAttackUnit())
            {
                Combat.CurrentTargetCombatTimeLeft = Core.Me.CurrentTarget.CombatTimeLeft();
            }

            if (!EnemyInfos.Any())
            {
                Combat.CombatTotalTimeLeft = 0;
            }
            else
            {
                Combat.CombatTotalTimeLeft = (int)Math.Max(0, EnemyInfos.Select(r => r.CombatTimeLeft).Sum());
            }


            var removeDpsUnits = EnemyInfos.Where(r => !_enemyCache.Contains(r.Unit) || r.Unit == null || r.Unit.HasAnyAura(Auras.Invincibility) || r.Unit.IsDead || !r.Unit.IsValid).ToArray();

            foreach (var unit in removeDpsUnits)
            {
                EnemyInfos.Remove(unit);

                if (BaseSettings.Instance.DebugEnemyInfo)
                {
                    Logger.WriteInfo($@"[Debug] Removing {unit.Unit} From Enemy Infos");
                }
            }

            Debug.Instance.Enemies = new ObservableCollection<EnemyInfo>(EnemyInfos);

            StunTracker.Update(Combat.Enemies);

            if (Core.Me.InCombat) mWasInCombat = true;
            if (!Core.Me.InCombat && mWasInCombat)
            {
                mWasInCombat = false;
            }
        }

        private static void UpdateCurrentPosition()
        {
            var currentTarget = Core.Me.CurrentTarget;

            if (currentTarget == null)
            {
                if (ViewModels.BaseSettings.Instance.CurrentPosition != PositionalState.None)
                {
                    ViewModels.BaseSettings.Instance.CurrentPosition = PositionalState.None;
                }
                return;
            }

            if (currentTarget.IsBehind)
            {
                if (ViewModels.BaseSettings.Instance.CurrentPosition != PositionalState.Rear)
                {
                    ViewModels.BaseSettings.Instance.CurrentPosition = PositionalState.Rear;
                }
                return;
            }

            if (currentTarget.IsFlanking)
            {
                if (ViewModels.BaseSettings.Instance.CurrentPosition != PositionalState.Flank)
                {
                    ViewModels.BaseSettings.Instance.CurrentPosition = PositionalState.Flank;
                }
                return;
            }

            if (!currentTarget.IsFlanking && !currentTarget.IsBehind)
            {
                if (ViewModels.BaseSettings.Instance.CurrentPosition != PositionalState.Front)
                {
                    ViewModels.BaseSettings.Instance.CurrentPosition = PositionalState.Front;
                }
            }
        }

        private static void UpdateEnemyCastedSpells(Character unit)
        {
            if (!unit.IsCasting)
                return;

            if (Debug.Instance.EnemySpellCasts.ContainsKey(unit.CastingSpellId))
                return;

            var newSpellCast = new EnemySpellCastInfo(unit.SpellCastInfo.Name, unit.CastingSpellId, unit.Name, unit.NpcId, WorldManager.ZoneId, WorldManager.CurrentZoneName);

            // Initialize the lookup data if needed
            InitializeKnownData();

            // Check if this spell exists in any FightLogic encounter as a tank buster
            if (KnownFightLogicTBs.Contains(new Tuple<uint, uint>(unit.CastingSpellId, unit.NpcId)))
            {
                newSpellCast.InFightLogicBuilderTB = "[-] FightLogic TB";

                // Add to the TB FightLogic builder collection
                if (!Debug.Instance.FightLogicBuilderTB.Contains(newSpellCast))
                {
                    Debug.Instance.FightLogicBuilderTB.Add(newSpellCast);
                }
            }
            // Check if this spell exists in any FightLogic encounter as an AOE
            else if (KnownFightLogicAOEs.Contains(new Tuple<uint, uint>(unit.CastingSpellId, unit.NpcId)))
            {
                newSpellCast.InFightLogicBuilderAOE = "[-] FightLogic AOE";

                // Add to the AOE FightLogic builder collection
                if (!Debug.Instance.FightLogicBuilderAOE.Contains(newSpellCast))
                {
                    Debug.Instance.FightLogicBuilderAOE.Add(newSpellCast);
                }
            }

            // Check if this spell exists in any FightLogic encounter as a knockback
            else if (KnownFightLogicKnockbacks.Contains(new Tuple<uint, uint>(unit.CastingSpellId, unit.NpcId)))
            {
                newSpellCast.InFightLogicBuilderKnockback = "[-] FightLogic KB";

                // Add to the Knockback FightLogic builder collection
                if (!Debug.Instance.FightLogicBuilderKnockbacks.Contains(newSpellCast))
                {
                    Debug.Instance.FightLogicBuilderKnockbacks.Add(newSpellCast);
                }
            }

            Logger.WriteInfo($@"[Debug] Adding [{newSpellCast.Id}] {newSpellCast.Name} To Enemy Spell Casts");
            Debug.Instance.EnemySpellCasts.Add(newSpellCast.Id, newSpellCast);
        }

        private static void UpdateEnemyAuraHistory(Character unit)
        {
            foreach (var aura in unit.CharacterAuras)
            {
                if (Debug.Instance.EnemyAuras.ContainsKey(aura.Id))
                    continue;

                var newAura = new TargetAuraInfo(aura.Name, aura.Id, unit.Name);
                Logger.WriteInfo($@"[Debug] Adding {aura.Name} To Enemy Aura History");
                Debug.Instance.EnemyAuras.Add(aura.Id, newAura);
            }
        }

        private static void UpdateEnemyTargetHistory(Character unit)
        {
            if (Debug.Instance.EnemyTargetHistory.ContainsKey(unit.NpcId))
                return;

            var newEnemyTargetInfo = new EnemyTargetInfo(unit.Name, unit.NpcId, WorldManager.ZoneId, unit.MaxHealth, unit.ClassLevel);
            Logger.WriteInfo($@"[Debug] Adding {newEnemyTargetInfo.Name} To Enemy Target History");
            Debug.Instance.EnemyTargetHistory.Add(newEnemyTargetInfo.Id, newEnemyTargetInfo);
        }

        private static void UpdateLockOnHistory()
        {
            if (!BaseSettings.Instance.EnemySpellCastHistory)
                return;

            // Only track LockOns in dungeon duty zones
            if (!Globals.InActiveDuty || Globals.OnPvpMap)
                return;

            // Initialize the lookup data if needed
            InitializeKnownData();

            // Get enemy with highest HP
            var highestHpEnemy = _enemyCache.OrderByDescending(e => e.MaxHealth).FirstOrDefault();
            var castedById = highestHpEnemy?.NpcId ?? 0;

            // Check for LockOns on the player
            foreach (var lockOn in Core.Me.VfxContainer.LockOns)
            {
                var targetedPlayerName = Core.Me.Name;

                // Check for duplicates based on LockOn ID, CastedById, and TargetedPlayerName
                if (Debug.Instance.LockOnHistory.ContainsKey(new Tuple<uint, uint, string>(lockOn.Id, castedById, targetedPlayerName)))
                    continue;

                var newLockOn = new LockOnInfo(lockOn.Id, highestHpEnemy?.Name, WorldManager.ZoneId, WorldManager.CurrentZoneName);
                newLockOn.CastedById = castedById;
                newLockOn.TargetedPlayerName = targetedPlayerName;

                // Check if this lockon exists in any FightLogic encounter
                if (KnownFightLogicLockOns.Contains(new Tuple<uint, uint>(lockOn.Id, castedById)))
                {
                    newLockOn.InFightLogicBuilder = "[-] FightLogic LockOn";

                    // Add to the FightLogic builder collection
                    if (!Debug.Instance.FightLogicBuilderLockOns.Contains(newLockOn))
                    {
                        Debug.Instance.FightLogicBuilderLockOns.Add(newLockOn);
                    }
                }

                Logger.WriteInfo($@"[Debug] Adding LockOn [{newLockOn.Id}] from {newLockOn.CastedByName} targeting to LockOn History");
                Debug.Instance.LockOnHistory.Add(new Tuple<uint, uint, string>(newLockOn.Id, castedById, targetedPlayerName), newLockOn);
            }

            // Check for LockOns on party members
            foreach (var partyMember in Group.CastableAlliesWithin50)
            {
                if (partyMember == null || !partyMember.IsValid)
                    continue;

                var battleCharacter = partyMember;
                if (battleCharacter == null)
                    continue;

                foreach (var lockOn in battleCharacter.VfxContainer.LockOns)
                {
                    var targetedPlayerName = battleCharacter.Name;

                    // Check for duplicates based on LockOn ID, CastedById, and TargetedPlayerName
                    if (Debug.Instance.LockOnHistory.ContainsKey(new Tuple<uint, uint, string>(lockOn.Id, castedById, targetedPlayerName)))
                        continue;

                    var newLockOn = new LockOnInfo(lockOn.Id, highestHpEnemy?.Name, WorldManager.ZoneId, WorldManager.CurrentZoneName);
                    newLockOn.CastedById = castedById;
                    newLockOn.TargetedPlayerName = targetedPlayerName;

                    // Check if this lockon exists in any FightLogic encounter
                    if (KnownFightLogicLockOns.Contains(new Tuple<uint, uint>(lockOn.Id, castedById)))
                    {
                        newLockOn.InFightLogicBuilder = "[-] FightLogic LockOn";

                        // Add to the FightLogic builder collection
                        if (!Debug.Instance.FightLogicBuilderLockOns.Contains(newLockOn))
                        {
                            Debug.Instance.FightLogicBuilderLockOns.Add(newLockOn);
                        }
                    }

                    Logger.WriteInfo($@"[Debug] Adding LockOn [{newLockOn.Id}] from {newLockOn.CastedByName} targeting to LockOn History");
                    Debug.Instance.LockOnHistory.Add(new Tuple<uint, uint, string>(newLockOn.Id, castedById, targetedPlayerName), newLockOn);
                }
            }
        }
    }
}

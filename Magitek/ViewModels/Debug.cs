using Buddy.Overlay.Commands;
using Clio.Utilities.Collections;
using ff14bot.Objects;
using Magitek.Models;
using Magitek.Models.Debugging;
using Magitek.Models.QueueSpell;
using Magitek.Utilities;
using Magitek.Utilities.Collections;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Debug
    {
        private static Debug _instance;
        public static Debug Instance => _instance ?? (_instance = new Debug());

        public Models.Account.BaseSettings Settings => Models.Account.BaseSettings.Instance;

        public float ActionLock { get; set; }

        public bool ActionQueued { get; set; }

        public bool CastingHeal { get; set; }
        public SpellData CastingSpell { get; set; }
        public SpellData LastSpell { get; set; }
        public GameObject LastSpellTarget { get; set; }
        public GameObject SpellTarget { get; set; }
        public bool DoHealthChecks { get; set; }
        public bool NeedAura { get; set; }
        public bool CastingGambit { get; set; }
        public uint Aura { get; set; }
        public bool UseRefreshTime { get; set; }
        public int RefreshTime { get; set; }
        public string CastingTime { get; set; }
        public long InCombatTime { get; set; }
        public int InCombatTimeLeft { get; set; }
        public int OutOfCombatTime { get; set; }
        public int InCombatMovingTime { get; set; }
        public int NotMovingInCombatTime { get; set; }
        public double TargetCombatTimeLeft { get; set; }
        public Duty.States DutyState { get; set; } = Duty.States.NotInDuty;
        public long DutyTime { get; set; }
        public string IsBoss { get; set; }

        public string FightLogicData { get; set; }

        public ObservableCollection<EnemyInfo> Enemies { get; set; } = new AsyncObservableCollection<EnemyInfo>();
        public ConcurrentObservableDictionary<uint, EnemySpellCastInfo> EnemySpellCasts { get; set; } = new ConcurrentObservableDictionary<uint, EnemySpellCastInfo>();
        public ConcurrentObservableDictionary<uint, TargetAuraInfo> EnemyAuras { get; set; } = new ConcurrentObservableDictionary<uint, TargetAuraInfo>();
        public ConcurrentObservableDictionary<uint, EnemyTargetInfo> EnemyTargetHistory { get; set; } = new ConcurrentObservableDictionary<uint, EnemyTargetInfo>();
        public ConcurrentObservableDictionary<uint, TargetAuraInfo> PartyMemberAuras { get; set; } = new ConcurrentObservableDictionary<uint, TargetAuraInfo>();
        public ObservableCollection<QueueSpell> Queue { get; set; } = new AsyncObservableCollection<QueueSpell>();
        public AsyncObservableCollection<Enmity> Enmity { get; set; } = new AsyncObservableCollection<Enmity>();
        public ObservableCollection<GameObject> CastableWithin10 { get; set; } = new ObservableCollection<GameObject>();
        public ObservableCollection<GameObject> CastableWithin15 { get; set; } = new ObservableCollection<GameObject>();
        public ObservableCollection<GameObject> CastableWithin30 { get; set; } = new ObservableCollection<GameObject>();
        public System.Collections.Generic.List<SpellCastHistoryItem> SpellCastHistory { get; set; } = new System.Collections.Generic.List<SpellCastHistoryItem>();

        public ObservableCollection<EnemySpellCastInfo> FightLogicBuilderTB { get; set; } = new ObservableCollection<EnemySpellCastInfo>();
        public ObservableCollection<EnemySpellCastInfo> FightLogicBuilderAOE { get; set; } = new ObservableCollection<EnemySpellCastInfo>();
        public ObservableCollection<LockOnInfo> FightLogicBuilderLockOns { get; set; } = new ObservableCollection<LockOnInfo>();
        public ConcurrentObservableDictionary<Tuple<uint, uint, string>, LockOnInfo> LockOnHistory { get; set; } = new ConcurrentObservableDictionary<Tuple<uint, uint, string>, LockOnInfo>();

        public ICommand CopyFightLogicBuilderCommand { get; } = new RelayCommand(CopyFightLogicBuilder);

        private static void CopyFightLogicBuilder(object parameter)
        {
            var instance = Instance;
            if (instance == null) return;

            // Combine and group by unique enemy names
            var combinedList = instance.FightLogicBuilderTB.Concat(instance.FightLogicBuilderAOE)
                                .GroupBy(e => e.CastedBy);

            if (combinedList == null || !combinedList.Any()) return;

            // Example hardcoded beginning
            var sb = new StringBuilder();
            sb.AppendLine("new Encounter {");
            sb.AppendLine($"    ZoneId = {combinedList.First().First().ZoneId},");
            sb.AppendLine($"    Name = \"{combinedList.First().First().ZoneName}\",");
            sb.AppendLine($"    Expansion = FfxivExpansion.Dawntrail,");
            sb.AppendLine("    Enemies = new List<Enemy> {");

            foreach (var enemyGroup in combinedList)
            {
                var enemy = enemyGroup.First();
                sb.AppendLine("        new Enemy {");
                sb.AppendLine($"            Id = {enemy.CastedById},");
                sb.AppendLine($"            Name = \"{enemy.CastedBy}\",");

                // TankBusters
                var tankBusters = enemyGroup.Where(e => instance.FightLogicBuilderTB.Contains(e));
                if (tankBusters.Any())
                {
                    sb.AppendLine("            TankBusters = new List<uint> {");
                    foreach (var tb in tankBusters)
                    {
                        sb.AppendLine($"                {tb.Id}, // {tb.Name}");
                    }
                    sb.AppendLine("            },");
                }
                else
                {
                    sb.AppendLine("            TankBusters = null,");
                }

                // AOEs
                var aoes = enemyGroup.Where(e => instance.FightLogicBuilderAOE.Contains(e));
                if (aoes.Any())
                {
                    sb.AppendLine("            Aoes = new List<uint> {");
                    foreach (var aoe in aoes)
                    {
                        sb.AppendLine($"                {aoe.Id}, // {aoe.Name}");
                    }
                    sb.AppendLine("            },");
                }
                else
                {
                    sb.AppendLine("            Aoes = null,");
                }

                // LockOns
                var lockOns = instance.FightLogicBuilderLockOns.Where(l => l.CastedById == enemy.CastedById);
                if (lockOns.Any())
                {
                    sb.AppendLine("            AoeLockOns = new List<uint> {");
                    foreach (var lockOn in lockOns)
                    {
                        sb.AppendLine($"                {lockOn.Id},");
                    }
                    sb.AppendLine("            },");
                }
                else
                {
                    sb.AppendLine("            AoeLockOns = null,");
                }

                sb.AppendLine("            SharedTankBusters = null,");
                sb.AppendLine("            BigAoes = null");
                sb.AppendLine("        },");
            }

            sb.AppendLine("    }");
            sb.AppendLine("},");

            try
            {
                Clipboard.SetDataObject(sb.ToString());
            }
            catch
            {
                Logger.Error("Failed to copy to clipboard");
            }
        }

        public ICommand ClearFightLogicBuilderCommand { get; } = new RelayCommand(ClearFightLogicBuilder);

        private static void ClearFightLogicBuilder(object parameter)
        {
            var instance = Instance;
            if (instance == null) return;

            foreach (var x in instance.FightLogicBuilderTB)
            {
                x.InFightLogicBuilderTB = "[+] FightLogic TB";
            }
            foreach (var x in instance.FightLogicBuilderAOE)
            {
                x.InFightLogicBuilderAOE = "[+] FightLogic AOE";
            }
            foreach (var x in instance.FightLogicBuilderLockOns)
            {
                x.InFightLogicBuilder = "[+] FightLogic LockOn";
            }

            instance.FightLogicBuilderTB.Clear();
            instance.FightLogicBuilderAOE.Clear();
            instance.FightLogicBuilderLockOns.Clear();
        }

        public ICommand ClearEnemySpellCastsCommand { get; } = new RelayCommand(ClearEnemySpellCasts);

        private static void ClearEnemySpellCasts(object parameter)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.EnemySpellCasts.Clear();
            instance.LockOnHistory.Clear();
            instance.FightLogicBuilderTB.Clear();
            instance.FightLogicBuilderAOE.Clear();
            instance.FightLogicBuilderLockOns.Clear();
            Logger.WriteInfo("[Debug] Cleared Enemy Spell Casts History");
        }
    }
}

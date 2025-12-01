using Clio.Utilities.Collections;
using Magitek.Commands;
using Magitek.Enumerations;
using Magitek.Models.Astrologian;
using Magitek.Models.Bard;
using Magitek.Models.BlackMage;
using Magitek.Models.BlueMage;
using Magitek.Models.Dancer;
using Magitek.Models.DarkKnight;
using Magitek.Models.Dragoon;
using Magitek.Models.Gunbreaker;
using Magitek.Models.Machinist;
using Magitek.Models.Monk;
using Magitek.Models.Ninja;
using Magitek.Models.Paladin;
using Magitek.Models.Pictomancer;
using Magitek.Models.Reaper;
using Magitek.Models.RedMage;
using Magitek.Models.Sage;
using Magitek.Models.Samurai;
using Magitek.Models.Scholar;
using Magitek.Models.Summoner;
using Magitek.Models.Viper;
using Magitek.Models.Warrior;
using Magitek.Models.WhiteMage;
using Magitek.Toggles;
using Magitek.Utilities.Overlays;
using Magitek.Views;
using PropertyChanged;
using System.Windows.Input;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class BaseSettings
    {
        private static BaseSettings _instance;
        public static BaseSettings Instance => _instance ?? (_instance = new BaseSettings());

        public ICommand ShowSettingsModal => new DelegateCommand(() =>
        {
            Magitek.Form.ShowModal(new SettingsModal());
        });

        public ICommand ResetOverlayPositions => new DelegateCommand(() =>
        {
            Models.Account.BaseSettings.ResetOverlayPositions();

            OverlayManager.RestartMainOverlay();
            OverlayManager.RestartCombatMessageOverlay();
            OverlayManager.RestartPvpAggroCountOverlay();
        });

        public ICommand RestartOverlay => new DelegateCommand(() =>
        {
            OverlayManager.RestartMainOverlay();
        });

        public ICommand ReloadOverlay => new DelegateCommand(() =>
        {
            TogglesManager.LoadTogglesForCurrentJob();
        });

        public ICommand RestartCombatMessageOverlay => new DelegateCommand(() =>
        {
            OverlayManager.RestartCombatMessageOverlay();
        });

        public ICommand RestartPvpAggroCountOverlay => new DelegateCommand(() =>
        {
            OverlayManager.RestartPvpAggroCountOverlay();
        });

        public AsyncObservableCollection<double> FontSizes { get; set; } = new AsyncObservableCollection<double>() { 9, 10, 11, 12 };

        public Models.Account.BaseSettings GeneralSettings => Models.Account.BaseSettings.Instance;

        // Properties always return current Instance to survive hot reload
        // Using expression-bodied members for minimal overhead (static property access is ~2ns)
        public ScholarSettings ScholarSettings
        {
            get => ScholarSettings.Instance;
            set => ScholarSettings.Instance = value;
        }
        public WhiteMageSettings WhiteMageSettings
        {
            get => WhiteMageSettings.Instance;
            set => WhiteMageSettings.Instance = value;
        }
        public AstrologianSettings AstrologianSettings
        {
            get => AstrologianSettings.Instance;
            set => AstrologianSettings.Instance = value;
        }
        public PaladinSettings PaladinSettings
        {
            get => PaladinSettings.Instance;
            set => PaladinSettings.Instance = value;
        }
        public DarkKnightSettings DarkKnightSettings
        {
            get => DarkKnightSettings.Instance;
            set => DarkKnightSettings.Instance = value;
        }
        public WarriorSettings WarriorSettings
        {
            get => WarriorSettings.Instance;
            set => WarriorSettings.Instance = value;
        }
        public BardSettings BardSettings
        {
            get => BardSettings.Instance;
            set => BardSettings.Instance = value;
        }
        public DancerSettings DancerSettings
        {
            get => DancerSettings.Instance;
            set => DancerSettings.Instance = value;
        }
        public MachinistSettings MachinistSettings
        {
            get => MachinistSettings.Instance;
            set => MachinistSettings.Instance = value;
        }
        public DragoonSettings DragoonSettings
        {
            get => DragoonSettings.Instance;
            set => DragoonSettings.Instance = value;
        }
        public MonkSettings MonkSettings
        {
            get => MonkSettings.Instance;
            set => MonkSettings.Instance = value;
        }
        public NinjaSettings NinjaSettings
        {
            get => NinjaSettings.Instance;
            set => NinjaSettings.Instance = value;
        }
        public SamuraiSettings SamuraiSettings
        {
            get => SamuraiSettings.Instance;
            set => SamuraiSettings.Instance = value;
        }
        public BlueMageSettings BlueMageSettings
        {
            get => BlueMageSettings.Instance;
            set => BlueMageSettings.Instance = value;
        }
        public BlackMageSettings BlackMageSettings
        {
            get => BlackMageSettings.Instance;
            set => BlackMageSettings.Instance = value;
        }
        public RedMageSettings RedMageSettings
        {
            get => RedMageSettings.Instance;
            set => RedMageSettings.Instance = value;
        }
        public SummonerSettings SummonerSettings
        {
            get => SummonerSettings.Instance;
            set => SummonerSettings.Instance = value;
        }
        public GunbreakerSettings GunbreakerSettings
        {
            get => GunbreakerSettings.Instance;
            set => GunbreakerSettings.Instance = value;
        }
        public ReaperSettings ReaperSettings
        {
            get => ReaperSettings.Instance;
            set => ReaperSettings.Instance = value;
        }
        public SageSettings SageSettings
        {
            get => SageSettings.Instance;
            set => SageSettings.Instance = value;
        }
        public ViperSettings ViperSettings
        {
            get => ViperSettings.Instance;
            set => ViperSettings.Instance = value;
        }
        public PictomancerSettings PictomancerSettings
        {
            get => PictomancerSettings.Instance;
            set => PictomancerSettings.Instance = value;
        }
        public string CurrentRoutine { get; set; }
        public string RoutineSelectedInUi { get; set; }
        public bool SettingsFirstInitialization { get; set; }
        public bool InPvp { get; set; }

        public string PositionalText { get; set; } = "Positional!";
        public string PositionalStatus { get; set; } = "Neutral";

        public PositionalState CurrentPosition { get; set; } = PositionalState.None;
        public PositionalState ExpectedPosition { get; set; } = PositionalState.None;
        public PositionalState UpcomingPosition { get; set; } = PositionalState.None;
    }
}

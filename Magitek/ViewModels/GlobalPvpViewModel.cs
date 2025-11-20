using PropertyChanged;
using System.ComponentModel;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class GlobalPvpViewModel
    {
        private static GlobalPvpViewModel _instance;
        public static GlobalPvpViewModel Instance => _instance ?? (_instance = new GlobalPvpViewModel());

        // Match the pattern used in other ViewModels (expose Models.Account.BaseSettings via property)
        public Models.Account.BaseSettings GeneralSettings { get; set; } = Models.Account.BaseSettings.Instance;

        public GlobalPvpViewModel()
        {
            // Listen for property changes to save settings and re-register hotkey
            GeneralSettings.PropertyChanged += GeneralSettings_PropertyChanged;
        }

        private void GeneralSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Save immediately when any property changes
            GeneralSettings.Save();

            // Re-register hotkey when hotkey properties change
            if (e.PropertyName == nameof(GeneralSettings.HoldPvpBurstKey) ||
                e.PropertyName == nameof(GeneralSettings.HoldPvpBurstModkey))
            {
                Magitek.RegisterHoldPvpBurstHotkey();
            }
        }
    }
}


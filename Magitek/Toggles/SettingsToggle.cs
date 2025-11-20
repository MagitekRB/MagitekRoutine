using ff14bot.Helpers;
using ff14bot.Managers;
using Magitek.Commands;
using Magitek.Extensions;
using Magitek.Utilities;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace Magitek.Toggles
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsToggle : INotifyPropertyChanged
    {
        private Keys _toggleKey;
        private ModifierKeys _toggleModifierKey;

        // Text that's displayed on the toggle
        public string ToggleText { get; set; }

        // Job the toggle belongs to
        public string ToggleJob { get; set; }

        // Show toggle on overlay?
        public bool ToggleShowOnOverlay { get; set; } = true;

        // Is PVP Toggle ?
        public bool IsPvpToggle { get; set; } = false;

        // Property that's bound to the IsChecked on the toggle
        public bool ToggleChecked { get; set; }

        // Collection of setting values
        public ObservableCollection<SettingsToggleSetting> Settings { get; set; }

        // Command that is bound to the Command (XAML) on the toggle
        public ICommand ExecuteToggleCommand => new DelegateCommand(ExecuteToggle);

        // Hotkey for the toggle
        public Keys ToggleKey
        {
            get => _toggleKey;
            set
            {
                if (_toggleKey != value)
                {
                    _toggleKey = value;
                    OnPropertyChanged(nameof(ToggleKey));
                }
            }
        }

        // Modifier key for the toggle
        public ModifierKeys ToggleModifierKey
        {
            get => _toggleModifierKey;
            set
            {
                if (_toggleModifierKey != value)
                {
                    _toggleModifierKey = value;
                    OnPropertyChanged(nameof(ToggleModifierKey));
                }
            }
        }

        public void SetToggleState()
        {
            // Sets the state of this current toggle to checked or unchecked
            if (Settings == null || !Settings.Any())
            {
                Logging.Write($@"No Settings In Toggle: {ToggleText}");
                return;
            }

            var jobSettingsInstance = ToggleJob.GetIRoutineSettingsFromJobString();
            if (jobSettingsInstance == null)
                return;

            foreach (var settingsToggleSetting in Settings)
            {
                // Get the appropriate instance (BaseSettings for PvP_ properties, otherwise job settings)
                var targetInstance = SettingsHandler.GetSettingsInstanceForProperty(settingsToggleSetting.Name, jobSettingsInstance);
                var targetProperty = targetInstance.GetType().GetProperty(settingsToggleSetting.Name);

                // If there's no property, continue the loop
                if (targetProperty == null)
                    continue;

                // Check to see if the value on the property matches what our Checked value should be 
                if (SettingsHandler.SettingToggleSettingMatchesProperty(settingsToggleSetting, targetProperty, jobSettingsInstance))
                    continue;

                // Toggle is unchecked because one of its properties does not match its checked value
                ToggleChecked = false;
                return;
            }

            // Toggle is checked because all properties match their checked values
            ToggleChecked = true;
        }

        // Method that acts as the delegate for the command; executed on checked and unchecked
        private void ExecuteToggle()
        {
            var settingsInstance = ToggleJob.GetIRoutineSettingsFromJobString();

            if (settingsInstance == null)
            {
                Logger.Error("[Toggles] Settings Instance is null");
                return;
            }

            if (Settings == null || Settings.Count == 0)
                return;

            // Change the settings property
            SettingsHandler.SetPropertyOnSettingsInstance(Settings.ToList(), settingsInstance, ToggleChecked);

            // Reset the state of toggles, we have to check every toggle here because some toggles may
            // effect the state of other toggles
            TogglesManager.ResetToggles();
        }

        public ICommand AddToggleSettingCommand => new DelegateCommand<ToggleProperty>(toggleProperty =>
        {
            if (Settings.Any(r => r.Name == toggleProperty.Name))
                return;

            var newToggleSetting = new SettingsToggleSetting
            {
                Name = toggleProperty.Name,
                BoolCheckedValue = true,
                BoolUncheckedValue = false,
                IntCheckedValue = 0,
                IntUncheckedValue = 0,
                FloatCheckedValue = 0,
                FloatUncheckedValue = 0,
                Type = toggleProperty.Type
            };

            Settings.Add(newToggleSetting);
            SetToggleState();
        });

        public ICommand RemoveToggleSettingCommand => new DelegateCommand<SettingsToggleSetting>(settingsToggleSetting =>
        {
            if (settingsToggleSetting == null)
                return;

            Settings.Remove(settingsToggleSetting);
            SetToggleState();
        });

        // We can either use the built-in RB hotkey manager for this or we can write our own
        public void RegisterHotkey()
        {
            // Create a unique hotkey ID that includes both job and toggle name to avoid conflicts
            string hotkeyId = $@"Magitek{ToggleJob}{ToggleText.Replace(" ", "")}";

            // Unregister this toggle's hotkey first
            HotkeyManager.Unregister(hotkeyId);

            // Does the toggle even have keys set?
            if (ToggleKey == Keys.None && ToggleModifierKey == ModifierKeys.None)
                return;

            // Check for existing hotkeys with the same combination and unregister them
            var existingHotkeys = HotkeyManager.RegisteredHotkeys
                .Where(h => h.Key == ToggleKey && h.Name.Contains("Magitek"))
                .ToList();

            foreach (var existing in existingHotkeys)
            {
                // Logger.WriteInfo($@"[Toggles] Unregistering conflicting hotkey {existing.Name} with same key combination");
                HotkeyManager.Unregister(existing.Name);
            }

            // Register the hotkey with the unique ID
            HotkeyManager.Register(hotkeyId, ToggleKey, ToggleModifierKey, r =>
            {
                if (ToggleChecked)
                {
                    // Set the checked to false
                    ToggleChecked = false;
                    // Run ExecuteToggle
                    ExecuteToggle();
                }
                else
                {
                    // Set the checked to true
                    ToggleChecked = true;
                    // Run ExecuteToggle
                    ExecuteToggle();
                }
            });

            // Log the registration for debugging
            Logger.WriteInfo($@"[Toggles] Registered hotkey for {ToggleJob} - {ToggleText}: {ToggleModifierKey} + {ToggleKey}");
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

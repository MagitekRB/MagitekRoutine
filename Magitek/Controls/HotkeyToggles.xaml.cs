using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;
using WPFTextBox = System.Windows.Controls.TextBox;

namespace Magitek.Controls
{
    /// <summary>
    /// Interaction logic for HotkeyToggles.xaml
    /// </summary>
    public partial class HotkeyToggles : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ToggleHotkeyTextProperty = DependencyProperty.Register("ToggleHotkeyText", typeof(string), typeof(HotkeyToggles), new UIPropertyMetadata("TOGGLE HOTKEY:"));
        public static readonly DependencyProperty ToggleKeySettingProperty = DependencyProperty.Register("ToggleKeySetting", typeof(Keys), typeof(HotkeyToggles), new PropertyMetadata(Keys.None, OnHotkeyChanged));
        public static readonly DependencyProperty ToggleModKeySettingProperty = DependencyProperty.Register("ToggleModKeySetting", typeof(ModifierKeys), typeof(HotkeyToggles), new PropertyMetadata(ModifierKeys.None, OnHotkeyChanged));
        public static readonly DependencyProperty HkTextProperty = DependencyProperty.Register("HkText", typeof(string), typeof(HotkeyToggles), new PropertyMetadata(string.Empty));

        private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyToggles hotkeyToggles)
            {
                // Update the HkText property when keys change
                hotkeyToggles.HkText = $"{hotkeyToggles.ToggleModKeySetting} + {hotkeyToggles.ToggleKeySetting}";

                // Also update the textbox directly if needed
                if (hotkeyToggles._txtHk != null)
                {
                    hotkeyToggles._txtHk.Text = hotkeyToggles.HkText;
                }
            }
        }

        private WPFTextBox _txtHk;

        public HotkeyToggles()
        {
            InitializeComponent();
            _txtHk = (WPFTextBox)FindName("TxtHk");
            PreviewKeyDown += OnPreviewKeyDown;
            LostFocus += OnLostFocus;

            // Set initial text
            if (_txtHk != null)
            {
                HkText = $"{ToggleModKeySetting} + {ToggleKeySetting}";
                _txtHk.Text = HkText;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            // Re-register hotkey for the currently selected toggle
            // Find the parent TogglesSettings control
            var togglesSettings = FindParent<UserControl>(this);
            if (togglesSettings != null)
            {
                // Get the data context which should be TogglesViewModel
                var togglesViewModel = togglesSettings.DataContext as ViewModels.TogglesViewModel;
                if (togglesViewModel != null && togglesViewModel.SelectedToggle != null)
                {
                    // Register hotkey for the currently selected toggle
                    togglesViewModel.SelectedToggle.RegisterHotkey();

                    // Make sure to save the toggle settings to persist them
                    togglesViewModel.SaveToggles();
                }
            }
        }

        // Helper method to find parent of specific type
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        public string ToggleHotkeyText
        {
            get => (string)GetValue(ToggleHotkeyTextProperty);
            set => SetValue(ToggleHotkeyTextProperty, value);
        }

        public string HkText
        {
            get => (string)GetValue(HkTextProperty);
            set
            {
                SetValue(HkTextProperty, value);
                OnPropertyChanged("HkText");
            }
        }

        public Keys ToggleKeySetting
        {
            get => (Keys)GetValue(ToggleKeySettingProperty);
            set
            {
                SetValue(ToggleKeySettingProperty, value);
                OnPropertyChanged("HkText");
            }
        }

        public ModifierKeys ToggleModKeySetting
        {
            get => (ModifierKeys)GetValue(ToggleModKeySettingProperty);
            set
            {
                SetValue(ToggleModKeySettingProperty, value);
                OnPropertyChanged("HkText");
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The text box grabs all input.
            e.Handled = true;

            // Fetch the actual shortcut key.
            var key = (e.Key == Key.System ? e.SystemKey : e.Key);

            switch (key)
            {
                case Key.Escape:
                    ToggleModKeySetting = ModifierKeys.None;
                    ToggleKeySetting = Keys.None;
                    if (_txtHk != null)
                    {
                        HkText = $"{ToggleModKeySetting} + {ToggleKeySetting}";
                        _txtHk.Text = HkText;
                    }
                    return;
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LWin:
                case Key.RWin:
                    return;
            }

            // Ignore modifier keys.

            // Build the shortcut key name.
            var shortcutText = new StringBuilder();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append("Ctrl + ");
                ToggleModKeySetting = ModifierKeys.Control;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift + ");
                ToggleModKeySetting = ModifierKeys.Shift;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt + ");
                ToggleModKeySetting = ModifierKeys.Alt;
            }

            if (Keyboard.Modifiers == 0)
            {
                shortcutText.Append("None + ");
                ToggleModKeySetting = ModifierKeys.None;
            }

            shortcutText.Append(key);

            var newKey = (Keys)KeyInterop.VirtualKeyFromKey(key);
            ToggleKeySetting = newKey;

            // Update the HkText property
            HkText = $"{ToggleModKeySetting} + {ToggleKeySetting}";

            // Update the text box directly
            if (_txtHk != null)
            {
                _txtHk.Text = HkText;
            }

            // Immediately save the toggle
            var togglesSettings = FindParent<UserControl>(this);
            if (togglesSettings != null)
            {
                var togglesViewModel = togglesSettings.DataContext as ViewModels.TogglesViewModel;
                if (togglesViewModel != null && togglesViewModel.SelectedToggle != null)
                {
                    togglesViewModel.SelectedToggle.RegisterHotkey();
                    togglesViewModel.SaveToggles();
                }
            }
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

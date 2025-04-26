using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;
using System.Linq;

namespace Magitek.Controls
{
    public partial class Hotkey : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Hotkey), new UIPropertyMetadata("Hotkey"));
        public static readonly DependencyProperty KeySettingProperty = DependencyProperty.Register("KeySetting", typeof(Keys), typeof(Hotkey), new PropertyMetadata(Keys.None, OnKeyChanged));
        public static readonly DependencyProperty ModKeySettingProperty = DependencyProperty.Register("ModKeySetting", typeof(ModifierKeys), typeof(Hotkey), new PropertyMetadata(ModifierKeys.None, OnModKeyChanged));
        public static readonly DependencyProperty HkTextProperty = DependencyProperty.Register("HkText", typeof(string), typeof(Hotkey), new PropertyMetadata(string.Empty));

        public event PropertyChangedEventHandler PropertyChanged;

        private static void OnKeyChanged(DependencyObject keySetting, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (keySetting is Hotkey hotkey)
            {
                hotkey.UpdateHkText();
            }
        }

        private static void OnModKeyChanged(DependencyObject keySetting, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (keySetting is Hotkey hotkey)
            {
                hotkey.UpdateHkText();
            }
        }

        private void UpdateHkText()
        {
            HkText = $"{ModKeySetting} + {KeySetting}";
        }

        public Hotkey()
        {
            InitializeComponent();
            PreviewKeyDown += OnPreviewKeyDown;
            LostFocus += OnLostFocus;
            UpdateHkText();
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            // Re - Registering Code
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string HkText
        {
            get => (string)GetValue(HkTextProperty);
            set => SetValue(HkTextProperty, value);
        }

        public Keys KeySetting
        {
            get => (Keys)GetValue(KeySettingProperty);
            set => SetValue(KeySettingProperty, value);
        }

        public ModifierKeys ModKeySetting
        {
            get => (ModifierKeys)GetValue(ModKeySettingProperty);
            set => SetValue(ModKeySettingProperty, value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    HkText = "None + None";
                    ModKeySetting = ModifierKeys.None;
                    KeySetting = Keys.None;
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
                ModKeySetting = ModifierKeys.Control;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift + ");
                ModKeySetting = ModifierKeys.Shift;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt + ");
                ModKeySetting = ModifierKeys.Alt;
            }

            if (Keyboard.Modifiers == 0)
            {
                shortcutText.Append("None + ");
                ModKeySetting = ModifierKeys.None;
            }

            shortcutText.Append(key);

            var newKey = (Keys)KeyInterop.VirtualKeyFromKey(key);
            KeySetting = newKey;
            // Update the text box.
            HkText = shortcutText.ToString();
        }
    }
}

using System.Windows;
using Magitek.Utilities;

namespace Magitek.Views
{
    public partial class SettingsModal : Window
    {
        public SettingsModal()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ZoomHackCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ZoomHack.Toggle();
        }

        //private void OverlayCheckChanged(object sender, RoutedEventArgs e)
        //{
        //    Application.Current.Dispatcher.Invoke(Utilities.Overlays.TogglePositional);
        //}
    }
}

using System.Windows.Controls;
using Magitek.ViewModels;

namespace Magitek.Controls
{
    public partial class CurrentNews : UserControl
    {
        public CurrentNews()
        {
            InitializeComponent();
            Loaded += CurrentNews_Loaded;
        }

        private void CurrentNews_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            MagitekApi.Instance.RefreshIfNeeded();
        }
    }
}

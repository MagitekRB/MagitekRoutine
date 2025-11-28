using Magitek.Models;
using PropertyChanged;
using BaseSettingsModel = Magitek.Models.Account.BaseSettings;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class PvpAggroCountOverlayViewModel
    {
        private static PvpAggroCountOverlayViewModel _instance;
        public static PvpAggroCountOverlayViewModel Instance => _instance ?? (_instance = new PvpAggroCountOverlayViewModel());

        public PvpAggroCountModel AggroCountModel => PvpAggroCountModel.Instance;
        public BaseSettingsModel BaseSettingsInstance => BaseSettingsModel.Instance;
    }
}


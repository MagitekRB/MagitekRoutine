using PropertyChanged;

namespace Magitek.Models
{
    [AddINotifyPropertyChangedInterface]
    public class PvpAggroCountModel
    {
        private static PvpAggroCountModel _instance;
        public static PvpAggroCountModel Instance => _instance ?? (_instance = new PvpAggroCountModel());

        public int EnemiesTargetingMe { get; set; } = 0;
    }
}


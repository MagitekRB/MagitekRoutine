using PropertyChanged;
using System;

namespace Magitek.Models.MagitekApi
{
    [AddINotifyPropertyChangedInterface]
    public class MagitekVersion
    {
        public String LocalVersion { get; set; }
        public String DistantVersion { get; set; }
        public bool IsOutOfSync { get; set; }
    }
}

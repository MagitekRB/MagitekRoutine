using Magitek.Enumerations;
using Magitek.Models.Roles;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Viper
{
    [AddINotifyPropertyChangedInterface]
    public class ViperSettings : PhysicalDpsSettings, IRoutineSettings
    {
        public ViperSettings() : base(CharacterSettingsDirectory + "/Magitek/Viper/ViperSettings.json") { }

        public static ViperSettings Instance { get; set; } = new ViperSettings();

        #region Placeholder

        [Setting]
        [DefaultValue(true)]
        public bool PlcBool { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float PlcFloat { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int PlcInt { get; set; }

        #endregion
    }
}
using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Viper
{
    [AddINotifyPropertyChangedInterface]
    public class ViperSettings : PhysicalDpsSettings, IRoutineSettings
    {
        public ViperSettings() : base(CharacterSettingsDirectory + "/Magitek/Viper/ViperSettings.json") { }

        public static ViperSettings Instance { get; set; } = new ViperSettings();

        #region General-Stuff
        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HidePositionalMessage { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool EnemyIsOmni { get; set; }

        #endregion

        #region SingleTarget-Abilities

        [Setting]
        [DefaultValue(true)]
        public bool UseVicewinder { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseReawaken { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseUncoiledFury { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseWrithingSnap { get; set; }

        #endregion

        #region AoE-Abilities

        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSteelReavingMaw { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseVicepit { get; set; }

        #endregion

        #region Cooldowns

        [Setting]
        [DefaultValue(true)]
        public bool UseDeathRattle { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLastLash { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSerpentIre { get; set; }
        #endregion

        #region Utility-Abilities
        [Setting]
        [DefaultValue(true)]
        public bool UseSlither { get; set; }
        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SnakeScales { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float Pvp_SnakeScalesHealthPercent { get; set; }

        [Setting]
        [DefaultValue(100f)]
        public float Pvp_UncoiledFuryHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_WorldSwallower { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float Pvp_WorldSwallowerHealthPercent { get; set; }
        #endregion
    }
}
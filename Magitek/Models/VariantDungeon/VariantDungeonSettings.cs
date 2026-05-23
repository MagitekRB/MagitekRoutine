using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.VariantDungeon
{
    [AddINotifyPropertyChangedInterface]
    public class VariantDungeonSettings : JsonSettings
    {
        public VariantDungeonSettings() : base(CharacterSettingsDirectory + "/Magitek/VariantDungeon/VariantDungeonSettings.json") { }

        public static VariantDungeonSettings Instance { get; set; } = new VariantDungeonSettings();

        #region General
        [Setting]
        [DefaultValue(true)]
        public bool Enable { get; set; }
        #endregion

        #region Variant Cure
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantCure { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float VariantCureHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VariantCureOnAllies { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float VariantCureAllyHealthPercent { get; set; }
        #endregion

        #region Variant Ultimatum
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantUltimatum { get; set; }
        #endregion

        #region Variant Raise
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantRaise { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSwiftcastForVariantRaise { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SlowcastVariantRaise { get; set; }
        #endregion

        #region Variant Spirit Dart
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantSpiritDart { get; set; }
        #endregion

        #region Variant Rampart
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantRampart { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float VariantRampartHealthPercent { get; set; }
        #endregion

        #region Variant Eagle Eye Shot
        [Setting]
        [DefaultValue(true)]
        public bool UseVariantEagleEyeShot { get; set; }
        #endregion
    }
}

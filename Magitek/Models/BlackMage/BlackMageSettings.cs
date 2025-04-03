using Magitek.Enumerations;
using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.BlackMage
{
    [AddINotifyPropertyChangedInterface]
    public class BlackMageSettings : MagicDpsSettings, IRoutineSettings
    {
        public BlackMageSettings() : base(CharacterSettingsDirectory + "/Magitek/BlackMage/BlackMageSettings.json") { }

        public static BlackMageSettings Instance { get; set; } = new BlackMageSettings();

        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        [Setting]
        [DefaultValue(BuffStrategy.Always)]
        public BuffStrategy BuffStrategy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicManaward { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ThunderSingle { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Scathe { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float ScatheOnlyAboveManaPercent { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int ThunderRefreshSecondsLeft { get; set; }

        [Setting]
        [DefaultValue(6)]
        public int ThunderTimeTillDeathSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool TripleCast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Xenoglossy { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int SaveXenoglossyCharges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTransposeToAstral { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseTransposeToUmbral { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FlareStar { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Paradox { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Despair { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool TripleCastWhileMoving { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UmbralSoul { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ManaFont { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Amplifier { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LeyLines { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LeyLinesBossOnly { get; set; }

        #region AOE
        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeEnemies { get; set; }
        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_ToggleFireOrIceCombo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseParadoxOnFire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseParadoxOnIce { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseSuperFlareOnFire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseSuperFlareOnIce { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseAetherialManipulation { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float Pvp_UseAetherialManipulationtHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SoulResonance { get; set; }
        #endregion
    }
}
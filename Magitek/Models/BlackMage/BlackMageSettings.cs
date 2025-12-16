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
        [DefaultValue(true)]
        public bool UseTTDForThunderSingle { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int ThunderSingleTTDSeconds { get; set; }

        [Setting]
        [DefaultValue(6)]
        public int ThunderTimeTillDeathSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTTDForThunderAoe { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int ThunderAoeTTDSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool TripleCast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Xenoglossy { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int SaveXenoglossyCharges { get; set; }

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

        [Setting]
        [DefaultValue(true)]
        public bool UsePreCombatTranspose { get; set; }

        #region AOE
        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ThunderAoe { get; set; }
        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SoulResonance { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float Pvp_SoulResonanceHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseXenoglossy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseLethargy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseElementalWeave { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_SaveXenoglossyForKills { get; set; }
        #endregion
    }
}
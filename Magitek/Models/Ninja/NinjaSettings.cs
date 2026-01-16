using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Ninja
{
    [AddINotifyPropertyChangedInterface]
    public class NinjaSettings : PhysicalDpsSettings, IRoutineSettings
    {
        public NinjaSettings() : base(CharacterSettingsDirectory + "/Magitek/Ninja/NinjaSettings.json") { }

        public static NinjaSettings Instance { get; set; } = new NinjaSettings();

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicShadeShift { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseForkedRaiju { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFleetingRaiju { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HidePositionalMessage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseThrowingDagger { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMug { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHellfrogMedium { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDeathfrogMedium { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomKamaitachi { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTenriJindo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBhavacakra { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseZeshoMeppo { get; set; }

        [Setting]
        [DefaultValue(8)]
        public int DontMugIfEnemyDyingWithinSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTrickAttack { get; set; }

        [Setting]
        [DefaultValue(8)]
        public int DontTrickAttackIfEnemyDyingWithinSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTenChiJin { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseKassatsu { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMeisui { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBunshin { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAssassinate { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool BurstLogicHoldBurst { get; set; }

        #region PVP

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Assassinate { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Bunshin { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_FumaShuriken { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_DoNotUseThreeMudra { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Doton { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Huton { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float Pvp_HutonHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Meisui { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float Pvp_MeisuiHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_ForkedRaiju { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_FleetingRaiju { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_HyoshoRanryu { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_GokaMekkyaku { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int Pvp_GokaMekkyakuMinEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Shukuchi { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SeitonTenchu { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float Pvp_SeitonTenchuHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Dokumori { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int Pvp_DotonMinEnemies { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_FumaShurikenOnlyWithBunshin { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_UseSeitonTenchuAnyTarget { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_SeitonTenchuForKillsOnly { get; set; }

        #endregion
    }
}

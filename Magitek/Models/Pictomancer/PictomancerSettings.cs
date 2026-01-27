using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Pictomancer
{
    [AddINotifyPropertyChangedInterface]
    public class PictomancerSettings : MagicDpsSettings, IRoutineSettings
    {
        public PictomancerSettings() : base(CharacterSettingsDirectory + "/Magitek/Pictomancer/PictomancerSettings.json") { }

        public static PictomancerSettings Instance { get; set; } = new PictomancerSettings();

        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        #region General-Stuff
        [Setting]
        [DefaultValue(false)]
        public bool UseHammerDuringHyperphantasia { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveHammerForStarry { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveCometInBlackForStarry { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool CometInBlackOnlyDuringStarry { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveCometInBlackForStarryMovement { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveMogForStarry { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMogDuringHyperphantasia { get; set; }

        [Setting]
        [DefaultValue(24000)]
        public int SaveForStarryMsComet { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSwiftcast { get; set; }
        #endregion

        #region Palette-Abilities
        [Setting]
        [DefaultValue(true)]
        public bool PrePaletteOutOfCombat { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PrePaletteOutOfCombatOnlyInDuty { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PaletteDuringDowntime { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool PaletteDuringStarry { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastMotifsOnlyWhenMoving { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastCreatureMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastWeaponMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastLandscapeMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMuses { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHammers { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveHammerForMovement { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveHammerForMovementOnlyBoss { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMogOfTheAges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseStarrySky { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseStarrySkyWhileMoving { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveStarryForHammers { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SaveStarryForMog { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRainbowDrip { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseStarPrism { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int StarrySkyCount { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool StarrySkyEntireParty { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSimplifiedRotationBelow100 { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSimplifiedRotation { get; set; }
        #endregion

        #region Single-Target-Abilities
        [Setting]
        [DefaultValue(true)]
        public bool UseSingleHolyinWhiteMoving { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSingleCometinBlack { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSinglePaint { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int WhitePaintSaveXCharges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool WhitePaintUseWhenFull { get; set; }
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
        public bool UseAOECometInBlack { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAOEHolyInWhite { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAOEPaint { get; set; }
        #endregion

        #region Utility-Abilities
        [Setting]
        [DefaultValue(true)]
        public bool UseSubtractivePalette { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicTemperaCoat { get; set; }
        #endregion

        #region Paint

        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseAdventofChocobastion { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int Pvp_AdventofChocobastionCount { get; set; }

        [Setting]
        [DefaultValue(10)]
        public int Pvp_AdventofChocobastionYalms { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseSubtractivePalette { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseStarstruck { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SwapToBlackWhileMoving { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePaintRGB { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePaintWhite { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePaintWhiteOnlyToHeal { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float Pvp_UsePaintWhiteOnlyToHealHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePaintBlack { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseMotif { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseMuse { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseMog { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseTemperaCoat { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float Pvp_TemperaCoatHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseTemperaGrassa { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_MogOfTheAgesAnyTarget { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_MogOfTheAgesForKillsOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseCometInBlack { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_CometInBlackAnyTarget { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Pvp_CometInBlackForKillsOnly { get; set; }
        #endregion
    }
}

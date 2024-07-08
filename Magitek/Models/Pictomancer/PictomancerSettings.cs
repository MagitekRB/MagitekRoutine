using Magitek.Models.Roles;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magitek.Models.Pictomancer
{
    [AddINotifyPropertyChangedInterface]
    public class PictomancerSettings : MagicDpsSettings, IRoutineSettings
    {
        public PictomancerSettings() : base(CharacterSettingsDirectory + "/Magitek/Pictomancer/PictomancerSettings.json") { }

        public static PictomancerSettings Instance { get; set; } = new PictomancerSettings();

        #region General-Stuff
        [Setting]
        [DefaultValue(true)]
        public bool UseTTD { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int SaveIfEnemyDyingWithin { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseWeaving { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SaveHammerForStarry { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SaveCometInBlackForStarry { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SaveMogForStarry { get; set; }

        [Setting]
        [DefaultValue(15)]
        public int SaveForStarryMSeconds { get; set; }

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
        public bool UseMotifs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMuses { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHammers { get; set; }

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

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicAddle { get; set; }
        #endregion

        #region Paint

        #endregion

        #region PVP

        #endregion
    }
}

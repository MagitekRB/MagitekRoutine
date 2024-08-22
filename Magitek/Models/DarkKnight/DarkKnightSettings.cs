using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.DarkKnight
{
    [AddINotifyPropertyChangedInterface]
    public class DarkKnightSettings : TankSettings, IRoutineSettings
    {
        public DarkKnightSettings() : base(CharacterSettingsDirectory + "/Magitek/DarkKnight/DarkKkightSettings.json") { }

        public static DarkKnightSettings Instance { get; set; } = new DarkKnightSettings();

        // Autonomous
        #region Autonomous
        [Setting]
        [DefaultValue(3.0f)]
        public float AutonomousPullDistance { get; set; }

        [Setting]
        [DefaultValue(3.0f)]
        public float AutonomousCombatDistance { get; set; }
        #endregion

        //General
        #region General
        [Setting]
        [DefaultValue(3000)]
        public int SaveXMana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UnmendToDps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UnmendToPullOrAggro { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int UnmendMinDistance { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBloodspiller { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDisesteem { get; set; }
        #endregion


        //Defensives
        #region Defensives
        [Setting]
        [DefaultValue(true)]
        public bool Grit { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOblation { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseOblationOnTanks { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseOblationOnHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseOblationOnDPS { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float UseOblationAtHpPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseTheBlackestNight { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float TheBlackestNightHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDarkMind { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float DarkMindHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDarkMissionary { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float DarkMissionaryHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShadowWall { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float ShadowWallHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLivingDead { get; set; }

        [Setting]
        [DefaultValue(10.0f)]
        public float LivingDeadHealth { get; set; }
        #endregion

        //Buffs
        #region Buffs
        [Setting]
        [DefaultValue(true)]
        public bool BloodWeapon { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Delirium { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LivingShadow { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ShadowedVigil { get; set; }
        #endregion

        //oGCDs
        #region oGCDs
        [Setting]
        [DefaultValue(false)]
        public bool UsePlunge { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PlungeOnlyInMelee { get; set; }

        [Setting]
        [DefaultValue(0)]
        public int SavePlungeCharges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShadowbringer { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCarveAndSpit { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCarveOnlyWithBloodWeapon { get; set; }
        #endregion

        //AoE
        #region AoE
        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseUnleash { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int UnleashEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseQuietus { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int QuietusEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSaltedEarth { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int SaltedEarthEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAbyssalDrain { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int AbyssalDrainEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFloodDarknessShadow { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int FloodEnemies { get; set; }
        #endregion

        //Pvp
        #region
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Bloodspiller { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Shadowbringer { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float Pvp_ShadowbringerHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Plunge { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SafePlunge { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_BlackestNight { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Quietus { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SaltedEarth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SaltAndDarkness { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Eventide { get; set; }

        [Setting]
        [DefaultValue(90.0f)]
        public float Pvp_EventideHealthPercent { get; set; }
        #endregion

    }
}
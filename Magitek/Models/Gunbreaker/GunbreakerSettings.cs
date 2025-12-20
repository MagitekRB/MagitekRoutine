using Magitek.Enumerations;
using Magitek.Models.Roles;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Gunbreaker
{
    [AddINotifyPropertyChangedInterface]
    public class GunbreakerSettings : TankSettings, IRoutineSettings
    {
        public GunbreakerSettings() : base(CharacterSettingsDirectory + "/Magitek/Gunbreaker/GunbreakerSettings.json")
        {
        }

        protected override void Migrate()
        {
            int originalVersion = SettingsVersion;
            base.Migrate();

            // If original version was -1 (new file), base.Migrate() set it to 1, now set to 2 (latest for tanks)
            if (originalVersion == -1)
            {
                SettingsVersion = 2;
                Save();
                return;
            }

            // Version 1 -> 2: Migrate TrajectoryOnlyInMelee to new checkbox settings
            // Only migrate if version is < 2 (existing users with old settings)
            if (SettingsVersion < 2)
            {
                // Migrate from old TrajectoryOnlyInMelee boolean to new checkbox settings
                if (TrajectoryOnlyInMelee)
                {
                    TrajectoryUseForMobility = false;
                    TrajectoryUseForDps = true;
                    TrajectoryOnlyDuringBurst = false;
                }
                else
                {
                    TrajectoryUseForMobility = true;
                    TrajectoryUseForDps = true;
                    TrajectoryOnlyDuringBurst = false;
                }

                SettingsVersion = 2;
                Save();
            }
        }

        public static GunbreakerSettings Instance { get; set; } = new GunbreakerSettings();

        #region General
        [Setting]
        [DefaultValue(GunbreakerStrategy.FastGCD)]
        public GunbreakerStrategy GunbreakerStrategy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRoyalGuard { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAmmoCombo { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HoldAmmoCombo { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SaveGnashingFangForNoMercy { get; set; }

        [Setting]
        [DefaultValue(19)]
        public int HoldAmmoComboSeconds { get; set; }

        /// <summary>
        /// V49 setting: Milliseconds before No Mercy to save ammo combo (legacy, used by V49 implementation).
        /// </summary>
        [Setting]
        [DefaultValue(6000)]
        public int SaveAmmoComboMseconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLionHeartCombo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBowShock { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int BowShockEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTrajectory { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool TrajectoryUseForMobility { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool TrajectoryUseForDps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool TrajectoryOnlyDuringBurst { get; set; }

        [Setting]
        [DefaultValue(0)]
        public int SaveTrajectoryCharges { get; set; }

        // Legacy property for migration - will be removed in a future version
        [Setting]
        [DefaultValue(true)]
        [Obsolete("Use TrajectoryUseForMobility, TrajectoryUseForDps, and TrajectoryOnlyDuringBurst instead")]
        public bool TrajectoryOnlyInMelee { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBloodfest { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBurstStrike { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ForceBurstStrike { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBlastingZone { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HoldBlastingZone { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int HoldBlastingZoneSeconds { get; set; }

        /// <summary>
        /// V49 setting: Save Blasting Zone for No Mercy window (legacy, used by V49 implementation).
        /// </summary>
        [Setting]
        [DefaultValue(true)]
        public bool SaveBlastingZone { get; set; }

        /// <summary>
        /// V49 setting: Milliseconds before No Mercy to save Blasting Zone (legacy, used by V49 implementation).
        /// </summary>
        [Setting]
        [DefaultValue(6000)]
        public int SaveBlastingZoneMseconds { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool BurstLogicHoldBurst { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool BurstLogicHoldNoMercy { get; set; }
        #endregion

        #region Buff
        [Setting]
        [DefaultValue(true)]
        public bool UseNoMercy { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseNoMercyMaxCartridge { get; set; }
        #endregion

        #region AOE
        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFatedCircle { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFatedBrand { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int UseAoeEnemies { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int PrioritizeFatedCircleOverGnashingFangEnemies { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int PrioritizeFatedCircleOverBurstStrikeEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDoubleDown { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int DoubleDownEnemies { get; set; }
        #endregion

        #region Defensives
        [Setting]
        [DefaultValue(true)]
        public bool UseSuperbolide { get; set; }

        [Setting]
        [DefaultValue(15)]
        public int SuperbolideHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCamouflage { get; set; }

        [Setting]
        [DefaultValue(80)]
        public int CamouflageHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseNebula { get; set; }

        [Setting]
        [DefaultValue(70)]
        public int NebulaHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHeartofLight { get; set; }

        [Setting]
        [DefaultValue(60)]
        public int HeartofLightHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHeartofCorundum { get; set; }

        [Setting]
        [DefaultValue(60)]
        public int HeartofCorundumHealthPercent { get; set; }
        #endregion

        #region Heal
        [Setting]
        [DefaultValue(true)]
        public bool UseAurora { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseAuroraHealer { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int AuroraHealerHealthPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseAuroraDps { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int AuroraDpsHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAuroraSelf { get; set; }

        [Setting]
        [DefaultValue(60)]
        public int AuroraSelfHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAuroraMainTank { get; set; }

        [Setting]
        [DefaultValue(60)]
        public int AuroraMainTankHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAuroraTank { get; set; }

        [Setting]
        [DefaultValue(60)]
        public int AuroraTankHealthPercent { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int AuroraPrioritySelf { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int AuroraPriorityMainTank { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AuroraPriorityTank { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int AuroraPriorityHealer { get; set; }

        [Setting]
        [DefaultValue(5)]
        public int AuroraPriorityDps { get; set; }

        #endregion

        #region Pull
        [Setting]
        [DefaultValue(3)]
        public int LightningShotMinDistance { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LightningShotToPullAggro { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LightningShotToDps { get; set; }

        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_RoughDivide { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SafeRoughDivide { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_BurstStrike { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_GnashingFangCombo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Continuation { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Nebula { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_BlastingZone { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Aurora { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_RelentlessRush { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int Pvp_RelentlessRushEnemyCount { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_HeartOfCorundum { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_FatedCircle { get; set; }

        #endregion


    }
}
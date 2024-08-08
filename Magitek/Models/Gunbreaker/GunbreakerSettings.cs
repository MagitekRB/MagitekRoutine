using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Gunbreaker
{
    [AddINotifyPropertyChangedInterface]
    public class GunbreakerSettings : TankSettings, IRoutineSettings
    {
        public GunbreakerSettings() : base(CharacterSettingsDirectory + "/Magitek/Gunbreaker/GunbreakerSettings.json") { }

        public static GunbreakerSettings Instance { get; set; } = new GunbreakerSettings();

        #region General
        [Setting]
        [DefaultValue(true)]
        public bool UseRoyalGuard { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAmmoCombo { get; set; }

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
        public bool UseRoughDivide { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool RoughDivideOnlyInMelee { get; set; }

        [Setting]
        [DefaultValue(0)]
        public int SaveRoughDivideCharges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBloodfest { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBurstStrike { get; set; }

        [Setting]
        [DefaultValue(6000)]
        public int SaveBurstStrikeMseconds { get; set; }
        #endregion

        #region Buff
        [Setting]
        [DefaultValue(true)]
        public bool UseNoMercy { get; set; }
        #endregion

        [Setting]
        [DefaultValue(false)]
        public bool UseNoMercyMaxCartridge { get; set; }

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
        [DefaultValue(2)]
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

        [Setting]
        [DefaultValue(true)]
        public bool SaveBlastingZone { get; set; }

        [Setting]
        [DefaultValue(6000)]
        public int SaveBlastingZoneMseconds { get; set; }

        #region PVP

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_DoubleDown { get; set; }

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
        public bool Pvp_Hypervelocity { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_DrawandJunction { get; set; }

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

        #endregion


    }
}
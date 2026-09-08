using Magitek.Models.Roles;
using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.BeastMaster
{
    [AddINotifyPropertyChangedInterface]
    public class BeastMasterSettings : PhysicalDpsSettings, IRoutineSettings
    {
        public BeastMasterSettings() : base(CharacterSettingsDirectory + "/Magitek/BeastMaster/BeastMasterSettings.json") { }

        public static BeastMasterSettings Instance { get; set; } = new BeastMasterSettings();

        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        #region Familiar

        [Setting]
        [DefaultValue(true)]
        public bool SummonFamiliar { get; set; }

        // Which horn to blow first (1-3); the others follow when it is on cooldown.
        [Setting]
        [DefaultValue(1)]
        public int PreferredBattlehorn { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTemperedRelease { get; set; }

        // One with Nature is spent by either Tempered Release or Borrow; when both are on, Borrow wins if this is set.
        [Setting]
        [DefaultValue(false)]
        public bool PreferBorrow { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBorrow { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTrick { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePartingBlow { get; set; }

        // Parting Blow sends the familiar home and starts the horn's 90 s cooldown, so by default it only goes out
        // under Lingering Vantage (1,500) and when another horn can be blown right after.
        [Setting]
        [DefaultValue(true)]
        public bool PartingBlowOnlyWithVantage { get; set; }

        // Swap familiars mid-fight when another horn's beast continues the lit Heart and the one out cannot.
        [Setting]
        [DefaultValue(true)]
        public bool UseBattlehornSwaps { get; set; }

        // Which beast each horn (1-3) summons, learned by watching who shows up; replaced whole, never edited in place.
        [Setting]
        public Dictionary<int, string> BattlehornFamiliars { get; set; } = new Dictionary<int, string>();

        #endregion

        #region Rotation

        [Setting]
        [DefaultValue(true)]
        public bool UseInstinctualSkills { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRally { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRallyingCheer { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShieldCharge { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int ShieldChargeKeepCharges { get; set; }

        #endregion

        #region Kinship actions (Beast Mode after Borrow)

        [Setting]
        [DefaultValue(true)]
        public bool UseSoulCrush { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseQuellingWave { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseScouringAsh { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSeedsower { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseBeastskin { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseScaleskin { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseVileskin { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float DefensiveKinshipHealthPercent { get; set; }

        #endregion

        #region Master's Bestiary

        // Gauge each new kind of beast and capture the ones the bestiary lacks.
        [Setting]
        [DefaultValue(true)]
        public bool UseCapture { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float CaptureHealthPercent { get; set; }

        // While a capturable beast is unmarked, only auto-attacks go out (after one Smash Axe to start them).
        [Setting]
        [DefaultValue(true)]
        public bool HoldForCapture { get; set; }

        // Gauge's five answers, 1 (exceedingly difficult) to 5 (no effort at all): capture only from this one up.
        [Setting]
        [DefaultValue(1)]
        public int CaptureMinimumOdds { get; set; }

        // What the game answered per mob name id (see BeastMasterBestiary); replaced whole, never edited in place.
        [Setting]
        public Dictionary<uint, int> CaptureVerdicts { get; set; } = new Dictionary<uint, int>();

        [Setting]
        public List<string> BefriendedBeasts { get; set; } = new List<string>();

        #endregion
    }
}

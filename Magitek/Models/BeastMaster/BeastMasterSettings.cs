using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.BeastMaster
{
    [AddINotifyPropertyChangedInterface]
    public class BeastMasterSettings : JobSettings, IRoutineSettings
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

        // Out of combat, a familiar whose One with Nature is spent goes Away before the next pull and the horn brings
        // it back with a fresh one (its cooldowns reset while the horn itself is not on cooldown). Never in the Crucible.
        [Setting]
        [DefaultValue(true)]
        public bool AwayResetBetweenPulls { get; set; }

        // How near an enemy has to be, out of combat, for the reset to count a pull as coming.
        [Setting]
        [DefaultValue(30)]
        public int AwayResetRange { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTemperedRelease { get; set; }

        // Tempered Release policies by the ability's class (see Resources/BeastMasterFamiliars.json).
        [Setting]
        [DefaultValue(false)]
        public bool TemperedReleaseKnockbacksInParty { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float TemperedReleaseMitigationHealthPercent { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int TemperedReleaseSleepMinEnemies { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float TemperedReleaseFinisherHealthPercent { get; set; }

        // A party buff that costs you health waits until you are above this.
        [Setting]
        [DefaultValue(60.0f)]
        public float TemperedReleaseSelfDamageHealthPercent { get; set; }

        // A damaging dispel waits this long into a fight for a buff to strip before going out anyway.
        [Setting]
        [DefaultValue(8)]
        public int TemperedReleaseDispelWaitSeconds { get; set; }

        // Enemies for the sleep are counted within this many yalms of the familiar.
        [Setting]
        [DefaultValue(8)]
        public int TemperedReleaseSleepRadius { get; set; }

        // One with Nature is spent by either Tempered Release or Borrow; with this set, Borrow wins whenever no Kinship is up.
        [Setting]
        [DefaultValue(true)]
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

        // Keep each familiar out at least this long before Parting Blow (0 = as soon as a horn is ready). Three
        // horns on a 90 s recast give one summon per 45 s at best; spacing them keeps every exit under Vantage.
        [Setting]
        [DefaultValue(45)]
        public int PartingBlowSpacingSeconds { get; set; }

        // Swap familiars mid-fight when another horn's beast continues the lit Heart and the one out cannot.
        [Setting]
        [DefaultValue(true)]
        public bool UseBattlehornSwaps { get; set; }

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

        // Crucible of the Unbroken: the familiar out leaves only below this health, and only when another horn
        // can bring a beast. HP carries from node to node there, so the field horn cycle is off.
        [Setting]
        [DefaultValue(55f)]
        public float CrucibleSwapHealthPercent { get; set; }

        // Crucible enmity control. Snarl puts the familiar in front of the piece and makes it take every hit meant
        // for you for 45 s; Challenge takes the piece back onto you and cancels that cover.
        [Setting]
        [DefaultValue(true)]
        public bool UseSnarlAndChallenge { get; set; }

        [Setting]
        [DefaultValue(50f)]
        public float CrucibleSnarlFamiliarHealthPercent { get; set; }

        [Setting]
        [DefaultValue(40f)]
        public float CrucibleSnarlPlayerHealthPercent { get; set; }

        [Setting]
        [DefaultValue(30f)]
        public float CrucibleChallengeFamiliarHealthPercent { get; set; }

        // Challenge only while you can hold the piece yourself.
        [Setting]
        [DefaultValue(60f)]
        public float CrucibleChallengePlayerHealthPercent { get; set; }

        #endregion

        #region Master's Bestiary

        // Capture the beasts the bestiary lacks (RebornBuddy reads the bestiary; see BeastMasterRoutine.CaptureWanted).
        [Setting]
        [DefaultValue(true)]
        public bool UseCapture { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float CaptureHealthPercent { get; set; }

        // A beast this many levels or more below you dies in a hit or two, so it is marked at once instead of at the threshold.
        [Setting]
        [DefaultValue(3)]
        public int CaptureAtOnceLevelGap { get; set; }

        // While a capturable beast is unmarked, only auto-attacks go out (after one Smash Axe to start them).
        [Setting]
        [DefaultValue(true)]
        public bool HoldForCapture { get; set; }

        // A new pact fills the first empty battlehorn slot you have the horn for; horns already assigned are left alone.
        [Setting]
        [DefaultValue(true)]
        public bool AssignPactsToEmptyHorns { get; set; }

        #endregion
    }
}

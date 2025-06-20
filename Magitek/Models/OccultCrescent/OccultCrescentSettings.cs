using PropertyChanged;
using System.ComponentModel;
using System.Configuration;
using Magitek.Enumerations;

namespace Magitek.Models.OccultCrescent
{
    [AddINotifyPropertyChangedInterface]
    public class OccultCrescentSettings : JsonSettings
    {
        public OccultCrescentSettings() : base(CharacterSettingsDirectory + "/Magitek/OccultCrescent/OccultCrescentSettings.json") { }

        public static OccultCrescentSettings Instance { get; set; } = new OccultCrescentSettings();

        #region General
        [Setting]
        [DefaultValue(true)]
        public bool Enable { get; set; }

        [Setting]
        [DefaultValue(15.0f)]
        public float PartyBuffRefreshMinutes { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ReviveNonPartyPlayers { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float ReviveNonPartyMinimumManaPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ReviveNonPartyOutOfCombat { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ReviveNonPartyInCombat { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EnableAutomaticPhantomJobSwitching { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool AutoSwitchToKnightForEnduringFortitude { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool AutoSwitchToBardForRomeosBallad { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool AutoSwitchToMonkForFleetfooted { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool RestoreOriginalPhantomJobAfterAutoBuff { get; set; }
        #endregion

        #region Phantom Bard
        [Setting]
        [DefaultValue(true)]
        public bool UseOffensiveAria { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRomeosBallad { get; set; }



        [Setting]
        [DefaultValue(true)]
        public bool UseMightyMarch { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float MightyMarchHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool MightyMarchCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHerosRime { get; set; }
        #endregion

        #region Phantom Knight
        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomGuard { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float PhantomGuardHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePray { get; set; }



        [Setting]
        [DefaultValue(75.0f)]
        public float PrayHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultHeal { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float OccultHealHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultHealCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePledge { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float PledgeHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PledgeCastOnAllies { get; set; }
        #endregion

        #region Phantom Monk
        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomKick { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PhantomKickMeleeRangeOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultCounter { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCounterstance { get; set; }



        [Setting]
        [DefaultValue(true)]
        public bool UseOccultChakra { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float OccultChakraHealthPercent { get; set; }
        #endregion

        #region Phantom Berserker
        [Setting]
        [DefaultValue(true)]
        public bool UseRage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool RageMeleeRangeOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDeadlyBlow { get; set; }
        #endregion

        #region Phantom Chemist
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultPotion { get; set; }

        [Setting]
        [DefaultValue(15.0f)]
        public float OccultPotionHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultPotionCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseOccultEther { get; set; }

        [Setting]
        [DefaultValue(15.0f)]
        public float OccultEtherManaPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultEtherCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRevive { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ReviveOutOfCombat { get; set; }

        [Setting]
        [DefaultValue(3.0f)]
        public float ReviveDelay { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseOccultElixir { get; set; }

        [Setting]
        [DefaultValue(10.0f)]
        public float OccultElixirPartyHealthPercent { get; set; }
        #endregion

        #region Phantom Cannoneer
        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomFire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHolyCannon { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDarkCannon { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShockCannon { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSilverCannon { get; set; }
        #endregion

        #region Phantom Time Mage
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultSlowga { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultComet { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultCometOnlyWithJobSpecificBuffs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultCometAllowSwiftcast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultMageMasher { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultDispel { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultQuick { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultQuickCastOnAllies { get; set; }
        #endregion

        #region Phantom Ranger
        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomAim { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultFalcon { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultUnicorn { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float OccultUnicornHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultUnicornCastOnAllies { get; set; }
        #endregion

        #region Phantom Thief
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultSprint { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool OccultSprintOnlyInCombat { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSteal { get; set; }

        [Setting]
        [DefaultValue(7.0f)]
        public float StealHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseVigilance { get; set; }

        [Setting]
        [DefaultValue(20.0f)]
        public float VigilanceTargetDistance { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePilferWeapon { get; set; }
        #endregion

        #region Phantom Samurai
        [Setting]
        [DefaultValue(true)]
        public bool UseMineuchi { get; set; }

        [Setting]
        [DefaultValue(InterruptStrategy.AnyEnemy)]
        public InterruptStrategy MineuchiStrategy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShirahadori { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float ShirahadoriHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseIainuki { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseZeninage { get; set; }
        #endregion

        #region Phantom Oracle
        [Setting]
        [DefaultValue(true)]
        public bool UsePredict { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomJudgment { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float PhantomJudgmentHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCleansing { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float CleansingHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBlessing { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float BlessingHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseStarfall { get; set; }

        [Setting]
        [DefaultValue(100.0f)]
        public float StarfallHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePhantomRejuvenation { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float PhantomRejuvenationHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PhantomRejuvenationCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseInvulnerability { get; set; }

        [Setting]
        [DefaultValue(10.0f)]
        public float InvulnerabilityHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool InvulnerabilityCastOnAllies { get; set; }
        #endregion

        #region Phantom Geomancer
        [Setting]
        [DefaultValue(true)]
        public bool UseBattleBell { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool BattleBellAlwaysIncludeSelf { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRingingRespite { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool RingingRespiteCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool RingingRespiteAlwaysIncludeSelf { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSunbath { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float SunbathHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SunbathCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCloudyCaress { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBlessedRain { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMistyMirage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHastyMirage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAetherialGain { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseSuspend { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SuspendCastOnAllies { get; set; }
        #endregion

        #region Ninja
        [Setting]
        [DefaultValue(true)]
        public bool UseDokumori { get; set; }

        [Setting]
        [DefaultValue(7.0f)]
        public float DokumoriHealthPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DokumoriOnlyMultipleTargets { get; set; }
        #endregion
    }
}
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
        public bool PreferInquiringMind { get; set; }

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
        public bool AutoSwitchToDancerForQuickerStep { get; set; }

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

        #region Phantom Dancer
        [Setting]
        [DefaultValue(true)]
        public bool UseDance { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseQuickstep { get; set; }

        [Setting]
        [DefaultValue(90.0f)]
        public float QuickstepHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSteadfastStance { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float SteadfastStanceHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SteadfastStanceCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMesmerize { get; set; }
        #endregion

        #region Phantom Mystic Knight
        [Setting]
        [DefaultValue(true)]
        public bool UseMagicShell { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float MagicShellHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool MagicShellCastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSunderingSpellblade { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHolySpellblade { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBlazingSpellblade { get; set; }
        #endregion

        #region Phantom Gladiator
        [Setting]
        [DefaultValue(true)]
        public bool UseDefend { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float DefendHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFinisher { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLongReach { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBladeblitz { get; set; }
        #endregion

        #region Phantom Red Mage
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultFireII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultBlizzardII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultThunderII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultLibra { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRedMageOccultCureII { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float RedMageOccultCureIIHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool RedMageOccultCureIICastOnAllies { get; set; }
        #endregion

        #region Phantom Black Mage
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultFireIII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultBlizzardIII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultThunderIII { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultFlare { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultToad { get; set; }
        #endregion

        #region Phantom White Mage
        [Setting]
        [DefaultValue(true)]
        public bool UseWhiteMageOccultCureII { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float WhiteMageOccultCureIIHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool WhiteMageOccultCureIICastOnAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseWhiteMageOccultCureIII { get; set; }

        [Setting]
        [DefaultValue(65.0f)]
        public float WhiteMageOccultCureIIIHealthPercent { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int WhiteMageOccultCureIIIAllyCount { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultHoly { get; set; }
        #endregion

        #region Phantom Ninja
        [Setting]
        [DefaultValue(true)]
        public bool UseFumaShuriken { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSmoke { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLightningScroll { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFlameScroll { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseImage { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float ImageHealthPercent { get; set; }
        #endregion

        #region Phantom Dragoon
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultJump { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool OccultJumpMeleeRangeOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseLance { get; set; }
        #endregion

        #region Phantom Summoner
        [Setting]
        [DefaultValue(true)]
        public bool UseHellfire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseJudgmentBolt { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseThunderstorm { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMegaflare { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseEarthenWall { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float EarthenWallHealthPercent { get; set; }
        #endregion

        #region Phantom Blue Mage
        [Setting]
        [DefaultValue(true)]
        public bool UseOccultAero { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultMissile { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultAquaBreath { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultMightyGuard { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float OccultMightyGuardHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultWhiteWind { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float OccultWhiteWindHealthPercent { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float OccultWhiteWindMinimumOwnHealthPercent { get; set; }
        #endregion

        #region Phantom Necromancer
        [Setting]
        [DefaultValue(true)]
        public bool UseDrainTouch { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDeepFreeze { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseHellWind { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseChaosDrive { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseDoomsday { get; set; }

        // Every Necromancer attack bar Drain Touch costs 10% of maximum HP, unconditionally.
        [Setting]
        [DefaultValue(50.0f)]
        public float NecromancerMinimumHealthPercent { get; set; }
        #endregion
    }
}
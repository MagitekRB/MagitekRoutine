﻿using Magitek.Enumerations;
using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Bard
{
    [AddINotifyPropertyChangedInterface]
    public class BardSettings : PhysicalDpsSettings, IRoutineSettings
    {
        public BardSettings() : base(CharacterSettingsDirectory + "/Magitek/Bard/BardSettings.json") { }

        public static BardSettings Instance { get; set; } = new BardSettings();

        #region SingleTarget

        [Setting]
        [DefaultValue(true)]
        public bool UseHeavyShot { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseStraightShot { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBloodletter { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PrioritizeBloodletterDuringMagesBallard { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSidewinder { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseEmpyrealArrow { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DelayEmpyrealArrowUntilAPEnds { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int DontUseEmpyrealArrowWhenSongEndsInXSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePitchPerfect { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int UsePitchPerfectAtRepertoire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePitchPerfectAtTheEndOfWanderersMinuet { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int UsePitchPerfectWithinTheLastXSecondsOfWanderersMinuet { get; set; }

        #endregion

        #region DamageOverTime

        [Setting]
        [DefaultValue(4)]
        public int RefreshDotsWithLessThanXSecondsRemaining { get; set; }

        [Setting]
        [DefaultValue(850)]
        public int RefreshDotsWithXmsLeftAfterLastGCD { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DontDotIfCurrentTargetIsDyingSoon { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int DontDotIfCurrentTargetIsDyingWithinXSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DontDotIfMultiDotTargetIsDyingSoon { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int DontDotIfMultiDotTargetIsDyingWithinXSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseStormbite { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseCausticBite { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseIronJaws { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SnapShotWithIronJaws { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EnableMultiDotting { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool MultiDotWindBite { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int MultiDotWindBiteUpToXEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool MultiDotVenomousBite { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int MultiDotVenomousBiteUpToXEnemies { get; set; }

        #endregion

        #region AoE

        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseQuickNock { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int QuickNockEnemiesInCone { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRainOfDeath { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int RainOfDeathEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseShadowBite { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int ShadowBiteEnemies { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int ShadowBiteAfterBarrageEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseApexArrow { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBlastArrow { get; set; }

        [Setting]
        [DefaultValue(100)]
        public int UseApexArrowWithAtLeastXSoulVoice { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBuffedApexArrow { get; set; }

        [Setting]
        [DefaultValue(24)]
        public int UseBuffedApexArrowWithAtLeastXBonusDamage { get; set; }

        [Setting]
        [DefaultValue(80)]
        public int UseBuffedApexArrowWithAtLeastXSoulVoice { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int DontUseBlastArrowWhenAPEndsInXSeconds { get; set; }

        #endregion

        #region Songs

        [Setting]
        [DefaultValue(SongStrategyEnum.WM_MB_AP)]
        public SongStrategyEnum CurrentSongPlaylist { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSongs { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EndArmysPaeonEarly { get; set; }

        [Setting]
        [DefaultValue(2500)]
        public int EndArmysPaeonEarlyWithXMilliSecondsRemaining { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EndMagesBalladEarly { get; set; }

        [Setting]
        [DefaultValue(12500)]
        public int EndMagesBalladEarlyWithXMilliSecondsRemaining { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EndWanderersMinuetEarly { get; set; }

        [Setting]
        [DefaultValue(2500)]
        public int EndWanderersMinuetEarlyWithXMilliSecondsRemaining { get; set; }


        #endregion

        #region Cooldowns

        [Setting]
        [DefaultValue(BuffStrategy.Always)]
        public BuffStrategy UseCoolDowns { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRageingStrikes { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRageingStrikesOnlyDuringWanderersMinuet { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DelayRageingStrikesUntilBarrageIsReady { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DelayRageingStrikesDuringWanderersMinuet { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int DelayRageingStrikesDuringWanderersMinuetUntilXSecondsInWM { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBarrage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBarrageOnlyWithBuff { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBattleVoice { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseRadiantFinale { get; set; }

        #endregion

        #region Utilities

        [Setting]
        [DefaultValue(false)]
        public bool ForceTroubadour { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool RepellingShot { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool RepellingShotOnlyWhenTargeted { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicNaturesMinne { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicTroubadour { get; set; }

        #endregion

        [Setting]
        [DefaultValue(false)]
        public bool Dispel { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool NaturesMinne { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float NaturesMinneHealthPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool NaturesMinneTanks { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool NaturesMinneHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool NaturesMinneDps { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseEmpyrealArrow { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int Pvp_UseEmpyrealArrowCharges { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseFinalFantasia { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int Pvp_UseFinalFantasiaAlliesCount { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SilentNocturne { get; set; }
        #endregion
    }
}
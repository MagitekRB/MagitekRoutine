using Magitek.Enumerations;
using Magitek.Models.Roles;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.RedMage
{
    [AddINotifyPropertyChangedInterface]
    public class RedMageSettings : MagicDpsSettings, IRoutineSettings
    {
        public RedMageSettings() : base(CharacterSettingsDirectory + "/Magitek/RedMage/RedMageSettings.json") { }

        public static RedMageSettings Instance { get; set; } = new RedMageSettings();

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicMagickBarrier { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseAoe { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMelee { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Scatter { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SwiftcastScatter { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Acceleration { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Displacement { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Engagement { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CorpsACorps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool CorpsACorpsInMeleeRangeOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Fleche { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool MeleeComboBossesOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Moulinet { get; set; }

        [Setting]
        [DefaultValue(30)]
        public int EmboldenFinisherPercent { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int HoldAccelForEmboldenSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseContreSixte { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Ver2 { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Embolden { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Manafication { get; set; }

        [Setting]
        [DefaultValue(0)]
        public int ManaficationMinimumBlackAndWhiteMana { get; set; }

        [Setting]
        [DefaultValue(50)]
        public int ManaficationMaximumBlackAndWhiteMana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSmartTargeting { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LucidDreaming { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float LucidDreamingManaPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Vercure { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool VercureOutOfCombat { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureDualcast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureLongCast { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float VercureHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureTank { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureHealer { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureDps { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VercureSelf { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool VercureInCombo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Verraise { get; set; }

        [Setting]
        [DefaultValue(3.0f)]
        public float ResDelay { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VerraiseTank { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VerraiseHealer { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool VerraiseDps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool VerraiseInCombo { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SwiftcastVerthunderVeraero { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseReprise { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool MagickBarrier { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ForceMagickBarrier { get; set; }

        [Setting]
        [DefaultValue(0)]
        public int SaveCorpsACorpsCharges { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int SaveAccelChargesForMovement { get; set; }

        [Setting]
        [DefaultValue(RedMageOpenerStrategy.AlwaysUseOpener)]
        public RedMageOpenerStrategy RedMageOpenerStrategy { get; set; }

        public void CycleOpenerStrategy()
        {
            switch (RedMageOpenerStrategy)
            {
                case RedMageOpenerStrategy.NeverOpener:
                    RedMageOpenerStrategy = RedMageOpenerStrategy.OpenerOnlyBosses;
                    break;
                case RedMageOpenerStrategy.OpenerOnlyBosses:
                    RedMageOpenerStrategy = RedMageOpenerStrategy.AlwaysUseOpener;
                    break;
                case RedMageOpenerStrategy.AlwaysUseOpener:
                    RedMageOpenerStrategy = RedMageOpenerStrategy.NeverOpener;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseGrandImpact { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseEnchantedRiposte { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseEnchantedZwerchhau { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseEnchantedRedoublement { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseScorch { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePrefulgence { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseViceOfThorns { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseForte { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float Pvp_ForteHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseEmbolden { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float Pvp_EmboldenTargetHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseCorpsACorps { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseDisplacement { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float Pvp_DisplacementHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseResolution { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float Pvp_ResolutionTargetHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseSouthernCross { get; set; }
        #endregion
    }
}

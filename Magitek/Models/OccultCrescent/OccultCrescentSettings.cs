using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

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
        public bool RomeosBallad_KnowledgeCrystal { get; set; }

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
        [DefaultValue(true)]
        public bool PrayKnowledgeCrystal { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float PrayHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseOccultHeal { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
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
        public bool CounterstanceKnowledgeCrystal { get; set; }

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
    }
}
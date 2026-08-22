using Magitek.Models.Roles;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Astrologian
{
    [AddINotifyPropertyChangedInterface]
    public class AstrologianSettings : HealerSettings, IRoutineSettings
    {
        public AstrologianSettings() : base(CharacterSettingsDirectory + "/Magitek/Astrologian/AstrologianSettings.json") { }

        public static AstrologianSettings Instance { get; set; } = new AstrologianSettings();

        [Setting]
        [DefaultValue(70.0f)]
        public float RestHealthPercent { get; set; }

        #region Combat

        [Setting]
        [DefaultValue(true)]
        public bool Malefic { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool InterruptHealing { get; set; }

        [Setting]
        [DefaultValue(90.0f)]
        public float InterruptHealingHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool InterruptDamageToHeal { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DoDamage { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Combust { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool CombustMultipleTargets { get; set; }

        [Setting]
        [DefaultValue(3050)]
        public int CombustRefreshMSeconds { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTTDForCombust { get; set; }

        [Setting]
        [DefaultValue(21)]
        public int DontCombustIfEnemyDyingWithin { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DontDotIfMoreEnemies { get; set; }

        [Setting]
        [DefaultValue(5)]
        public int DontDotIfMoreEnemiesThan { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Gravity { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int GravityEnemies { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float MinimumManaPercentToDoDamage { get; set; }

        [Setting]
        [DefaultValue(20)]
        public int DoDamageIfTimeLeftLessThan { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool SmartAoe { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool Oracle { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int OracleEnemies { get; set; }

        #endregion

        #region Buffs

        [Setting]
        [DefaultValue(true)]
        public bool Lightspeed { get; set; }

        [Setting]
        [DefaultValue(40f)]
        public float LightspeedHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LightspeedWithDivination { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LightspeedWithNeutralSect { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LucidDreaming { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float LucidDreamingManaPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Exaltation { get; set; }

        [Setting]
        [DefaultValue(40f)]
        public float ExaltationHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Divination { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int DivinationAllies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool NeutralSect { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float NeutralSectHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SunSign { get; set; }

        #endregion

        #region Heals

        [Setting]
        [DefaultValue(2)]
        public int AoeNeedHealingLightParty { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int AoeNeedHealingFullParty { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DisableSingleHealWhenNeedAoeHealing { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float AoEHealHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Synastry { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float SynastryHealthPercent { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int SynastryAmountOfPeople { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SynastryTankOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EssentialDignity { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EssentialDignityTankOnly { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float EssentialDignityHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Helios { get; set; }

        [Setting]
        [DefaultValue(60)]
        public float HeliosHealthPercent { get; set; }

        [Setting]
        [DefaultValue(20)]
        public float HeliosMinManaPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalHelios { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float DiurnalHeliosHealthPercent { get; set; }

        [Setting]
        [DefaultValue(30.0f)]
        public float DiurnalHeliosMinManaPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalHeliosNoSwiftcast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Horoscope { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float HoroscopeHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CelestialOpposition { get; set; }

        [Setting]
        [DefaultValue(75)]
        public float CelestialOppositionHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBenefic { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float DiurnalBeneficMinMana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBeneficOnTanks { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBeneficOnHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalBeneficOnDps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalBeneficKeepUpOnTanks { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalBeneficKeepUpOnHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalBeneficKeepUpOnDps { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float DiurnalBeneficHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBeneficWhileMoving { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float DiurnalBeneficWhileMovingMinMana { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool DiurnalBeneficDontBeneficUnlessUnderTank { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBeneficDontBeneficUnlessUnderHealer { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DiurnalBeneficDontBeneficUnlessUnderDps { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float DiurnalBeneficDontBeneficUnlessUnderHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Benefic { get; set; }

        [Setting]
        [DefaultValue(55.0f)]
        public float BeneficHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Benefic2 { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool NoBeneficIfBenefic2Available { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float Benefic2HealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Benefic2AlwaysWithEnhancedBenefic2 { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CelestialIntersection { get; set; }

        [Setting]
        [DefaultValue(90.0f)]
        public float CelestialIntersectionHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CelestialIntersectionTankOnly { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CollectiveUnconscious { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int CollectiveUnconsciousAllies { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float CollectiveUnconsciousHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EarthlyStar { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int EarthlyStarEnemiesNearTarget { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool StellarDetonation { get; set; }

        [Setting]
        [DefaultValue(5)]
        public int StellarDetonationPullEndingSeconds { get; set; }

        [Setting]
        [DefaultValue(4)]
        public int EarthlyDominanceCount { get; set; }

        [Setting]
        [DefaultValue(70f)]
        public float EarthlyDominanceHealthPercent { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int GiantDominanceCount { get; set; }

        [Setting]
        [DefaultValue(60)]
        public float GiantDominanceHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Macrocosmos { get; set; }

        [Setting]
        [DefaultValue(65f)]
        public float MacrocosmosHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool WeaveOGCDHeals { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DontLetTheDRKDie { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogic_NeutralSectAspectedHelios { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogic_Exaltation { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogic_Macrocosmos { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogic_CollectiveUnconscious { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogic_Lightspeed { get; set; }

        #endregion

        #region Dispels

        [Setting]
        [DefaultValue(true)]
        public bool Dispel { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool DispelOnlyAbove { get; set; }

        [Setting]
        [DefaultValue(75.0f)]
        public float DispelOnlyAboveHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool AutomaticallyDispelAnythingThatsDispellable { get; set; }


        #endregion

        #region AlliancesAndPets

        [Setting]
        [DefaultValue(false)]
        public bool IgnoreAlliance { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HealAllianceHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HealAllianceTanks { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HealAllianceDps { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool ResAllianceHealers { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ResAllianceTanks { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ResAllianceDps { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool HealAllianceOnlyBenefic { get; set; }

        #endregion

        #region Cards

        [Setting]
        [DefaultValue(true)]
        public bool DrawCards { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseMinorArcana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Play { get; set; }

        [Setting]
        [DefaultValue(25)]
        public int DontPlayWhenCombatTimeIsLessThan { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool AlignCardsWithDivination { get; set; }

        // The reactive cards carry their own controls; the anticipatory cards (the Bole and
        // the Spire) are played by fight logic at incoming tankbusters and have no threshold.
        [Setting]
        [DefaultValue(true)]
        public bool PlayArrow { get; set; }

        [Setting]
        [DefaultValue(80)]
        public int ArrowHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool PlayEwer { get; set; }

        [Setting]
        [DefaultValue(80)]
        public int EwerHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CardRuleDefaultToMinorArcana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LadyOfCrowns { get; set; }

        [Setting]
        [DefaultValue(80.0f)]
        public float LadyOfCrownsHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool LordOfCrowns { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int LordOfCrownsEnemies { get; set; }
        #endregion

        #region Card Weights

        // Two tables, one per hand: the Balance gives its full 6% to melee DPS and tanks, the
        // Spear to ranged DPS and healers, so each card ranks the half of the party it empowers.
        // Blue Mage counts as every role in game data, so it appears in both tables.

        #region AstralCardWeights
        [Setting]
        [DefaultValue(4)]
        public int MnkAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(5)]
        public int DrgAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(6)]
        public int NinAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(1)]
        public int SamAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(3)]
        public int RprAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(2)]
        public int VprAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(10)]
        public int PldAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(9)]
        public int WarAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(7)]
        public int DrkAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(8)]
        public int GnbAstralCardWeight { get; set; }
        [Setting]
        [DefaultValue(11)]
        public int BluAstralCardWeight { get; set; }

        #endregion

        #region UmbralCardWeights
        [Setting]
        [DefaultValue(6)]
        public int BrdUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(4)]
        public int MchUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(7)]
        public int DncUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(2)]
        public int BlmUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(3)]
        public int SmnUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(5)]
        public int RdmUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(1)]
        public int PctUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(11)]
        public int WhmUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(10)]
        public int SchUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(8)]
        public int AstUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(9)]
        public int SgeUmbralCardWeight { get; set; }
        [Setting]
        [DefaultValue(12)]
        public int BluUmbralCardWeight { get; set; }

        #endregion

        #endregion

        #region FightLogic
        [Setting]
        [DefaultValue(true)]
        public bool FightLogicNeutralSect { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicEarthlyStar { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool EarthlyStarCenterParty { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicCollectiveUnconscious { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool CollectiveUnconsciousCenterParty { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicHoroscope { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicAspectedHelios { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicCelestialIntersection { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicExaltation { get; set; }
        #endregion

        #region PVP
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_FallMalefic { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_AspectedBenefic { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float Pvp_AspectedBeneficHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_GravityII { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int Pvp_GravityIIEnemies { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_DoubleCast { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Macrocosmos { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Microcosmos { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float Pvp_MicrocosmosHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_MinorArcana { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_Oracle { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_CelestialRiver { get; set; }

        [Setting]
        [DefaultValue(1)]
        public int Pvp_CelestialRiverNearbyAllies { get; set; }

        [Setting]
        [DefaultValue(85.0f)]
        public float Pvp_CelestialRiverHealthPercent { get; set; }

        [Setting]
        [DefaultValue(3)]
        public int Pvp_MacrocosmosEnemies { get; set; }

        [Setting]
        [DefaultValue(2)]
        public int Pvp_LordOfCrownsEnemies { get; set; }
        #endregion

    }
}

using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Magitek.Extensions.GameObjectExtensions;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class Heals
    {

        public static int AoeThreshold => PartyManager.NumMembers > 4 ? AstrologianSettings.Instance.AoeNeedHealingFullParty : AstrologianSettings.Instance.AoeNeedHealingLightParty;

        public static bool NeedAoEHealing()
        {
            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.AoEHealHealthPercent);

            var needAoEHealing = targets.Count() >= AoeThreshold;

            if (needAoEHealing)
                return true;

            return false;
        }


        #region Single Target No Regen Heals
        public static async Task<bool> Benefic()
        {
            if (!AstrologianSettings.Instance.Benefic)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // oGCD-first, same rule as Benefic II below.
            var beneficThreshold = AstrologianSettings.Instance.BeneficHealthPercent;
            if (AstrologianSettings.Instance.PreferOgcdHeals && Utilities.Routines.Astrologian.SingleTargetOgcdHealReady())
                beneficThreshold = System.Math.Min(beneficThreshold, AstrologianSettings.Instance.GcdHealOnlyBelowHealthPercent);

            if (Globals.InParty)
            {
                foreach (var ally in Group.CastableAlliesWithin30)
                {
                    if (Utilities.Routines.Astrologian.DontBenefic.Contains(ally.Name))
                        continue;

                    if (ally.CheckTankImmunity() == TankImmunityCheck.DontHealThem)
                        continue;

                    if (ally.CurrentHealthPercent > beneficThreshold
                        || ally.CurrentHealth <= 0)
                        continue;

                    if (!ally.HasAura(Auras.AspectedBenefic))
                        return await CastBenefic(ally);

                    if (!AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderDps
                        && ally.IsDps())
                        return await CastBenefic(ally);

                    if (!AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderHealer
                        && ally.IsHealer())
                        return await CastBenefic(ally);

                    if (!AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderTank
                        && ally.IsTank())
                        return await CastBenefic(ally);

                    if (AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderDps
                        && ally.IsDps()
                        && ally.CurrentHealthPercent < AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderHealth)
                        return await CastBenefic(ally);

                    if (AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderHealer && ally.IsHealer()
                        && ally.CurrentHealthPercent < AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderHealth)
                        return await CastBenefic(ally);

                    if (AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderTank && ally.IsTank()
                        && ally.CurrentHealthPercent < AstrologianSettings.Instance.DiurnalBeneficDontBeneficUnlessUnderHealth)
                        return await CastBenefic(ally);
                }

                return false;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent > beneficThreshold)
                    return false;

                if (Spells.Benefic2.IsKnownAndReady())
                {
                    if (Core.Me.HasAura(Auras.EnhancedBenefic2)
                        && AstrologianSettings.Instance.Benefic2AlwaysWithEnhancedBenefic2
                        && Core.Me.CurrentManaPercent >= Spells.Benefic2.Cost)
                        return await Spells.Benefic2.Heal(Core.Me);

                    if (Core.Me.CurrentHealthPercent <= AstrologianSettings.Instance.Benefic2HealthPercent
                        && Core.Me.CurrentManaPercent >= Spells.Benefic2.Cost)
                        return await Spells.Benefic2.Heal(Core.Me);
                }

                return await Spells.Benefic.Heal(Core.Me);
            }

            async Task<bool> CastBenefic(GameObject ally)
            {
                if (AstrologianSettings.Instance.NoBeneficIfBenefic2Available)
                    if (Spells.Benefic2.IsKnown() && AstrologianSettings.Instance.Benefic2)
                        return await Spells.Benefic2.Heal(ally);

                return await Spells.Benefic.Heal(ally);
            }
        }

        public static async Task<bool> Benefic2()
        {
            if (!AstrologianSettings.Instance.Benefic2)
                return false;

            if (!Spells.Benefic2.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // oGCD-first: while a free single-target tool is ready, only hardcast for real
            // emergencies - the weave window answers everything above that line.
            var benefic2Threshold = AstrologianSettings.Instance.Benefic2HealthPercent;
            if (AstrologianSettings.Instance.PreferOgcdHeals && Utilities.Routines.Astrologian.SingleTargetOgcdHealReady())
                benefic2Threshold = System.Math.Min(benefic2Threshold, AstrologianSettings.Instance.GcdHealOnlyBelowHealthPercent);

            var shouldBenefic2WithEnhancedBenefic2 = AstrologianSettings.Instance.Benefic2AlwaysWithEnhancedBenefic2
                && Core.Me.CurrentManaPercent >= Spells.Benefic2.Cost;

            if (Globals.InParty)
            {
                // Added this to test (Exmortem)
                if (Casting.LastSpell == Spells.Benefic2)
                {
                    if (Casting.LastSpellTarget != Globals.HealTarget)
                    {
                        if (Core.Me.HasAura(Auras.EnhancedBenefic2)
                            && Globals.HealTarget?.CurrentHealthPercent <= AstrologianSettings.Instance.BeneficHealthPercent
                            && shouldBenefic2WithEnhancedBenefic2)
                        {
                            return await Spells.Benefic2.Heal(Globals.HealTarget);
                        }
                    }
                }

                if (Core.Me.CurrentHealthPercent < benefic2Threshold)
                    return await Spells.Benefic2.Heal(Core.Me);

                var benefic2Target = Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontBenefic2.Contains(r.Name)
                && r.CurrentHealth > 0
                && r.CurrentHealthPercent <= benefic2Threshold
                && r.CheckTankImmunity() == TankImmunityCheck.HealThem);

                if (benefic2Target == null)
                    return false;

                // Added this to test (Exmortem)
                if (Casting.LastSpell == Spells.Benefic2)
                {
                    if (Casting.LastSpellTarget == benefic2Target)
                        return false;
                }

                return await Spells.Benefic2.Heal(benefic2Target);
            }
            else
            {
                if (Core.Me.CurrentHealthPercent > benefic2Threshold)
                    return false;

                return await Spells.Benefic2.Heal(Core.Me);
            }
        }

        public static async Task<bool> DontLetTheDrkDie()
        {
            if (!AstrologianSettings.Instance.DontLetTheDRKDie)
                return false;

            if (!Globals.InParty)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            var walkingDeadMan = Group.CastableTanks.FirstOrDefault(r =>
                !Utilities.Routines.Astrologian.DontBenefic2.Contains(r.Name)
                && r.HasAura(Auras.WalkingDead)
                && r.CurrentHealthPercent < 100);

            if (walkingDeadMan == null)
                return false;

            return await Spells.Benefic2.Heal(walkingDeadMan);
        }

        public static async Task<bool> CelestialIntersection()
        {
            if (!AstrologianSettings.Instance.CelestialIntersection)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.CelestialIntersection.IsKnownAndReady())
                return false;

            if (Casting.LastSpell == Spells.CelestialIntersection)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (AstrologianSettings.Instance.CelestialIntersectionTankOnly)
            {
                var celestialIntersectionTank = Group.CastableTanks.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontCelestialIntersection.Contains(r.Name)
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.CelestialIntersectionHealthPercent
                && Combat.Enemies.Any(x => x.TargetCharacter == r)
                && !r.HasAura(Auras.CelestialIntersection)
                && r.CheckTankImmunity() == TankImmunityCheck.HealThem);

                if (celestialIntersectionTank == null)
                    return false;

                return await Spells.CelestialIntersection.Heal(celestialIntersectionTank, false);
            }

            var celestialIntersectionTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontCelestialIntersection.Contains(r.Name)
            && r.CurrentHealthPercent <= AstrologianSettings.Instance.CelestialIntersectionHealthPercent
            && !r.HasAura(Auras.CelestialIntersection)
            && r.CheckTankImmunity() == TankImmunityCheck.HealThem);

            if (celestialIntersectionTarget == null)
                return false;

            return await Spells.CelestialIntersection.Heal(celestialIntersectionTarget);
        }

        public static async Task<bool> EssentialDignity()
        {
            if (!AstrologianSettings.Instance.EssentialDignity)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // At full charges every tick of recharge is wasted free healing, so spend one
            // far more liberally than the normal threshold.
            var edThreshold = AstrologianSettings.Instance.EssentialDignityHealthPercent;
            if (Spells.EssentialDignity.Charges >= Spells.EssentialDignity.MaxCharges)
                edThreshold = System.Math.Max(edThreshold, AstrologianSettings.Instance.EssentialDignityCappedHealthPercent);

            if (Globals.InParty)
            {
                if (AstrologianSettings.Instance.EssentialDignityTankOnly)
                {
                    var tar = Group.CastableTanks.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontEssentialDignity.Contains(r.Name)
                    && r.IsAlive
                    && r.CurrentHealthPercent <= edThreshold
                    && r.CheckTankImmunity() == TankImmunityCheck.HealThem);

                    if (tar == null)
                        return false;

                    return await Spells.EssentialDignity.Heal(tar, false);
                }

                if (Core.Me.CurrentHealthPercent < edThreshold)
                    return await Spells.EssentialDignity.Heal(Core.Me, false);

                var essentialDignityTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontEssentialDignity.Contains(r.Name)
                && r.CurrentHealth > 0
                && r.CurrentHealthPercent <= edThreshold
                && r.CheckTankImmunity() == TankImmunityCheck.HealThem);

                if (essentialDignityTarget == null)
                    return false;

                return await Spells.EssentialDignity.Heal(essentialDignityTarget);
            }
            else
            {
                if (Core.Me.CurrentHealthPercent > edThreshold)
                    return false;

                return await Spells.EssentialDignity.Heal(Core.Me, false);
            }
        }

        public static async Task<bool> Exaltation()
        {
            if (!AstrologianSettings.Instance.Exaltation)
                return false;

            if (!Globals.InParty)
                return false;

            if (!Spells.Exaltation.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // Fight-logic mode reserves Exaltation for catalogued busters. When the enemies
            // we are fighting have none catalogued, fall through to the threshold path
            // instead of never firing at all.
            if (AstrologianSettings.Instance.FightLogicExaltation && FightLogic.EnemyHasAnyTankbusterLogic())
            {

                var tankBusterOnPartyMember = FightLogic.EnemyIsCastingTankBuster();

                if (tankBusterOnPartyMember == null)
                    return false;

                return await FightLogic.DoAndBuffer(
                    Spells.Exaltation.HealAura(tankBusterOnPartyMember, Auras.Exaltation));
            }

            var tankToShieldAndHeal = Group.CastableTanks.FirstOrDefault(x =>
                x.CurrentHealthPercent < AstrologianSettings.Instance.ExaltationHealthPercent &&
                x.CheckTankImmunity() == TankImmunityCheck.HealThem);

            if (tankToShieldAndHeal == null)
                return false;

            return await Spells.Exaltation.HealAura(tankToShieldAndHeal, Auras.Exaltation);
        }

        #endregion

        #region AOE No Regen Heals

        public static async Task<bool> Helios()
        {
            if (!AstrologianSettings.Instance.Helios)
                return false;

            if (Core.Me.CurrentManaPercent <= AstrologianSettings.Instance.HeliosMinManaPercent)
                return false;

            // oGCD-first, same rule as Aspected Helios below.
            var heliosThreshold = AstrologianSettings.Instance.HeliosHealthPercent;
            if (AstrologianSettings.Instance.PreferOgcdHeals && Utilities.Routines.Astrologian.AoeOgcdHealReady())
                heliosThreshold = System.Math.Min(heliosThreshold, AstrologianSettings.Instance.GcdHealOnlyBelowHealthPercent);

            var heliosCount = PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r => r.CurrentHealth > 0
            && r.WithinSpellRange(Spells.Helios.Radius)
            && r.CurrentHealthPercent <= r.AdjustHealthThresholdByRegen(heliosThreshold));

            //if (heliosCount < AstrologianSettings.Instance.HeliosAllies)
            if (heliosCount <= AoeThreshold)
                return false;

            return await Spells.Helios.Heal(Core.Me, false);
        }

        public static async Task<bool> LadyOfCrowns()
        {
            //if (ActionResourceManager.Astrologian.CurrentDraw != ActionResourceManager.Astrologian.AstrologianDraw.Umbral)
            //    return false;

            if (!AstrologianSettings.Instance.LadyOfCrowns)
                return false;

            if (!Spells.LadyofCrowns.IsKnownAndReady())
                return false;

            if (!Globals.InParty && Core.Me.CurrentHealthPercent <= Core.Me.AdjustHealthThresholdByRegen(AstrologianSettings.Instance.LadyOfCrownsHealthPercent))
                return await Spells.LadyofCrowns.Heal(Core.Me);

            if (Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= r.AdjustHealthThresholdByRegen(AstrologianSettings.Instance.LadyOfCrownsHealthPercent)) <= AoeThreshold)
                return false;

            return await Spells.LadyofCrowns.Heal(Core.Me);
        }

        #endregion

        #region Single Target Regen Heals

        public static async Task<bool> AspectedBenefic()
        {
            if (!AstrologianSettings.Instance.DiurnalBenefic)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (MovementManager.IsMoving)
            {
                if (!AstrologianSettings.Instance.DiurnalBeneficWhileMoving)
                    return false;

                if (Core.Me.CurrentManaPercent <= AstrologianSettings.Instance.DiurnalBeneficWhileMovingMinMana)
                    return false;
            }

            if (Core.Me.CurrentManaPercent < AstrologianSettings.Instance.DiurnalBeneficMinMana)
                return false;

            if (Globals.InParty)
            {
                if (await AspectedBeneficTanks())
                    return true;
                if (await AspectHeliosInsteadOfDiurnalBenefic())
                    return true;
                if (await AspectedBeneficHealers())
                    return true;
                return await AspectedBeneficDps();
            }
            else
            {
                if (Core.Me.HasAura(Auras.AspectedBenefic))
                    return false;

                if (!AstrologianSettings.Instance.DiurnalBeneficKeepUpOnHealers
                    && Core.Me.CurrentHealthPercent > AstrologianSettings.Instance.DiurnalBeneficHealthPercent)
                    return false;

                return await Spells.AspectedBenefic.HealAura(Core.Me, Auras.AspectedBenefic);
            }
        }

        private static async Task<bool> AspectedBeneficTanks()
        {
            if (Core.Me.HasAllAuras(new List<uint> { Auras.NeutralSect, Auras.NeutralSectShield }))
            {
                var tankShieldTarget = Group.CastableTanks.FirstOrDefault(x => !x.HasAura(Auras.NeutralSectShield));

                if (tankShieldTarget != null)
                    return await Spells.AspectedBenefic.HealAura(tankShieldTarget, Auras.NeutralSectShield);
            }

            if (!AstrologianSettings.Instance.DiurnalBeneficOnTanks)
                return false;

            var diurnalBeneficTarget = AstrologianSettings.Instance.DiurnalBeneficKeepUpOnTanks ?
                Group.CastableTanks.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && !r.HasMyAura(Auras.AspectedBenefic)) :
                Group.CastableTanks.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && !r.HasAura(Auras.AspectedBenefic)
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.DiurnalBeneficHealthPercent);

            if (diurnalBeneficTarget == null)
                return false;

            return await Spells.AspectedBenefic.HealAura(diurnalBeneficTarget, Auras.AspectedBenefic);
        }

        private static async Task<bool> AspectHeliosInsteadOfDiurnalBenefic()
        {
            if (!AstrologianSettings.Instance.DiurnalHelios)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // Add check to ensure we don't double cast
            if (Casting.LastSpell == Spells.AspectedHelios)
                return false;

            if (!Spells.AspectedHelios.IsKnown())
                return false;

            // RB masks the spell to Helios Conjunction at 96+, but not the aura it applies.
            var heliosAura = (uint)(Spells.HeliosConjunction.IsKnown() ? Auras.HeliosConjunction : Auras.AspectedHelios);

            var alliesNeedingRegen = Group.CastableAlliesWithin15.Where(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && !r.HasMyAura(Auras.AspectedBenefic)
                && !r.HasMyAura(Auras.AspectedHelios)
                && !r.HasMyAura(Auras.HeliosConjunction)
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.DiurnalBeneficHealthPercent).ToList();

            if (alliesNeedingRegen.Count() <= AoeThreshold)
                return false;

            return await Spells.AspectedHelios.HealAura(Core.Me, heliosAura);
        }

        private static async Task<bool> AspectedBeneficHealers()
        {
            if (!AstrologianSettings.Instance.DiurnalBeneficOnHealers)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            var aspectedBeneficTarget = AstrologianSettings.Instance.DiurnalBeneficKeepUpOnHealers
                ? Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && r.IsHealer()
                && !r.HasMyAura(Auras.AspectedBenefic))
                : Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && r.IsHealer()
                && !r.HasMyAura(Auras.AspectedBenefic)
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.DiurnalBeneficHealthPercent);

            if (aspectedBeneficTarget == null)
                return false;

            return await Spells.AspectedBenefic.HealAura(aspectedBeneficTarget, Auras.AspectedBenefic);
        }

        private static async Task<bool> AspectedBeneficDps()
        {
            if (!AstrologianSettings.Instance.DiurnalBeneficOnDps)
                return false;

            if (AstrologianSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            var aspectedBeneficTarget = AstrologianSettings.Instance.DiurnalBeneficKeepUpOnDps
                ? Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && !r.IsTank()
                && !r.IsHealer()
                && !r.HasMyAura(Auras.AspectedBenefic))
                : Group.CastableAlliesWithin30.FirstOrDefault(r => !Utilities.Routines.Astrologian.DontDiurnalBenefic.Contains(r.Name)
                && r.CurrentHealth > 0
                && !r.IsTank()
                && !r.IsHealer()
                && !r.HasMyAura(Auras.AspectedBenefic)
                && r.CurrentHealthPercent <= r.AdjustHealthThresholdByRegen(AstrologianSettings.Instance.DiurnalBeneficHealthPercent));

            if (aspectedBeneficTarget == null)
                return false;

            return await Spells.AspectedBenefic.HealAura(aspectedBeneficTarget, Auras.AspectedBenefic);
        }



        #endregion

        #region Aoe Regen Heals
        public static async Task<bool> AspectedHelios()
        {
            if (!AstrologianSettings.Instance.DiurnalHelios)
                return false;

            if (!Spells.AspectedHelios.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.NeutralSect) &&
                Group.CastableAlliesWithin15.Count(x => !x.HasAura(Auras.NeutralSectShield)) >= AoeThreshold && !Core.Me.HasAura(Auras.NeutralSectShield))
                // Swiftcast stays reserved for Ascend. While moving, the Lightspeed pairing
                // with Neutral Sect (on by default) is what makes this castable; without it
                // the shield waits for the next standstill.
                return !MovementManager.IsMoving
                    && await Spells.AspectedHelios.HealAura(Core.Me, Auras.NeutralSectShield, false);

            if (Casting.LastSpell == Spells.AspectedHelios)
                return false;

            if (Core.Me.CurrentManaPercent <= AstrologianSettings.Instance.DiurnalHeliosMinManaPercent)
                return false;

            // oGCD-first: while a free AoE tool is ready, group healing belongs to the weave
            // window unless someone is genuinely low.
            var aspectedHeliosThreshold = AstrologianSettings.Instance.DiurnalHeliosHealthPercent;
            if (AstrologianSettings.Instance.PreferOgcdHeals && Utilities.Routines.Astrologian.AoeOgcdHealReady())
                aspectedHeliosThreshold = System.Math.Min(aspectedHeliosThreshold, AstrologianSettings.Instance.GcdHealOnlyBelowHealthPercent);

            var diurnalHeliosCount =
                PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r => r.CurrentHealth > 0 &&
                                                        r.WithinSpellRange(Spells.AspectedHelios.Radius) &&
                                                        r.CurrentHealthPercent <=
                                                        aspectedHeliosThreshold &&
                                                        !r.HasAura(Auras.AspectedHelios, true) && !r.HasAura(Auras.HeliosConjunction, true));

            if (diurnalHeliosCount >= AoeThreshold)
            {
                // RB masks the spell to Helios Conjunction at 96+, but not the aura it applies.
                var heliosAura = (uint)(Spells.HeliosConjunction.IsKnown() ? Auras.HeliosConjunction : Auras.AspectedHelios);

                return await Spells.AspectedHelios.HealAura(Core.Me, heliosAura);
            }
            return false;
        }

        public static async Task<bool> CelestialOpposition()
        {
            if (!AstrologianSettings.Instance.CelestialOpposition)
                return false;

            if (!Spells.CelestialOpposition.IsKnownAndReady())
                return false;

            if (Casting.LastSpell == Spells.Horoscope)
                return false;

            var celestialOppositionCount = Group.CastableAlliesWithin20.Count(r => r.CurrentHealth > 0
            && r.CurrentHealthPercent <= AstrologianSettings.Instance.CelestialOppositionHealthPercent);

            if (celestialOppositionCount < AoeThreshold)
                return false;

            return await Spells.CelestialOpposition.HealAura(Core.Me, Auras.Opposition, false);
        }

        public static async Task<bool> CollectiveUnconscious()
        {
            if (!AstrologianSettings.Instance.CollectiveUnconscious)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.CollectiveUnconscious.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.FightLogicCollectiveUnconscious && FightLogic.EnemyIsCastingAoe() &&
                Group.CastableAlliesWithin30.Count(x => x.WithinSpellRange(Spells.CollectiveUnconscious.Radius)) >= AoeThreshold)
                return await FightLogic.DoAndBuffer(
                    Spells.CollectiveUnconscious.HealAura(Core.Me, Auras.CollectiveUnconsciousMitigation));


            if (Group.CastableAlliesWithin30.Count(r => r.WithinSpellRange(30)
                                                    && r.IsAlive
                                                    && r.CurrentHealthPercent <= AstrologianSettings.Instance.CollectiveUnconsciousHealth)
                                                    < AoeThreshold)
                return false;

            return await Spells.CollectiveUnconscious.HealAura(Core.Me, Auras.WheelOfFortune, false);
        }

        #endregion

        #region Delayed Heals
        public static async Task<bool> EarthlyStar()
        {
            if (!Core.Me.InCombat)
                return false;


            var earthlyStarLocation = Utilities.Routines.Astrologian.EarthlyStarLocation;

            // Lazy on purpose: this method runs on every heal pulse, but the only consumers of
            // the party list are the two dominance-gated pop checks below, which fail their
            // aura gates on the overwhelming majority of pulses. PartyManager (rather than the
            // caster-centred cached Group collections) is required here because the star heals
            // around ITS placement — allies inside the star's radius can be outside the
            // caster's 30y, and a cached caster-centred list would miss them.
            List<BattleCharacter> EarthlyStarTargets() =>
                PartyManager.VisibleMembers.Select(r => r.BattleCharacter).ToList();

            // The pop checks gate on Stellar Detonation's own action: the base Earthly Star
            // action sits on its 60s recast for the star's whole deployment, so putting these
            // behind EarthlyStar.IsKnownAndReady() made them unreachable.

            // Pop a cooking star just before the pull ends so the damage half still lands —
            // auto-detonation after everything is dead whiffs it. A star that will still reach
            // Giant Dominance before the pull ends is left to mature for the full explosion.
            // Wall-clock, not the summed estimate: the pull ends when the last enemy dies, and
            // in multi-target pulls the sum overstates that badly (four mobs at 3s each sum to
            // 12s), so a summed check never trips and the star expires after combat unspent.
            if (AstrologianSettings.Instance.StellarDetonation
                && Spells.StellarDetonation.IsKnownAndReady()
                && Utilities.Routines.Astrologian.EarthlyStarLocation != Vector3.Zero
                && Utilities.Combat.CombatWallClockTimeLeft > 0
                && Utilities.Combat.CombatWallClockTimeLeft <= AstrologianSettings.Instance.StellarDetonationPullEndingSeconds
                && (Core.Me.HasAura(Auras.GiantDominance)
                    || Core.Me.HasAura(Auras.EarthlyDominance, false, Utilities.Combat.CombatWallClockTimeLeft * 1000)))
                return await Spells.StellarDetonation.Heal(Core.Me);

            if (Core.Me.HasAura(Auras.EarthlyDominance)
                && Spells.StellarDetonation.IsKnownAndReady()
                && Utilities.Routines.Astrologian.EarthlyStarLocation != Vector3.Zero
                && AstrologianSettings.Instance.StellarDetonation)
            {
                if (EarthlyStarTargets().Count(r => r.Distance(earthlyStarLocation) <= 30
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.EarthlyDominanceHealthPercent) > AstrologianSettings.Instance.EarthlyDominanceCount)
                    return await Spells.StellarDetonation.Heal(Core.Me);
            }

            if (Core.Me.HasAura(Auras.GiantDominance)
                && Spells.StellarDetonation.IsKnownAndReady()
                && Utilities.Routines.Astrologian.EarthlyStarLocation != Vector3.Zero
                && AstrologianSettings.Instance.StellarDetonation)
            {
                if (EarthlyStarTargets().Count(r => r.Distance(earthlyStarLocation) <= 30
                && r.CurrentHealthPercent <= AstrologianSettings.Instance.GiantDominanceHealthPercent) > AstrologianSettings.Instance.GiantDominanceCount)
                    return await Spells.StellarDetonation.Heal(Core.Me);
            }

            if (!Spells.EarthlyStar.IsKnownAndReady())
                return false;

            if (!AstrologianSettings.Instance.EarthlyStar)
                return false;


            if (!Core.Me.HasTarget)
                return false;

            // The plant anchors to the hard target's position, so it must be an enemy —
            // with an ally targeted the star would land at their feet on cooldown.
            if (!Core.Target.ThoroughCanAttack())
                return false;

            // Hold the plant until the pull's time-to-death is computable: freshly tracked
            // enemies report zero and enemies that have not taken damage yet saturate the
            // total to int.MaxValue (float-to-int conversion saturates on .NET Core), so a
            // pull still being gathered reads as one or the other — and a star planted
            // mid-gather lands where the party will not be.
            if (Utilities.Combat.CombatTotalTimeLeft <= 0 || Utilities.Combat.CombatTotalTimeLeft >= int.MaxValue)
                return false;

            // Plant proactively: an unpopped star detonates on its own at the end of Giant
            // Dominance with the full damage + heal, so holding the plant until allies are
            // hurt only delays it. Heal-need gating lives in the Stellar Detonation checks above.
            if (Core.Target.EnemiesNearby(30).Count() >= AstrologianSettings.Instance.EarthlyStarEnemiesNearTarget
                && Core.Target.WithinSpellRange(30))
                if (await Spells.EarthlyStar.Cast(Core.Target))
                {
                    Utilities.Routines.Astrologian.EarthlyStarLocation = Core.Target.Location;
                    return true;
                }
            return false;
        }

        public static async Task<bool> Horoscope()
        {
            if (!AstrologianSettings.Instance.Horoscope)
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.HoroscopeHealthPercent) < AoeThreshold)
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.HasMyAura(Auras.Horoscope)) >= AoeThreshold)
                return await AspectedHelios() ? true : await Spells.Helios.Cast(Core.Me);

            if (await Spells.Horoscope.Cast(Core.Me))
                if (!await AspectedHelios())
                    return await Spells.Helios.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> HoroscopePop()
        {
            if (!AstrologianSettings.Instance.Horoscope)
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.HasMyAura(Auras.HoroscopeHelios) && r.CurrentHealthPercent <= AstrologianSettings.Instance.HoroscopeHealthPercent) < AoeThreshold)
                return false;

            return await Spells.Horoscope.Cast(Core.Me);
        }

        public static async Task<bool> Macrocosmos()
        {
            if (!AstrologianSettings.Instance.Macrocosmos)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.InParty)
                return false;

            if (!Spells.Macrocosmos.IsKnown())
                return false;

            if (Core.Me.HasMyAura(Auras.Macrocosmos))
                return await Microcosmos();

            if (!Spells.Macrocosmos.IsReady())
                return false;

            if (Group.CastableAlliesWithin20.Any(x => x.HasAura(Auras.Macrocosmos)))
                return false;

            if (AstrologianSettings.Instance.FightLogic_Macrocosmos && FightLogic.EnemyIsCastingBigAoe())
                return await FightLogic.DoAndBuffer(Spells.Macrocosmos.HealAura(Core.Me, Auras.Macrocosmos));

            var enemyCount = Combat.Enemies.Count();
            if (enemyCount == 0)
                return false;

            if (enemyCount > PartyManager.NumMembers)
                if (Combat.Enemies.All(x => x.WithinSpellRange(Spells.Macrocosmos.Radius) && Group.CastableAlliesWithin20.Count() == PartyManager.NumMembers))
                    return await Spells.Macrocosmos.HealAura(Core.Me, Auras.Macrocosmos);

            var isThereABoss = Combat.Enemies.Any(x => x.IsBoss());

            if (isThereABoss || !Group.CastableTanks.All(x =>
                    x.WithinSpellRange(Spells.Macrocosmos.Radius) && x.CurrentHealthPercent < 30f) ||
                enemyCount <= AoeThreshold) return false;

            return await Spells.Macrocosmos.HealAura(Core.Me, Auras.Macrocosmos);
        }

        private static async Task<bool> Microcosmos()
        {
            if (!Group.CastableAlliesWithin30.Any(x => x.HasMyAura(Auras.Macrocosmos)))
                return false;

            if (Group.CastableAlliesWithin30.Count(x => x.HasMyAura(Auras.Macrocosmos)
                    && x.CurrentHealthPercent < AstrologianSettings.Instance.MacrocosmosHealthPercent) <= AoeThreshold) return false;

            return await Spells.Microcosmos.Heal(Core.Me);

        }

        #endregion

        #region Raise

        public static async Task<bool> Ascend()
        {
            return await Roles.Healer.Raise(
                Spells.Ascend,
                AstrologianSettings.Instance.SwiftcastRes,
                AstrologianSettings.Instance.SlowcastRes,
                AstrologianSettings.Instance.ResOutOfCombat,
                AstrologianSettings.Instance.ResDelay
            );
        }

        #endregion

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return Healer.ForceLimitBreak(Spells.HealingWind, Spells.BreathoftheEarth, Spells.AstralStasis, Spells.Malefic);
        }

        //public static int AoeThreshold => PartyManager.NumMembers == 4 ? 2 : 3;
    }
}

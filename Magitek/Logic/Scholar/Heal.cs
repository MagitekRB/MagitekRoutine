using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Scholar;
using Magitek.Toggles;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using ScholarRoutine = Magitek.Utilities.Routines.Scholar;

namespace Magitek.Logic.Scholar
{
    internal static class Heal
    {

        public static int AoeNeedHealing => PartyManager.NumMembers > 4 ? ScholarSettings.Instance.AoeNeedHealingFullParty : ScholarSettings.Instance.AoeNeedHealingLightParty;

        public static bool NeedAoEHealing()
        {
            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= ScholarSettings.Instance.AoEHealHealthPercent);

            var needAoEHealing = targets.Count() >= AoeNeedHealing;

            if (needAoEHealing)
                return true;

            return false;
        }

        #region ForceToggle

        public static async Task<bool> ForceWhispDawn()
        {
            if (!ScholarSettings.Instance.ForceWhispDawn)
                return false;

            if (!await Spells.WhisperingDawn.Cast(Core.Me)) return false;
            ScholarSettings.Instance.ForceWhispDawn = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceAdlo()
        {
            if (!ScholarSettings.Instance.ForceAdlo)
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                target = Core.Me;

            if (!await ScholarRoutine.AdloquiumSpell.Heal(target, false)) return false;
            ScholarSettings.Instance.ForceAdlo = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceIndom()
        {
            if (!ScholarSettings.Instance.ForceIndom)
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                target = Core.Me;

            if (!await Spells.Indomitability.Cast(target)) return false;
            ScholarSettings.Instance.ForceIndom = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceExcog()
        {
            if (!ScholarSettings.Instance.ForceExcog)
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                target = Core.Me;

            // Deliberately NOT HasPrimaryShield(): Excogitation is a delayed heal, not a barrier, so
            // the Adloquium/Succor non-stacking rule does not apply. This keeps the original check.
            if (target.HasAura(Auras.Galvanize))
                return false;

            if (!await Spells.Excogitation.Cast(target)) return false;
            ScholarSettings.Instance.ForceExcog = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceSacredSoil()
        {
            if (!ScholarSettings.Instance.ForceSacredSoil)
                return false;

            if (!await Spells.SacredSoil.Cast(Core.Me)) return false;
            ScholarSettings.Instance.ForceSacredSoil = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceSuccor()
        {
            if (!ScholarSettings.Instance.ForceSuccor)
                return false;

            if (!await ScholarRoutine.SuccorSpell.Cast(Core.Me)) return false;
            ScholarSettings.Instance.ForceSuccor = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceEmergencySuccor()
        {
            if (!ScholarSettings.Instance.ForceEmergencySuccor)
                return false;

            // Ready OR already armed: the helper below fires Emergency Tactics on one pulse and the
            // follow-up heal consumes the aura on a later one, so "on cooldown but armed" must pass
            // this gate or the forced Succor and the toggle reset become unreachable.
            if (!Spells.EmergencyTactics.IsKnownAndReady() && !Core.Me.HasAura(Auras.EmergencyTactics))
                return false;

            if (!await UsedEmergencyTactics(forced: true))
                return false;

            if (!await ScholarRoutine.SuccorSpell.Cast(Core.Me)) return false;
            ScholarSettings.Instance.ForceEmergencySuccor = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceDeployAdloWithRecitation()
        {
            if (!ScholarSettings.Instance.ForceDeployAdloWithRecitation)
                return false;

            if (!Spells.DeploymentTactics.IsKnownAndReady() || !Spells.Recitation.IsKnownAndReady())
                return false;

            if (!await UsedRecitation())
                return false;

            if (!await UsedAdloquium())
                return false;

            if (!await Spells.DeploymentTactics.Cast(Core.Me))
                return false;


            ScholarSettings.Instance.ForceDeployAdloWithRecitation = false;
            TogglesManager.ResetToggles();
            return true;
        }


        private static async Task<bool> UsedRecitation()
        {
            if (Core.Me.HasAura(Auras.Recitation))
                return true;
            if (!await Spells.Recitation.Cast(Core.Me))
                return false;
            if (!await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Recitation)))
                return false;
            return await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.Adloquium.Id, Core.Me));
        }

        /// <summary>
        /// True when the AUTOMATIC Emergency Tactics conversion could run right now — the same
        /// gate set UsedEmergencyTactics enforces (Recitation/queue crit protection, the master
        /// opt-out, ready-or-armed). Target selection consults this so it never picks an ally
        /// that only the conversion could serve while the conversion itself would refuse.
        /// </summary>
        private static bool EmergencyTacticsConvertible()
        {
            if (Core.Me.HasAura(Auras.Recitation) || SpellQueueLogic.SpellQueue.Any())
                return false;

            if (!ScholarSettings.Instance.EmergencyTactics)
                return false;

            return Spells.EmergencyTactics.IsKnownAndReady() || Core.Me.HasAura(Auras.EmergencyTactics);
        }

        private static async Task<bool> UsedEmergencyTactics(bool forced = false)
        {
            // Same guards as Buff.EmergencyTactics, and they must run BEFORE the armed short-circuit:
            // with Emergency Tactics and Recitation both up, accepting the armed aura would let the
            // caller's shield heal convert the Recitation-guaranteed crit — the exact theft these
            // guards exist to prevent. This was the second, unguarded ET site. They apply to the
            // forced path too — Recitation expires or is consumed, so this delays a forced cast,
            // never latches it.
            if (Core.Me.HasAura(Auras.Recitation) || SpellQueueLogic.SpellQueue.Any())
                return false;

            // Same master opt-out as Buff.EmergencyTactics, in the same position: this helper is
            // reachable from Accession/Manifestation barrier branches without any settings check
            // in between, and disabling the feature must disable every automatic ET. The explicit
            // force toggle is user intent, not automation, so it bypasses only this check.
            if (!forced && !ScholarSettings.Instance.EmergencyTactics)
                return false;

            if (Core.Me.HasAura(Auras.EmergencyTactics))
                return true;

            if (!await Spells.EmergencyTactics.Cast(Core.Me))
                return false;

            // Keep the arm→heal pair atomic (see Buff.EmergencyTactics): a bounded wait for the
            // aura and the paired heal's castability, so the conversion cannot drift to another
            // target or caller between pulses.
            return await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.EmergencyTactics) && ActionManager.CanCast(Spells.Succor.Id, Core.Me));
        }

        private static async Task<bool> UsedAdloquium()
        {
            if (Core.Me.HasAura(Auras.Galvanize))
                return true;
            if (!await ScholarRoutine.AdloquiumSpell.Cast(Core.Me))
                return false;
            if (!await Coroutine.Wait(2000, () => Core.Me.HasAura(Auras.Galvanize)))
                return false;
            return await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.DeploymentTactics.Id, Core.Me));
        }

        private static async Task<bool> UsedSuccor()
        {
            if (Core.Me.HasAura(Auras.Galvanize))
                return true;
            if (!await ScholarRoutine.SuccorSpell.Cast(Core.Me))
                return false;
            return await Coroutine.Wait(2500, () => Core.Me.HasAura(Auras.Galvanize));
        }

        #endregion


        public static async Task<bool> Physick()
        {
            if (!ScholarSettings.Instance.Physick)
                return false;

            if (ScholarSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var physickTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent < ScholarSettings.Instance.PhysickHpPercent);

                if (physickTarget != null)
                    return await Spells.Physick.Heal(physickTarget);

                if (!ScholarSettings.Instance.HealAllianceOnlyPhysick)
                    return false;

                physickTarget = Utilities.Routines.Scholar.AlliancePhysickOnly.FirstOrDefault(r => r.CurrentHealthPercent < ScholarSettings.Instance.PhysickHpPercent);

                if (physickTarget == null)
                    return false;

                return await Spells.Physick.Heal(physickTarget);

            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.PhysickHpPercent)
                return false;

            return await Spells.Physick.Heal(Core.Me);
        }

        public static async Task<bool> EmergencyTacticsAdloquium()
        {
            if (!ScholarSettings.Instance.Adloquium || !ScholarSettings.Instance.EmergencyTacticsAdloquium)
                return false;

            // On cooldown but ARMED still proceeds: the cast and the converted heal now happen on
            // different pulses, and this gate runs before the aura branch can consume the buff.
            if (Spells.EmergencyTactics.Cooldown != TimeSpan.Zero && !Core.Me.HasAura(Auras.EmergencyTactics))
                return false;

            if (Globals.InParty)
            {
                var adloTarget = Group.CastableAlliesWithin30.Where(CanAdlo).OrderBy(a => a.CurrentHealthPercent).FirstOrDefault();

                if (adloTarget == null)
                    return false;

                if (!await Buff.EmergencyTactics())
                    return false;

                return await ScholarRoutine.AdloquiumSpell.Heal(adloTarget, false);
            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.EmergencyTacticsAdloquiumHealthPercent)
                return false;

            if (!await Buff.EmergencyTactics())
                return false;

            return await ScholarRoutine.AdloquiumSpell.Heal(Core.Me, false);

            bool CanAdlo(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.EmergencyTacticsAdloquiumHealthPercent)
                    return false;

                if (unit.HasAura(Auras.Excogitation))
                    return false;

                if (!ScholarSettings.Instance.AdloquiumOnlyHealer && !ScholarSettings.Instance.AdloquiumOnlyTank)
                    return true;

                if (ScholarSettings.Instance.AdloquiumOnlyHealer && unit.IsHealer())
                    return true;

                return ScholarSettings.Instance.AdloquiumOnlyTank && unit.IsTank();
            }
        }

        public static async Task<bool> Adloquium()
        {
            if (!ScholarSettings.Instance.Adloquium)
                return false;

            if (!ScholarSettings.Instance.AdloOutOfCombat && !Core.Me.InCombat)
                return false;

            if (ScholarSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                // If the lowest heal target is higher than Adloquium health, check to see if the user wants us to Galvanize the tank
                if (ScholarSettings.Instance.AdloquiumTankForBuff && Globals.HealTarget?.CurrentHealthPercent > ScholarSettings.Instance.AdloquiumHpPercent)
                {
                    // Pick any tank who doesn't have Galvanize on them
                    var tankAdloTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.IsTank() && !r.HasPrimaryShield());

                    if (tankAdloTarget == null)
                        return false;

                    await UseRecitation();

                    return await ScholarRoutine.AdloquiumSpell.HealAura(tankAdloTarget, Auras.Galvanize, false);
                }

                var adloTarget = Group.CastableAlliesWithin30.FirstOrDefault(CanAdlo);

                if (adloTarget == null)
                    return false;

                await UseRecitation();

                return await ScholarRoutine.AdloquiumSpell.HealAura(adloTarget, Auras.Galvanize);

                bool CanAdlo(Character unit)
                {
                    if (unit == null)
                        return false;

                    if (unit.CurrentHealthPercent > ScholarSettings.Instance.AdloquiumHpPercent)
                        return false;

                    if (unit.HasPrimaryShield())
                        return false;

                    if (unit.HasAura(Auras.Excogitation))
                        return false;

                    if (!ScholarSettings.Instance.AdloquiumOnlyHealer && !ScholarSettings.Instance.AdloquiumOnlyTank)
                        return true;

                    if (ScholarSettings.Instance.AdloquiumOnlyHealer && unit.IsHealer())
                        return true;

                    return ScholarSettings.Instance.AdloquiumOnlyTank && unit.IsTank();
                }
            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.AdloquiumHpPercent || Core.Me.HasPrimaryShield())
                return false;

            return await ScholarRoutine.AdloquiumSpell.HealAura(Core.Me, Auras.Galvanize);

            async Task UseRecitation()
            {
                if (!ScholarSettings.Instance.Recitation)
                    return;
                if (!ScholarSettings.Instance.RecitationWithAdlo)
                    return;
                // An armed Emergency Tactics would convert the crit shield this Recitation exists
                // to guarantee — never arm the pair while a deferred conversion is still live.
                if (Core.Me.HasAura(Auras.EmergencyTactics))
                    return;
                if (Spells.Recitation.Cooldown != TimeSpan.Zero)
                    return;
                if (ScholarSettings.Instance.RecitationOnlyNoAetherflow && Core.Me.HasAetherflow())
                    return;
                if (!await Spells.Recitation.Cast(Core.Me))
                    return;
                // Recitation is instant, but the paired heal must also clear its animation lock —
                // one capped wait on both (instead of the old two sequential 1s stalls) keeps the
                // guaranteed-crit pairing on this pulse.
                await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Recitation) && ActionManager.CanCast(Spells.Adloquium.Id, Core.Me));
            }
        }


        public static async Task<bool> EmergencyTacticsSuccor()
        {
            if (!ScholarSettings.Instance.Succor || !ScholarSettings.Instance.EmergencyTactics || !ScholarSettings.Instance.EmergencyTacticsSuccor)
                return false;

            // On cooldown but ARMED still proceeds: the cast and the converted heal now happen on
            // different pulses, and this gate runs before the aura branch can consume the buff.
            if (Spells.EmergencyTactics.Cooldown != TimeSpan.Zero && !Core.Me.HasAura(Auras.EmergencyTactics))
                return false;

            var needSuccor = Group.CastableAlliesWithin20.Count(r => r.IsAlive &&
                                                                     r.CurrentHealthPercent <= ScholarSettings.Instance.EmergencyTacticsSuccorHealthPercent) >= AoeNeedHealing;

            if (!needSuccor)
                return false;

            if (!await Buff.EmergencyTactics())
                return false;

            // The cast-tracker holds the pulse while Succor is casting, so it won't re-fire — no need to
            // block here confirming LastSpell.
            return await ScholarRoutine.SuccorSpell.Heal(Core.Me);
        }

        public static async Task<bool> Succor()
        {
            if (!ScholarSettings.Instance.Succor)
                return false;

            //if (Casting.LastSpell == Spells.Indomitability)
            //    return false;

            //if (Casting.LastSpell == Spells.Succor)
            //    return false;

            var needSuccor = Group.CastableAlliesWithin20.Count(r => r.IsAlive &&
                                                                     r.CurrentHealthPercent <= ScholarSettings.Instance.SuccorHpPercent &&
                                                                     !r.HasPrimaryShield()) >= AoeNeedHealing;

            if (!needSuccor)
                return false;

            // The cast-tracker holds the pulse while Succor is casting, so it won't re-fire — no need to
            // block here confirming LastSpell.
            return await ScholarRoutine.SuccorSpell.Heal(Core.Me);
        }

        public static async Task<bool> Accession()
        {
            if (!Spells.Accession.IsKnown())
                return false;

            if (!ScholarSettings.Instance.Accession)
                return false;

            if (!Core.Me.HasAura(Auras.Seraphism))
                return false;

            var needAccession = Group.CastableAlliesWithin20.Count(r => r.IsAlive && r.CurrentHealthPercent <= ScholarSettings.Instance.AccessionHpPercent) >= AoeNeedHealing;
            var needShields = Group.CastableAlliesWithin20.Count(r => r.IsAlive && r.CurrentHealthPercent <= ScholarSettings.Instance.AccessionHpPercent && !r.HasPrimaryShield()) > 0;

            if (!needAccession)
                return false;

            if (!needShields && !await UsedEmergencyTactics())
                return false;

            return await Spells.Accession.Heal(Core.Me);

        }

        public static async Task<bool> Manifestation()
        {
            if (!Spells.Manifestation.IsKnown())
                return false;

            if (!ScholarSettings.Instance.Manifestation)
                return false;

            if (!Core.Me.HasAura(Auras.Seraphism))
                return false;

            if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= ScholarSettings.Instance.ManifestationHpPercent) > AoeNeedHealing)
                return false;

            if (ScholarSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                // A target already carrying a barrier is only actionable through the Emergency
                // Tactics conversion. When that path cannot run, skip barriered allies in the
                // pick — otherwise the first barriered ally latches this method every pulse
                // while a shieldable ally further down the weight order goes without.
                var etConvertible = EmergencyTacticsConvertible();

                var ManifestationTarget = etConvertible
                    ? Group.CastableAlliesWithin30.FirstOrDefault(CanLustrate)
                    : Group.CastableAlliesWithin30.FirstOrDefault(r => CanLustrate(r) && !r.HasPrimaryShield());

                if (ManifestationTarget == null)
                    return false;

                var needsShields = !ManifestationTarget.HasPrimaryShield();

                if (!needsShields && !await UsedEmergencyTactics())
                    return false;

                return await Spells.Manifestation.Cast(ManifestationTarget);
            }

            if (Core.Me.HasAura(Auras.Excogitation))
                return false;

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.ManifestationHpPercent)
                return false;

            var needsShieldsMe = !Core.Me.HasPrimaryShield();

            if (!needsShieldsMe && !await UsedEmergencyTactics())
                return false;

            return await Spells.Manifestation.Cast(Core.Me);

            bool CanLustrate(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.HasAura(Auras.Excogitation))
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.ManifestationHpPercent)
                    return false;

                return true;
            }
        }

        public static async Task<bool> Excogitation()
        {
            if (!ScholarSettings.Instance.Excogitation)
                return false;

            if (!Core.Me.HasAetherflow())
                return false;

            if (Spells.Excogitation.Cooldown != TimeSpan.Zero)
                return false;

            if (Globals.InParty)
            {
                var excogitationTarget = Group.CastableAlliesWithin30.FirstOrDefault(CanExcogitation);

                if (excogitationTarget == null)
                    return false;

                await UseRecitation();

                return await Spells.Excogitation.Cast(excogitationTarget);
            }

            if (Core.Me.HasAura(Auras.Excogitation))
                return false;

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.ExcogitationHpPercent)
                return false;

            await UseRecitation();

            return await Spells.Excogitation.Cast(Core.Me);

            bool CanExcogitation(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.HasAura(Auras.Excogitation))
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.ExcogitationHpPercent)
                    return false;

                if (Casting.LastSpellTarget?.ObjectId == unit.ObjectId)
                {
                    if (Casting.LastSpell == Spells.Lustrate || Casting.LastSpell == Spells.Excogitation)
                        return false;
                }

                if (!ScholarSettings.Instance.ExcogitationOnlyHealer && !ScholarSettings.Instance.ExcogitationOnlyTank)
                    return true;

                if (ScholarSettings.Instance.ExcogitationOnlyHealer && unit.IsHealer())
                    return true;

                return ScholarSettings.Instance.ExcogitationOnlyTank && unit.IsTank();
            }
            async Task UseRecitation()
            {
                if (!ScholarSettings.Instance.Recitation)
                    return;

                if (!ScholarSettings.Instance.RecitationWithExcog)
                    return;
                if (Spells.Recitation.Cooldown != TimeSpan.Zero)
                    return;
                if (ScholarSettings.Instance.RecitationOnlyNoAetherflow && Core.Me.HasAetherflow())
                    return;

                if (!await Spells.Recitation.Cast(Core.Me))
                    return;

                // Recitation is instant, but the paired heal must also clear its animation lock —
                // one capped wait on both (instead of the old two sequential 1s stalls) keeps the
                // guaranteed-crit pairing on this pulse.
                await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Recitation) && ActionManager.CanCast(Spells.Excogitation.Id, Core.Me));
            }
        }

        public static async Task<bool> Lustrate()
        {
            if (!ScholarSettings.Instance.Lustrate)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasAetherflow())
                return false;

            if (Spells.Lustrate.Cooldown != TimeSpan.Zero)
                return false;

            if (Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= ScholarSettings.Instance.IndomitabilityHpPercent) > AoeNeedHealing)
                return false;

            if (ScholarSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var lustrateTarget = Group.CastableAlliesWithin30.FirstOrDefault(CanLustrate);

                if (lustrateTarget == null)
                    return false;

                await UseRecitation();

                return await Spells.Lustrate.Cast(lustrateTarget);
            }

            if (Core.Me.HasAura(Auras.Excogitation))
                return false;

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.LustrateHpPercent)
                return false;

            await UseRecitation();

            return await Spells.Lustrate.Cast(Core.Me);

            bool CanLustrate(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.HasAura(Auras.Excogitation))
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.LustrateHpPercent)
                    return false;

                if (Casting.LastSpellTarget?.ObjectId == unit.ObjectId)
                {
                    if (Casting.LastSpell == Spells.Lustrate || Casting.LastSpell == Spells.Excogitation)
                        return false;
                }

                if (!ScholarSettings.Instance.LustrateOnlyHealer && !ScholarSettings.Instance.LustrateOnlyTank)
                    return true;

                if (ScholarSettings.Instance.LustrateOnlyHealer && unit.IsHealer())
                    return true;

                return ScholarSettings.Instance.LustrateOnlyTank && unit.IsTank();
            }

            async Task UseRecitation()
            {
                if (!ScholarSettings.Instance.Recitation)
                    return;

                if (!ScholarSettings.Instance.RecitationWithLustrate)
                    return;

                if (ScholarSettings.Instance.RecitationOnlyNoAetherflow && Core.Me.HasAetherflow())
                    return;

                if (!await Spells.Recitation.Cast(Core.Me))
                    return;

                // Recitation is instant, but the paired heal must also clear its animation lock —
                // one capped wait on both (instead of the old two sequential 1s stalls) keeps the
                // guaranteed-crit pairing on this pulse.
                await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Recitation) && ActionManager.CanCast(Spells.Lustrate.Id, Core.Me));
            }
        }

        public static async Task<bool> Indomitability()
        {
            if (!ScholarSettings.Instance.Indomitability)
                return false;

            if (!Core.Me.HasAetherflow())
                return false;

            if (Spells.Indomitability.Cooldown != TimeSpan.Zero)
                return false;

            if (Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= ScholarSettings.Instance.IndomitabilityHpPercent) < AoeNeedHealing)
                return false;

            await UseRecitation();

            return await Spells.Indomitability.Cast(Core.Me);

            async Task UseRecitation()
            {
                if (!ScholarSettings.Instance.Recitation)
                    return;

                if (!ScholarSettings.Instance.RecitationWithIndomitability)
                    return;

                if (ScholarSettings.Instance.RecitationOnlyNoAetherflow && Core.Me.HasAetherflow())
                    return;

                if (!await Spells.Recitation.Cast(Core.Me))
                    return;

                // Recitation is instant, but the paired heal must also clear its animation lock —
                // one capped wait on both (instead of the old two sequential 1s stalls) keeps the
                // guaranteed-crit pairing on this pulse.
                await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Recitation) && ActionManager.CanCast(Spells.Indomitability.Id, Core.Me));
            }
        }

        public static async Task<bool> SacredSoil()
        {
            if (!ScholarSettings.Instance.SacredSoil)
                return false;

            // Extra double cast spell since Sacred Soil is a quick animation instant spell
            if (Casting.LastSpell == Spells.SacredSoil)
                return false;

            if (!Core.Me.HasAetherflow())
                return false;

            if (Spells.SacredSoil.Cooldown != TimeSpan.Zero)
                return false;

            // The count is "wounded allies within Soil's radius of me" — it never depended on the lambda
            // parameter, so the old FirstOrDefault picked an arbitrary first party member and could drop
            // the circle away from the very cluster it counted. Trigger on the count, then place the
            // circle by coverage of the SAME wounded set — centering on the whole party would pull the
            // placement toward a healthy remote cluster when the party is split. The frame-cached ally
            // list already excludes dead and despawned members, so a corpse at 0% can neither trigger
            // the count nor win placement.
            var wounded = Group.CastableAlliesWithin30
                .Where(x => x.CurrentHealthPercent < ScholarSettings.Instance.SacredSoilHpPercent
                    && x.WithinSpellRange(Spells.SacredSoil.Radius))
                .ToList();

            if (wounded.Count < AoeNeedHealing)
                return false;

            if (!ScholarSettings.Instance.SacredSoilCenterParty)
                return await Spells.SacredSoil.Cast(Core.Me);

            // Rank candidates by how many of the wounded their circle would actually cover — a
            // centrality pick can still leave a triggering ally outside the radius when they are
            // spread out, and coverage is what the trigger promised. Coverage is pure point
            // geometry: the spell is ground-placed at the candidate's location with a fixed
            // radius, so the candidate's own hitbox cannot enlarge the circle, and whether the
            // server reach-expands the allies it tests is not knowable from the client — center
            // distance against the radius is the only assumption-free model. The trigger keeps
            // the caster-anchored spell-range check it has always used; the two predicates answer
            // different questions (who counts as wounded near me vs who stands inside this circle),
            // and the disagreement band between them is narrower than the distance players drift
            // between the pulse that scores and the circle landing.
            // Ties break by centrality over the SAME wounded set, not by distance to us — a zero
            // self-distance tie-break would hand every tie to the caster and reduce CenterParty to a
            // self-cast; the caster only wins when it genuinely covers more, or is itself most central.
            var soilTarget = wounded
                .Concat(new Character[] { Core.Me })
                .OrderByDescending(r => wounded.Count(ot => r.Distance2D(ot) <= Spells.SacredSoil.Radius))
                .ThenBy(r => wounded.Sum(ot => r.Distance2D(ot)))
                .First();

            return await Spells.SacredSoil.Cast(soilTarget);
        }

        public static async Task<bool> Resurrection()
        {
            return await Roles.Healer.Raise(
                Spells.Resurrection,
                ScholarSettings.Instance.SwiftcastRes,
                ScholarSettings.Instance.SlowcastRes,
                ScholarSettings.Instance.ResOutOfCombat,
                ScholarSettings.Instance.ResDelay
            );
        }

        public static async Task<bool> WhisperingDawn()
        {
            if (!ScholarSettings.Instance.WhisperingDawn)
                return false;

            if (Core.Me.Pet == null)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Spells.WhisperingDawn.Cooldown != TimeSpan.Zero)
                return false;

            if (ScholarSettings.Instance.WhisperingDawnOnlyWithSeraph && Core.Me.Pet.EnglishName != "Seraph")
                return false;

            if (ScholarSettings.Instance.ForceWhisperingDawnWithSeraph && Utilities.Routines.Scholar.SeraphTimeRemaining() < 15)
                return await Spells.WhisperingDawn.Cast(Core.Me);

            if (Globals.InParty)
            {
                var canWhisperingDawnTargets = Group.CastableAlliesWithin30.Where(CanWhisperingDawn).ToList();

                if (canWhisperingDawnTargets.Count < AoeNeedHealing)
                    return false;

                if (ScholarSettings.Instance.WhisperingDawnOnlyWithTank && !canWhisperingDawnTargets.Any(r => r.IsTank()))
                    return false;

                return await Spells.WhisperingDawn.Cast(Core.Me);
            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.WhisperingDawnHealthPercent)
                return false;

            return await Spells.WhisperingDawn.Cast(Core.Me);

            bool CanWhisperingDawn(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.WhisperingDawnHealthPercent)
                    return false;

                return unit.Distance(Core.Me.Pet) <= 20;
            }
        }

        public static async Task<bool> FeyIllumination()
        {
            if (!ScholarSettings.Instance.FeyIllumination)
                return false;

            if (Core.Me.Pet == null)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Spells.FeyIllumination.Cooldown != TimeSpan.Zero)
                return false;

            if (ScholarSettings.Instance.FeyIlluminationOnlyWithSeraph && Core.Me.Pet.EnglishName != "Seraph")
                return false;

            if (ScholarSettings.Instance.ForceFeyIlluminationWithSeraph && Utilities.Routines.Scholar.SeraphTimeRemaining() < 15)
                return await Spells.FeyIllumination.Cast(Core.Me);

            if (Globals.InParty)
            {
                var canFeyIlluminationTargets = Group.CastableAlliesWithin30.Where(CanFeyIllumination).ToList();

                if (canFeyIlluminationTargets.Count < AoeNeedHealing)
                    return false;

                if (ScholarSettings.Instance.FeyIlluminationOnlyWithTank && !canFeyIlluminationTargets.Any(r => r.IsTank()))
                    return false;

                return await Spells.FeyIllumination.Cast(Core.Me);
            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.FeyIlluminationHpPercent)
                return false;

            return await Spells.FeyIllumination.Cast(Core.Me);

            bool CanFeyIllumination(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.FeyIlluminationHpPercent)
                    return false;

                //Radius is now 30y
                return unit.Distance(Core.Me.Pet) <= 30;
            }
        }

        public static async Task<bool> FeyBlessing()
        {
            if (!ScholarSettings.Instance.FeyBlessing)
                return false;

            if (Core.Me.Pet == null)
                return false;

            if (Core.Me.Pet.EnglishName == "Seraph")
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Spells.FeyBlessing.Cooldown != TimeSpan.Zero)
                return false;

            if (ActionResourceManager.Scholar.FaerieGauge < ScholarSettings.Instance.FeyBlessingMinimumFairieGauge)
                return false;

            if (Globals.InParty)
            {
                var canFeyBlessingTargets = Group.CastableAlliesWithin30.Where(CanFeyBlessing).ToList();

                if (canFeyBlessingTargets.Count < AoeNeedHealing)
                    return false;

                if (ScholarSettings.Instance.FeyBlessingOnlyWithTank && !canFeyBlessingTargets.Any(r => r.IsTank()))
                    return false;

                return await Spells.FeyBlessing.Cast(Core.Me);
            }

            if (Core.Me.CurrentHealthPercent > ScholarSettings.Instance.FeyBlessingHpPercent)
                return false;

            return await Spells.FeyBlessing.Cast(Core.Me);

            bool CanFeyBlessing(Character unit)
            {
                if (unit == null)
                    return false;
                if (unit.CurrentHealthPercent > ScholarSettings.Instance.FeyBlessingHpPercent)
                    return false;

                return unit.Distance(Core.Me.Pet) <= 20;
            }
        }

        // Prevent blowing the second consolation stack before seraph gets a chance to cast
        // the first one.
        public static DateTime ConsolationCooldown = DateTime.Now;

        public static async Task<bool> Consolation()
        {
            if (!ScholarSettings.Instance.Consolation)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.Pet == null)
                return false;

            if (Core.Me.Pet.EnglishName != "Seraph")
                return false;

            if (DateTime.Now <= ConsolationCooldown)
                return false;

            if (Utilities.Routines.Scholar.SeraphTimeRemaining() <= 6.5 || Spells.Consolation.Charges == 2)
                return await Spells.Consolation.Cast(Core.Me);

            if (Globals.InParty)
            {
                var canConsolationTargets = Group.CastableAlliesWithin30.Where(CanConsolation).ToList();

                if (canConsolationTargets.Count < AoeNeedHealing)
                    return false;

                if (Utilities.Routines.Scholar.SeraphTimeRemaining() >= 10 && ScholarSettings.Instance.ConsolationOnlyWithTank && !canConsolationTargets.Any(r => r.IsTank()))
                    return false;

                ConsolationCooldown = DateTime.Now.AddSeconds(5);

                return await Spells.Consolation.Cast(Core.Me);
            }

            if (Utilities.Routines.Scholar.SeraphTimeRemaining() >= 10 && Core.Me.CurrentHealthPercent > ScholarSettings.Instance.ConsolationHpPercent)
                return false;

            ConsolationCooldown = DateTime.Now.AddSeconds(5);

            return await Spells.Consolation.Cast(Core.Me);

            bool CanConsolation(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > ScholarSettings.Instance.ConsolationHpPercent)
                    return false;

                if (unit.HasAura(Auras.SeraphicVeil))
                    return false;


                //Range is now 30y
                return unit.Distance(Core.Me.Pet) <= 30;
            }
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return Healer.ForceLimitBreak(Spells.HealingWind, Spells.BreathoftheEarth, Spells.AngelFeathers, Spells.Ruin);
        }
    }
}

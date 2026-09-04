using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Sage;
using Magitek.Toggles;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Sage;
using Auras = Magitek.Utilities.Auras;
using SageRoutine = Magitek.Utilities.Routines.Sage;

namespace Magitek.Logic.Sage
{
    internal static class Heal
    {
        public static int AoeNeedHealing => PartyManager.NumMembers > 4 ? SageSettings.Instance.AoeNeedHealingFullParty : SageSettings.Instance.AoeNeedHealingLightParty;

        public static bool IsEukrasiaReady()
        {
            return Core.Me.HasAura(Auras.Eukrasia, true) || Spells.Eukrasia.IsKnownAndReady();
        }

        /// <summary>
        /// Eukrasian Prognosis II is an INDEPENDENT action, not a mask of the base spell: at 96+
        /// the client refuses CanCast on the base id even with Eukrasia up, and GetMaskedAction
        /// leaves the base id unchanged. Asking for the base spell there waits out its timeout and
        /// casts nothing, so every caller picks the shield through here.
        /// </summary>
        public static SpellData EukrasianPrognosisSpell =>
            Spells.EukrasianPrognosisII.IsKnown() ? Spells.EukrasianPrognosisII : Spells.EukrasianPrognosis;

        public static async Task<bool> UseEukrasia(uint spellId = 24291, GameObject targetObject = null)
        {
            var target = targetObject == null ? Core.Me : targetObject;

            // Armed already: only report success when the paired spell can actually go out now.
            // Trusting the aura alone failed silently in the field - the aura read lags a consume
            // by tens of milliseconds, and a rolling GCD refuses the press either way - and the
            // caller's cast then died without a log line while the dot path took the Eukrasia.
            if (Core.Me.HasAura(Auras.Eukrasia, true))
                return ActionManager.CanCast(spellId, target);
            if (!SageSettings.Instance.Eukrasia)
                return false;
            if (!IsEukrasiaReady())
                return false;
            if (!await Spells.Eukrasia.Cast(Core.Me))
                return false;

            // Keep the arm->cast pair atomic, exactly as Scholar's Emergency Tactics helper does:
            // one bounded wait for the aura AND the paired spell's castability, so the Eukrasia
            // cannot drift to another caller between pulses (the damage path deliberately spends a
            // banked Eukrasia on the dot, which is the drift this prevents). One wait rather than
            // two sequential ones, and Scholar's 1000ms bound instead of 2500 twice over.
            // 1250ms, not 1000: Eukrasia's own recast is exactly 1000ms and the paired spell only
            // becomes castable when it reaches zero, so a 1000ms bound raced it and lost by a few ms.
            return await Coroutine.Wait(1250, () => Core.Me.HasAura(Auras.Eukrasia, true)
                                                    && ActionManager.CanCast(spellId, target));
        }
        private static async Task<bool> UseZoe()
        {
            if (Core.Me.HasAura(Auras.Zoe))
                return true;

            if (!Spells.Zoe.IsKnownAndReady())
                return false;

            if (!await Spells.Zoe.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Zoe));
        }

        private static readonly List<uint> HealingBuffAoEAuras = new List<uint> {
            Auras.EukrasianPrognosis,
            Auras.Kerachole,
            Auras.Panhaimatinon,
            Auras.PhysisII,
            Auras.Holos,
            Auras.Eudaimonia,
            // A co-healer's party mitigation saturates a target the same way ours does
            Auras.Galvanize,
            Auras.SacredSoilReceiver,
            Auras.FeyIllumination
        };

        private static readonly List<uint> HealingBuffSingleAuras = new List<uint> {
            Auras.EukrasianDiagnosis,
            Auras.Taurochole,
            Auras.Haimatinon
        };

        public static bool UseAoEHealingBuff(IEnumerable<Character> wantHealTargets)
        {
            if (!SageSettings.Instance.HealingBuffsLimitAtOnce)
                return true;

            if (!wantHealTargets.Any())
                return true;

            var nAuras = wantHealTargets.Select(c => c.CountAuras(HealingBuffAoEAuras)).Max();

            if (nAuras >= SageSettings.Instance.HealingBuffsMaxAtOnce)
            {
                if (nAuras >= SageSettings.Instance.HealingBuffsMaxUnderHp)
                    return false;

                var nUnderHp = wantHealTargets.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.HealingBuffsMoreHpHealthPercentage).Count();
                if (nUnderHp >= SageSettings.Instance.HealingBuffsMoreHpNeedHealing)
                    return true;

                return false;
            }

            return true;
        }

        public static bool NeedAoEHealing()
        {
            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.AoEHealHealthPercent);

            var needAoEHealing = targets.Count() >= AoeNeedHealing;

            if (needAoEHealing)
                return true;

            return false;
        }

        public static async Task<bool> Diagnosis()
        {
            if (!SageSettings.Instance.Diagnosis)
                return false;

            if (SageSettings.Instance.DiagnosisOnlyBelowXAddersgall && Addersgall > SageSettings.Instance.DiagnosisOnlyAddersgallValue)
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var DiagnosisTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent < SageSettings.Instance.DiagnosisHpPercent);

                if (DiagnosisTarget == null)
                    return false;

                return await Spells.Diagnosis.Heal(DiagnosisTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.DiagnosisHpPercent)
                return false;

            return await Spells.Diagnosis.Heal(Core.Me);
        }

        public static async Task<bool> EukrasianDiagnosis()
        {
            if (!SageSettings.Instance.EukrasianDiagnosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var target = Group.CastableAlliesWithin30.FirstOrDefault(CanEukrasianDiagnosis);

                if (target == null)
                    return false;

                if (SageSettings.Instance.Zoe && SageSettings.Instance.ZoeEukrasianDiagnosis)
                    if (SageSettings.Instance.ZoeHealer && target.IsHealer()
                        || SageSettings.Instance.ZoeTank && target.IsTank(SageSettings.Instance.ZoeMainTank))
                        if (target.CurrentHealthPercent <= SageSettings.Instance.ZoeHealthPercent)
                            await UseZoe(); // intentionally ignore failures

                if (!await UseEukrasia(targetObject: target))
                    return false;

                return await Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis);

                bool CanEukrasianDiagnosis(Character unit)
                {
                    if (unit == null)
                        return false;

                    if (unit.CurrentHealthPercent > SageSettings.Instance.EukrasianDiagnosisHpPercent)
                        return false;

                    // Any primary shield, ours or another healer's. This previously checked
                    // Eukrasian Diagnosis and Galvanize but not Eukrasian Prognosis, so a target
                    // already carrying Prognosis could have it overwritten by Diagnosis.
                    if (unit.HasPrimaryShield())
                        return false;

                    if (!SageSettings.Instance.EukrasianDiagnosisOnlyHealer && !SageSettings.Instance.EukrasianDiagnosisOnlyTank)
                        return true;

                    if (SageSettings.Instance.EukrasianDiagnosisOnlyHealer && unit.IsHealer())
                        return true;

                    return SageSettings.Instance.EukrasianDiagnosisOnlyTank && unit.IsTank(SageSettings.Instance.EukrasianDiagnosisOnlyMainTank);
                }
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.EukrasianDiagnosisHpPercent || Core.Me.HasAura(Auras.EukrasianDiagnosis))
                return false;

            if (!await UseEukrasia())
                return false;

            return await Spells.EukrasianDiagnosis.HealAura(Core.Me, Auras.EukrasianDiagnosis);
        }

        public static async Task<bool> ForceEukrasianDiagnosis()
        {

            if (!SageSettings.Instance.ForceEukrasianDiagnosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            // Validate before arming: Eukrasia is a real GCD, and this takes the current target
            // raw - which can be an enemy, out of range, or nothing at all.
            var target = Core.Me.CurrentTarget as Character;

            if (target == null || !target.IsAlive || target.CanAttack || !target.WithinSpellRange(30))
                return false;

            if (!await UseEukrasia(Spells.EukrasianDiagnosis.Id, targetObject: target))
                return false;

            if (!await Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis))
                return false;

            SageSettings.Instance.ForceEukrasianDiagnosis = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Prognosis()
        {
            if (!SageSettings.Instance.Prognosis)
                return false;

            if (!Spells.Prognosis.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.PrognosisOnlyBelowXAddersgall && Addersgall > SageSettings.Instance.PrognosisOnlyAddersgallValue)
                return false;

            if (Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PrognosisHpPercent) < AoeNeedHealing)
                return false;

            return await Spells.Prognosis.Heal(Core.Me);
        }

        public static async Task<bool> EukrasianPrognosis()
        {
            if (!SageSettings.Instance.EukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            var targets = Group.CastableAlliesWithin20.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.EukrasianPrognosisHealthPercent &&
                                                                !r.HasPrimaryShield());

            var needEukrasianPrognosis = targets.Count() >= AoeNeedHealing;

            if (!needEukrasianPrognosis)
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            if (SageSettings.Instance.Zoe && SageSettings.Instance.ZoeEukrasianPrognosis)
                if (SageSettings.Instance.ZoeHealer && targets.Any(r => r.IsHealer())
                    || SageSettings.Instance.ZoeTank && targets.Any(r => r.IsTank(SageSettings.Instance.ZoeMainTank)))
                    if (targets.Any(r => r.CurrentHealthPercent <= SageSettings.Instance.ZoeHealthPercent))
                        await UseZoe(); // intentionally ignore failures

            var prognosis = EukrasianPrognosisSpell;

            if (!await UseEukrasia(prognosis.Id))
                return false;

            return await prognosis.HealAura(Core.Me, Auras.EukrasianPrognosis);
        }
        public static async Task<bool> ForceEukrasianPrognosis()
        {
            if (!SageSettings.Instance.ForceEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            var forcedPrognosis = EukrasianPrognosisSpell;

            if (!await UseEukrasia(forcedPrognosis.Id))
                return false;

            if (!await forcedPrognosis.HealAura(Core.Me, Auras.EukrasianPrognosis))
                return false;

            SageSettings.Instance.ForceEukrasianPrognosis = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Physis()
        {
            if (!SageSettings.Instance.Physis)
                return false;

            var spell = Spells.PhysisII;
            uint aura = Auras.PhysisII;

            if (!Spells.PhysisII.IsKnown())
            {
                spell = Spells.Physis;
                aura = Auras.Physis;
            }

            if (!spell.IsKnownAndReady())
                return false;

            // No single-target suppression here: Physis is an AoE regen, and every other AoE heal
            // in this file is judged purely by its own party-count gate below. Carrying the
            // single-target rule hard-blocked it exactly when the party needed it most.
            var targets = Spells.PhysisII.IsKnown()
                ? Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.PhysisHpPercent && !r.HasAura(aura))
                : Group.CastableAlliesWithin20.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.PhysisHpPercent && !r.HasAura(aura));

            if (targets.Count() < AoeNeedHealing)
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            return await spell.HealAura(Core.Me, aura);
        }
        public static async Task<bool> Druochole()
        {
            if (!SageSettings.Instance.Druochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (!Spells.Druochole.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            // The Addersgall timer stops at three charges, so every second spent capped forfeits
            // both the next charge and the 700 MP that comes with spending one. Druochole is last
            // in the weave block, so a dump cannot preempt a real heal - everything above it has
            // already declined this pulse. In combat only: this method is also reached on
            // InActiveDuty, where a capped gauge would otherwise fire Druochole between pulls.
            var overcapDump = SageSettings.Instance.DruocholeOnAddersgallOvercap
                              && Addersgall >= SageRoutine.MaxAddersgall
                              && Core.Me.InCombat;

            if (Globals.InParty)
            {
                var DruocholeTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent <= SageSettings.Instance.DruocholeHpPercent);

                // Whoever is furthest from full rather than whoever the party list happens to put
                // first, because at the overcap threshold most of the party usually qualifies.
                // Full-health allies are excluded outright: the default threshold is 100, and
                // <= 100 is true at full HP, so a gauge capped during downtime dumped into an
                // unhurt party the moment combat started (field-observed 2026-08-29 - the
                // pull opened with a Druochole that healed nobody).
                if (DruocholeTarget == null && overcapDump)
                    DruocholeTarget = Group.CastableAlliesWithin30
                        .Where(r => r.CurrentHealth < r.MaxHealth
                            && r.CurrentHealthPercent <= SageSettings.Instance.DruocholeOvercapHpPercent)
                        .OrderBy(r => r.CurrentHealthPercent)
                        .FirstOrDefault();

                if (DruocholeTarget == null)
                    return false;

                return await Spells.Druochole.Heal(DruocholeTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.DruocholeHpPercent
                && !(overcapDump
                    && Core.Me.CurrentHealth < Core.Me.MaxHealth
                    && Core.Me.CurrentHealthPercent <= SageSettings.Instance.DruocholeOvercapHpPercent))
                return false;

            return await Spells.Druochole.Heal(Core.Me);
        }
        public static async Task<bool> Ixochole()
        {
            if (!SageSettings.Instance.Ixochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (!Spells.Ixochole.IsKnownAndReady())
                return false;

            if (Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.IxocholeHpPercent) < AoeNeedHealing)
                return false;

            return await Spells.Ixochole.Heal(Core.Me);
        }
        public static async Task<bool> Pepsis()
        {
            if (!SageSettings.Instance.Pepsis)
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            var needPepsis = Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PepsisHpPercent &&
                                                        (r.HasAura(Auras.EukrasianPrognosis, true) || r.HasAura(Auras.EukrasianDiagnosis, true))) >= AoeNeedHealing;

            if (!needPepsis)
                return false;

            return await Spells.Pepsis.Heal(Core.Me);

        }
        public static async Task<bool> PepsisEukrasianPrognosis()
        {
            if (!SageSettings.Instance.PepsisEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            var needPepsis = Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PepsisEukrasianPrognosisHealthPercent) >= AoeNeedHealing;

            if (!needPepsis)
                return false;

            if (!await UseEukrasianPrognosisIfNeeded(Group.CastableAlliesWithin20.Count(), Spells.Pepsis, Core.Me))
                return false;

            return await Spells.Pepsis.Heal(Core.Me);
        }
        public static async Task<bool> ForcePepsisEukrasianPrognosis()
        {
            if (!SageSettings.Instance.ForcePepsisEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            if (!await UseEukrasianPrognosisIfNeeded(Group.CastableAlliesWithin20.Count(), Spells.Pepsis, Core.Me))
                return false;

            if (!await Spells.Pepsis.Heal(Core.Me))
                return false;

            SageSettings.Instance.ForcePepsisEukrasianPrognosis = false;
            TogglesManager.ResetToggles();
            return true;
        }

        private static async Task<bool> UseEukrasianPrognosisIfNeeded(int NeedShields, SpellData forSpell, Character target)
        {
            var needPrognosis = Group.CastableAlliesWithin20.Count(r => r.HasAura(Auras.EukrasianPrognosis, true) || r.HasAura(Auras.EukrasianDiagnosis, true)) < NeedShields;

            if (needPrognosis)
            {
                var prognosis = EukrasianPrognosisSpell;

                if (!await UseEukrasia(prognosis.Id))
                    return false;

                if (!await prognosis.Cast(Core.Me))
                    return false;

                if (!await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.EukrasianPrognosis, true)))
                    return false;

                if (!await Coroutine.Wait(1000, () => SpellDataExtensions.CanCast(forSpell, target)))
                    return false;
            }

            return true;
        }

        public static async Task<bool> Taurochole()
        {
            if (!SageSettings.Instance.Taurochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (Core.Me.HasAura(Auras.Kerachole))
                return false;

            if (!Spells.Taurochole.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var taurocholeCandidates = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent < SageSettings.Instance.TaurocholeHpPercent
                                                                              && !r.HasAura(Auras.Taurochole)
                                                                              && !r.HasAura(Auras.Kerachole));

                if (SageSettings.Instance.TaurocholeTankOnly)
                    taurocholeCandidates = taurocholeCandidates.Where(r => r.IsTank(SageSettings.Instance.TaurocholeMainTankOnly) || r.CurrentHealthPercent <= SageSettings.Instance.TaurocholeOthersHpPercent);

                var taurocholeTarget = taurocholeCandidates.FirstOrDefault();

                if (taurocholeTarget == null)
                    return false;

                return await Spells.Taurochole.HealAura(taurocholeTarget, Auras.Taurochole);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.TaurocholeHpPercent)
                return false;

            return await Spells.Taurochole.HealAura(Core.Me, Auras.Taurochole);
        }
        public static async Task<bool> Haima()
        {
            if (!SageSettings.Instance.Haima)
                return false;

            if (!Spells.Haima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                if (SageSettings.Instance.FightLogic_Haima && FightLogic.EnemyHasAnyTankbusterLogic())
                    return false;

                var haimaCandidates = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent < SageSettings.Instance.HaimaHpPercent
                                                                     && !r.HasAura(Auras.Weakness)
                                                                     && !r.HasAura(Auras.Haimatinon));

                if (SageSettings.Instance.HaimaTankForBuff)
                    haimaCandidates = haimaCandidates.Where(r => r.IsTank(SageSettings.Instance.HaimaMainTankForBuff));

                var haimaTarget = haimaCandidates.FirstOrDefault();

                if (haimaTarget == null)
                    return false;

                return await Spells.Haima.CastAura(haimaTarget, Auras.Haimatinon);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.HaimaHpPercent)
                return false;

            return await Spells.Haima.CastAura(Core.Me, Auras.Haimatinon);
        }
        public static async Task<bool> ForceHaima()
        {
            if (!SageSettings.Instance.ForceHaima)
                return false;

            if (!Spells.Haima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                var haimaCandidates = Group.CastableAlliesWithin30.Where(r => !r.HasAura(Auras.Weakness));

                if (SageSettings.Instance.HaimaTankForBuff)
                    haimaCandidates = haimaCandidates.Where(r => r.IsTank(SageSettings.Instance.HaimaMainTankForBuff));

                var haimaTarget = haimaCandidates.FirstOrDefault();

                if (haimaTarget == null)
                    return false;

                if (!await Spells.Haima.CastAura(haimaTarget, Auras.Haimatinon))
                    return false;
            }
            else
            {
                if (!await Spells.Haima.Cast(Core.Me))
                    return false;
            }

            SageSettings.Instance.ForceHaima = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Panhaima()
        {
            if (!SageSettings.Instance.Panhaima)
                return false;

            if (!Spells.Panhaima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                if (SageSettings.Instance.FightLogic_Panhaima && FightLogic.EnemyHasAnyAoeLogic())
                    return false;

                var targets = Group.CastableAlliesWithin30.Where(CanPanhaima);

                if (targets.Count() < AoeNeedHealing)
                    return false;

                if (SageSettings.Instance.PanhaimaOnlyWithTank && !targets.Any(r => r.IsTank(SageSettings.Instance.PanhaimaOnlyWithMainTank)))
                    return false;

                if (!UseAoEHealingBuff(targets))
                    return false;

                return await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PanhaimaHpPercent)
                return false;

            return await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon);

            bool CanPanhaima(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > SageSettings.Instance.PanhaimaHpPercent)
                    return false;

                if (unit.HasAura(Auras.Panhaimatinon))
                    return false;
                //Range is now 30y
                return unit.Distance(Core.Me) <= 30;
            }
        }
        public static async Task<bool> ForcePanhaima()
        {
            if (!SageSettings.Instance.ForcePanhaima)
                return false;

            if (!Spells.Panhaima.IsKnownAndReady())
                return false;

            if (!await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon))
                return false;

            SageSettings.Instance.ForcePanhaima = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Egeiro()
        {
            return await Roles.Healer.Raise(
                Spells.Egeiro,
                SageSettings.Instance.SwiftcastRes,
                SageSettings.Instance.SlowcastRes,
                SageSettings.Instance.ResOutOfCombat,
                SageSettings.Instance.ResDelay
            );
        }
        public static async Task<bool> Pneuma()
        {
            if (!SageSettings.Instance.Pneuma)
                return false;

            // Zoe off = "Only With Pneuma" mode, where ZoePneuma() owns the cast
            if (!SageSettings.Instance.Zoe)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Globals.InParty)
            {
                // 20y, not 25: Pneuma's damage is a 25y line but its HEAL is a 20y circle centred
                // on us, so allies between 20 and 25 were counted for a heal that cannot reach them.
                // PneumaNeedHealing is the ability's own threshold - it has a control on the Combat
                // tab and, until now, nothing read it.
                var pneumaTarget = Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PneumaHpPercent) >= SageSettings.Instance.PneumaNeedHealing;

                if (!pneumaTarget)
                    return false;

                return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PneumaHpPercent)
                return false;

            return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ZoePneuma()
        {
            if (!SageSettings.Instance.Pneuma)
                return false;

            // Zoe off = "Only With Pneuma" mode, where this always applies
            if (SageSettings.Instance.Zoe && !SageSettings.Instance.ZoePneuma)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Globals.InParty)
            {
                // 20y, not 25: Pneuma's damage is a 25y line but its HEAL is a 20y circle centred
                // on us, so allies between 20 and 25 were counted for a heal that cannot reach them.
                // PneumaNeedHealing is the ability's own threshold - it has a control on the Combat
                // tab and, until now, nothing read it.
                var pneumaTarget = Group.CastableAlliesWithin20.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PneumaHpPercent) >= SageSettings.Instance.PneumaNeedHealing;

                if (!pneumaTarget)
                    return false;

                if (!await UseZoe())
                    return false;

                return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PneumaHpPercent)
                return false;

            if (!await UseZoe())
                return false;

            if (!await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.Pneuma.Id, Core.Me.CurrentTarget)))
                return false;

            return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
        }


        public static async Task<bool> ForceZoePneuma()
        {
            if (!SageSettings.Instance.ForceZoePneuma)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (!await UseZoe())
                return false;

            if (!await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.Pneuma.Id, Core.Me.CurrentTarget)))
                return false;

            if (!await Spells.Pneuma.Heal(Core.Me.CurrentTarget))
                return false;

            SageSettings.Instance.ForceZoePneuma = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Kerachole()
        {
            if (!SageSettings.Instance.Kerachole)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Kerachole.IsKnownAndReady())
                return false;

            if (Addersgall == 0)
                return false;

            if (Globals.InParty)
            {
                var targets = Group.CastableAlliesWithin30.Where(CanKerachole).ToList();

                if (targets.Count < AoeNeedHealing)
                    return false;

                if (SageSettings.Instance.KeracholeOnlyWithTank && !Group.CastableAlliesWithin30.Any(r => r.IsTank(SageSettings.Instance.KeracholeOnlyWithMainTank)))
                    return false;

                if (!UseAoEHealingBuff(targets))
                    return false;

                return await Spells.Kerachole.CastAura(Core.Me, Auras.Kerachole);
            }

            if (!CanKerachole(Core.Me))
                return false;

            return await Spells.Kerachole.CastAura(Core.Me, Auras.Kerachole);

            bool CanKerachole(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > SageSettings.Instance.KeracholeHealthPercent)
                    return false;

                if (unit.HasAura(Auras.Kerachole))
                    return false;

                return unit.WithinSpellRange(Spells.Kerachole.Radius);
            }
        }


        public static async Task<bool> Holos()
        {
            if (!SageSettings.Instance.Holos)
                return false;

            if (!Spells.Holos.IsKnown())
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.Holos.IsKnownAndReady())
                return false;

            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.HolosHealthPercent
                                                             && !r.HasAura(Auras.Holos));

            if (targets.Count() < AoeNeedHealing)
                return false;

            if (SageSettings.Instance.HolosTankOnly && !targets.Any(r => r.IsTank(SageSettings.Instance.HolosMainTankOnly)))
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            return await Spells.Holos.HealAura(Core.Me, Auras.Holos);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return Healer.ForceLimitBreak(Spells.HealingWind, Spells.BreathoftheEarth, Spells.TechneMakre, Spells.Dosis);
        }
    }
}

using Clio.Utilities;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Astrologian
    {
        public static bool OnGcd => Spells.Malefic.Cooldown.TotalMilliseconds > 100;

        public static HashSet<string> DontBenefic = new HashSet<string>();
        public static HashSet<string> DontBenefic2 = new HashSet<string>();
        public static HashSet<string> DontDiurnalBenefic = new HashSet<string>();
        public static HashSet<string> DontEssentialDignity = new HashSet<string>();
        public static HashSet<string> DontCelestialIntersection = new HashSet<string>();

        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Astrologian, Spells.Malefic);

        // Modern AST heals oGCD-first: a hardcast GCD heal is justified only when the free
        // tools cannot answer, so the GCD heals shrink to their emergency threshold for an
        // ally one of these WILL cover. The question is "will a free tool heal this person",
        // never "is a free tool off cooldown": each clause mirrors its caster's own gates -
        // combat state, tank-only mode, blacklist - so an ally the tools would never touch
        // keeps the full GCD threshold. On stock settings all three are tank-only, which
        // once left a DPS at 50% unhealed until 45%.
        public static bool SingleTargetOgcdHealReadyFor(Character target)
        {
            if (target == null)
                return false;

            var s = Models.Astrologian.AstrologianSettings.Instance;
            var isTank = target.IsTank();
            var soloSelf = !Globals.InParty && target == Core.Me;

            if (s.EssentialDignity && Core.Me.InCombat && Spells.EssentialDignity.IsKnownAndReady()
                && !DontEssentialDignity.Contains(target.Name)
                && (!s.EssentialDignityTankOnly || isTank || soloSelf))
                return true;

            if (s.CelestialIntersection && Globals.PartyInCombat && Spells.CelestialIntersection.IsKnownAndReady()
                && !DontCelestialIntersection.Contains(target.Name)
                && (!s.CelestialIntersectionTankOnly || isTank))
                return true;

            // Exaltation's threshold path only ever covers a tank - and only while that path
            // is live: in fight-logic mode against catalogued busters it is reserved for the
            // buster, not for upkeep.
            if (s.Exaltation && Globals.InParty && isTank && Spells.Exaltation.IsKnownAndReady()
                && (!s.FightLogicExaltation || !FightLogic.EnemyHasAnyTankbusterLogic()))
                return true;

            return false;
        }

        public static bool AoeOgcdHealReady()
        {
            var s = Models.Astrologian.AstrologianSettings.Instance;

            if (s.CelestialOpposition && Spells.CelestialOpposition.IsKnownAndReady())
                return true;

            if (s.CollectiveUnconscious && Core.Me.InCombat && Spells.CollectiveUnconscious.IsKnownAndReady())
                return true;

            return false;
        }

        public static List<Character> AllianceBeneficOnly = new List<Character>();
        public static int AoeThreshold => PartyManager.NumMembers > 4 ? AstrologianSettings.Instance.AoeNeedHealingFullParty : AstrologianSettings.Instance.AoeNeedHealingLightParty;

        public static bool NeedToInterruptCast()
        {
            // Scalebound Extreme Rathalos
            if (Core.Me.HasAura(1495))
                return false;

            if (Casting.CastingSpell != Spells.Ascend && Casting.SpellTarget?.CurrentHealth < 1)
            {
                Logger.Error($@"Stopped Cast: Unit Died");
                return true;
            }

            if (Casting.CastingSpell == Spells.Ascend && (Casting.SpellTarget?.HasAura(Auras.Raise) == true || Casting.SpellTarget?.CurrentHealth > 0))
            {
                Logger.Error($@"Stopped Resurrection: Unit has raise aura");
                return true;
            }

            if (AstrologianSettings.Instance.InterruptHealing && Casting.DoHealthChecks &&
                Casting.SpellTarget?.CurrentHealthPercent >= AstrologianSettings.Instance.InterruptHealingHealthPercent)
            {
                // Exhaustive chain: each AoE heal is judged by its own party count, and only
                // single-target casts fall to the target-health rule. The old shape let a
                // still-needed Helios fall into the single-target else and get cancelled.
                if (Casting.CastingSpell == Spells.Helios)
                {
                    if (PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r =>
                            r.CurrentHealth > 0 && r.WithinSpellRange(Spells.Helios.Radius) && r.CurrentHealthPercent <=
                            AstrologianSettings.Instance.HeliosHealthPercent) < AoeThreshold)
                    {
                        Logger.Error($@"Stopped Healing: Party's Health Too High");
                        return true;
                    }
                }
                else if (Casting.CastingSpell == Spells.AspectedHelios)
                {
                    if (PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r =>
                            r.CurrentHealth > 0 &&
                            r.WithinSpellRange(Spells.AspectedHelios.Radius) &&
                            r.CurrentHealthPercent <=
                            AstrologianSettings.Instance.DiurnalHeliosHealthPercent &&
                            !r.HasAura(Auras.AspectedHelios, true) && !r.HasAura(Auras.HeliosConjunction, true)) < AoeThreshold)
                    {
                        Logger.Error($@"Stopped Healing: Party's Health Too High");
                        return true;
                    }
                }
                else
                {
                    Logger.Error($@"Stopped Healing: Target's Health Too High");
                    return true;
                }
            }

            if (AstrologianSettings.Instance.InterruptDamageToHeal && !Core.Me.HasAura(1495))
            {
                if (Casting.CastingSpell == Spells.Malefic || Casting.CastingSpell == Spells.Malefic2 ||
                    Casting.CastingSpell == Spells.Malefic3 || Casting.CastingSpell == Spells.Gravity)
                {

                    var lowestHealthToInterruptList = new[]
                    {
                        AstrologianSettings.Instance.Benefic ? AstrologianSettings.Instance.BeneficHealthPercent -10 : 0,
                        AstrologianSettings.Instance.Benefic2 ? AstrologianSettings.Instance.Benefic2HealthPercent -10 : 0
                    };

                    var lowestHealthToInterrupt = lowestHealthToInterruptList.Max();

                    if (PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Any(r => r.CurrentHealth > 0 &&
                        r.CurrentHealthPercent <= lowestHealthToInterrupt && r.WithinSpellRange(30) && r.InLineOfSight()))
                    {
                        Logger.Error($@"Stopping Cast: Need To Heal Someone In The Party");
                        return true;
                    }
                }
            }

            if (!Globals.InParty || !Globals.PartyInCombat)
                return false;

            return false;
        }

        public static void GroupExtension()
        {
            Group.UpdateAlliance(
                AstrologianSettings.Instance.IgnoreAlliance,
                AstrologianSettings.Instance.HealAllianceDps,
                AstrologianSettings.Instance.HealAllianceHealers,
                AstrologianSettings.Instance.HealAllianceTanks,
                AstrologianSettings.Instance.ResAllianceDps,
                AstrologianSettings.Instance.ResAllianceHealers,
                AstrologianSettings.Instance.ResAllianceTanks
            );
        }

        public static readonly uint[] ScholarAndSageShieldsNotToOverwrite = {
            Auras.Catalyze,
            Auras.Galvanize,
            Auras.SeraphicVeil,
            Auras.EukrasianDiagnosis,
            Auras.EukrasianPrognosis
        };

        public static Vector3 EarthlyStarLocation { get; set; }

        // Set on every Earthly Star placement attempt, accepted or rejected, so a refused
        // ground-target cannot re-dispatch at pulse rate.
        public static long LastEarthlyStarAttemptTick { get; set; }

        // How close Divination's cooldown has to be before we report the burst as
        // imminent. Covers the pre-burst weave where Divination is about to go out and
        // an interruption would delay the press.
        private const int DivinationImminentMs = 5000;

        /// <summary>
        /// Reports AST burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the AST rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Imminent-only guard: Divination (+6% party damage, 20s, every 2 minutes)
        /// heads the densest healer press cluster (Divination, held cards, Lord,
        /// Oracle) — the press must not be delayed, but everything after it is
        /// tolerant (Divining lasts 30s), so no active window is reported.
        /// Sources: official job guide, The Balance AST basic guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Divination almost off cooldown: the press is about to happen, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.Divination.IsKnown())
            {
                var cooldownMs = Spells.Divination.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= DivinationImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "AST Divination");
            }
        }
    }
}

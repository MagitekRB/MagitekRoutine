using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Scholar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Scholar
    {
        public static List<Character> AlliancePhysickOnly = new List<Character>();

        private static readonly HashSet<uint> DamageSpells = new HashSet<uint>()
        {
            Spells.SchRuin.Id,
            Spells.Broil.Id,
            Spells.Broil2.Id,
            Spells.Broil3.Id,
            Spells.BroilIV.Id,
        };

        public static double SeraphTimeRemaining()
        {
            return ActionResourceManager.Scholar.Timer.TotalSeconds;
        }

        public static bool NeedToInterruptCast()
        {
            if (Casting.CastingSpell != Spells.Resurrection && Casting.SpellTarget?.CurrentHealth < 1)
            {
                Logger.Error($@"Stopped Cast: Unit Died");
                return true;
            }

            if (Casting.CastingSpell == Spells.Resurrection && (Casting.SpellTarget?.HasAura(Auras.Raise) == true || Casting.SpellTarget?.CurrentHealth > 0))
            {
                Logger.Error($@"Stopped Resurrection: Unit has raise aura");
                return true;
            }

            // Scalebound Extreme Rathalos
            if (Core.Me.HasAura(1495))
                return false;

            if (Casting.CastingSpell == Spells.Succor || Casting.CastingSpell == Spells.Adloquium)
                return false;

            if (ScholarSettings.Instance.InterruptHealing && Casting.DoHealthChecks && Casting.SpellTarget?.CurrentHealthPercent >= ScholarSettings.Instance.InterruptHealingPercent)
            {
                Logger.Error($@"Stopped Healing: Target's Health Too High");
                return true;
            }

            if (ScholarSettings.Instance.StopCastingIfBelowHealthPercent && DamageSpells.Contains(Core.Me.CastingSpellId))
            {
                if (Globals.InParty)
                {
                    if (Group.CastableAlliesWithin30.Any(c => c?.CurrentHealthPercent < ScholarSettings.Instance.DamageOnlyIfAboveHealthPercent && c.IsAlive))
                    {
                        Logger.Error($@"Stopped Cast: Ally below {ScholarSettings.Instance.DamageOnlyIfAboveHealthPercent}% Health");
                        return true;
                    }
                }
                else
                {
                    if (Core.Me.CurrentHealthPercent < ScholarSettings.Instance.DamageOnlyIfAboveHealthPercent)
                    {
                        Logger.Error($@"Stopped Cast: Self below {ScholarSettings.Instance.DamageOnlyIfAboveHealthPercent}% Health");
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
                ScholarSettings.Instance.IgnoreAlliance,
                ScholarSettings.Instance.HealAllianceDps,
                ScholarSettings.Instance.HealAllianceHealers,
                ScholarSettings.Instance.HealAllianceTanks,
                ScholarSettings.Instance.ResAllianceDps,
                ScholarSettings.Instance.ResAllianceHealers,
                ScholarSettings.Instance.ResAllianceTanks
            );
        }

        // Seraphism masks Adloquium -> Manifestation (37015) and Succor -> Accession (37016), BOTH
        // INSTANT. Casting the base id while masked still resolves (DoAction proxies), but every
        // wait on the base cast BAR burns its full timeout: a live run measured 3.0s of dead air per
        // attempt at two independent call sites, because IsCasting never flips for an instant
        // masked action. Cast the resolved spell instead so the framework's timing data matches
        // what actually executes. HasAura(Seraphism) is one aura scan - cheap enough for cast sites.
        public static SpellData AdloquiumSpell =>
            Core.Me.HasAura(Utilities.Auras.Seraphism) ? Spells.Manifestation : Spells.Adloquium;

        public static SpellData SuccorSpell =>
            Core.Me.HasAura(Utilities.Auras.Seraphism) ? Spells.Accession : Spells.Succor;

        public static int EnemiesInCone;

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesInCone = Core.Me.EnemiesInCone(8);

        }

        // How close Chain Stratagem's cooldown has to be before we report the burst as
        // imminent. Covers the pre-burst weave where CS is about to go out and an
        // interruption would delay the press.
        private const int ChainStratagemImminentMs = 5000;

        /// <summary>
        /// Reports SCH burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the SCH rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Imminent-only guard: Chain Stratagem (+10% crit on the target, 20s, every
        /// 2 minutes) is a party buff whose press must not be delayed by a foreign 2s
        /// cast — but nothing after the press is interruption-sensitive (Impact
        /// Imminent lasts 30s), so no active window is reported.
        /// Sources: official job guide, The Balance, live client via rb
        /// (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // SCH's own damage (Broil spam, Baneful Impaction on a 30s leash) never
            // justifies an active window — imminent only.
            // Chain Stratagem almost off cooldown: the press is about to happen, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.ChainStrategem.IsKnown())
            {
                var cooldownMs = Spells.ChainStrategem.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= ChainStratagemImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "SCH Chain Stratagem");
            }
        }
    }
}

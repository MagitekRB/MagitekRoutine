using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Sage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Sage;
using Auras = Magitek.Utilities.Auras;
using SageRoutine = Magitek.Utilities.Routines.Sage;

namespace Magitek.Logic.Sage
{
    internal static class AoE
    {
        // Pneuma's line is 4 yalms across (game data: CastType 4, XAxisModified 4). Its length is
        // the spell's own Radius, so only the width needs stating here.
        private const float PneumaLineWidth = 4f;

        public static async Task<bool> Phlegma()
        {
            // No AoeControl gate: the enemy-count check below is deliberately commented out
            // because Phlegma is used as a single-target GCD, and it has no single-target
            // fallback to fall through to - gating it on the AoE toggle just deletes it from
            // the rotation for anyone who turns AoE off.
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!Spells.Phlegma.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                return false;

            // Phlegma is a great single target attack (690 potency at III), so it is not gated
            // on an enemy count - it is a gain even on one target.
            //if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Phlegma.Radius + r.CombatReach) < SageSettings.Instance.AoEEnemies)
            //    return false;
            var spell = Spells.PhlegmaIII;
            if (!Spells.PhlegmaIII.IsKnown())
                spell = Spells.PhlegmaII.IsKnown() ? Spells.PhlegmaII : Spells.Phlegma;

            if (!spell.IsKnownAndReady())
                return false;

            return await spell.Cast(target);
        }

        public static async Task<bool> Dyskrasia()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.AoE)
                return false;

            if (!Spells.Dyskrasia.IsKnown())
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.Dyskrasia.Radius)) < SageSettings.Instance.AoEEnemies)
                return false;

            var spell = Spells.DyskrasiaII.IsKnown() ? Spells.DyskrasiaII : Spells.Dyskrasia;
            //Not cast on target, cast on self
            return await spell.Cast(Core.Me);
        }

        public static async Task<bool> EukrasianDyskrasia()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.AoE)
                return false;

            if (!SageSettings.Instance.EukrasianDyskrasia)
                return false;

            // Same gate the single-target dot uses: the checkbox above the threshold governs both,
            // and a boss is always worth the dot because its time-to-death estimate is unreliable.
            if (SageSettings.Instance.UseTTDForDots
                && Combat.CurrentTargetCombatTimeLeft <= SageSettings.Instance.DontDotIfEnemyDyingWithin
                && !Core.Me.CurrentTarget.IsBoss())
                return false;

            if (!Spells.EukrasianDyskrasia.IsKnownAndReady())
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.EukrasianDyskrasia.Radius)) < SageSettings.Instance.AoEEnemies)
                return false;

            if (!Heal.IsEukrasiaReady())
                return false;

            var targetChar = Core.Me.CurrentTarget as Character;

            if (targetChar != null && targetChar.CharacterAuras.Count() >= 25)
                return false;

            // "no dot at all, or a dot inside the refresh window" is just "no dot with more than
            // the refresh window left" - the old shape walked each enemy's aura list three times
            // per pulse to ask the same question.
            if (!Combat.Enemies.Any(x => !x.HasAnyAura(DotAuras, true, SageSettings.Instance.DotRefreshMSeconds)
                                         && x.WithinSpellRange(Spells.EukrasianDyskrasia.Radius)))
                return false;

            return await UseEukrasianDyskrasia(Core.Me.CurrentTarget);
        }

        private static readonly uint[] DotAuras =
        {
            Auras.EukrasianDosis,
            Auras.EukrasianDosisII,
            Auras.EukrasianDosisIII,
            Auras.EukrasianDyskrasia
        };

        private static async Task<bool> UseEukrasianDyskrasia(GameObject target)
        {
            var spell = Spells.EukrasianDyskrasia;
            var aura = Auras.EukrasianDyskrasia;

            if (!await Heal.UseEukrasia(spell.Id, target))
                return false;

            return await spell.CastAura(target, (uint)aura);
        }

        public static async Task<bool> Toxikon()
        {
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!Spells.Toxikon.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                return false;

            if (Addersting == 0)
                return false;

            // Four independent reasons, each honouring its own setting. Previously moving set this
            // unconditionally - so the "while moving" checkbox did nothing - and a compound guard
            // above could disable the full-Addersting and low-mana reasons outright whenever AoE
            // was off. Only the enemy-count reason is an AoE decision, so only it reads AoeControl.
            var movingCheck = MovementManager.IsMoving && SageSettings.Instance.ToxiconWhileMoving;
            var enemyCountCheck = AoeControl.Enabled && SageSettings.Instance.AoE
                && Combat.Enemies.Count(r => r.Distance(target) <= Spells.Toxikon.Radius + r.CombatReach) >= SageSettings.Instance.AoEEnemies;
            var adderstingCheck = SageSettings.Instance.ToxiconOnFullAddersting && Addersting == 3;
            var lowManaCheck = SageSettings.Instance.ToxiconOnLowMana && Core.Me.CurrentManaPercent < SageSettings.Instance.MinimumManaPercentToDoDamage;

            var doToxicon = movingCheck || enemyCountCheck || adderstingCheck || lowManaCheck;

            if (doToxicon)
            {
                var spell = Spells.ToxikonII.IsKnown() ? Spells.ToxikonII : Spells.Toxikon;
                return await spell.Cast(target);
            }
            else
            {
                return false;
            }
        }
        public static async Task<bool> Pneuma()
        {
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.AoE)
                return false;

            if (!SageSettings.Instance.Pneuma)
                return false;

            if (!Spells.Pneuma.IsKnown())
                return false;

            if (SageSettings.Instance.PneumaHealOnly)
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                return false;

            // Pneuma's damage is a LINE - 25y ahead, 4y across - not a circle. Counting every enemy
            // within 25y of the target described a 50y-wide disc, so three mobs anywhere nearby spent
            // a 120s cooldown on what was often a single-target hit.
            if (SageRoutine.EnemiesInLine(Spells.Pneuma.Radius, PneumaLineWidth) < SageSettings.Instance.AoEEnemies)
                return false;

            if (!Spells.Pneuma.IsReady())
                return false;

            return await Spells.Pneuma.Cast(target);
        }

        public static async Task<bool> Psyche()
        {
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.UsedPsyche)
                return false;

            var target = Core.Me.CurrentTarget;

            if (target == null)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Psyche.Radius) < SageSettings.Instance.PsycheAoEEnemies)
                return false;

            if (!Spells.Psyche.IsKnown())
                return false;

            return await Spells.Psyche.Cast(target);
        }
    }
}

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

namespace Magitek.Logic.Sage
{
    internal static class AoE
    {
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

            // Phlegma is a great 550 potency single target attack.
            //if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Phlegma.Radius + r.CombatReach) < SageSettings.Instance.AoEEnemies)
            //    return false;
            var spell = Spells.PhlegmaIII;
            if (!Spells.PhlegmaIII.IsKnown())
                spell = Spells.PhlegmaII.IsKnown() ? Spells.PhlegmaII : Spells.Phlegma;

            if (spell.Charges == 0)
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

            if (Combat.CurrentTargetCombatTimeLeft <= SageSettings.Instance.DontDotIfEnemyDyingWithin)
                return false;

            if (!SageSettings.Instance.EukrasianDyskrasia)
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

            if (!Combat.Enemies.Any(x => (!x.HasAnyAura(DotAuras, true) || (x.HasAnyAura(DotAuras, true) && !x.HasAnyAura(DotAuras, true, SageSettings.Instance.DotRefreshMSeconds)))
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

            if (Combat.Enemies.Count(r => r.Distance(target) <= Spells.Pneuma.Radius) < SageSettings.Instance.AoEEnemies)
                return false;

            if (Spells.Pneuma.Cooldown != TimeSpan.Zero)
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

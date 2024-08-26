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
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (Core.Me.ClassLevel < Spells.Phlegma.LevelAcquired)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            // Phlegma is a great 550 potency single target attack.
            //if (Combat.Enemies.Count(r => r.Distance(Core.Me.CurrentTarget) <= Spells.Phlegma.Radius + r.CombatReach) < SageSettings.Instance.AoEEnemies)
            //    return false;
            var spell = Spells.PhlegmaIII;

            if (Core.Me.ClassLevel < 82)
            {
                spell = Spells.PhlegmaII;
            }
            if (Core.Me.ClassLevel < 72)
            {
                spell = Spells.Phlegma;
            }

            if (spell.Charges == 0)
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Dyskrasia()
        {
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.AoE)
                return false;

            if (Core.Me.ClassLevel < Spells.Dyskrasia.LevelAcquired)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= Spells.Dyskrasia.Radius + r.CombatReach) < SageSettings.Instance.AoEEnemies)
                return false;

            var spell = Spells.DyskrasiaII;

            if (Core.Me.ClassLevel < 82)
            {
                spell = Spells.Dyskrasia;
            }

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EukrasianDyskrasia()
        {
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

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= Spells.EukrasianDyskrasia.Radius + r.CombatReach) < SageSettings.Instance.AoEEnemies)
                return false;

            if (!Heal.IsEukrasiaReady())
                return false;

            var targetChar = Core.Me.CurrentTarget as Character;

            if (targetChar != null && targetChar.CharacterAuras.Count() >= 25)
                return false;

            if (!Combat.Enemies.Any(x => (!x.HasAnyAura(DotAuras, true) || (x.HasAnyAura(DotAuras, true) && !x.HasAnyAura(DotAuras, true, SageSettings.Instance.DotRefreshMSeconds)))
                                         && x.Distance(Core.Me) <= Spells.EukrasianDyskrasia.Radius + Core.Me.CombatReach))
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

            if (!SageSettings.Instance.ToxiconWhileMoving && !SageSettings.Instance.AoE)
                return false;

            if (Core.Me.ClassLevel < Spells.Toxikon.LevelAcquired)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Addersting == 0)
                return false;

            var doToxicon = false;

            if (MovementManager.IsMoving)
            {
                doToxicon = true;
            }
            else
            {
                var enemyCountCheck = SageSettings.Instance.AoE && Combat.Enemies.Count(r => r.Distance(Core.Me.CurrentTarget) <= Spells.Toxikon.Radius + r.CombatReach) >= SageSettings.Instance.AoEEnemies;
                var adderstingCheck = SageSettings.Instance.ToxiconOnFullAddersting && Addersting == 3;
                var lowManaCheck = SageSettings.Instance.ToxiconOnLowMana && Core.Me.CurrentManaPercent < SageSettings.Instance.MinimumManaPercentToDoDamage;

                if (enemyCountCheck || adderstingCheck || lowManaCheck)
                    doToxicon = true;
            }

            if (doToxicon)
            {
                var spell = Spells.ToxikonII;

                if (Core.Me.ClassLevel < 82)
                {
                    spell = Spells.Toxikon;
                }
                return await spell.Cast(Core.Me.CurrentTarget);
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

            if (Core.Me.ClassLevel < Spells.Pneuma.LevelAcquired)
                return false;

            if (SageSettings.Instance.PneumaHealOnly)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me.CurrentTarget) <= Spells.Pneuma.Radius) < SageSettings.Instance.AoEEnemies)
                return false;

            if (Spells.Pneuma.Cooldown != TimeSpan.Zero)
                return false;

            return await Spells.Pneuma.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Psyche()
        {
            if (!SageSettings.Instance.DoDamage)
                return false;

            if (!SageSettings.Instance.UsedPsyche)
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me.CurrentTarget) <= Spells.Psyche.Radius) < SageSettings.Instance.PsycheAoEEnemies)
                return false;

            if (Core.Me.ClassLevel < Spells.Psyche.LevelAcquired)
                return false;

            return await Spells.Psyche.Cast(Core.Me.CurrentTarget);
        }
    }
}

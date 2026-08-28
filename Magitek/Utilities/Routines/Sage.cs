using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Sage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Sage
    {
        /// <summary>
        /// Addersgall caps at three charges at every level that has the gauge, and the 20s
        /// generation timer stops while capped.
        /// </summary>
        public const int MaxAddersgall = 3;

        /// <summary>
        /// Enemies inside a rectangular line AoE cast forward from the player: <paramref name="length"/>
        /// yalms ahead, <paramref name="width"/> yalms across in total. Decomposed from the angle off
        /// our heading - forward = distance * cos, sideways = distance * sin - because the repo's
        /// other AoE counters are circles and cones, and a line gated as a circle describes a disc
        /// twice its length across (Pneuma is 25y long and 4y wide: a circle test passes on mobs
        /// 25y off to either side that the line never touches).
        /// Combat reach is added to both axes, matching WithinSpellRange's edge-to-edge convention.
        /// Job-scoped while Sage is the only caller; promote to GameObjectExtensions when a second
        /// job needs it.
        /// </summary>
        public static int EnemiesInLine(float length, float width)
        {
            var halfWidth = width / 2f;

            return Combat.Enemies.Count(r =>
            {
                if (r == null)
                    return false;

                var angle = r.RadiansFromPlayerHeading();

                // Behind us: a forward line cannot reach them however close they are.
                if (angle >= Math.PI / 2)
                    return false;

                var distance = r.Distance(Core.Me);
                var forward = distance * Math.Cos(angle);
                var sideways = distance * Math.Sin(angle);

                return forward <= length + r.CombatReach
                       && sideways <= halfWidth + r.CombatReach;
            });
        }




        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Sage, Spells.Diagnosis);

        public static bool CanWeave()
        {
            if (SageSettings.Instance.WeaveOGCDHeals
                && Core.Me.CurrentMana >= SageSettings.Instance.WeaveOGCDHealsManaPercent)
            {
                if (GlobalCooldown.CanWeave(1))
                    return true;
                else if (Casting.LastSpellTimeFinishAge.ElapsedMilliseconds > 1750 + BaseSettings.Instance.UserLatencyOffset)
                    return true;
            }
            else
            {
                if (Casting.LastSpellTimeFinishAge.ElapsedMilliseconds > 750 + BaseSettings.Instance.UserLatencyOffset)
                    return true;
            }

            return false;
        }

        public static bool NeedToInterruptCast()
        {
            // Scalebound Extreme Rathalos
            if (Core.Me.HasAura(1495))
                return false;

            // Don't interrupt FightLogic spells... just in case.
            if (!FightLogic.IsFlReady)
                return false;

            if (Casting.CastingSpell != Spells.Egeiro && Casting.SpellTarget?.CurrentHealth < 1)
            {
                Logger.Error($@"Stopped Cast: Unit Died");
                return true;
            }

            if (Casting.CastingSpell == Spells.Egeiro && (Casting.SpellTarget?.HasAura(Auras.Raise) == true || Casting.SpellTarget?.CurrentHealth > 0))
            {
                Logger.Error($@"Stopped Resurrection: Unit has raise aura");
                return true;
            }

            /*
            if (SageSettings.Instance.InterruptHealing && Casting.DoHealthChecks &&
                Casting.SpellTarget?.CurrentHealthPercent >= SageSettings.Instance.InterruptHealingHealthPercent)
            {
                if (Casting.CastingSpell == Spells.Prognosis && PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r =>
                        r.CurrentHealth > 0 && r.WithinSpellRange(Spells.Prognosis.Radius) && r.CurrentHealthPercent <=
                        SageSettings.Instance.PrognosisHpPercent) <
                    Logic.Sage.Heal.AoeNeedHealing)
                {
                    Logger.Error($@"Stopped Healing Prognosis: Party's Health Too High");
                    return true;
                }
                else if (Casting.CastingSpell == Spells.Pneuma && PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Count(r =>
                         r.CurrentHealth > 0 && r.WithinSpellRange(Spells.Pneuma.Radius) && r.CurrentHealthPercent <=
                         SageSettings.Instance.PneumaHpPercent) <
                    Logic.Sage.Heal.AoeNeedHealing)
                {
                    Logger.Error($@"Stopped Healing Pneuma: Party's Health Too High");
                    return true;
                }
                else if (Casting.CastingSpell == Spells.Diagnosis)
                {
                    Logger.Error($@"Stopped Healing Diagnosis: Target's Health Too High");
                    return true;
                }

                return false;
            }

            if (SageSettings.Instance.InterruptDamageToHeal && !Core.Me.HasAura(1495))
            {
                if (Casting.CastingSpell == Spells.Dosis || Casting.CastingSpell == Spells.DosisII ||
                    Casting.CastingSpell == Spells.DosisIII || Casting.CastingSpell == Spells.Dyskrasia ||
                    Casting.CastingSpell == Spells.DyskrasiaII)
                {
                    if (PartyManager.VisibleMembers.Select(r => r.BattleCharacter).Any(r => r.CurrentHealth > 0 &&
                        r.CurrentHealthPercent <= SageSettings.Instance.InterruptDamageHealthPercent && r.WithinSpellRange(30) && r.InLineOfSight()))
                    {
                        Logger.Error($@"Stopping Cast: Need To Heal Someone In The Party");
                        return true;
                    }
                }
            }
            */

            if (!Globals.InParty || !Globals.PartyInCombat)
                return false;

            return false;
        }
        public static void GroupExtension()
        {
            Group.UpdateAlliance(
                SageSettings.Instance.IgnoreAlliance,
                SageSettings.Instance.HealAllianceDps,
                SageSettings.Instance.HealAllianceHealers,
                SageSettings.Instance.HealAllianceTanks,
                SageSettings.Instance.ResAllianceDps,
                SageSettings.Instance.ResAllianceHealers,
                SageSettings.Instance.ResAllianceTanks
            );
        }


    }
}

using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Scholar;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities.Routines
{
    internal static class Scholar
    {
        public static List<Character> AlliancePhysickOnly = new List<Character>();

        private static readonly HashSet<uint> DamageSpells = new HashSet<uint>()
        {
            Spells.Ruin.Id,
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

        public static int EnemiesInCone;

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesInCone = Core.Me.EnemiesInCone(8);

        }
    }
}

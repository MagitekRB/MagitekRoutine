using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.OccultCrescent;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;

namespace Magitek.Logic.Ninja
{
    internal static class Cooldown
    {
        public static async Task<bool> Mug()
        {

            if (!Spells.Mug.IsKnown())
                return false;

            // Don't use regular Mug in Occult Crescent content if Dokumori is enabled for gold farming
            // Only disable for multi-target scenarios (2+ enemies) - single target should use normal rotation
            if (Core.Me.OnOccultCrescent() && OccultCrescentSettings.Instance.UseDokumori)
            {
                var nearbyEnemies = Combat.Enemies.Count();
                if (nearbyEnemies >= 2 || !OccultCrescentSettings.Instance.DokumoriOnlyMultipleTargets)
                    return false;
            }

            if (Combat.CombatTime.ElapsedMilliseconds < Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds * NinjaRoutine.OpenerBurstAfterGCD - 770)
                return false;

            if (ActionResourceManager.Ninja.NinkiGauge + 40 > 100)
                return false;

            return await Spells.Mug.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> TrickAttack()
        {

            if (!Spells.TrickAttack.IsKnown())
                return false;

            if (Spells.Mug.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Spells.Bunshin.IsKnown() && Spells.Bunshin.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Spells.SpinningEdge.Cooldown.TotalMilliseconds >= 800)
                return false;

            return await Spells.TrickAttack.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DreamWithinaDream()
        {

            if (!Spells.DreamWithinaDream.IsKnown())
                return false;

            if (Spells.TrickAttack.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Casting.SpellCastHistory.First().Spell == Spells.TrickAttack && Spells.SpinningEdge.Cooldown.TotalMilliseconds < 800)
                return false;

            return await Spells.DreamWithinaDream.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> ZeshoMeppo()
        {

            if (!Spells.ZeshoMeppo.IsKnown())
                return false;

            if (Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 20))
                return false;

            if (Casting.SpellCastHistory.First().Spell == Spells.TrickAttack)
                return false;

            if (NinjaRoutine.AoeEnemies6Yards > 1)
                return false;

            return await Spells.ZeshoMeppo.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> TenriJindo()
        {

            if (!Spells.TenriJindo.IsKnown())
                return false;

            if (Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 20))
                return false;

            if (Casting.SpellCastHistory.First().Spell == Spells.TrickAttack)
                return false;

            return await Spells.TenriJindo.Cast(Core.Me.CurrentTarget);

        }
    }
}

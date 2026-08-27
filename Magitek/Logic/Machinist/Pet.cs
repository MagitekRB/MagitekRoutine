using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Threading.Tasks;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Logic.Machinist
{
    internal static class Pet
    {
        public static async Task<bool> RookQueen()
        {
            if (!MachinistSettings.Instance.UseRookQueen)
                return false;

            if (ActionResourceManager.Machinist.SummonRemaining > TimeSpan.Zero)
                return false;

            if (Core.Me.HasAura(Auras.Overheated))
                return false;

            // Deploy at the configured target - or early, but ONLY when the next Battery
            // generator would overcap: the +20 tools past 80, the +10 Clean Shot past 90.
            // Queen potency scales with Battery spent, so an early summon with no overcap
            // threat just ships a smaller Queen.
            var plus20Imminent = (Spells.AirAnchor.IsKnown() && Spells.AirAnchor.IsReady(2500))
                || (Spells.ChainSaw.IsKnown() && Spells.ChainSaw.IsReady(2500))
                || Core.Me.HasAura(Auras.ExcavatorReady);
            var overcapImminent = ActionResourceManager.Machinist.Battery > 90
                || (ActionResourceManager.Machinist.Battery > 80 && plus20Imminent);
            if (ActionResourceManager.Machinist.Battery < MachinistSettings.Instance.UseRookQueenBattery
                && !overcapImminent)
                return false;

            return await MachinistRoutine.RookQueenPet.Cast(Core.Me);
        }

        public static async Task<bool> RookQueenOverdrive()
        {
            if (!MachinistSettings.Instance.UseRookQueenOverdrive)
                return false;

            if (Core.Me.CurrentTarget.CombatTimeLeft() <= 2 && Core.Me.CurrentTarget.CurrentHealthPercent < 2)
                return await MachinistRoutine.RookQueenOverdrive.Cast(Core.Me.CurrentTarget);

            return false;
        }
    }
}

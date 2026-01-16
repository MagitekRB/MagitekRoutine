using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;


namespace Magitek.Logic.Ninja
{
    internal static class Buff
    {

        public static async Task<bool> Kassatsu()
        {

            if (!Spells.Kassatsu.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || NinjaRoutine.UsedMudras.Count() > 0)
                return false;

            return await Spells.Kassatsu.Cast(Core.Me);

        }

        public static async Task<bool> Bunshin()
        {

            if (!Spells.Bunshin.IsKnown())
                return false;

            if (Spells.Mug.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            return await Spells.Bunshin.Cast(Core.Me);

        }

        public static async Task<bool> Meisui()
        {

            if (!Spells.Meisui.IsKnown())
                return false;

            if (ActionResourceManager.Ninja.NinkiGauge + 50 > 100)
                return false;

            if (Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 20))
                return false;

            if (Casting.SpellCastHistory.First().Spell == Spells.TrickAttack)
                return false;

            if (!NinjaRoutine.GlobalCooldown.IsWeaveWindow(1))
                return false;

            return await Spells.Meisui.Cast(Core.Me);

        }


    }
}

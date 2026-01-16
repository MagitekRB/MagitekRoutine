using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Ninja;
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

            if (!NinjaSettings.Instance.UseKassatsu)
                return false;

            if (!Spells.Kassatsu.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || NinjaRoutine.UsedMudras.Count() > 0)
                return false;

            return await Spells.Kassatsu.Cast(Core.Me);

        }

        public static async Task<bool> Bunshin()
        {
            if (!Spells.Bunshin.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseBunshin)
                return false;

            if (!Spells.Bunshin.IsKnownAndReady())
                return false;

            if (Spells.Mug.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            return await Spells.Bunshin.Cast(Core.Me);

        }

        public static async Task<bool> Meisui()
        {
            if (!Spells.Meisui.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseMeisui)
                return false;

            if (!Spells.Meisui.IsKnownAndReady())
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

        public static async Task<bool> TrueNorth()
        {
            if (NinjaSettings.Instance.EnemyIsOmni || !NinjaSettings.Instance.UseTrueNorth)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(10)) >= NinjaSettings.Instance.AoeEnemies)
                return false;

            if (Core.Me.HasAura(Auras.TrueNorth))
                return false;

            if (ActionManager.LastSpell != Spells.GustSlash)
                return false;

            if (Core.Me.CurrentTarget.IsBehind && ActionResourceManager.Ninja.Kazematoi == 0)
                return await Spells.TrueNorth.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.Mug.IsKnown() && !Spells.Mug.IsReady())
                return false;

            return await PhysicalDps.UsePotion(NinjaSettings.Instance);
        }


    }
}

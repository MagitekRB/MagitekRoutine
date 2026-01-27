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
    internal static class SingleTarget
    {

        #region Base Combo

        public static async Task<bool> SpinningEdge()
        {
            if (!Spells.SpinningEdge.CanCast(Core.Me.CurrentTarget))
                return false;


            return await Spells.SpinningEdge.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GustSlash()
        {
            if (!Spells.GustSlash.IsKnown())
                return false;

            if (ActionManager.LastSpell != Spells.SpinningEdge)
                return false;

            if (!Spells.GustSlash.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.GustSlash.Cast(Core.Me.CurrentTarget);

        }

        //Flank Modifier
        //should be used over aeolian edge if no true north or not in rear
        public static async Task<bool> ArmorCrush()
        {
            if (!Spells.ArmorCrush.IsKnown())
                return false;

            if (ActionManager.LastSpell != Spells.GustSlash)
                return false;

            if (!Spells.ArmorCrush.CanCast(Core.Me.CurrentTarget))
                return false;

            if (ActionResourceManager.Ninja.Kazematoi >= 1)
                return false;

            return await Spells.ArmorCrush.Cast(Core.Me.CurrentTarget);

        }

        //Rear Modifier
        public static async Task<bool> AeolianEdge()
        {
            if (!Spells.AeolianEdge.IsKnown())
                return false;

            if (ActionManager.LastSpell != Spells.GustSlash)
                return false;

            if (!Spells.AeolianEdge.CanCast(Core.Me.CurrentTarget))
                return false;

            return await Spells.AeolianEdge.Cast(Core.Me.CurrentTarget);

        }

        #endregion

        //Missing logic for st and mt
        public static async Task<bool> Bhavacakra()
        {
            if (!Spells.Bhavacakra.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseBhavacakra)
                return false;

            if (!Spells.Bhavacakra.IsKnownAndReady())
                return false;

            if (Spells.TrickAttack.Cooldown >= new TimeSpan(0, 0, 45))
                return await Spells.Bhavacakra.Cast(Core.Me.CurrentTarget);

            //dumping Bhavacakra during Burst Window is missing
            if (ActionResourceManager.Ninja.NinkiGauge < 90 || (Spells.Mug.Cooldown > new TimeSpan(0, 0, 7) && ActionResourceManager.Ninja.NinkiGauge + 40 < 90))
                return false;

            if (NinjaRoutine.AoeEnemies6Yards > 2 && !Core.Me.HasMyAura(Auras.Meisui)
                || NinjaRoutine.AoeEnemies6Yards > 3 && Core.Me.HasMyAura(Auras.Meisui))
                return false;

            //Smart Target Logic needs to be addded
            return await Spells.Bhavacakra.Cast(Core.Me.CurrentTarget);
        }

        //Missing range check
        public static async Task<bool> FleetingRaiju()
        {
            if (!Spells.FleetingRaiju.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseFleetingRaiju)
                return false;

            if (!Spells.FleetingRaiju.IsKnownAndReady())
                return false;

            if (!Core.Me.HasMyAura(Auras.RaijuReady))
                return false;

            return await Spells.FleetingRaiju.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> ForkedRaiju()
        {
            if (!NinjaSettings.Instance.UseForkedRaiju)
                return false;

            if (!Spells.ForkedRaiju.IsKnown())
                return false;

            if (!Spells.ForkedRaiju.IsKnownAndReady())
                return false;

            if (!Core.Me.HasMyAura(Auras.RaijuReady))
                return false;

            return await Spells.ForkedRaiju.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> ThrowingDagger()
        {

            if (!NinjaSettings.Instance.UseThrowingDagger)
                return false;

            if (!Spells.ThrowingDagger.IsKnown())
                return false;

            if (!Spells.ThrowingDagger.IsKnownAndReady())
                return false;

            if (NinjaRoutine.AoeEnemies4Yards > 0)
                return false;

            // Do not start with throwing dagger
            if (Combat.CombatTime.ElapsedMilliseconds < Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds - 770)
                return false;

            return await Spells.ThrowingDagger.Cast(Core.Me.CurrentTarget);

        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            if (!Core.Me.HasTarget)
                return false;

            return PhysicalDps.ForceLimitBreak(Spells.Braver, Spells.Bladedance, Spells.TheEnd, Spells.SpinningEdge);
        }
    }
}

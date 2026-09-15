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

            // Bunshin spends 50 Ninki too and sits above this in the weave list, so once it is ready it goes
            // first. About to come off cooldown, it keeps its 50: a Bhavacakra that drops the gauge under it
            // leaves Bunshin waiting for the gauge to rebuild (twenty seconds, twice in the census). With the
            // gauge full there is room for both. Zesho Meppo and Hellfrog Medium are left alone: they are
            // worth more than the wait.
            var bunshinDue = NinjaSettings.Instance.UseBunshin && Spells.Bunshin.IsKnown() && Spells.Bunshin.Cooldown <= new TimeSpan(0, 0, 7);
            if (bunshinDue && ActionResourceManager.Ninja.NinkiGauge < 100)
                return false;

            if (Spells.TrickAttack.Cooldown >= new TimeSpan(0, 0, 45))
                return await Spells.Bhavacakra.Cast(Core.Me.CurrentTarget);

            // Outside the window Ninki is pooled, and spent only to keep it off the cap: at 90 or more, or when
            // Dokumori is due within seven seconds and its 40 Ninki would not fit. The second clause used to ask
            // for a gauge under 90 after the first had already required 90 or more, so it never fired: Dokumori
            // was refused above 60 Ninki and drifted while the gauge sat at 100.
            var ninki = ActionResourceManager.Ninja.NinkiGauge;
            var dokumoriDue = NinjaSettings.Instance.UseMug && Spells.Mug.IsKnown() && Spells.Mug.Cooldown <= new TimeSpan(0, 0, 7);
            if (ninki < 90 && !(dokumoriDue && ninki + 40 > 100))
                return false;

            if (AoeControl.Enabled && NinjaSettings.Instance.UseAoe && NinjaSettings.Instance.UseHellfrogMedium
                && NinjaRoutine.AoeEnemies6Yards >= Aoe.NinkiAoeEnemies)
                return false;

            //Smart Target Logic needs to be addded
            return await Spells.Bhavacakra.Cast(Core.Me.CurrentTarget);
        }

        // A single-target GCD in its own right (700 potency, 5-yalm splash), not an AoE option: it fires
        // whenever Bunshin has granted it, whatever the AoE toggle says.
        public static async Task<bool> PhantomKamaitachi()
        {
            if (!NinjaSettings.Instance.UsePhantomKamaitachi)
                return false;

            if (!Spells.PhantomKamaitachi.IsKnown())
                return false;

            if (!Core.Me.HasMyAura(Auras.PhantomKamaitachiReady) && Casting.SpellCastHistory.FirstOrDefault()?.Spell != Spells.Bunshin)
                return false;

            return await Spells.PhantomKamaitachi.Cast(Core.Me.CurrentTarget);
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

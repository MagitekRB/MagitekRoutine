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

            // A target about to die is no reason to let Kassatsu go. "Not wanted" below covers a Kunai's Bane
            // that is disabled or held and a target that will not live to see one alike, and on the second
            // the Kassatsu went out at once, onto a corpse. Held here, it waits for the next target; and with
            // Shadow Walker up, where Kunai's Bane is wanted whatever the estimate says, the same wait.
            var target = Core.Me.CurrentTarget;
            if (!Cooldown.OutlivesBurstPair(Spells.Kassatsu, target))
                return false;

            // Pop Kassatsu just ahead of Kunai's Bane so the Kassatsu ninjutsu is the first GCD inside the
            // window instead of a weave spent inside it. Free to go whenever Kunai's Bane is not wanted.
            // Never before the Suiton is built: no ninjutsu can be started under Kassatsu, so an early
            // Kassatsu would lock Shadow Walker out for its whole duration.
            if (Cooldown.KunaisBaneWanted(target)
                && !target.HasAura(Auras.KunaisBane, true) && !target.HasAura(Auras.TrickAttack, true)
                && (!Core.Me.HasMyAura(Auras.ShadowWalker) || Spells.TrickAttack.Cooldown.TotalMilliseconds > Cooldown.KassatsuLeadInMs))
                return false;

            // On the lead-in the pair lands when Trick Attack's recharge ends, not now, so the target has to
            // stand until then as well. With Kunai's Bane already on the target, or not wanted at all, the
            // Kassatsu ninjutsu goes at once and the pair check above is the whole question.
            if (Cooldown.KunaisBaneWanted(target) && Core.Me.HasMyAura(Auras.ShadowWalker)
                && !target.HasAura(Auras.KunaisBane, true) && !target.HasAura(Auras.TrickAttack, true)
                && !Cooldown.OutlivesLeadIn(Spells.Kassatsu, target))
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

            if (DefersToDokumori())
                return false;

            return await Spells.Bunshin.Cast(Core.Me);

        }

        /// <summary>
        /// A ready Dokumori goes before Bunshin, unless it is the one waiting for Kunai's Bane. Bhavacakra
        /// reads this too: a Bunshin that is itself waiting must not have Ninki kept for it, or a ready
        /// Dokumori (refused above 60 Ninki), Bunshin (behind the Dokumori) and Bhavacakra (keeping Bunshin
        /// its 50) all wait on each other until the gauge fills.
        /// </summary>
        public static bool DefersToDokumori()
        {
            return Spells.Mug.Cooldown == new TimeSpan(0, 0, 0) && !Cooldown.DokumoriWaitingForKunaisBane();
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

            if (Casting.SpellCastHistory.FirstOrDefault()?.Spell == Spells.TrickAttack)
                return false;

            if (!NinjaRoutine.GlobalCooldown.IsWeaveWindow(1))
                return false;

            return await Spells.Meisui.Cast(Core.Me);

        }

        public static async Task<bool> TrueNorth()
        {
            if (Core.Me.CurrentTarget.IgnorePositionals(NinjaSettings.Instance.Positionals) || !NinjaSettings.Instance.UseTrueNorth)
                return false;

            if (AoeControl.Enabled && Combat.Enemies.Count(x => x.WithinSpellRange(10)) >= NinjaSettings.Instance.AoeEnemies)
                return false;

            if (Core.Me.HasAura(Auras.TrueNorth))
                return false;

            if (ActionManager.LastSpell != Spells.GustSlash)
                return false;

            // Standing on the other finisher's spot: behind with Armor Crush next, or on the flank with
            // Aeolian Edge next. Behind with Aeolian Edge next is already the right place.
            if (SingleTarget.ArmorCrushIsNext)
                return Core.Me.CurrentTarget.IsBehind && await Spells.TrueNorth.Cast(Core.Me);

            if (Spells.AeolianEdge.IsKnown() && Core.Me.CurrentTarget.IsFlanking)
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

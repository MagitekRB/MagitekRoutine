using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using BeastMasterRoutine = Magitek.Utilities.Routines.BeastMaster;

namespace Magitek.Logic.BeastMaster
{
    internal static class SingleTarget
    {
        // Smash Axe > Axeblade Bite > Shieldsplitter: the TP builder (+13, +15 on the combo hits).
        public static async Task<bool> Combo()
        {
            if (Spells.Shieldsplitter.IsKnown() && ActionManager.LastSpell == Spells.AxebladeBite)
                return await Spells.Shieldsplitter.Cast(Core.Me.CurrentTarget);

            if (Spells.AxebladeBite.IsKnown() && ActionManager.LastSpell == Spells.SmashAxe)
                return await Spells.AxebladeBite.Cast(Core.Me.CurrentTarget);

            if (!await Spells.SmashAxe.Cast(Core.Me.CurrentTarget))
                return false;

            BeastMasterRoutine.NoteEngaged(Core.Me.CurrentTarget);
            return true;
        }

        /// <summary>The one hit allowed on a beast we are holding for: Smash Axe, which starts the auto-attacks.</summary>
        public static async Task<bool> Engage()
        {
            if (BeastMasterRoutine.EngagedTargetId == Core.Me.CurrentTarget.ObjectId)
                return false;

            if (!await Spells.SmashAxe.Cast(Core.Me.CurrentTarget))
                return false;

            BeastMasterRoutine.NoteEngaged(Core.Me.CurrentTarget);
            return true;
        }

        /// <summary>
        /// The instinctual axe. With 100 TP banked the client lets one through; which one is a compass decision:
        /// continue the lit Heart clockwise (an intentional combo), or, with nothing lit and a familiar out, take the
        /// affinity one step before the familiar's Trick so that the Trick completes the combo. Otherwise the first
        /// castable axe, since holding TP earns nothing.
        /// </summary>
        public static async Task<bool> InstinctualSkill()
        {
            if (!BeastMasterSettings.Instance.UseInstinctualSkills)
                return false;

            // Inside the 1-2-3 the chain's TP bonuses (+13, +15) are worth finishing first; the axe goes out once the
            // last known link has landed. Whether an axe would even break the chain is unverified.
            var comboOpen = (ActionManager.LastSpell == Spells.SmashAxe && Spells.AxebladeBite.IsKnown())
                || (ActionManager.LastSpell == Spells.AxebladeBite && Spells.Shieldsplitter.IsKnown());
            if (comboOpen)
                return false;

            // The familiar was just ordered: its Heart is the one to continue, once it is there. And a pair that is
            // still resolving (Wavering Heart) is not to be stepped on.
            if (BeastMasterRoutine.TrickPending || BeastMasterRoutine.WaveringHeart)
                return false;

            // Level 50: a 250 TP axe of the opposite affinity to the open window is Universality, and beats any pair.
            var universality = BeastMasterRoutine.UniversalityAxe();
            if (universality != null)
            {
                if (!await universality.Cast(Core.Me.CurrentTarget))
                    return false;

                BeastMasterRoutine.NoteInstinct(BeastMasterRoutine.AxeAffinity(universality));
                return true;
            }

            var heart = BeastMasterRoutine.EffectiveHeart;

            // Our axe finishes a pair; it does not open one. The Trick opens (earlier in the pulse, once both bars
            // can pay) and our axe answers, which is the pair that earns Mastered Instinct. An axe thrown to open
            // for the familiar lit a Heart nobody could answer when its TP was short, and when it could answer it
            // only turned a Mastered pair into a Natural one (dummy, 2026-09-09). While the axe waits, our TP
            // keeps raising its potency, so nothing is lost by holding it.
            // The one exception: with the yellow diamonds full and Rally not ready, the axe opens and the Trick
            // finishes, so the stack lands on blue for Rallying Cheer instead of overflowing.
            string wanted = null;
            if (heart != null)
                wanted = Affinity.Next(heart);
            else if (BeastMasterRoutine.NaturalPreferred && BeastMasterRoutine.FamiliarCanAnswer && !BeastMasterRoutine.HoldPairForRally)
                wanted = Affinity.Previous(BeastMasterRoutine.FamiliarAffinity);

            var axe = BeastMasterRoutine.AxeFor(wanted);
            if (BeastMasterRoutine.HasTpFor(axe))
            {
                if (!await axe.Cast(Core.Me.CurrentTarget))
                    return false;

                BeastMasterRoutine.NoteInstinct(wanted);
                return true;
            }

            // The right axe exists but cannot go now (its own 5 s recast, or TP): while the Heart still has time,
            // waiting for it beats throwing another axe into a lesser, non-intentional pair.
            if (axe != null && axe.IsKnown() && heart != null && BeastMasterRoutine.HeartMsLeft > 1500)
                return false;

            // Nothing to pair: an axe alone is worth throwing only when the bar is full, where it has its whole
            // potency and every further point would be lost. Below the cap the TP is worth more in the next pair.
            // Once the axes have turned at 50 this opens a Sunstrider or Moonstalker window; the rushing forms go last.
            if (!BeastMasterRoutine.TpFull)
                return false;

            foreach (var candidate in BeastMasterRoutine.Axes.OrderBy(a => a != null && (a.Id == Spells.BrutalRage.Id || a.Id == Spells.HawkishTalons.Id) ? 1 : 0))
            {
                if (candidate == axe || !BeastMasterRoutine.HasTpFor(candidate))
                    continue;
                if (!await candidate.Cast(Core.Me.CurrentTarget))
                    return false;

                BeastMasterRoutine.NoteInstinct(BeastMasterRoutine.AxeAffinity(candidate));
                return true;
            }

            return false;
        }

        /// <summary>Gap-closer first; otherwise spent on cooldown while keeping the configured charges.</summary>
        public static async Task<bool> ShieldCharge()
        {
            if (!BeastMasterSettings.Instance.UseShieldCharge || !Spells.ShieldCharge.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget;
            var outOfMelee = !target.WithinSpellRange(5);

            // The reserve may be every charge there is (one until Enhanced Shield Charge at 36): then it is only
            // ever the gap-closer.
            var keep = System.Math.Min(BeastMasterSettings.Instance.ShieldChargeKeepCharges, (int)Spells.ShieldCharge.MaxCharges);
            if (!outOfMelee && Spells.ShieldCharge.Charges < keep + 1)
                return false;

            return await Spells.ShieldCharge.Cast(target);
        }
    }
}

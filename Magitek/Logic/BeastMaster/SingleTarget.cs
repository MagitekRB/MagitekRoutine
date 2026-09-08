using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
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

            return await Spells.SmashAxe.Cast(Core.Me.CurrentTarget);
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

            // Inside the 1-2-3 the chain's TP bonuses (+13, +15) are worth finishing first; the axe goes out after
            // Shieldsplitter or when no chain is open. Whether an axe would even break the chain is unverified.
            if (ActionManager.LastSpell == Spells.SmashAxe || ActionManager.LastSpell == Spells.AxebladeBite)
                return false;

            var heart = BeastMasterRoutine.CurrentHeart;
            var familiar = BeastMasterRoutine.FamiliarAffinity;

            string wanted = null;
            if (heart != null)
                wanted = Affinity.Next(heart);
            else if (familiar != null)
                wanted = Affinity.Previous(familiar);

            var axe = BeastMasterRoutine.AxeFor(wanted);
            if (BeastMasterRoutine.HasTpFor(axe))
                return await axe.Cast(Core.Me.CurrentTarget);

            foreach (var candidate in BeastMasterRoutine.Axes)
            {
                if (candidate == axe)
                    continue;
                if (BeastMasterRoutine.HasTpFor(candidate))
                    return await candidate.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        /// <summary>Gap-closer first; otherwise spent on cooldown while keeping the configured charges.</summary>
        public static async Task<bool> ShieldCharge()
        {
            if (!BeastMasterSettings.Instance.UseShieldCharge || !Spells.ShieldCharge.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget;
            var outOfMelee = target.Distance(Core.Me) > 5 + target.CombatReach;

            // One charge until Enhanced Shield Charge at 36: the kept count cannot exceed what exists.
            var keep = System.Math.Min(BeastMasterSettings.Instance.ShieldChargeKeepCharges, System.Math.Max(0, (int)Spells.ShieldCharge.MaxCharges - 1));
            if (!outOfMelee && Spells.ShieldCharge.Charges < keep + 1)
                return false;

            return await Spells.ShieldCharge.Cast(target);
        }
    }
}

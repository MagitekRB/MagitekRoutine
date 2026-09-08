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

            // Inside the 1-2-3 the chain's TP bonuses (+13, +15) are worth finishing first; the axe goes out after
            // Shieldsplitter or when no chain is open. Whether an axe would even break the chain is unverified.
            if (ActionManager.LastSpell == Spells.SmashAxe || ActionManager.LastSpell == Spells.AxebladeBite)
                return false;

            // The familiar was just ordered: its Heart is the one to continue, once it is there.
            if (BeastMasterRoutine.TrickPending)
                return false;

            var heart = BeastMasterRoutine.EffectiveHeart;
            var familiar = BeastMasterRoutine.WaveringHeart ? null : BeastMasterRoutine.FamiliarAffinity;

            string wanted = null;
            if (heart != null)
                wanted = Affinity.Next(heart);
            else if (familiar != null)
                wanted = Affinity.Previous(familiar);

            var axe = BeastMasterRoutine.AxeFor(wanted);
            if (BeastMasterRoutine.HasTpFor(axe))
            {
                if (!await axe.Cast(Core.Me.CurrentTarget))
                    return false;

                if (BeastMasterRoutine.MsSinceTrick < 10000)
                    Logger.WriteInfo($"[Beastmaster] {axe.LocalizedName} {BeastMasterRoutine.MsSinceTrick:0} ms after the Trick order (heart {heart ?? "none"}).");
                return true;
            }

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

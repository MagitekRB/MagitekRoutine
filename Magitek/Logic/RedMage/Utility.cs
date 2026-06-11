using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using Magitek.Utilities;
using System.Linq;
using static ff14bot.Managers.ActionResourceManager.RedMage;
using RedMageRoutine = Magitek.Utilities.Routines.RedMage;

namespace Magitek.Logic.RedMage
{
    internal static class Utility
    {
        public static bool InCombo()
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (!Spells.Riposte.IsKnown())
                return false;

            if (!Spells.Zwerchhau.IsKnown())
            {
                if (RedMageRoutine.CanContinueComboAfter(Spells.Riposte) || RedMageRoutine.CanContinueComboAfter(Spells.EnchantedRiposte))
                    return true;
            }
            else
            {
                if (RedMageRoutine.CanContinueComboAfter(Spells.Riposte) || RedMageRoutine.CanContinueComboAfter(Spells.EnchantedRiposte)
                || RedMageRoutine.CanContinueComboAfter(Spells.Zwerchhau) || RedMageRoutine.CanContinueComboAfter(Spells.EnchantedZwerchhau))
                    return true;
            }
            return false;
        }
        public static int ManaStacks()
        {

            return ActionResourceManager.RedMage.ManaStacks;
        }

        public static bool InAoeCombo()
        {

            if (!Spells.Moulinet.IsKnown())
                return false;

            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!Spells.EnchantedMoulinet.IsKnown())
                return false;

            if (!RedMageRoutine.CanContinueComboAfter(Spells.EnchantedMoulinet)
                && !RedMageRoutine.CanContinueComboAfter(Spells.EnchantedMoulinetDeux)
                && !RedMageRoutine.CanContinueComboAfter(Spells.EnchantedMoulinetTrois))
                return false;

            if (ManaStacks() == 3)
                return false;

            return true;
        }

        public static bool InComboEnder()
        {
            if (!Spells.Scorch.IsKnown())
                return false;

            // The magicked-combo finisher (Verholy/Verflare -> Scorch -> Resolution) is in progress
            // only while a follow-up is actually castable.
            if (Spells.Scorch.CanCast())
                return true;

            if (Spells.Resolution.IsKnown() && RedMageRoutine.CanContinueComboAfter(Spells.Scorch))
                return true;

            return false;
        }

        public static bool ShouldApproachForCombo()
        {
            if (!Core.Me.HasTarget)
                return false;

            // Always close to avoid stranding an in-progress combo or a free Magicked Swordplay window.
            bool inProgress = Core.Me.HasAura(Auras.MagickedSwordplay, true) || InAoeCombo() || InCombo();

            // Starting the single-target melee combo from full mana respects the melee toggles.
            bool canStartMeleeCombo = RedMageSettings.Instance.UseMelee
                && (!RedMageSettings.Instance.MeleeComboBossesOnly || Combat.IsBoss())
                && !(WhiteMana < 50 || BlackMana < 50);

            if (inProgress || canStartMeleeCombo)
                return Core.Me.CurrentTarget.Distance() > (3 + Core.Me.CurrentTarget.CombatReach);

            return false;
        }
    }
}

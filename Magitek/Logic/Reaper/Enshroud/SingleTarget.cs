using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Reaper.Enshroud
{
    internal static class SingleTarget
    {
        public static async Task<bool> VoidReaping()
        {
            if (!ReaperSettings.Instance.UseVoidReaping || !Spells.VoidReaping.IsKnown())
                return false;

            if (ActionResourceManager.Reaper.LemureShroud < 2 && Spells.Communio.IsKnown())
                return false;

            // Yield to the enhanced partner only while the partner is enabled: with Cross Reaping unticked, the
            // shroud would otherwise have no Reaping to cast after the first one and sit until it expired.
            if (Core.Me.HasAura(Auras.EnhancedCrossReaping) && ReaperSettings.Instance.UseCrossReaping)
                return false;

            if (Core.Me.HasAura(Auras.EnhancedVoidReaping))
            {
                if (!AoeControl.Enabled || !ReaperSettings.Instance.UseAoe || !ReaperSettings.Instance.EfficientAoEPotencyCalculation || Utilities.Routines.Reaper.EnemiesIn8YardCone * Utilities.Routines.Reaper.GrimReapingPotency < Utilities.Routines.Reaper.EnhancedReapingPotency)
                    return await Spells.VoidReaping.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                if (!AoeControl.Enabled || !ReaperSettings.Instance.UseAoe || !ReaperSettings.Instance.EfficientAoEPotencyCalculation || Utilities.Routines.Reaper.EnemiesIn8YardCone * Utilities.Routines.Reaper.GrimReapingPotency < Utilities.Routines.Reaper.ReapingPotency)
                    return await Spells.VoidReaping.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> CrossReaping()
        {
            if (!ReaperSettings.Instance.UseCrossReaping || !Spells.CrossReaping.IsKnown())
                return false;

            if (ActionResourceManager.Reaper.LemureShroud < 2 && Spells.Communio.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.EnhancedVoidReaping) && ReaperSettings.Instance.UseVoidReaping)
                return false;

            if (Core.Me.HasAura(Auras.EnhancedCrossReaping))
            {
                if (!AoeControl.Enabled || !ReaperSettings.Instance.UseAoe || !ReaperSettings.Instance.EfficientAoEPotencyCalculation || Utilities.Routines.Reaper.EnemiesIn8YardCone * Utilities.Routines.Reaper.GrimReapingPotency < Utilities.Routines.Reaper.EnhancedReapingPotency)
                    return await Spells.CrossReaping.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                if (!AoeControl.Enabled || !ReaperSettings.Instance.UseAoe || !ReaperSettings.Instance.EfficientAoEPotencyCalculation || Utilities.Routines.Reaper.EnemiesIn8YardCone * Utilities.Routines.Reaper.GrimReapingPotency < Utilities.Routines.Reaper.ReapingPotency)
                    return await Spells.CrossReaping.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> LemuresSlice()
        {
            //Add level check so it doesn't hang here
            if (!ReaperSettings.Instance.UseLemuresSlice || !Spells.LemuresSlice.IsKnown())
                return false;

            if (ActionResourceManager.Reaper.VoidShroud < 2)
                return false;

            if (!AoeControl.Enabled || !ReaperSettings.Instance.UseAoe || !ReaperSettings.Instance.EfficientAoEPotencyCalculation || Utilities.Routines.Reaper.EnemiesIn8YardCone * Utilities.Routines.Reaper.LemuresScythePotency < Utilities.Routines.Reaper.LemuresSlicePotency)
                return await Spells.LemuresSlice.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> LemuresSliceOfFWeave()
        {
            //Add level check so it doesn't hang here
            if (!ReaperSettings.Instance.UseLemuresSlice || !Spells.LemuresSlice.IsKnown())
                return false;

            // Only use Lemures Slice off weave if resources are deadlocked
            if (ActionResourceManager.Reaper.VoidShroud == 2 && ActionResourceManager.Reaper.LemureShroud == 1)
                return await Spells.LemuresSlice.Cast(Core.Me.CurrentTarget);

            return false;
        }
    }
}

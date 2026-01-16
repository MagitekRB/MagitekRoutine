using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Reaper.Enshroud
{
    internal static class AoE
    {
        public static async Task<bool> GrimReaping()
        {
            if (!ReaperSettings.Instance.UseAoe)
                return false;

            if (!ReaperSettings.Instance.UseGrimReaping || !Spells.GrimReaping.IsKnown())
                return false;

            if (ActionResourceManager.Reaper.LemureShroud < 2 && Spells.Communio.IsKnown()) return false;

            if (ReaperSettings.Instance.EfficientAoEPotencyCalculation)
            {
                if (Core.Me.HasAura(Auras.EnhancedVoidReaping) || Core.Me.HasAura(Auras.EnhancedCrossReaping))
                {
                    if (Utilities.Routines.Reaper.EnemiesIn8YardCone * 200 >= 520)
                        return await Spells.GrimReaping.Cast(Core.Me.CurrentTarget);
                }
                else
                {
                    if (Utilities.Routines.Reaper.EnemiesIn8YardCone * 200 >= 460)
                        return await Spells.GrimReaping.Cast(Core.Me.CurrentTarget);
                }
            }
            else
            {
                if (Utilities.Routines.Reaper.EnemiesIn8YardCone >= ReaperSettings.Instance.GrimReapingTargetCount)
                    return await Spells.GrimReaping.Cast(Core.Me.CurrentTarget);
            }

            return false;

        }

        public static async Task<bool> LemuresScythe()
        {
            if (!ReaperSettings.Instance.UseAoe)
                return false;

            if (!ReaperSettings.Instance.UseLemuresScythe || !Spells.LemuresScythe.IsKnown())
                return false;

            if (ActionResourceManager.Reaper.VoidShroud < 2 && Spells.Communio.IsKnown()) return false;

            if (ReaperSettings.Instance.EfficientAoEPotencyCalculation)
            {
                if (Utilities.Routines.Reaper.EnemiesIn8YardCone * 100 >= 200)
                    return await Spells.LemuresScythe.Cast(Core.Me.CurrentTarget);
            }
            else
            {
                if (Utilities.Routines.Reaper.EnemiesIn8YardCone >= ReaperSettings.Instance.LemuresScytheTargetCount)
                    return await Spells.LemuresScythe.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> LemuresScytheOffWeave()
        {
            if (!ReaperSettings.Instance.UseAoe)
                return false;

            if (!ReaperSettings.Instance.UseLemuresScythe || !Spells.LemuresScythe.IsKnown())
                return false;


            if ((ReaperSettings.Instance.EfficientAoEPotencyCalculation && Utilities.Routines.Reaper.EnemiesIn8YardCone * 100 >= 200)
            || Utilities.Routines.Reaper.EnemiesIn8YardCone >= ReaperSettings.Instance.LemuresScytheTargetCount)
            {
                // Only use Lemures Scythe off weave if resources are deadlocked
                if (ActionResourceManager.Reaper.VoidShroud == 2 && ActionResourceManager.Reaper.LemureShroud == 1)
                    return await Spells.LemuresScythe.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        //Logic for Smart targeting or burst sniping maybe
        public static async Task<bool> Communio()
        {
            if (!ReaperSettings.Instance.UseCommunio || !Spells.Communio.IsKnown())
                return false;

            var shroudEndingSoon = Core.Me.HasAura(Auras.Enshrouded) && !Core.Me.HasAura(Auras.Enshrouded, true, 3000);

            // Prevent blowing remaining enshrouded if for some reason the weaving window for Lemure was missed
            // due to fight mechanics or disconnect with the target.
            if (ActionResourceManager.Reaper.VoidShroud > 1 && !shroudEndingSoon)
                return false;

            if (ActionResourceManager.Reaper.LemureShroud > 1 && !shroudEndingSoon)
                return false;

            return await Spells.Communio.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Sacrificium()
        {
            if (!ReaperSettings.Instance.UseSacrificium || !Spells.Sacraficium.IsKnown())
                return false;

            var shroudEndingSoon = Core.Me.HasAura(Auras.Enshrouded) && !Core.Me.HasAura(Auras.Enshrouded, true, 3000);

            if (!Core.Me.HasAura(Auras.Oblatio))
                return false;

            return await Spells.Sacraficium.Cast(Core.Me.CurrentTarget);
        }
    }
}

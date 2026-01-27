using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Monk;
using Magitek.Toggles;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Monk;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Monk
{
    public class SingleTarget
    {
        public static async Task<bool> Bootshine()
        {
            if (Core.Me.HasAura(Auras.RaptorForm) || Core.Me.HasAura(Auras.CoeurlForm))
                return false;

            if (!Core.Me.HasAura(Auras.OpoOpoForm) && !Core.Me.HasAura(Auras.FormlessFist) && Spells.FormShift.IsKnown())
                return false;

            if (Spells.DragonKick.IsKnown() && ActionResourceManager.Monk.OpoOpoFury == 0)
                return false;

            return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Fallback Bootshine with relaxed form requirements - used as last resort to initiate combat
        /// </summary>
        public static async Task<bool> BootshineFallback()
        {
            // Only check if we're in wrong form (Raptor/Coeurl), but allow casting without form
            if (Core.Me.HasAura(Auras.RaptorForm) || Core.Me.HasAura(Auras.CoeurlForm))
                return false;

            // Don't cast if we have PerfectBalance active (let PerfectBalance logic handle it)
            if (Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            // Basic check: Bootshine must be known and ready
            if (!Spells.Bootshine.IsKnownAndReady())
                return false;

            return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TrueStrike()
        {
            if (!Spells.TrueStrike.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.RaptorForm) && Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (Spells.TwinSnakes.IsKnown() && ActionResourceManager.Monk.RaptorFury == 0)
                return false;

            return await Spells.TrueStrike.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SnapPunch()
        {
            if (!Spells.SnapPunch.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.CoeurlForm) && Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (Spells.Demolish.IsKnown() && ActionResourceManager.Monk.CoeurlFury == 0)
                return false;

            return await Spells.SnapPunch.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DragonKick()
        {
            if (!Spells.DragonKick.IsKnown())
                return false;

            if(Core.Me.HasAura(Auras.FormlessFist))
                return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);

            if (!Core.Me.HasAura(Auras.OpoOpoForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.OpoOpoFury == 1)
                return false;

            return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TwinSnakes()
        {
            if (!Spells.TwinSnakes.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.RaptorForm) && Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.RaptorFury == 1)
                return false;

            return await Spells.TwinSnakes.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> Demolish()
        {

            if (!Spells.Demolish.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.CoeurlForm) && Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.CoeurlFury >= 1)
                return false;

            return await Spells.Demolish.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TheForbiddenChakra()
        {
            if (!Spells.SteelPeak.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseTheForbiddenChakra)
                return false;

            if (ActionResourceManager.Monk.ChakraCount < 5)
                return false;

            if (!MonkSettings.Instance.BurstLogicHoldBurst && Spells.RiddleofFire.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            return await Spells.SteelPeak.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PerfectBalance()
        {
            if (!Spells.PerfectBalance.IsKnown())
                return false;

            if (!MonkSettings.Instance.UsePerfectBalance)
                return false;

            if (!Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            if (!ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Both) && ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Lunar) && !MonkSettings.Instance.DoubleLunar)
            {
                if (ActionResourceManager.Monk.MasterGaugeCount == 0)
                {
                    if (ActionResourceManager.Monk.OpoOpoFury == 0)
                        return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
                    else
                        return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
                }

                if (ActionResourceManager.Monk.MasterGaugeCount == 1)
                {
                    if (ActionResourceManager.Monk.RaptorFury == 0)
                        return await Spells.TwinSnakes.Cast(Core.Me.CurrentTarget);
                    else
                        return await Spells.TrueStrike.Cast(Core.Me.CurrentTarget);
                }

                if (ActionResourceManager.Monk.MasterGaugeCount == 2)
                {
                    if (ActionResourceManager.Monk.CoeurlFury == 0)
                        return await Spells.Demolish.Cast(Core.Me.CurrentTarget);
                    else
                        return await Spells.SnapPunch.Cast(Core.Me.CurrentTarget);
                }
            }
            else
            {

                if (ActionResourceManager.Monk.OpoOpoFury == 0)
                    return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
                else
                    return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);

            }

            return false;

        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            if (!Core.Me.HasTarget)
                return false;

            return PhysicalDps.ForceLimitBreak(Spells.Braver, Spells.Bladedance, Spells.FinalHeaven, Spells.Bootshine);
        }
    }
}

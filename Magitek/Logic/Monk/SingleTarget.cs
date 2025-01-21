using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Monk;
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

            if (!Core.Me.HasAura(Auras.OpoOpoForm) && !Core.Me.HasAura(Auras.FormlessFist) && Core.Me.ClassLevel >= Spells.FormShift.LevelAcquired)
                return false;

            if (Core.Me.ClassLevel >= Spells.DragonKick.LevelAcquired && ActionResourceManager.Monk.OpoOpoFury == 0)
                return false;

            return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TrueStrike()
        {
            if (Core.Me.ClassLevel < Spells.TrueStrike.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.RaptorForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (Core.Me.ClassLevel >= Spells.TwinSnakes.LevelAcquired && ActionResourceManager.Monk.RaptorFury == 0)
                return false;

            return await Spells.TrueStrike.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SnapPunch()
        {
            if (Core.Me.ClassLevel < Spells.SnapPunch.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.CoeurlForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (Core.Me.ClassLevel >= Spells.Demolish.LevelAcquired && ActionResourceManager.Monk.CoeurlFury == 0)
                return false;

            return await Spells.SnapPunch.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DragonKick()
        {
            if (Core.Me.ClassLevel < Spells.DragonKick.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.OpoOpoForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.OpoOpoFury == 1)
                return false;

            return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TwinSnakes()
        {
            if (Core.Me.ClassLevel < Spells.TwinSnakes.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.RaptorForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.RaptorFury == 1)
                return false;

            return await Spells.TwinSnakes.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> Demolish()
        {

            if (Core.Me.ClassLevel < Spells.Demolish.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.CoeurlForm) && !Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (ActionResourceManager.Monk.CoeurlFury >= 1)
                return false;

            return await Spells.Demolish.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> TheForbiddenChakra()
        {
            if (Core.Me.ClassLevel < Spells.SteelPeak.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UseTheForbiddenChakra)
                return false;

            if (ActionResourceManager.Monk.ChakraCount < 5)
                return false;

            return await Spells.SteelPeak.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PerfectBalance()
        {
            if (Core.Me.ClassLevel < Spells.PerfectBalance.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UsePerfectBalance)
                return false;

            if (!Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            if (!ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Both) && ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Lunar))
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
                        return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
                    else
                        return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
                }

                if (ActionResourceManager.Monk.MasterGaugeCount == 2)
                {
                    if (ActionResourceManager.Monk.CoeurlFury == 0)
                        return await Spells.DragonKick.Cast(Core.Me.CurrentTarget);
                    else
                        return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);
                }

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

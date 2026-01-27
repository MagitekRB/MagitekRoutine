using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Monk;
using Magitek.Toggles;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Monk;
using MonkRoutine = Magitek.Utilities.Routines.Monk;

namespace Magitek.Logic.Monk
{
    internal static class Aoe
    {

        public static async Task<bool> Enlightenment()
        {
            if (!Spells.Enlightenment.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseEnlightenment)
                return false;

            if (!MonkSettings.Instance.UseAoe)
                return false;

            if (MonkRoutine.EnemiesInCone < MonkSettings.Instance.AoeEnemies)
                return false;

            if (ActionResourceManager.Monk.ChakraCount < 5)
                return false;

            if (!MonkSettings.Instance.BurstLogicHoldBurst && Spells.RiddleofFire.IsKnownAndReady())
                return false;

            return await Spells.HowlingFist.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MasterfulBlitz()
        {
            if (!Spells.MasterfulBlitz.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseMasterfulBlitz)
                return false;

            if (!Spells.MasterfulBlitz.IsKnownAndReadyAndCastable())
                return false;

            if (ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Lunar) && MonkSettings.Instance.DoubleLunar)
            {
                MonkSettings.Instance.DoubleLunar = false;
                TogglesManager.ResetToggles();
            }

            return await Spells.MasterfulBlitz.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> PerfectBalance()
        {
            if (!Spells.PerfectBalance.IsKnown())
                return false;

            if (!MonkSettings.Instance.UsePerfectBalance)
                return false;

            if (!Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            if (Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < MonkSettings.Instance.AoeEnemies)
                return false;

            if (!ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Both) && ActionResourceManager.Monk.ActiveNadi.HasFlag(Nadi.Lunar))
            {
                if (ActionResourceManager.Monk.MasterGaugeCount == 0)
                {
                    return await Spells.ArmOfTheDestroyer.Cast(Core.Me.CurrentTarget);
                }

                if (ActionResourceManager.Monk.MasterGaugeCount == 1)
                {
                    return await Spells.FourPointFury.Cast(Core.Me.CurrentTarget);
                }

                if (ActionResourceManager.Monk.MasterGaugeCount == 2)
                {
                    return await Spells.Rockbreaker.Cast(Core.Me.CurrentTarget);
                }
            }
            else
            {

                return await Spells.Rockbreaker.Cast(Core.Me.CurrentTarget);

            }

            return false;

        }

        public static async Task<bool> WindReply()
        {
            if (!Spells.WindReply.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseWindReply)
                return false;

            if (!Core.Me.HasAura(Auras.WindRumination, true))
                return false;

            if (!Spells.WindReply.IsKnownAndReady())
                return false;

            return await Spells.WindReply.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> FireReply()
        {
            if (!Spells.FireReply.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseFireReply)
                return false;

            if (!Core.Me.HasAura(Auras.FireRumination, true))
                return false;

            if (!Spells.FireReply.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            return await Spells.FireReply.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> Rockbreaker()
        {
            if (!Spells.Rockbreaker.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseAoe)
                return false;

            if (MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies)
                return false;

            if (!Core.Me.HasAura(Auras.CoeurlForm) && !Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            return await Spells.Rockbreaker.Cast(Core.Me);
        }

        public static async Task<bool> FourPointStrike()
        {
            if (!Spells.FourPointFury.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseAoe)
                return false;

            if (MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies)
                return false;

            if (!Core.Me.HasAura(Auras.RaptorForm) && !Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            return await Spells.FourPointFury.Cast(Core.Me);
        }

        public static async Task<bool> ArmOfDestroyer()
        {
            if (!Spells.ArmOfTheDestroyer.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseAoe)
                return false;

            if (MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies)
                return false;

            return await Spells.ArmOfTheDestroyer.Cast(Core.Me);
        }
    }
}

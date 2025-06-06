using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Monk;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using MonkRoutine = Magitek.Utilities.Routines.Monk;

namespace Magitek.Logic.Monk
{
    internal static class Buff
    {
        public static async Task<bool> TrueNorth()
        {
            if (MonkSettings.Instance.EnemyIsOmni || !MonkSettings.Instance.UseTrueNorth)
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 10 + x.CombatReach) >= MonkSettings.Instance.AoeEnemies)
                return false;

            if (Core.Me.HasAura(Auras.TrueNorth))
                return false;

            if (Core.Me.CurrentTarget.IsBehind)
                return false;

            if (Spells.Bootshine.Cooldown.TotalMilliseconds > Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset + 100)
                return false;

            if (Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury == 0 && !Core.Me.HasAura(Auras.PerfectBalance))
            {
                if (Core.Me.CurrentTarget.IsBehind)
                    return false;

                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);
            }

            if (Core.Me.ClassLevel >= Spells.PouncingCoeurl.LevelAcquired && Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury >= 0 && !Core.Me.HasAura(Auras.PerfectBalance))
            {
                if (Core.Me.CurrentTarget.IsFlanking)
                    return false;

                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);
            }

            if (Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury >= 1 && !Core.Me.HasAura(Auras.PerfectBalance))
            {
                if (Core.Me.CurrentTarget.IsFlanking)
                    return false;

                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);
            }

            return false;
        }

        public static async Task<bool> Meditate()
        {
            if (Core.Me.ClassLevel < 15)
                return false;

            if (!MonkSettings.Instance.UseAutoMeditate)
                return false;

            if (!Core.Me.IsAlive)
                return false;

            if (!Core.Me.InCombat && ActionResourceManager.Monk.ChakraCount < 5)
                return await Spells.SteeledMeditation.Masked().Cast(Core.Me);

            if (!Core.Me.HasTarget && ActionResourceManager.Monk.ChakraCount < 5)
                return await Spells.SteeledMeditation.Masked().Cast(Core.Me);

            return false;
        }

        public static async Task<bool> PerfectBalance()
        {
            if (Core.Me.ClassLevel < Spells.PerfectBalance.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UsePerfectBalance || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (MonkSettings.Instance.UsePerfectBalanceOnlyAfterOpo && !Core.Me.HasAura(Auras.RaptorForm))
                return false;

            if (Core.Me.ClassLevel >= Spells.Brotherhood.LevelAcquired)
            {
                if (Core.Me.HasAura(Auras.Brotherhood, true))
                    return await Spells.PerfectBalance.Cast(Core.Me);
            }

            if (Core.Me.ClassLevel >= Spells.RiddleofFire.LevelAcquired)
            {
                if (!Spells.RiddleofFire.IsKnownAndReady())
                    return false;
            }

            return await Spells.PerfectBalance.Cast(Core.Me);
        }

        public static async Task<bool> RiddleOfEarth()
        {
            if (Core.Me.ClassLevel < Spells.RiddleofEarth.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UseRiddleOfEarth)
                return false;

            return await Spells.RiddleofEarth.Cast(Core.Me);

        }

        public static async Task<bool> RiddleOfFire()
        {
            if (Core.Me.ClassLevel < Spells.RiddleofFire.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UseRiddleOfFire || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (Spells.PerfectBalance.IsKnownAndReady() && !Core.Me.HasAura(Auras.PerfectBalance, true))
                return false;

            return await Spells.RiddleofFire.Cast(Core.Me);
        }

        public static async Task<bool> RiddleOfWind()
        {
            if (Core.Me.ClassLevel < Spells.RiddleofWind.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UseRiddleOfWind)
                return false;

            if (!MonkSettings.Instance.BurstLogicHoldBurst && Spells.RiddleofFire.IsKnownAndReady())
                return false;

            if (MonkSettings.Instance.BurstLogicDelayWind && Spells.RiddleofFire.IsKnownAndReady(4000))
                return false;

            return await Spells.RiddleofWind.Cast(Core.Me);
        }

        public static async Task<bool> Brotherhood()
        {
            if (Core.Me.ClassLevel < Spells.Brotherhood.LevelAcquired)
                return false;

            if (!MonkSettings.Instance.UseBrotherhood || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            //if (Spells.PerfectBalance.IsKnownAndReady() && !Core.Me.HasAura(Auras.PerfectBalance, true))
            //    return false;

            return await Spells.Brotherhood.Cast(Core.Me);
        }

        public static async Task<bool> Mantra()
        {
            if (Core.Me.ClassLevel < Spells.Mantra.LevelAcquired)
                return false;

            if (CustomOpenerLogic.InOpener)
                return false;

            if (!MonkSettings.Instance.UseMantra)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!ActionManager.CanCast(Spells.Mantra.Id, Core.Me))
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.CurrentHealthPercent <= MonkSettings.Instance.MantraHealthPercent) < MonkSettings.Instance.MantraAllies)
                return false;

            return await Spells.Mantra.Cast(Core.Me);
        }

        public static async Task<bool> EarthReply()
        {
            if (Core.Me.ClassLevel < Spells.EarthReply.LevelAcquired)
                return false;

            if (CustomOpenerLogic.InOpener)
                return false;

            if (!MonkSettings.Instance.UseEarthReply)
                return false;

            if (!Core.Me.HasAura(Auras.EarthRumination, true))
                return false;

            if (!ActionManager.CanCast(Spells.EarthReply.Id, Core.Me))
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.CurrentHealthPercent <= MonkSettings.Instance.EarthReplyHealthPercent) < MonkSettings.Instance.EarthReplyAllies)
                return false;

            return await Spells.EarthReply.Cast(Core.Me);
        }


        public static async Task<bool> FormShiftIC()
        {
            if (Core.Me.ClassLevel < Spells.FormShift.LevelAcquired)
                return await Spells.Bootshine.Cast(Core.Me.CurrentTarget);

            if (!Spells.FormShift.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            if (Core.Me.HasAura(Auras.FormlessFist))
                return false;

            if (Core.Me.HasAura(Auras.OpoOpoForm))
                return false;

            if (Core.Me.HasAura(Auras.RaptorForm))
                return false;

            if (Core.Me.HasAura(Auras.CoeurlForm))
                return false;

            return await Spells.FormShift.Cast(Core.Me);
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.Brotherhood.IsKnown() && !Spells.Brotherhood.IsReady(11000))
                return false;

            return await PhysicalDps.UsePotion(MonkSettings.Instance);
        }
    }
}

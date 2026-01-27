using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Monk;
using Magitek.Utilities;
using System;
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

            if (Spells.PouncingCoeurl.IsKnown() && Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury >= 0 && !Core.Me.HasAura(Auras.PerfectBalance))
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
            if (!Spells.SteeledMeditation.IsKnown())
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
            if (!Spells.PerfectBalance.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.PerfectBalance))
                return false;

            if (!MonkSettings.Instance.UsePerfectBalance || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (MonkSettings.Instance.UsePerfectBalanceOnlyAfterOpo && !Core.Me.HasAura(Auras.RaptorForm))
                return false;

            if (Spells.RiddleofFire.IsKnownAndReady() && Spells.Brotherhood.IsKnownAndReady())
                return await Spells.PerfectBalance.Cast(Core.Me);

            if (Spells.RiddleofFire.IsKnown())
            {
                if (!Core.Me.HasAura(Auras.RiddleOfFire))
                    return false;
            }

            if (Spells.Brotherhood.IsKnown())
            {
                if (Spells.Brotherhood.Cooldown <= new TimeSpan(0, 0, 50))
                    return false;
            }

            return await Spells.PerfectBalance.Cast(Core.Me);
        }

        public static async Task<bool> RiddleOfEarth()
        {
            if (!Spells.RiddleofEarth.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseRiddleOfEarth)
                return false;

            return await Spells.RiddleofEarth.Cast(Core.Me);

        }

        public static async Task<bool> RiddleOfFire()
        {
            if (!Spells.RiddleofFire.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseRiddleOfFire || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (Spells.Brotherhood.IsKnownAndReady())
                return false;

            return await Spells.RiddleofFire.Cast(Core.Me);
        }

        public static async Task<bool> RiddleOfWind()
        {
            if (!Spells.RiddleofWind.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseRiddleOfWind)
                return false;

            if (!MonkSettings.Instance.BurstLogicHoldBurst && Spells.RiddleofFire.IsKnownAndReady())
                return false;

            if (MonkSettings.Instance.BurstLogicDelayWind && Spells.RiddleofFire.IsKnownAndReady(4000))
                return false;

            if (Spells.Brotherhood.IsKnownAndReady())
                return false;

            return await Spells.RiddleofWind.Cast(Core.Me);
        }

        public static async Task<bool> Brotherhood()
        {
            if (!Spells.Brotherhood.IsKnown())
                return false;

            if (!MonkSettings.Instance.UseBrotherhood || MonkSettings.Instance.BurstLogicHoldBurst)
                return false;

            //if (Spells.PerfectBalance.IsKnownAndReady() && !Core.Me.HasAura(Auras.PerfectBalance, true))
            //    return false;

            if (Combat.CombatTime.ElapsedMilliseconds < (MonkSettings.Instance.UseBrotherhoodInitialDelay * 1000)  - 770)
                return false;

            return await Spells.Brotherhood.Cast(Core.Me);
        }

        public static async Task<bool> Mantra()
        {
            if (!Spells.Mantra.IsKnown())
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
            if (!Spells.EarthReply.IsKnown())
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
            if (!Spells.FormShift.IsKnown())
                return false;

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

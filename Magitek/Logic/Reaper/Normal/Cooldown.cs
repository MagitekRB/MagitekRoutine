using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Reaper
{
    internal static class Cooldown
    {

        /// <summary>
        /// Every gate Gluttony has except its own cooldown. Blood Stalk and Grim Swathe defer to Gluttony only
        /// while this is true: deferring to a ready Gluttony that its other gates refuse (Shroud above 80, no
        /// Death's Design, a dying target) parked the Soul gauge at 100 with nothing spending it.
        /// </summary>
        public static bool GluttonyWanted()
        {
            if (!Spells.Gluttony.IsKnown())
                return false;
            if (!ReaperSettings.Instance.UseGluttony) return false;
            if (Core.Me.HasAura(Auras.SoulReaver)) return false;
            if (Core.Me.HasAura(Auras.Executioner)) return false;
            if (!Core.Me.CurrentTarget.HasAura(Auras.DeathsDesign, true)) return false;
            if (ActionResourceManager.Reaper.ShroudGauge > 80)
                return false;
            if (Utilities.Routines.Reaper.CheckTTDIsEnemyDyingSoon())
                return false;
            return true;
        }

        public static async Task<bool> Gluttony()
        {
            // Any weave slot: the guides use it the moment it is ready. It waited for the late slot only, which
            // measured a median of one GCD late and up to four.
            if (!GluttonyWanted())
                return false;

            // wait for Executioner aura on self to prevent canceling it with another action
            if (Spells.ExecutionersGibbet.IsKnown())
                return await Spells.Gluttony.CastAura(Core.Me.CurrentTarget, Auras.Executioner, auraTarget: Core.Me);
            else
                return await Spells.Gluttony.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Enshroud()
        {
            //Add level check so it doesn't hang here
            if (!Spells.Enshroud.IsKnown())
                return false;
            if (Core.Me.HasAura(Auras.SoulReaver))
                return false;
            if (Core.Me.HasAura(Auras.Executioner)) return false;
            if (Core.Me.HasAura(Auras.PerfectioParata)) return false;
            if (!ReaperSettings.Instance.UseEnshroud) return false;
            if (ActionResourceManager.Reaper.ShroudGauge < 50 && !Core.Me.HasAura(Auras.IdealHost)) return false;
            if (!Core.Me.CurrentTarget.HasAura(Auras.DeathsDesign, true)) return false;

            var idealHost = Core.Me.HasAura(Auras.IdealHost);

            // The Ideal Host shroud carries Communio and, with Perfectio Occulta up, Perfectio: 2,400 potency on
            // anything that lives eight seconds. On trash the dying-enemy gate threw that away (8 of 21 harvests
            // in one Occult Crescent session ended without a Perfectio).
            if (Utilities.Routines.Reaper.CheckTTDIsEnemyDyingSoon() && !(idealHost && Core.Me.HasAura(Auras.PerfectioOcculta)))
                return false;

            // Only my own Arcane Circle counts: another Reaper's buff landing here would lift the bank and open an
            // odd shroud a few seconds before my own buff.
            if (Utilities.Routines.Reaper.DoubleEnshroudActive && !idealHost
                && !Utilities.Routines.Reaper.ArcaneCircleImminent && !Core.Me.HasAura(Auras.ArcaneCircle, true))
            {
                // Odd-minute shrouds: none inside the banking window before Arcane Circle, and none with Gluttony
                // ready or under thirteen seconds away - Gluttony goes first. Both rules belong to the two-minute
                // structure and switch off with it.
                if (Utilities.Routines.Reaper.BankingShroud)
                    return false;
                if (GluttonyWanted() && Spells.Gluttony.Cooldown.TotalMilliseconds <= Utilities.Routines.Reaper.GluttonyHoldMs)
                    return false;
            }

            return await Spells.Enshroud.Cast(Core.Me);
        }

        public static async Task<bool> ArcaneCircle()
        {
            if (!ReaperSettings.Instance.UseArcaneCircle || !Spells.ArcaneCircle.IsKnown())
                return false;

            // Prevent blowing arcane circle before reaching target.
            if (Utilities.Routines.Reaper.EnemiesAroundPlayer5Yards < 1)
                return false;

            if (Core.Me.HasAura(Auras.ArcaneCircle))
                return false;

            if (Utilities.Routines.Reaper.CheckTTDIsEnemyDyingSoon())
                return false;

            if (Globals.InParty)
            {
                var couldArcane = Group.CastableAlliesWithin30.Count(r => !r.HasAura(Auras.ArcaneCircle));
                var arcaneNeededCount = ReaperSettings.Instance.ArcaneCircleCount;

                if (ReaperSettings.Instance.ArcaneCircleEntireParty)
                    arcaneNeededCount = Group.CastableParty.Count();

                if (couldArcane >= arcaneNeededCount)
                    return await Spells.ArcaneCircle.Cast(Core.Me);
                else
                    return false;
            }

            return await Spells.ArcaneCircle.Cast(Core.Me);
        }

    }
}
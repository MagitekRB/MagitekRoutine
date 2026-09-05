using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Ninja;
using Magitek.Models.OccultCrescent;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;

namespace Magitek.Logic.Ninja
{
    internal static class Cooldown
    {
        public static async Task<bool> Mug()
        {
            if (!Spells.Mug.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseMug || NinjaSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Spells.Mug.IsKnownAndReady())
                return false;

            // Don't use regular Mug in Occult Crescent content if Dokumori is enabled for gold farming
            // Only disable for multi-target scenarios (2+ enemies) - single target should use normal rotation
            if (Core.Me.OnOccultCrescent() && OccultCrescentSettings.Instance.UseDokumori)
            {
                var nearbyEnemies = Combat.Enemies.Count();
                if (nearbyEnemies >= 2 || !OccultCrescentSettings.Instance.DokumoriOnlyMultipleTargets)
                    return false;
            }

            // Opener alignment only: on a countdown pull Dokumori goes out on the second GCD. Any other
            // pull uses it as soon as it is up.
            if (NinjaRoutine.CountdownPull && Combat.CombatTime.ElapsedMilliseconds < Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds * NinjaRoutine.OpenerBurstAfterGCD - 770)
                return false;

            if (ActionResourceManager.Ninja.NinkiGauge + 40 > 100)
                return false;

            if (!CanMug(Core.Me.CurrentTarget))
                return false;

            return await Spells.Mug.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> TrickAttack()
        {
            if (!Spells.TrickAttack.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseTrickAttack || NinjaSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Spells.TrickAttack.IsKnownAndReady())
                return false;

            // Kunai's Bane goes out on cooldown. Dokumori and Bunshin sit above it in the weave list, so
            // when they are ready together they still land first; they no longer hold it when they are not.
            if (Spells.SpinningEdge.Cooldown.TotalMilliseconds >= 800)
                return false;

            // Opener alignment only: on a countdown pull Kunai's Bane is the late weave after the fourth GCD.
            if (NinjaRoutine.CountdownPull && Combat.CombatTime.ElapsedMilliseconds < Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds * (NinjaRoutine.OpenerBurstAfterGCD * 2) - 770)
                return false;

            if (!KunaisBaneWanted(Core.Me.CurrentTarget))
                return false;

            return await Spells.TrickAttack.Cast(Core.Me.CurrentTarget);
        }

        public static bool CanMug(GameObject unit)
        {
            return unit.CombatTimeLeft() >= NinjaSettings.Instance.DontMugIfEnemyDyingWithinSeconds;
        }

        public static bool CanTrickAttack(GameObject unit)
        {
            return unit.CombatTimeLeft() >= NinjaSettings.Instance.DontTrickAttackIfEnemyDyingWithinSeconds;
        }

        // Kassatsu is popped this far ahead of Kunai's Bane so the Kassatsu ninjutsu is the first GCD inside
        // the window; its buff lasts 15 s against Shadow Walker's 20 s. User setting, default five seconds.
        public static int KassatsuLeadInMs => NinjaSettings.Instance.KassatsuSecondsBeforeTrickAttack * 1000;

        // The Kassatsu ninjutsu stops waiting for Kunai's Bane once the buff has this little left.
        private const int KassatsuNinjutsuHoldFloorMs = 4000;

        // Game constants: Kassatsu's recast and the duration of its buff. The remaining buff time is derived
        // from the recast because a freshly applied aura reports zero time left on its first samples, which
        // made an aura-time check release the hold one second after the press.
        private const int KassatsuRecastMs = 60000;
        private const int KassatsuBuffMs = 15000;

        private static double KassatsuBuffLeftMs =>
            KassatsuBuffMs - (KassatsuRecastMs - Spells.Kassatsu.Cooldown.TotalMilliseconds);

        /// <summary>
        /// Kunai's Bane is enabled and this target is worth it. Suiton and Huton mirror this so a Shadow
        /// Walker is never built for a Kunai's Bane that will not be pressed.
        /// </summary>
        public static bool KunaisBaneWanted(GameObject unit)
        {
            if (!Spells.TrickAttack.IsKnown() || !NinjaSettings.Instance.UseTrickAttack || NinjaSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (unit == null)
                return false;

            // The time-to-die decision is taken once, when the Suiton is built. With Shadow Walker already up
            // the charge is spent, so Kunai's Bane goes out whatever the estimate says now: the estimate moves
            // every pulse and reads zero for a pulse after every target swap, and re-deciding here released
            // the Kassatsu hold early on most trash windows in a Forked Tower run.
            if (Core.Me.HasMyAura(Auras.ShadowWalker))
                return true;

            return CanTrickAttack(unit);
        }

        /// <summary>
        /// Kassatsu is up and Kunai's Bane is about to land on this target: hold the Kassatsu ninjutsu so it
        /// lands inside the window. Gives up once Kassatsu is nearly gone rather than lose it.
        /// </summary>
        public static bool HoldKassatsuNinjutsuForKunaisBane(GameObject unit)
        {
            if (!KunaisBaneWanted(unit))
                return false;

            if (unit.HasAura(Auras.KunaisBane, true) || unit.HasAura(Auras.TrickAttack, true))
                return false;

            // Without Shadow Walker no Kunai's Bane can arrive inside the hold, so there is nothing to wait for.
            if (!Core.Me.HasMyAura(Auras.ShadowWalker))
                return false;

            if (!Core.Me.HasAura(Auras.Kassatsu) || KassatsuBuffLeftMs < KassatsuNinjutsuHoldFloorMs)
                return false;

            return Spells.TrickAttack.Cooldown.TotalMilliseconds <= KassatsuLeadInMs;
        }

        public static async Task<bool> Assassinate()
        {
            if (!Spells.Assassinate.IsKnown())
                return false;
            if (!NinjaSettings.Instance.UseAssassinate || NinjaSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (!Spells.Assassinate.IsKnownAndReady())
                return false;

            if (Spells.TrickAttack.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Casting.SpellCastHistory.FirstOrDefault()?.Spell == Spells.TrickAttack && Spells.SpinningEdge.Cooldown.TotalMilliseconds < 800)
                return false;

            return await Spells.Assassinate.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ZeshoMeppo()
        {
            if (!Spells.ZeshoMeppo.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseZeshoMeppo)
                return false;

            if (Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 20))
                return false;

            if (Casting.SpellCastHistory.FirstOrDefault()?.Spell == Spells.TrickAttack)
                return false;

            if (NinjaRoutine.AoeEnemies6Yards >= Aoe.NinkiAoeEnemies)
                return false;

            return await Spells.ZeshoMeppo.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> TenriJindo()
        {
            if (!Spells.TenriJindo.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseTenriJindo)
                return false;

            if (Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 20))
                return false;

            if (Casting.SpellCastHistory.FirstOrDefault()?.Spell == Spells.TrickAttack)
                return false;

            return await Spells.TenriJindo.Cast(Core.Me.CurrentTarget);

        }
    }
}

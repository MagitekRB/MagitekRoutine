using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Utilities;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Magitek.Logic.Roles
{
    internal class CommonFightLogic
    {
        // While a DPS job reports a burst window (e.g. MCH overheat/Wildfire), fight-logic
        // responses wait it out — an oGCD defensive interjected mid-burst costs more than
        // it saves. Healers always respond, and a tank's mitigation outranks its burst
        // window, so only DPS hold. Deliberately not applied to FightLogic_TankDefensive
        // (tanks never hold) or FightLogic_Doom (a doom response is life-or-death).
        private static bool HoldForBurstWindow([CallerMemberName] string responder = null)
        {
            if (!BaseSettings.Instance.FightLogicRespectBurstWindows)
                return false;

            if (!Core.Me.IsDps())
                return false;

            if (!RoutineState.InBurstWindow)
                return false;

            // Without this a held response reads exactly like one that never detected.
            if (BaseSettings.Instance.DebugFightLogic)
                FightLogic.LogThrottled(
                    $"[FightLogic] Holding {responder} for burst window, {RoutineState.BurstWindowRemaining.TotalSeconds:F1}s left",
                    $"hold:{responder}");

            return true;
        }

        public static async Task<bool> FightLogic_TankDefensive(bool useDefensive, SpellData[] defensiveSpells, uint[] defensiveAuras, int castTimeRemainingMs = 0)
        {
            if (!useDefensive)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyTankbusterLogic())
                return false;

            // Personal defensives only answer a hit that will land on US: a single-target
            // buster aimed at someone else (now matchable since the victim widening) must not
            // burn our cooldowns. A shared buster hits every tank, so it always qualifies.
            var busterVictim = FightLogic.EnemyIsCastingTankBuster();
            if ((busterVictim != null && busterVictim == Core.Me)
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (Core.Me.HasAnyAura(defensiveAuras))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                foreach (var defensiveSpell in defensiveSpells)
                {
                    if (defensiveSpell.IsKnownAndReadyAndCastable(Core.Me))
                    {
                        if (BaseSettings.Instance.DebugFightLogic)
                            FightLogic.LogThrottled($"[TankDefensive Response] Attempting {defensiveSpell.Name}");
                        if (await FightLogic.DoAndBuffer(defensiveSpell.Cast(Core.Me)))
                            return true; // intentionally continue to next defensive in the list. 
                    }
                }
            }
            return false;
        }

        public static async Task<bool> FightLogic_SelfShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (HoldForBurstWindow())
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                // Now check if spell is ready before attempting to cast
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[SelfShield Response] Attempting {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_PartyShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint[] auras = null, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (HoldForBurstWindow())
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                // Now check if spell is ready before attempting to cast
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && auras != null && Core.Me.HasAnyAura(auras))
                    return false;

                if (selfAuraCheck && aura != 0 && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[PartyShield Response] Attempting {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_Debuff(bool useDebuff, SpellData spell, bool targetAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0, float range = 0f)
        {
            if (!useDebuff)
                return false;

            if (HoldForBurstWindow())
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe()
                || FightLogic.EnemyIsCastingBigAoe()
                || FightLogic.EnemyIsCastingTankBuster() != null
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (!spell.IsKnownAndReady())
                    return false;

                // Debuff the enemy we are REACTING to, not whatever we happen to be hitting — they are
                // frequently different, and Feint or Addle landing on the wrong one does nothing about the
                // mechanic. Reprisal is self-centred, so measuring range to the caster is right there too:
                // if the caster is out of its radius, Reprisal genuinely cannot mitigate this cast.
                // Falls back to the current target when nothing is mid-cast (lock-on reactions have no caster).
                var debuffTarget = (GameObject)FightLogic.DetectedCaster() ?? Core.Me.CurrentTarget;

                if (debuffTarget == null)
                    return false;

                // For range-based debuffs (e.g., Reprisal), check the caster is within range
                if (range > 0f && !debuffTarget.WithinSpellRange(range))
                    return false;

                // For target-based debuffs, check target aura
                if (targetAuraCheck && debuffTarget.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[Debuff Response] Attempting {spell.Name} on {debuffTarget.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(debuffTarget));
            }

            return false;
        }

        public static async Task<bool> FightLogic_Knockback(bool useAntiKnockback, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 3000)
        {
            if (!useAntiKnockback)
                return false;

            if (HoldForBurstWindow())
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyKnockbackLogic())
                return false;

            if (FightLogic.EnemyIsCastingKnockback())
            {
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[AntiKnockback Response] Attempting {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        /// <summary>
        /// Tops off an ally carrying a heal-to-full Doom. This Doom kills at expiry unless the
        /// target reaches FULL HP first and Esuna does not touch it, so it has to be answered
        /// ahead of discretionary healing rather than inside it - a carrier with seconds left
        /// loses to any other wounded ally if this waits its turn in the priority list.
        /// The heal is cast with health checks off: the carrier is usually near full, which is
        /// exactly where each job's heal-interrupt threshold would otherwise cancel the cast.
        /// </summary>
        public static async Task<bool> FightLogic_Doom(bool useHeal, SpellData heal)
        {
            if (!useHeal)
                return false;

            if (!heal.IsKnownAndReady())
                return false;

            // A heal with a cast bar fails instantly while moving, and this check runs ahead of
            // discretionary healing on every pulse — without this guard a Doom carried while
            // moving re-fires the failed cast every pulse (~25/s, seen live) until movement
            // stops. Swiftcast and Dualcast both make the cast instant, so both are exempt —
            // the same pair the shared cast gate whitelists. Dualcast is not RedMage-only:
            // Occult Crescent's Phantom Red Mage grants the same status on any job, so a
            // Scholar in the field ops carries it through most of a fight (observed in game).
            if (ff14bot.Managers.MovementManager.IsMoving
                && heal.AdjustedCastTime > System.TimeSpan.Zero
                && !Core.Me.HasAura(Utilities.Auras.Swiftcast)
                && !Core.Me.HasAura(Utilities.Auras.Dualcast))
                return false;

            // Skips a carrier whose last Doom heal landed less than a GCD ago - the aura outlives
            // the cast by a server round trip, so without that the next GCD answers it again.
            var doomed = FightLogic.DoomedHealTarget(heal);

            if (doomed == null)
                return false;

            if (BaseSettings.Instance.DebugFightLogic)
                FightLogic.LogThrottled($"[Doom Response] Attempting {heal.Name} on {doomed.CurrentJob}");

            return await heal.Heal(doomed, false);
        }
    }
}

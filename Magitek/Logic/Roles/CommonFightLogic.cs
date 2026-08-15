using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Roles
{
    internal class CommonFightLogic
    {
        public static async Task<bool> FightLogic_TankDefensive(bool useDefensive, SpellData[] defensiveSpells, uint[] defensiveAuras, int castTimeRemainingMs = 0)
        {
            if (!useDefensive)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyTankbusterLogic())
                return false;

            if (FightLogic.EnemyIsCastingTankBuster() != null
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
                            Logger.WriteInfo($"[TankDefensive Response] Cast {defensiveSpell.Name}");
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
                    Logger.WriteInfo($"[SelfShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_PartyShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint[] auras = null, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
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
                    Logger.WriteInfo($"[PartyShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_Debuff(bool useDebuff, SpellData spell, bool targetAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0, float range = 0f)
        {
            if (!useDebuff)
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
                    Logger.WriteInfo($"[Debuff Response] Cast {spell.Name} on {debuffTarget.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(debuffTarget));
            }

            return false;
        }

        public static async Task<bool> FightLogic_Knockback(bool useAntiKnockback, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 3000)
        {
            if (!useAntiKnockback)
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
                    Logger.WriteInfo($"[AntiKnockback Response] Cast {spell.Name}");

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

            var doomed = FightLogic.DoomedHealTarget();

            if (doomed == null)
                return false;

            if (BaseSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[Doom Response] Cast {heal.Name} on {doomed.Name}");

            return await heal.Heal(doomed, false);
        }
    }
}

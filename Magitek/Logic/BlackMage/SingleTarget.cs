using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;
using Auras = Magitek.Utilities.Auras;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            // 2-Target optimization: Foul is a mathematical DPS gain over Xenoglossy on 2+ targets
            if (Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= 2 && Spells.Foul.IsKnown() && PolyglotStatus)
                return await Aoe.Foul();

            // Fallback: If Xenoglossy isn't unlocked yet, default to Foul if we have Polyglot
            if (!Spells.Xenoglossy.IsKnown())
            {
                if (Spells.Foul.IsKnown() && PolyglotStatus)
                    return await Aoe.Foul();
                return false;
            }

            // Respect user setting
            if (!BlackMageSettings.Instance.Xenoglossy)
                return false;

            // Do not use in neutral stance
            if (AstralStacks == 0 && UmbralStacks == 0)
                return false;

            // Priority: Do not waste a GCD on Xenoglossy if Flare Star is fully stacked and ready to fire
            if (AstralSoulStacks == 6)
                return false;

            // Movement logic: Dump Xenoglossy to maintain uptime while running if we lack instant-cast buffs
            if (MovementManager.IsMoving)
            {
                if (!Core.Me.HasAura(Auras.Swiftcast) && !Core.Me.HasAura(Auras.Triplecast))
                    return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            }

            // Always prevent overcapping Polyglot stacks regardless of user settings
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);

            // Respect the user's saved charges setting before using Xenoglossy as a stationary filler
            if (PolyglotCount <= BlackMageSettings.Instance.SaveXenoglossyCharges)
                return false;

            return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Despair()
        {
            // Spell must be unlocked and user setting enabled
            if (!Spells.Despair.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Despair)
                return false;

            // Skip single-target Despair finisher during AoE encounters
            if (AoeControl.Enabled 
                && BlackMageSettings.Instance.UseAoe 
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // Prevent double-casting
            if (Casting.LastSpellWas(Spells.Despair))
                return false;

            // Allow 0 MP drop if Flare Star is queued right after
            // Wait to cast Despair until Flare Star is available (if known) to ensure proper sequence
            if (AstralSoulStacks == 6 && !Spells.FlareStar.IsKnown())
                return false;

            // Despair is strictly an Astral Fire ability
            if (UmbralStacks > 0)
                return false;

            // Standard API readiness check
            if (!Spells.Despair.IsKnownAndReadyAndCastable())
                return false;

            // Minimum MP requirement: Despair requires at least 800 MP to cast, despite draining all MP
            if (Core.Me.CurrentMana < 800)
                return false;

            // Do not cast Despair if we still have enough MP to fit another Fire 4
            int fire4Cost = UmbralHearts > 0 ? 800 : 1600;
            if (Core.Me.CurrentMana >= fire4Cost + 800)
                return false;

            return await Spells.Despair.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire()
        {
            // Spell must be unlocked
            if (!Spells.Fire.IsKnown())
                return false;

            // When Paradox is known (Lv 90+), Paradox entirely replaces Fire 1 for Astral Fire timer refreshes
            if (Spells.Paradox.IsKnown())
                return false;
                
            // Priority: Do not cast filler if Flare Star is ready
            if (AstralSoulStacks == 6)
                return false;

            // Fire is an Astral Fire / Neutral ability
            if (UmbralStacks > 0)
                return false;

            // Do not cast if we don't have the MP to support it in Astral Fire (costs double MP = 1600)
            if (AstralStacks > 0 && Core.Me.CurrentMana < 1600)
                return false;

            // Do not cast if we don't have the base MP to cast it in Neutral stance
            if (AstralStacks == 0 && Core.Me.CurrentMana < 800)
                return false;

            return await Spells.Fire.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire4()
        {
            // Spell must be unlocked
            if (!Spells.Fire4.IsKnown())
                return false;

            // Fire IV requires full Astral Fire III
            if (AstralStacks != 3)
                return false;

            // Calculate cost (800 with hearts, 1600 without) and verify MP is sufficient
            int cost = UmbralHearts > 0 ? 800 : 1600;
            if (Core.Me.CurrentMana < cost)
                return false;

            return await Spells.Fire4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire3()
        {
            // Spell must be unlocked
            if (!Spells.Fire3.IsKnown())
                return false;

            // Transition: If we lost our stance, cast Fire 3 to instantly jump back to Astral Fire III
            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire3.Cast(Core.Me.CurrentTarget);

            // Prevent double-casting
            if (Casting.LastSpellWas(Spells.Fire3))
                return false;

            // Consume Firestarter proc if we are low on MP
            if (Core.Me.CurrentMana < 2000 && Core.Me.HasAura(Auras.FireStarter))
                return false;

            // Hold Firestarter proc for movement or later weaving if we have plenty of MP
            if (Core.Me.HasAura(Auras.FireStarter) && Core.Me.CurrentMana < 8400)
                return false;

            // Priority: Do not waste a GCD on Fire 3 if Flare Star is ready
            if (AstralSoulStacks == 6)
                return false;

            // Standard rotation: Do not hardcast Fire 3 if we are already in AF3 or haven't maxed Umbral Ice yet
            if (AstralStacks == 3 || UmbralStacks < 3)
                return false;

            // Transition checks: Ensure we secure Umbral Hearts before leaving Umbral Ice
            if (Spells.Blizzard4.IsKnown())
            {
                if (UmbralHearts < 3 && !Casting.LastSpellWas(Spells.Blizzard4))
                    return false;
            }
            // Low level: Wait for MP to tick up before transitioning back to Astral Fire
            else if (Core.Me.CurrentMana < 7200)
            {
                return false;
            }

            return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder3()
        {
            // Base spell must be unlocked and user setting enabled
            if (!Spells.Thunder.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ThunderSingle)
                return false;

            // Require Thunderhead proc to instantly cast and refresh DoT
            if (!Core.Me.HasAura(Auras.Thunderhead))
                return false;

            // Do not use Single-Target Thunder during AoE encounters
            if (AoeControl.Enabled
                && BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // Do not consume a Thunderhead proc if we just popped Triplecast (use the buff for heavy nukes instead)
            if (Casting.LastSpellWas(Spells.Triplecast))
                return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            // Prevent double-casting or refreshing immediately
            if (Casting.LastSpellWas(Spells.Thunder)
                || Casting.LastSpellWas(Spells.Thunder2)
                || Casting.LastSpellWas(Spells.Thunder3)
                || Casting.LastSpellWas(Spells.Thunder4)
                || Casting.LastSpellWas(Spells.HighThunder)
                || Casting.LastSpellWas(Spells.HighThunderII))
                return false;

            // Do not clip Thunder if the target already has the DoT with plenty of time remaining
            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            // Time To Die (TTD) check: Don't cast DoTs on trash mobs that are about to die anyway
            if (BlackMageSettings.Instance.UseTTDForThunderSingle && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderSingleTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
                return false;

            // Spell Upgrade Fallbacks
            if (!Spells.Thunder3.IsKnown())
                return await Spells.Thunder.Cast(Core.Me.CurrentTarget);

            if (!Spells.HighThunder.IsKnown())
                return await Spells.Thunder3.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunder.Cast(Core.Me.CurrentTarget);
        }

        private static readonly uint[] ThunderAuras =
        {
            Auras.Thunder,
            Auras.Thunder2,
            Auras.Thunder3,
            Auras.Thunder4,
            Auras.HighThunder,
            Auras.HighThunder2
        };

        public static async Task<bool> Blizzard4()
        {
            // Spell must be unlocked
            if (!Spells.Blizzard4.IsKnown())
                return false;

            // Blizzard 4 requires max Umbral Ice III to cast
            if (UmbralStacks != 3)
                return false;

            // Do not cast if we already have max Umbral Hearts secured
            if (UmbralHearts == 3)
                return false;

            // Prevent double-casting or sequence breaking
            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;

            if (Casting.LastSpellWas(Spells.Transpose))
                return false;

            return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard3()
        {
            // Spell must be unlocked
            if (!Spells.Blizzard3.IsKnown())
                return false;

            // Prevent double-casting or breaking Manafont timing
            if (Casting.LastSpellWas(Spells.Blizzard3) || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            // Priority: Execute Flare Star before transitioning to Umbral Ice
            if (AstralSoulStacks == 6)
                return false;

            // Do not cast if we are trying to build Astral Fire or if we are already in max Umbral Ice
            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            // Do not use single-target Blizzard 3 in AoE encounters (use Freeze/Blizzard 2 instead)
            if (AoeControl.Enabled
                && BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // Allow Blizzard 3 immediately if our mana drops below 1600 (catching low MP states cleanly for fast cast transition)
            if (Core.Me.CurrentMana >= 1600)
                return false;

            return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard()
        {
            // Do not use base Blizzard if Blizzard 4 is available for Heart generation
            if (Spells.Blizzard4.IsKnown())
                return false;

            // Early game fallback logic when Blizzard 3 is not yet unlocked
            if (!Spells.Blizzard3.IsKnown())
            {
                if (Casting.LastSpellWas(Spells.Transpose) && AstralStacks > 0)
                    return false;

                if (AstralStacks > 0 && Core.Me.CurrentMana < 1600)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                if (UmbralStacks > 0 && Core.Me.CurrentMana < Core.Me.MaxMana)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                if (AstralStacks == 0 && UmbralStacks == 0 && Core.Me.CurrentMana < 1600)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                return false;
            }

            // Prevent double-casting or sequencing errors
            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;

            return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Paradox()
        {
            // Spell must be unlocked and setting enabled
            if (!Spells.Paradox.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Paradox)
                return false;

            // FFXIV MP costs (patch 7.x): Fire base cost 800 MP, doubled to 1600 in Astral Fire.
            // Spells.Fire.Cost is NOT reliably dynamic for AF stance; hardcode known game values.
            // If SQEX changes Fire's MP cost in a future patch, update these constants.
            if (AstralStacks > 0 && Core.Me.CurrentMana < 1600)
                return false;

            // Skip single-target Paradox during AoE encounters
            if (AoeControl.Enabled
                && BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // Prevent sequence breaking immediately after transitions
            if (Casting.LastSpellWas(Spells.Fire3))
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard3))
                return false;
                
            // Dawntrail check: Paradox is strictly available in Astral Fire III and Umbral Ice III
            if (AstralStacks != 3 && UmbralStacks != 3)
                return false;

            // If we are moving, prioritize dropping Paradox as an instant cast
            if (MovementManager.IsMoving)
                return await Spells.Paradox.Cast(Core.Me.CurrentTarget);

            // Maintain Astral Fire sequence: Wait until we have cast 3 Fire IVs (3 Astral Soul stacks) before weaving Paradox
            if (AstralSoulStacks < 3 && Spells.Fire4.IsKnown())
                return false;

            return await Spells.Paradox.Cast(Core.Me.CurrentTarget);
        }
    }
}
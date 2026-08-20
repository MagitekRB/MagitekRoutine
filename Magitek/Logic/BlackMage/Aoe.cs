using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class Aoe
    {
        public static async Task<bool> Foul()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Do not cast if we do not have a Polyglot stack available
            if (!PolyglotStatus)
                return false;

            // Do not cast if Foul is not unlocked yet
            if (!Spells.Foul.IsKnown())
                return false;

            // Priority: Do not waste a GCD on Foul if Flare Star is fully stacked and ready to fire
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // Movement logic: Handle casting while on the run
            if (MovementManager.IsMoving)
            {
                // This is a trait check, not a spell availability check
                // If we have instant cast Foul (level 80+) and have Swiftcast/Triplecast,
                // don't cast Foul - use procs on other spells instead
                if (Core.Me.ClassLevel >= 80 && (Core.Me.HasAura(Auras.Swiftcast) || Core.Me.HasAura(Auras.Triplecast)))
                    return false;

                // If under level 80 (Foul has a cast time) and we lack instant-cast buffs, we cannot cast while moving
                if (Core.Me.ClassLevel < 80 && !Core.Me.HasAura(Auras.Swiftcast) && !Core.Me.HasAura(Auras.Triplecast))
                    return false;

                return await Spells.Foul.Cast(Core.Me.CurrentTarget);
            }

            // Always prevent overcapping Polyglot stacks regardless of user settings
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            // Respect the user's saved charges setting before using Foul as a stationary filler
            if (PolyglotCount <= BlackMageSettings.Instance.SaveXenoglossyCharges)
                return false;

            // Standard use: Use Foul as instant filler in Umbral Ice to safely weave Transpose (only when above saved charge threshold)
            if (UmbralStacks > 0 && UmbralHearts == 3)
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> AoeTranspose()
        {
            // Transpose must be unlocked
            if (!Spells.Transpose.IsKnown())
                return false;

            // Dawntrail AoE Loop: In Umbral Ice, once we secure 3 Umbral Hearts, Transpose back to Astral Fire
            if (UmbralStacks > 0 && UmbralHearts == 3)
            {
                return await Spells.Transpose.Cast(Core.Me);
            }

            // Finisher Transition: In Astral Fire, if MP is depleted (<800), Transpose to Umbral Ice
            if (AstralStacks > 0 && Core.Me.CurrentMana < 800)
            {
                // Safety check: Do not Transpose yet if we still need to execute our Flare Star finisher
                if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                    return false;

                return await Spells.Transpose.Cast(Core.Me);
            }

            return false;
        }
        
        public static async Task<bool> UseAoeEther()
        {
            // Verify user has opted into using Ethers for AoE
            if (!BlackMageSettings.Instance.UseEtherInAoe)
                return false;

            // Only use ethers to extend the Astral Fire phase
            if (AstralStacks == 0)
                return false;

            // Do not pop an ether if we already have enough MP to cast Flare
            if (Core.Me.CurrentMana >= 800)
                return false;

            // Hold ether until after Flare Star is consumed
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // Respect settings: Only block Ether if Manafont is actually permitted to be used
            if (BlackMageSettings.Instance.ManaFont && Spells.ManaFont.IsKnownAndReady())
                return false;

            // Try each ether from best to worst; any granting 800+ MP enables an extra Flare
            foreach (var etherId in BlackMageRoutine.AoeEthers)
            {
                if (await Ether.UseEther((int)etherId))
                    return true;
            }

            return false;
        }

        public static async Task<bool> Flare()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Flare is strictly an Astral Fire ability
            if (UmbralStacks > 0)
                return false;

            // Priority: Execute Flare Star if fully stacked before casting another Flare
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // Spell must be unlocked
            if (!Spells.Flare.IsKnown())
                return false;

            // Must have Astral Fire active or enough MP to initiate it (800 MP is Flare's base cost)
            if (AstralStacks == 0 && Core.Me.CurrentMana < 800)
                return false;

            // If we are below the base MP cost (800) and lack Umbral Hearts to reduce the AF penalty, we cannot cast Flare
            if (Core.Me.CurrentMana < 800 && UmbralHearts == 0)
                return false;

            return await Spells.Flare.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FlareStar()
        {
            // Spell must be unlocked
            if (!Spells.FlareStar.IsKnown())
                return false;

            // Target must be valid and ability ready
            if (!Spells.FlareStar.IsKnownAndReadyAndCastableAtTarget())
                return false;

            // Prevent casting Flare Star immediately after a transition spell to avoid breaking sequences
            if (Casting.LastSpellWas(Spells.Fire3) || Casting.LastSpellWas(Spells.Blizzard3))
                return false;

            return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Freeze()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Spell must be unlocked
            if (!Spells.Freeze.IsKnown())
                return false;

            // Prevent double-casting Freeze
            if (Casting.LastSpellWas(Spells.Freeze))
                return false;

            // Priority: Never execute Freeze if we are holding a Flare Star proc
            if (AstralSoulStacks == 6)
                return false;

            // Freeze is strictly an Umbral Ice ability
            if (UmbralStacks == 0)
                return false;

            // Do not cast Freeze if we already have max Umbral Hearts
            if (UmbralHearts == 3)
                return false;

            return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder4()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Base Thunder II must be unlocked
            if (!Spells.Thunder2.IsKnown())
                return false;

            // Respect user settings for AoE Thunder
            if (!BlackMageSettings.Instance.ThunderAoe)
                return false;

            // Priority: Do not waste a GCD on Thunder if Flare Star is ready
            if (AstralSoulStacks == 6)
                return false;

            // Consume Thunderhead proc immediately in rotation weave slots without waiting for DoT expiration
            bool hasThunderhead = Core.Me.HasAura(Auras.Thunderhead);

            if (!hasThunderhead)
            {
                // Do not clip Thunder if the target already has the DoT with plenty of time remaining
                if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                    return false;
            }

            // Time To Die (TTD) check: Don't cast DoTs on trash mobs that are about to die anyway
            if (BlackMageSettings.Instance.UseTTDForThunderAoe && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderAoeTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
                return false;

            // Spell Upgrade Fallbacks
            if (!Spells.Thunder4.IsKnown())
                return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            if (!Spells.HighThunderII.IsKnown())
                return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunderII.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire2()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Spell must be unlocked
            if (!Spells.Fire2.IsKnown())
                return false;

            // Immediate priority: If we have 3 Umbral Hearts in Umbral Ice, transition back to AF right now.
            if (UmbralStacks > 0 && UmbralHearts == 3)
            {
                if (Spells.HighFireII.IsKnown()) 
                    return await Spells.HighFireII.Cast(Core.Me.CurrentTarget);
                return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
            }

            // Neutral/Dropped Stance Recovery: Cast Fire 2 to enter Astral Fire
            if (AstralStacks < 3 && UmbralStacks == 0)
            {
                if (Spells.HighFireII.IsKnown()) return await Spells.HighFireII.Cast(Core.Me.CurrentTarget);
                return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
            }

            // Priority: Stop casting fillers if Flare Star is fully stacked and ready
            if (AstralSoulStacks == 6)
                return false;

            // In Umbral Ice, do not cast Fire 2 if we still need to secure Umbral Hearts
            if (UmbralStacks == 3 && UmbralHearts != 3)
                return false;

            // Standard Rotation Rule (Lv 58+): Only use Fire 2 during UI -> AF transitions or if we dropped AF3
            if (Core.Me.ClassLevel >= 58)
            {
                if (AstralStacks == 3 || UmbralStacks < 3)
                    return false;
            }

            // Spell Upgrade Fallbacks
            if (Spells.HighFireII.IsKnown()) return await Spells.HighFireII.Cast(Core.Me.CurrentTarget);
            return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard2()
        {
            // Do not cast if AoE is disabled globally
            if (!AoeControl.Enabled)
                return false;

            // Spell must be unlocked
            if (!Spells.Blizzard2.IsKnown())
                return false;

            // Priority: Stop casting fillers if Flare Star is fully stacked and ready
            if (AstralSoulStacks == 6)
                return false;

            // Prevent double-casting or breaking Manafont timing
            if (Casting.LastSpellWas(Spells.Blizzard2)
                || Casting.LastSpellWas(Spells.HighBlizzardII)
                || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            // Do not cast if we are trying to build Astral Fire or if we are already in max Umbral Ice
            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            // Skip low-level filler if we already have the MP to re-enter Astral Fire
            if (Core.Me.CurrentMana >= 1600)
                return false;

            // Spell Upgrade Fallbacks
            if (Spells.HighBlizzardII.IsKnown()) return await Spells.HighBlizzardII.Cast(Core.Me.CurrentTarget);
            return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
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

        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Meteor, Spells.Blizzard);
        }
    }
}
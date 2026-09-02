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
        // The Balance: one filler, in Umbral Ice after Freeze, only while Transpose is still down.
        private static bool AoeFillerAllowed => !Spells.FlareStar.IsKnown()
            || MovementManager.IsMoving
            || (UmbralStacks > 0 && UmbralHearts == 3 && !Spells.Transpose.IsKnownAndReady());

        public static async Task<bool> Foul()
        {
            if (!AoeControl.Enabled)
                return false;

            //requires Polyglot
            if (!PolyglotStatus)
                return false;

            //Can't use whatcha don't have
            if (!Spells.Foul.IsKnown())
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // If we're moving in combat
            if (MovementManager.IsMoving)
            {
                // HARDCODED: Level 80 is when Foul becomes instant cast via trait
                // This is a trait check, not a spell availability check
                // If we have instant cast Foul (level 80+) and have Swiftcast/Triplecast,
                // don't cast Foul - use procs on other spells instead
                if (Core.Me.ClassLevel >= 80 && (Core.Me.HasAura(Auras.Swiftcast) || Core.Me.HasAura(Auras.Triplecast)))
                    return false;

                // HARDCODED: Level 80 is when Foul becomes instant cast via trait
                // This is a trait check, not a spell availability check
                // If below level 80 (Foul has cast time) and we don't have Swiftcast/Triplecast,
                // skip Foul - can't cast it while moving
                if (Core.Me.ClassLevel < 80 && !Core.Me.HasAura(Auras.Swiftcast) && !Core.Me.HasAura(Auras.Triplecast))
                    return false;
            }

            // Always cast if we're about to overcap Polyglot (prevent waste)
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            if (!AoeFillerAllowed)
                return false;

            return await Spells.Foul.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Flare()
        {
            if (!AoeControl.Enabled)
                return false;

            //Can't use in Umbral Ice anymore
            if (UmbralStacks > 0)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (!Spells.Flare.IsKnown())
                return false;

            // if (Core.Me.CurrentMana < 800)
            //     return false;

            // HARDCODED: Level 100 has special Flare behavior (likely trait-based)
            // This is a trait check, not a spell availability check
            if (Core.Me.ClassLevel == 100)
            {
                if (AstralStacks > 0)
                    return await Spells.Flare.Cast(Core.Me.CurrentTarget);
            }

            //No longer worth casting two HighFire2

            //Force flare after manafont
            if (Casting.LastSpellWas(Spells.ManaFont))
                return await Spells.Flare.Cast(Core.Me.CurrentTarget);

            return await Spells.Flare.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FlareStar()
        {
            if (!Spells.FlareStar.IsKnown())
                return false;

            if (!Spells.FlareStar.IsKnownAndReadyAndCastableAtTarget())
                return false;

            if (Casting.LastSpellWas(Spells.Fire3) || Casting.LastSpellWas(Spells.Blizzard3))
                return false;

            return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Freeze()
        {
            if (!AoeControl.Enabled)
                return false;

            //If we don't have Freeze, how can we cast it?
            if (!Spells.Freeze.IsKnown())
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            //Can only use in Umbral Ice - the Transpose loop enters ice at Umbral Ice 1
            if (UmbralStacks == 0)
                return false;

            // The Balance's Lv58+ ice filler slot is Foul/Thunder II/Freeze while Transpose is
            // down. Needs Freeze's 1000 MP plus Flare's 800 floor. Inert below the Umbral Heart trait.
            if (UmbralHearts == 3 && !Spells.Transpose.IsKnownAndReady() && Core.Me.CurrentMana >= 1800)
                return await Spells.Freeze.Cast(Core.Me.CurrentTarget);

            // HARDCODED: Level 58 is when the Umbral Heart trait unlocks
            // This is a trait check, not a spell availability check
            if (Core.Me.ClassLevel >= 58)
            {
                // One Freeze grants full hearts; the guard covers the pulse before the gauge updates
                if (Casting.LastSpellWas(Spells.Freeze))
                    return false;

                // The ice phase is done once Freeze has granted full hearts
                if (UmbralHearts == 3)
                    return false;
            }
            else
            {
                // Below the trait there are no hearts; Freeze chains until the MP refill is done
                if (Core.Me.CurrentMana == Core.Me.MaxMana)
                    return false;
            }

            return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AoeTranspose()
        {
            if (!AoeControl.Enabled)
                return false;

            // The Transpose AoE loop starts at Blizzard II (level 12); Freeze (40) and Umbral
            // Hearts (58) refine the ice phase but the stance cycle itself is the same
            if (!Spells.Blizzard2.IsKnown())
                return false;

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            // Astral Fire -> Umbral Ice: swap once MP can no longer support the fire phase
            if (AstralStacks > 0)
            {
                // Never strand a ready Flare Star
                if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                    return false;

                // FFXIV MP costs (patch 7.x): Flare fires down to its 800 MP minimum; without it
                // the fire spender is Fire II (1500 MP, doubled to 3000 in Astral Fire), and below
                // that base Fire (800 MP, doubled to 1600).
                var fireFloor = Spells.Flare.IsKnown() ? 800 : Spells.Fire2.IsKnown() ? 3000 : 1600;
                if (Core.Me.CurrentMana >= fireFloor)
                    return false;

                return await Spells.Transpose.Cast(Core.Me);
            }

            // Umbral Ice -> Astral Fire: swap once the ice phase has done its job
            if (UmbralStacks > 0)
            {
                // HARDCODED: Level 58 is when the Umbral Heart trait unlocks
                // This is a trait check, not a spell availability check
                if (Core.Me.ClassLevel >= 58)
                {
                    // Freeze granted full hearts and Flare's 800 MP floor is covered
                    if (UmbralHearts == 3 && Core.Me.CurrentMana >= 800)
                        return await Spells.Transpose.Cast(Core.Me);

                    return false;
                }

                // Below the trait there are no hearts; the ice phase ends at full MP
                if (Core.Me.CurrentMana == Core.Me.MaxMana)
                    return await Spells.Transpose.Cast(Core.Me);

                return false;
            }

            return false;
        }

        public static async Task<bool> UseAoeEther()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!BlackMageSettings.Instance.UseEtherInAoe)
                return false;

            // Only use ethers to extend the Astral Fire phase
            if (AstralStacks == 0)
                return false;

            // Don't pop an ether while MP can still pay for Flare
            if (Core.Me.CurrentMana >= 800)
                return false;

            // Hold the ether until a ready Flare Star is consumed
            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            // Manafont restores more than any ether - let it fire first when permitted
            if (BlackMageSettings.Instance.ManaFont && Spells.ManaFont.IsKnownAndReady())
                return false;

            // Best to worst; HQ (raw id + 1,000,000) ahead of NQ within each tier.
            // Any of these restores enough MP for at least one more Flare.
            foreach (var etherId in new[] {
                1023168, 23168, // Super-Ether
                1013638, 13638, // Max-Ether
                1004558, 4558,  // X-Ether
                1004557, 4557,  // Mega-Ether
                1004556, 4556   // Hi-Ether
            })
            {
                if (await Ether.UseEther(etherId))
                    return true;
            }

            return false;
        }

        public static async Task<bool> Thunder4()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Thunder2.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ThunderAoe)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            // If the last spell we cast is triple cast, stop
            if (Casting.LastSpellWas(Spells.Triplecast))
                return false;

            // If we have the triplecast aura, stop
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            if (!AoeFillerAllowed)
                return false;

            // Last-resort ice filler: refresh early only when Polyglot is empty and MP won't cover Freeze.
            if (Spells.FlareStar.IsKnown() && UmbralStacks > 0 && PolyglotCount == 0 && Core.Me.CurrentMana < 1800)
                return await Spells.HighThunderII.Cast(Core.Me.CurrentTarget);

            //If we don't need to refresh Thunder, skip
            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (BlackMageSettings.Instance.UseTTDForThunderAoe && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderAoeTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
                return false;

            if (!Spells.Thunder4.IsKnown())
                return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            if (!Spells.HighThunderII.IsKnown())
                return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunderII.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire2()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Fire2.IsKnown())
                return false;

            // In Astral Fire with Flare known the AoE fire phase belongs to Flare - don't hardcast
            // Fire II over it. Fire II stays the Umbral Ice exit hardcast and the sub-50 fire spender.
            if (AstralStacks > 0 && Spells.Flare.IsKnown())
                return false;

            //No stack, open with Fire3
            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire2.Cast(Core.Me.CurrentTarget);

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            // In Umbral Ice, Fire II is only the exit cast back into fire, and only once the ice
            // phase is done - never mid-refill, which flips the stance with no MP restored
            if (UmbralStacks > 0)
            {
                // HARDCODED: Level 58 is when the Umbral Heart trait unlocks
                // This is a trait check, not a spell availability check
                if (Core.Me.ClassLevel >= 58)
                {
                    // The ice phase is done once Freeze has granted full hearts
                    if (UmbralHearts != 3)
                        return false;
                }
                else
                {
                    // Below the trait the ice phase ends at full MP (Transpose exits first when ready)
                    if (Core.Me.CurrentMana < Core.Me.MaxMana)
                        return false;
                }
            }

            //Try and keep from doublecasting or using after manafont
            // if (Casting.LastSpell == Spells.Fire2
            //     || Casting.LastSpell == Spells.HighFireII
            //     || Casting.LastSpell == Spells.ManaFont)
            //     return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            // HARDCODED: Level 58 is when Umbral Hearts trait unlocks (allows two Flares)
            // This is a trait check, not a spell availability check
            // At level 58+, Umbral Hearts allow two Flares, so Fire2 is less valuable
            // Below level 58, we need Fire2 in Astral Fire (after Transpose from Umbral Ice)
            if (Core.Me.ClassLevel >= 58)
            {
                if (AstralStacks == 3 || UmbralStacks < 3)
                    return false;
            }
            else
            {
                // // Below level 58, need to be in Astral Fire to cast Fire2
                // if (AstralStacks >= 1)
                //     return false;
            }

            return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard2()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Blizzard2.IsKnown())
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            // Below Freeze, Blizzard II is the ice phase itself: castable at any Umbral Ice stack
            // count, it grants Umbral Ice III and carries the MP refill until full. Chaining it
            // back-to-back is intended here, so this sits above the doublecast guard below
            if (!Spells.Freeze.IsKnown() && UmbralStacks > 0)
            {
                if (Core.Me.CurrentMana == Core.Me.MaxMana)
                    return false;

                return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
            }

            if (Casting.LastSpellWas(Spells.Blizzard2)
                || Casting.LastSpellWas(Spells.HighBlizzardII)
                || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            // Flare keeps the fire phase going down to its 800 MP floor - don't hardcast into ice while it still can
            if (Spells.Flare.IsKnown() && Core.Me.CurrentMana >= 800)
                return false;

            if (Core.Me.CurrentMana >= 1600)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

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

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Meteor, Spells.Blizzard);
        }
    }
}



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

            // In AoE, don't save charges - use them for more damage
            return await Spells.Foul.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Flare()
        {
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
            //If we don't have Freeze, how can we cast it?
            if (!Spells.Freeze.IsKnown())
                return false;

            if (Casting.LastSpellWas(Spells.Freeze))
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            //Can only use in Umbral Ice
            if (UmbralStacks != 3)
                return false;

            if (Core.Me.CurrentMana == 10000)
                return false;

            return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder4()
        {
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
            if (!Spells.Fire2.IsKnown())
                return false;

            //No stack, open with Fire3
            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire2.Cast(Core.Me.CurrentTarget);

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks == 3 && UmbralHearts != 3)
                return false;

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
            if (!Spells.Blizzard2.IsKnown())
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard2)
                || Casting.LastSpellWas(Spells.HighBlizzardII)
                || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
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



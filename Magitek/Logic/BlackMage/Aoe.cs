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
            if (Core.Me.ClassLevel < 70)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            // If we're moving in combat
            if (MovementManager.IsMoving)
            {
                // If we don't have any procs (while in movement), cast
                if (!Core.Me.HasAura(Auras.Swiftcast)
                    && !Core.Me.HasAura(Auras.Triplecast))
                    return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            }

            //If at 2 stacks of polyglot, cast
            if (PolyglotCount <= BlackMageSettings.Instance.SaveXenoglossyCharges)
                return false;

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

            if (Core.Me.ClassLevel < Spells.Flare.LevelAcquired)
                return false;

            if (Core.Me.CurrentMana < 800)
                return false;

            if (Core.Me.ClassLevel == 100)
            {
                if (AstralStacks > 0)
                    return await Spells.Flare.Cast(Core.Me.CurrentTarget);
            }

            //No longer worth casting two HighFire2

            //Force flare after manafont
            if (Casting.LastSpell == Spells.ManaFont)
                return await Spells.Flare.Cast(Core.Me.CurrentTarget);

            return await Spells.Flare.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FlareStar()
        {
            if (Core.Me.ClassLevel < Spells.FlareStar.LevelAcquired)
                return false;

            if(!Spells.FlareStar.IsKnownAndReadyAndCastableAtTarget())
                return false;

            if (Casting.LastSpell == Spells.Fire3 || Casting.LastSpell == Spells.Blizzard3)
                return false;

            return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Freeze()
        {
            //If we don't have Freeze, how can we cast it?
            if (Core.Me.ClassLevel < Spells.Freeze.LevelAcquired)
                return false;

            if (Casting.LastSpell == Spells.Freeze)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            //Can only use in Umbral Ice
            if (UmbralStacks !=  3)
                return false;

            if (Core.Me.CurrentMana == 10000)
                return false;

            return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder4()
        {
            if (Core.Me.ClassLevel < Spells.Thunder2.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.ThunderSingle)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            // If the last spell we cast is triple cast, stop
            if (Casting.LastSpell == Spells.Triplecast)
                return false;

            // If we have the triplecast aura, stop
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;
            
            //If we don't need to refresh Thunder, skip
            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (Core.Me.ClassLevel < Spells.Thunder4.LevelAcquired)
                return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel < Spells.HighThunderII.LevelAcquired)
                return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunderII.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire2()
        {
            if (Core.Me.ClassLevel < Spells.Fire2.LevelAcquired)
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
            if (Casting.LastSpell == Spells.Fire2
                || Casting.LastSpell == Spells.HighFireII
                || Casting.LastSpell == Spells.ManaFont)
                return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            if (AstralStacks == 3 || UmbralStacks < 3)
                return false;

            if (UmbralStacks == 3 && Core.Me.CurrentMana == Core.Me.MaxMana && BlackMageSettings.Instance.UseTransposeToAstral)
            {
                if (!await UseTransposeToFire())
                    return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
            }

            return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard2()
        {
            if (Core.Me.ClassLevel < Spells.Blizzard2.LevelAcquired)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (Casting.LastSpell == Spells.Blizzard2
                || Casting.LastSpell == Spells.HighBlizzardII
                || Casting.LastSpell == Spells.ManaFont)
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            if (Core.Me.CurrentMana >= 1600)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            if (AstralStacks == 3 && BlackMageSettings.Instance.UseTransposeToUmbral)
            {
                if (!await UseTransposeToBlizzard())
                    return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
            }

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

        private static async Task<bool> UseTransposeToFire()
        {

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            if (!BlackMageSettings.Instance.UseTransposeToAstral)
                return false;

            if (!await Spells.Transpose.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, Spells.Flare.CanCast);
        }

        private static async Task<bool> UseTransposeToBlizzard()
        {

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            if (!BlackMageSettings.Instance.UseTransposeToUmbral)
                return false;

            if (!await Spells.Transpose.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, Spells.Freeze.CanCast);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Meteor, Spells.Blizzard);
        }
    }
}



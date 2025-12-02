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

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            if (Core.Me.ClassLevel < Spells.Xenoglossy.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.Xenoglossy)
                return false;

            if (AstralStacks == 0 && UmbralStacks == 0)
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

            return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Despair()
        {
            if (Core.Me.ClassLevel < Spells.Despair.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.Despair)
                return false;

            if (Casting.LastSpell == Spells.Despair)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks > 0)
                return false;

            if (!Spells.Despair.IsKnownAndReadyAndCastable())
                return false;

            if (Spells.ManaFont.IsKnownAndReadyAndCastable())
                return false;

            // If our mana is more then 1600
            if (Core.Me.CurrentMana >= 1600 || Core.Me.CurrentMana == 0)
                return false;

            return await Spells.Despair.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire()
        {

            if (Core.Me.ClassLevel < Spells.Fire.LevelAcquired)
                return false;

            if (Core.Me.ClassLevel >= Spells.Fire4.LevelAcquired)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            //only use in astral fire
            if (UmbralStacks > 0)
                return false;

            if (Core.Me.CurrentMana < 800)
                return false;

            return await Spells.Fire.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> Fire4()
        {
            if (Core.Me.ClassLevel < Spells.Fire4.LevelAcquired)
                return false;

            //only use in astral fire
            if (AstralStacks != 3)
                return false;

            return await Spells.Fire4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire3()
        {

            if (Core.Me.ClassLevel < Spells.Fire3.LevelAcquired)
                return false;

            //No stack, open with Fire3
            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire3.Cast(Core.Me.CurrentTarget);

            if (BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            if (Casting.LastSpell == Spells.Fire3)
                return false;

            //Don't waste firestarter if we are about to enter UI
            if (Core.Me.CurrentMana < 2000
                && Core.Me.HasAura(Auras.FireStarter))
                return false;

            //Keep from using firestarter early - wait for full mana
            if (Core.Me.HasAura(Auras.FireStarter)
                && Core.Me.CurrentMana < 8400)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            if (AstralStacks == 3 || UmbralStacks < 3)
                return false;

            if (UmbralStacks == 3 && Core.Me.CurrentMana == Core.Me.MaxMana && BlackMageSettings.Instance.UseTransposeToAstral)
            {
                if (!await UseTransposeToFire())
                    return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
            }

            return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder3()
        {

            if (Core.Me.ClassLevel < Spells.Thunder.LevelAcquired)
                return false;

            // Skip if we're in an AoE situation (use Thunder4 instead)
            if (BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // If the last spell we cast is triple cast, stop
            if (Casting.LastSpell == Spells.Triplecast)
                return false;

            // If we have the triplecast aura, stop
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            //Moved this up to see if it stops the doublecast
            if (Casting.LastSpell == Spells.Thunder || Casting.LastSpell == Spells.Thunder3 || Casting.LastSpell == Spells.HighThunder)
                return false;

            // Try to keep from double-casting thunder
            if (Casting.LastSpell == Spells.Thunder
                || Casting.LastSpell == Spells.Thunder2
                || Casting.LastSpell == Spells.Thunder3
                || Casting.LastSpell == Spells.Thunder4
                || Casting.LastSpell == Spells.HighThunder
                || Casting.LastSpell == Spells.HighThunderII)
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (Core.Me.ClassLevel < Spells.Thunder3.LevelAcquired)
                return await Spells.Thunder.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel < Spells.HighThunder.LevelAcquired)
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

            if (Core.Me.ClassLevel < Spells.Blizzard4.LevelAcquired)
                return false;

            if (UmbralStacks != 3)
                return false;

            if (Core.Me.CurrentMana == Core.Me.MaxMana)
                return false;

            if (Casting.LastSpell == Spells.Blizzard4)
                return false;

            if (Casting.LastSpell == Spells.Transpose)
                return false;

            return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard3()
        {

            if (Core.Me.ClassLevel < Spells.Blizzard3.LevelAcquired)
                return false;

            if (Casting.LastSpell == Spells.Blizzard3
                || Casting.LastSpell == Spells.ManaFont)
                return false;

            //If flarestar is ready, cast it
            if (AstralSoulStacks == 6)
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            if (BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
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

            return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Blizzard()
        {

            if (AstralStacks > 0)
                return false;

            //Low level logic
            if (Core.Me.ClassLevel < Spells.Blizzard3.LevelAcquired)
            {
                if (Casting.LastSpell == Spells.Transpose && AstralStacks > 0)
                    return false;

                if (Core.Me.CurrentMana < 1600 || (AstralStacks == 0 && UmbralStacks >= 0))
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                if (Core.Me.CurrentMana < 1000 && AstralStacks == 0 && UmbralStacks > 0)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                return false;
            }

            if (Casting.LastSpell == Spells.Blizzard4)
                return false;


            return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Paradox()
        {
            if (Core.Me.ClassLevel < Spells.Paradox.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.Paradox)
                return false;

            if (Casting.LastSpell == Spells.Fire3)
                return false;

            if (Casting.LastSpell == Spells.Blizzard3)
                return false;

            if (AstralStacks != 3 && UmbralStacks != 3)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            if (Spells.Fire4.IsKnownAndReadyAndCastableAtTarget() && Spells.ManaFont.Cooldown.TotalMilliseconds >= 70000)
                return false;

            return await Spells.Paradox.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> UseTransposeToFire()
        {

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            if (!BlackMageSettings.Instance.UseTransposeToAstral)
                return false;

            if (!await Spells.Transpose.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, Spells.Fire4.CanCast);
        }

        private static async Task<bool> UseTransposeToBlizzard()
        {

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            if (!BlackMageSettings.Instance.UseTransposeToUmbral)
                return false;

            if (!await Spells.Transpose.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, Spells.Blizzard4.CanCast);
        }

    }
}

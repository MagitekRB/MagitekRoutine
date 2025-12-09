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
            // If below Xenoglossy level, use Foul instead
            if (Core.Me.ClassLevel < Spells.Xenoglossy.LevelAcquired)
            {
                // Check if we can use Foul
                if (Core.Me.ClassLevel >= Spells.Foul.LevelAcquired && PolyglotStatus)
                    return await Aoe.Foul();
                return false;
            }

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

            // Cast if we're about to overcap Polyglot (prevent waste)
            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);

            //If at save threshold, don't cast
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

            if (Casting.LastSpellWas(Spells.Despair))
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

            // if (BlackMageSettings.Instance.UseAoe
            //     && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
            //     return false;

            if (Casting.LastSpellWas(Spells.Fire3))
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

            if (UmbralStacks > 0 && Core.Me.CurrentMana != Core.Me.MaxMana)
                return false;

            return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder3()
        {

            if (Core.Me.ClassLevel < Spells.Thunder.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.ThunderSingle)
                return false;

            // Skip if we're in an AoE situation (use Thunder4 instead)
            if (BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            // If the last spell we cast is triple cast, stop
            if (Casting.LastSpellWas(Spells.Triplecast))
                return false;

            // If we have the triplecast aura, stop
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            //Moved this up to see if it stops the doublecast
            if (Casting.LastSpellWas(Spells.Thunder) || Casting.LastSpellWas(Spells.Thunder3) || Casting.LastSpellWas(Spells.HighThunder))
                return false;

            // Try to keep from double-casting thunder
            if (Casting.LastSpellWas(Spells.Thunder)
                || Casting.LastSpellWas(Spells.Thunder2)
                || Casting.LastSpellWas(Spells.Thunder3)
                || Casting.LastSpellWas(Spells.Thunder4)
                || Casting.LastSpellWas(Spells.HighThunder)
                || Casting.LastSpellWas(Spells.HighThunderII))
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (BlackMageSettings.Instance.UseTTDForThunderSingle && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderSingleTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
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

            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;

            if (Casting.LastSpell == Spells.Transpose)
                return false;

            return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard3()
        {

            if (Core.Me.ClassLevel < Spells.Blizzard3.LevelAcquired)
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard3)
                || Casting.LastSpellWas(Spells.ManaFont))
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

            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;


            return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Paradox()
        {
            if (Core.Me.ClassLevel < Spells.Paradox.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.Paradox)
                return false;

            if (Casting.LastSpellWas(Spells.Fire3))
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard3))
                return false;

            if (AstralStacks != 3 && UmbralStacks != 3)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            if (Spells.Fire4.IsKnownAndReadyAndCastableAtTarget() && Spells.ManaFont.Cooldown.TotalMilliseconds >= 70000)
                return false;

            return await Spells.Paradox.Cast(Core.Me.CurrentTarget);
        }

    }
}

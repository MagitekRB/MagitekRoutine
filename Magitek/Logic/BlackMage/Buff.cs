﻿using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class Buff
    {
        public static async Task<bool> Triplecast()
        {
            if (Core.Me.ClassLevel < Spells.Triplecast.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.TripleCast)
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (!Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if ((MovementManager.IsMoving && AstralStacks == 3))
                return await Spells.Triplecast.Cast(Core.Me);

            if (BlackMageSettings.Instance.TripleCastWhileMoving && Spells.Triplecast.Charges<= 1)
                return false;

            // Don't dot if time in combat less than 30 seconds
            //if (Combat.CombatTotalTimeLeft <= 30)
            //    return false;

            // Add check for charges with new update
            if (Spells.Triplecast.Cooldown != TimeSpan.Zero
                && Spells.Triplecast.Charges == 0)
                return false;

            // Check to see if triplecast is already up
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            // Dun cast truplecast when in umbral
            if (UmbralStacks > 0)
                return false;

            return await Spells.Triplecast.Cast(Core.Me);
        }
        //Sharpcast was removed from game.

        public static async Task<bool> LeyLines()
        {
            if (Core.Me.ClassLevel < Spells.LeyLines.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.LeyLines)
                return false;

            if (BlackMageSettings.Instance.LeyLinesBossOnly
                && !Core.Me.CurrentTarget.IsBoss())
                return false;

            // Don't use if time in combat less than 30 seconds
            //if (Combat.CombatTotalTimeLeft <= 30)
            //    return false;

            //Don't use while moving
            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.LeyLines))
                return false;

            if (!Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            // Do not Ley Lines if we don't have 3 astral stacks
            if (AstralStacks != 3
                || UmbralStacks > 0)
                return false;

            if (Core.Me.HasAura(Auras.CircleOfPower))
                return false;

            // Do not Ley Lines if we don't have any umbral hearts (roundabout check to see if we're at the begining of astral)
            if (Casting.LastSpell == Spells.Fire3
                && (UmbralHearts == 3
                || Core.Me.HasAura(Auras.Triplecast)))
                // Fire 3 is always used at the start of Astral
                return await Spells.LeyLines.Cast(Core.Me);

            //Use in AoE rotation as well
            if (Casting.LastSpell == Spells.HighFireII
                && (UmbralHearts == 3
                || Core.Me.HasAura(Auras.Triplecast)))
                // High Fire II is always used at the start of Astral
                return await Spells.LeyLines.Cast(Core.Me);
           
            return await Spells.LeyLines.Cast(Core.Me);
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

        public static async Task<bool> Retrace()
        {
            if (Core.Me.ClassLevel < Spells.Retrace.LevelAcquired)
                return false;

            if (Spells.Retrace.Cooldown != TimeSpan.Zero)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.LeyLines))
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Retrace.IsKnownAndReady())
                return false;

            if (Casting.SpellCastHistory.Count() > 0
                && Casting.SpellCastHistory.Take(10).Any(s => s.Spell == Spells.LeyLines))
                return await Spells.LeyLines.Masked().Cast(Core.Me);

            return false;
        }
        public static async Task<bool> UmbralSoul()
        {
            if (Core.Me.ClassLevel < Spells.UmbralSoul.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.UmbralSoul)
                return false;

            if (Spells.UmbralSoul.Cooldown != TimeSpan.Zero)
                return false;

            // Do not Umbral Soul unless we have 1 umbral stack
            if (UmbralStacks == 0)
                return false;

            if (Core.Me.CurrentTarget != null)
                return false;

            //Try and not get stuck at 2 stacks
            if (UmbralStacks < 3
                && UmbralStacks > 1)
                return await Spells.UmbralSoul.Cast(Core.Me);

            return false;

        }

        public static async Task<bool> ManaFont()
        {
            if (Core.Me.ClassLevel < Spells.ManaFont.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.ManaFont)
                return false;

            if(UmbralStacks != 0)
                return false;

            /*
            if (Spells.ManaFont.Cooldown != TimeSpan.Zero)
                return false;

            if (!BlackMageSettings.Instance.ConvertAfterFire3)
                return false;

            // Don't use if time in combat less than 30 seconds
            if (Combat.CombatTotalTimeLeft <= 30)
                return false;

            if (Core.Me.CurrentMana >= 7000)
                return false;

            //Moved this up as it should go off regardless of toggle
            //Swapped mana check to be first as this was going off before we had 0 mana
            if (Core.Me.CurrentMana == 0
                && (Casting.LastSpell == Spells.Flare
                || Casting.LastSpell == Spells.Foul))
            //&& Spells.Fire.Cooldown.TotalMilliseconds > Globals.AnimationLockMs                
            {
                Logger.WriteInfo($@"[Debug] If we get to this point we should have cast flare and have 0 mana - actual last spell is {Casting.LastSpell} and we have {Core.Me.CurrentMana} mana.");

                return await Spells.ManaFont.Cast(Core.Me);

            }


            Logger.WriteInfo($@"[Debug] If we get to this point we should have less than 7000 mana - actual current mana is {Core.Me.CurrentMana}.");

            if (Core.Me.CurrentMana == 0
                && (Casting.LastSpell == Spells.Despair
                || Casting.LastSpell == Spells.Xenoglossy))
            {
                Logger.WriteInfo($@"[Debug] If we get to this point we should have cast xeno or despair - actual last spell is {Casting.LastSpell}.");

                return await Spells.ManaFont.Cast(Core.Me);
            }
            if (Casting.LastSpell == Spells.Fire3
                //&& Spells.Fire.Cooldown.TotalMilliseconds > Globals.AnimationLockMs
                && BlackMageSettings.Instance.ConvertAfterFire3
                && Core.Me.CurrentMana < 7000)
            {
                Logger.WriteInfo($@"[Debug] If we get to this point we should have cast fire III - actual last spell is {Casting.LastSpell}.");

                return await Spells.ManaFont.Cast(Core.Me);

            }
            */

            //Still got enough mana to make one more fire cast
            if (Core.Me.CurrentMana >= 1600)
                return false;

            return await Spells.ManaFont.Cast(Core.Me);
        }
        public static async Task<bool> Transpose()
        {
            if (Core.Me.ClassLevel < Spells.Transpose.LevelAcquired)
                return false;

            return await Spells.Transpose.Cast(Core.Me);
        }
        public static async Task<bool> Amplifier()
        {
            if (Core.Me.ClassLevel < Spells.Amplifier.LevelAcquired)
                return false;

            if (!BlackMageSettings.Instance.Amplifier)
                return false;

            if (ActionResourceManager.BlackMage.PolyglotCount > 2)
                return false;

            if (!Core.Me.InCombat)
                return false;

            return await Spells.Amplifier.Cast(Core.Me);
        }

    }

}

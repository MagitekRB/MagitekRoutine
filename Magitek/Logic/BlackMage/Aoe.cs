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

            if (Casting.LastSpell == Spells.Foul)
                return false;
            
            // If we need to refresh stack timer, stop
            if (StackTimer.TotalMilliseconds <= 5000)
                return false;

            //AoE with transpose optimization
            if (Core.Me.ClassLevel == 100)
            {
                if (Casting.LastSpell == Spells.FlareStar &&
                    !Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, msLeft:BlackMageSettings.Instance.ThunderRefreshSecondsLeft*1000+500))
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);

                if (Casting.LastSpell == Spells.Freeze &&
                    !Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, msLeft: BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            }

            //if you don't have Aspect Mastery, just SMASH THAT FOUL BUTTON
            if (Core.Me.ClassLevel < 80)
                if (UmbralStacks == 3)
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            //If at 2 stacks of polyglot and 5 seconds from another stack, cast
            if (PolyglotCount == 2
                && PolyglotTimer.TotalMilliseconds <= 5000)
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            // If we're moving in combat
            if (Core.Me.ClassLevel >= 80 && MovementManager.IsMoving)
            {
                // If we don't have any procs (while in movement), cast
                if (!Core.Me.HasAura(Auras.Swiftcast)
                    && !Core.Me.HasAura(Auras.Triplecast)
                    && !Core.Me.HasAura(Auras.FireStarter)
                    && !Core.Me.HasAura(Auras.Thunderhead))
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);
            }
            
            //If at max polyglot stacks, cast
            if (PolyglotCount == 2
                && Casting.LastSpell == Spells.Flare)
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            //Only use in Umbral 3
            if (UmbralStacks != 3)
                return false;            

            //If we have Umbral hearts, Freeze has gone off
            //Trying logic from xeno instead to see if this allows T4 to go off
            /*if (UmbralHearts >= 1)
                if (Casting.LastSpell != Spells.Foul)
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);
            */
            //If while in Umbral 3 and, we didn't use Thunder in the Umbral window
            if (UmbralStacks == 3 && Casting.LastSpell != Spells.Thunder4)
			{
                //We don't have max mana
                if (Core.Me.CurrentMana < 10000 && Core.Me.CurrentTarget.HasAura(Auras.Thunder4, true, 5000))
                    return await Spells.Foul.Cast(Core.Me.CurrentTarget);

                return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);
            }			

            return false;
        }

        public static async Task<bool> Flare()
        {
            //Can't use in Umbral Ice anymore
            if (UmbralStacks > 0)
                return false;

            if (Core.Me.ClassLevel < Spells.Flare.LevelAcquired)
                return false;

            if (Core.Me.CurrentMana < 800
                || Core.Me.CurrentMana == 10000)
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

            if (AstralSoulStacks < 6)
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

            //Can only use in Umbral Ice
            if (UmbralStacks < 1)
                return false;

            // If we need to refresh stack timer, stop
            if (StackTimer.TotalMilliseconds <= 5000)
                return false;

            // While in Umbral 
            if (UmbralStacks > 0)
            {
                // If we have less than 3 hearts, cast
                if (UmbralHearts < 3)
                    return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> Thunder4()
        {
            if (!BlackMageSettings.Instance.ThunderSingle)
                return false;
            
            // If we need to refresh stack timer, stop
            if (StackTimer.TotalMilliseconds <= 5000)
                return false;

            // If the last spell we cast is triple cast, stop
            if (Casting.LastSpell == Spells.Triplecast)
                return false;

            // If we have the triplecast aura, stop
            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            //Cast any time thunderhead procs - moved up as TC procs do full damage up front and it doesn't matter how much time in combat is left
            if (Core.Me.HasAura(Auras.Thunderhead))
                return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            //AoE with transpose optimization
            if (Core.Me.ClassLevel == 100)
            {
                if (Casting.LastSpell == Spells.FlareStar &&
                    Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, msLeft: BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                    return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

                if (Casting.LastSpell == Spells.Freeze &&
                    Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, msLeft: BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                    return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            }
            // Don't dot if time in combat less than configured seconds left
            if (Combat.CombatTotalTimeLeft <= BlackMageSettings.Instance.ThunderTimeTillDeathSeconds)
                return false;

            //Only cast in Umbral 3 - should be cast in either if needed
            //if (UmbralStacks != 3)
            //    return false;

            //If we don't need to refresh Thunder, skip
            if (!Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (Core.Me.ClassLevel < 68)
                if (Casting.LastSpell != Spells.Thunder2)
                    if (Casting.LastSpell == Spells.Transpose
                        || Casting.LastSpell == Spells.Blizzard2
                        || Casting.LastSpell == Spells.Freeze)
                        return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            if (Core.Me.ClassLevel < 72)
                if (Casting.LastSpell != Spells.Thunder4)
                {
                    if (Casting.LastSpell == Spells.Transpose
                        || Casting.LastSpell == Spells.Foul
                        || Casting.LastSpell == Spells.Freeze)
                        return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);
                }

            if (Core.Me.ClassLevel >= 72)
            {
                if (Casting.LastSpell != Spells.Thunder4)
                        return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);
                
            }
            return false;
        }

        public static async Task<bool> Fire2()
        {
            if (Core.Me.ClassLevel < Spells.Fire2.LevelAcquired)
                return false;
            
            if (Core.Me.CurrentMana == 10000)
                    return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
            
            return false;
        }

        public static async Task<bool> Blizzard2()
        {
            if (Core.Me.ClassLevel < Spells.Blizzard2.LevelAcquired)
                return false;

            if (Casting.LastSpell == Spells.Blizzard2
                || Casting.LastSpell == Spells.HighBlizzardII
                || Casting.LastSpell == Spells.ManaFont)
                return false;

            // If our mana is less than 3000 while in astral and can not cast flare
            if (AstralStacks > 0 && Core.Me.CurrentMana < 3000 && Core.Me.ClassLevel < Spells.Flare.LevelAcquired)
                return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);

            // If our mana is 0 then we have completed rotation with flare
            if (AstralStacks > 0 && Core.Me.CurrentMana == 0)
                return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);

            // If we have no umbral or astral stacks, cast 
            if (AstralStacks <= 1 && UmbralStacks <= 1)
                return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);

            return false;
        }
        private static readonly uint[] ThunderAuras =
        {
            Auras.Thunder,
            Auras.Thunder2,
            Auras.Thunder3,
            Auras.Thunder4
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



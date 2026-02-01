using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using Iaijutsu = ff14bot.Managers.ActionResourceManager.Samurai.Iaijutsu;
using SamuraiRoutine = Magitek.Utilities.Routines.Samurai;

namespace Magitek.Logic.Samurai
{
    internal static class SingleTarget
    {

        public static async Task<bool> Hakaze()
        {
            if (Spells.MidareSetsugekka.CanCast())
                return false;

            if (Spells.Higanbana.IsKnown() && Spells.Higanbana.CanCast() && !Core.Me.CurrentTarget.HasAura(Auras.Higanbana, true, 4000) && Core.Me.HasAura(Auras.Jinpu) && Core.Me.HasAura(Auras.Shifu)) { 
                if (!SamuraiSettings.Instance.HiganbanaOnlyBoss)
                {
                    return await Spells.Higanbana.Cast(Core.Me.CurrentTarget);
                } 
                else if (Core.Me.CurrentTarget.IsBoss())
                {
                    return await Spells.Higanbana.Cast(Core.Me.CurrentTarget);
                }
            }

            if (Spells.Gyofu.IsKnown())
                return await Spells.Gyofu.Cast(Core.Me.CurrentTarget);
            else
                return await Spells.Hakaze.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************************
         *                                    Combo 1 - Filler 3 GCD
         * ***********************************************************************************************/
        public static async Task<bool> Jinpu()
        {
            if (!SamuraiRoutine.CanContinueComboAfter(Spells.Hakaze) && !SamuraiRoutine.CanContinueComboAfter(Spells.Gyofu))
                return false;

            if (ActionResourceManager.Samurai.Sen.HasFlag(Iaijutsu.Getsu))
                return false;

            return await Spells.Jinpu.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Gekko()
        {
            if (ActionResourceManager.Samurai.Sen.HasFlag(Iaijutsu.Getsu))
                return false;

            if (!SamuraiRoutine.CanContinueComboAfter(Spells.Jinpu) && !Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            return await Spells.Gekko.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************************
         *                                    Combo 2 - Filler 3 GCD
         * ***********************************************************************************************/
        public static async Task<bool> Shifu()
        {
            if (!SamuraiRoutine.CanContinueComboAfter(Spells.Hakaze) && !SamuraiRoutine.CanContinueComboAfter(Spells.Gyofu))
                return false;

            if (ActionResourceManager.Samurai.Sen.HasFlag(Iaijutsu.Ka))
                return false;

            return await Spells.Shifu.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Kasha()
        {
            if (ActionResourceManager.Samurai.Sen.HasFlag(Iaijutsu.Ka))
                return false;

            if (!SamuraiRoutine.CanContinueComboAfter(Spells.Shifu) && !Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            return await Spells.Kasha.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************************
         *                                    Combo3 - Filler 2 GCD & 4GCD
         * ***********************************************************************************************/
        public static async Task<bool> Yukikaze()
        {
            if (ActionResourceManager.Samurai.Sen.HasFlag(Iaijutsu.Setsu))
                return false;

            if (!SamuraiRoutine.CanContinueComboAfter(Spells.Hakaze) && !SamuraiRoutine.CanContinueComboAfter(Spells.Gyofu)
                && !Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

           // if (SamuraiRoutine.isReadyFillerRotation && SamuraiSettings.Instance.SamuraiFillerStrategy.Equals(SamuraiFillerStrategy.ThreeGCD))
           //     return false;

            return await Spells.Yukikaze.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************************
         *                                    Filler 1 GCD
         * ***********************************************************************************************/
        public static async Task<bool> Enpi()
        {
            if (!SamuraiSettings.Instance.UseEnpi)
                return false;

            if (Core.Me.CurrentTarget.Distance() < 10f && !Core.Me.HasAura(Auras.EnhancedEnpi))
                return false;

            if (SamuraiRoutine.SenCount == 3)
                return false;

            return await Spells.Enpi.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************************
         *                                    oGCD
         * ***********************************************************************************************/
        public static async Task<bool> Shoha()
        {
            if (!SamuraiSettings.Instance.UseShoha)
                return false;

            if (ActionResourceManager.Samurai.Meditation < 3)
                return false;

            return await Spells.Shoha.Cast(Core.Me.CurrentTarget);
        }


        /**********************************************************************************************
        *                                    oGCD using Kenki
        * ********************************************************************************************/
        public static async Task<bool> HissatsuGyoten() //dash forward
        {
            if (!SamuraiSettings.Instance.UseHissatsuGyoten)
                return false;

            if (!Movement.CanUseGapCloser())
                return false;

            if (!SamuraiRoutine.GlobalCooldown.CanWeave(1))
                return false;

            if (Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            if (ActionResourceManager.Samurai.Kenki < 10 + SamuraiSettings.Instance.ReservedKenki)
                return false;

            if (SamuraiSettings.Instance.UseHissatsuGyotenOnlyWhenOutOfMeleeRange && !Core.Me.CurrentTarget.WithinSpellRange(Spells.Hakaze.Range))
            {
                return Combat.Enemies.Count(x => x.Distance(Core.Me) <= SamuraiRoutine.Fuko.Radius + Core.Me.CombatReach) < SamuraiSettings.Instance.AoeEnemies
                    ? await Spells.HissatsuGyoten.Cast(Core.Me.CurrentTarget)
                    : false;
            }

            return await Spells.HissatsuGyoten.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HissatsuYaten() //dash backward - to use with 1GCD Filler - Should not be used with Routine to avoid dying in a wall
        {
            if (!SamuraiSettings.Instance.UseHissatsuYaten)
                return false;

            if (!Movement.CanUseGapCloser())
                return false;

            if (!SamuraiRoutine.GlobalCooldown.CanWeave(1))
                return false;

            if (Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            if (ActionResourceManager.Samurai.Kenki < 10 + SamuraiSettings.Instance.ReservedKenki) //10 for Yaten + 10 for Guoten
                return false;

            if (Core.Me.HasAura(Auras.ZanshinReady))
                return false;

            if (!Spells.HissatsuGyoten.IsKnownAndReady())
                return false;

            if (Casting.LastSpell != Spells.MidareSetsugekka)
                return false;

            return await Spells.HissatsuYaten.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HissatsuSenei()
        {
            if (!SamuraiSettings.Instance.UseHissatsuSenei)
                return false;

            if (ActionResourceManager.Samurai.Kenki < 25 + SamuraiSettings.Instance.ReservedKenki)
                return false;

            return await Spells.HissatsuSenei.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HissatsuShinten()
        {
            if (!SamuraiSettings.Instance.UseHissatsuShinten)
                return false;

            if (ActionResourceManager.Samurai.Kenki < 25 + SamuraiSettings.Instance.ReservedKenki)
                return false;

            if (Core.Me.HasAura(Auras.ZanshinReady))
                return false;

            if (Casting.LastSpell == Spells.HissatsuSenei)
                return false;

            if (Spells.HissatsuSenei.IsKnownAndReady() || (Spells.HissatsuSenei.IsKnownAndReady(10000) && ActionResourceManager.Samurai.Kenki <  25 + SamuraiSettings.Instance.ReservedKenki))
                return false;

            return await Spells.HissatsuShinten.Cast(Core.Me.CurrentTarget);
        }


        /**********************************************************************************************
        *                                    Iaijutsu
        * ********************************************************************************************/

        public static async Task<bool> MidareSetsugekka()
        {
            if (!SamuraiSettings.Instance.UseMidareSetsugekka)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Core.Me.CurrentTarget.CombatReach + 6)
                return false;

            if (SamuraiRoutine.SenCount != 3)
                return false;

            if (Core.Me.HasAura(Auras.Tendo))
                return await Spells.TendoSetsugekka.Cast(Core.Me.CurrentTarget);
            else
                if (!await Spells.MidareSetsugekka.Cast(Core.Me.CurrentTarget))
                return false;
            /*
            if (SamuraiRoutine.prepareFillerRotation && (Spells.TsubameGaeshi.Charges < 1 || Spells.KaeshiSetsugekka.Charges < 1))
            {
                SamuraiRoutine.InitializeFillerVar(false, true); // Execute Filler
                Logger.WriteInfo($@"[Filler] Execute Filler: {SamuraiSettings.Instance.SamuraiFillerStrategy}");
            }
            */
            return true;
        }

        public static async Task<bool> Higanbana()
        {
            if (!SamuraiSettings.Instance.UseHiganbana)
                return false;

            if (SamuraiRoutine.SenCount == 0 || SamuraiRoutine.SenCount == 3)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Core.Me.CurrentTarget.CombatReach + 6)
                return false;

            if (Core.Me.CurrentTarget.HasAura(Auras.Higanbana, true, 6000))
                return false;

            if (!Core.Me.HasAura(Auras.Jinpu) || !Core.Me.HasAura(Auras.Shifu))
                return false;

            if (SamuraiRoutine.AoeEnemies5Yards >= SamuraiSettings.Instance.AoeEnemies)
                return false;

            if (!CanHiganbana(Core.Me.CurrentTarget))
                return false;

            return await Spells.Higanbana.Cast(Core.Me.CurrentTarget);
        }

        public static bool CanHiganbana(GameObject unit)
        {
            if (unit.IsBoss())
                return true;

            if (SamuraiSettings.Instance.HiganbanaOnlyBoss)
                return false;

            return unit.CombatTimeLeft() >= SamuraiSettings.Instance.DontHiganbanaIfEnemyDyingWithinSeconds;
        }

        /**********************************************************************************************
        *                              Tsubamegaeshi (after Iaijutsu)
        * ********************************************************************************************/


        public static async Task<bool> KaeshiSetsugekka()
        {
            if (!SamuraiSettings.Instance.UseKaeshiSetsugekka)
                return false;

            if (Spells.TendoKaeshiSetsugekka.IsKnownAndReadyAndCastable())
                return await Spells.TendoKaeshiSetsugekka.Cast(Core.Me.CurrentTarget);

            if (SamuraiRoutine.SenCount != 3)
                return false;

            return await Spells.KaeshiSetsugekka.Cast(Core.Me.CurrentTarget);

        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            if (!Core.Me.HasTarget)
                return false;

            return PhysicalDps.ForceLimitBreak(Spells.Braver, Spells.Bladedance, Spells.DoomoftheLiving, Spells.Hakaze);
        }
    }
}

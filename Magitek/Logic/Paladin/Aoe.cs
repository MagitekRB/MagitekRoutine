using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Paladin;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using PaladinRoutine = Magitek.Utilities.Routines.Paladin;


namespace Magitek.Logic.Paladin
{
    internal static class Aoe
    {

        /*************************************************************************************
         *                                    AOE on me
         * ***********************************************************************************/
        public static async Task<bool> CircleOfScorn()
        {
            if (!PaladinSettings.Instance.UseCircleOfScorn)
                return false;

            if (PaladinSettings.Instance.SaveCircleOfScorn && Spells.FightorFlight.Cooldown.TotalSeconds <= PaladinSettings.Instance.SaveCircleOfScornMseconds)
                return false;

            if (Spells.Requiescat.IsKnownAndReady())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.CircleofScorn.Radius)) < 1)
                return false;

            return await Spells.CircleofScorn.Cast(Core.Me);
        }

        public static async Task<bool> HolyCircle()
        {
            if (!PaladinSettings.Instance.UseAoe)
                return false;

            if (!PaladinSettings.Instance.UseHolyCircle)
                return false;

            if (!Spells.HolyCircle.IsKnownAndReady())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.HolyCircle.Radius)) < PaladinSettings.Instance.HolyCircleEnemies)
                return false;

            if (Core.Me.HasAura(Auras.Requiescat) && !Spells.BladeOfFaith.IsKnown())
            {
                return await Spells.HolyCircle.Cast(Core.Me);
            }

            if (!Core.Me.HasAura(Auras.DivineMight))
                return false;

            return await Spells.HolyCircle.Cast(Core.Me);
        }

        /*************************************************************************************
         *                                    AOE on Target
         * ***********************************************************************************/
        public static async Task<bool> Expiacion() //SpiritsWithin or Expiacion
        {

            if (!PaladinSettings.Instance.UseExpiacion)
                return false;

            if (PaladinSettings.Instance.SaveCircleOfScorn && Spells.FightorFlight.Cooldown.TotalSeconds <= PaladinSettings.Instance.SaveCircleOfScornMseconds)
                return false;

            return await PaladinRoutine.Expiacion.Cast(Core.Me.CurrentTarget);
        }



        /*************************************************************************************
         *                                    Combo
         * ***********************************************************************************/
        public static async Task<bool> TotalEclipse()
        {
            if (!PaladinSettings.Instance.UseAoe)
                return false;

            if (!PaladinSettings.Instance.UseEclipseCombo)
                return false;

            if (!Spells.TotalEclipse.IsKnown())
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.TotalEclipse.Radius)) < PaladinSettings.Instance.TotalEclipseEnemies)
                return false;

            return await Spells.TotalEclipse.Cast(Core.Me);
        }

        public static async Task<bool> Prominence()
        {
            if (!PaladinSettings.Instance.UseAoe)
                return false;

            if (!PaladinSettings.Instance.UseEclipseCombo)
                return false;

            if (!Spells.Prominence.IsKnownAndReady())
                return false;

            if (!PaladinRoutine.CanContinueComboAfter(Spells.TotalEclipse))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.Prominence.Radius)) < PaladinSettings.Instance.TotalEclipseEnemies)
                return false;

            return await Spells.Prominence.Cast(Core.Me);
        }

        /*************************************************************************************
         *                                    Combo 2
         * ***********************************************************************************/
        public static async Task<bool> Confiteor()
        {
            if (!PaladinSettings.Instance.UseConfiteorCombo)
                return false;

            if (!Spells.Confiteor.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.FightOrFlight))
                return false;

            return await Spells.Confiteor.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeOfFaith()
        {
            if (!PaladinSettings.Instance.UseConfiteorCombo)
                return false;

            if (!Spells.BladeOfFaith.IsKnownAndReady())
                return false;

            if (!PaladinRoutine.CanContinueComboAfter(Spells.Confiteor))
                return false;

            return await Spells.BladeOfFaith.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeOfTruth()
        {
            if (!PaladinSettings.Instance.UseConfiteorCombo)
                return false;

            if (!Spells.BladeOfTruth.IsKnownAndReady())
                return false;

            if (!PaladinRoutine.CanContinueComboAfter(Spells.BladeOfFaith))
                return false;

            return await Spells.BladeOfTruth.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeOfValor()
        {
            if (!PaladinSettings.Instance.UseConfiteorCombo)
                return false;

            if (!Spells.BladeOfValor.IsKnownAndReady())
                return false;

            if (!PaladinRoutine.CanContinueComboAfter(Spells.BladeOfTruth))
                return false;

            return await Spells.BladeOfValor.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BladeOfHonor()
        {
            if (!PaladinSettings.Instance.UseConfiteorCombo)
                return false;

            if (!Spells.BladeOfHonor.IsKnownAndReady())
                return false;

            return await Spells.BladeOfHonor.Cast(Core.Me.CurrentTarget);
        }
    }
}
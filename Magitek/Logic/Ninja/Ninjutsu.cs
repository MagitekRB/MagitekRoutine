using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Ninja;
using Magitek.Utilities;
using Magitek.Utilities.GamelogManager;
using System;
using System.Linq;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;


namespace Magitek.Logic.Ninja
{
    internal static class Ninjutsu
    {

        public static async Task<bool> Huton()
        {

            if (!Spells.Huton.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || Core.Me.HasAura(Auras.Kassatsu))
                return false;

            if (Spells.TrickAttack.Cooldown >= new TimeSpan(0, 0, 15))
                return false;

            if (Core.Me.HasMyAura(Auras.ShadowWalker))
                return false;

            if (!AoeControl.Enabled || NinjaRoutine.AoeEnemies5Yards <= 2)
                return false;

            // Decided only before the first mudra: a chain in progress is finished whatever the estimate
            // says now, because abandoning it wastes the charge and hands the mudra state to a Rabbit Medium.
            if (NinjaRoutine.UsedMudras.Count == 0 && !Cooldown.KunaisBaneWanted(Core.Me.CurrentTarget))
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Huton, Core.Me);

        }

        public static async Task<bool> Suiton()
        {

            if (!Spells.Suiton.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || Core.Me.HasAura(Auras.Kassatsu))
                return false;

            if (Spells.TrickAttack.Cooldown >= new TimeSpan(0, 0, 15))
                return false;

            if (Core.Me.HasMyAura(Auras.ShadowWalker))
                return false;

            // Suiton exists to enable Kunai's Bane. When Kunai's Bane is not going to be pressed on this
            // target the charge is worth more as a Raiton, and a Shadow Walker that expires unused is a
            // Raiton and a Raiju thrown away.
            // Decided only before the first mudra: the time-to-die estimate moves every pulse, and a chain
            // abandoned after its mudras wastes the charge and hands the mudra state to a Rabbit Medium.
            if (NinjaRoutine.UsedMudras.Count == 0 && !Cooldown.KunaisBaneWanted(Core.Me.CurrentTarget))
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Suiton, Core.Me.CurrentTarget);

        }

        #region PrePull

        public static async Task<bool> PrePullSuitonRamp()
        {

            if (!Spells.Suiton.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (!GamelogManagerCountdown.IsCountdownRunning())
                return false;

            if (GamelogManagerCountdown.GetCurrentCooldown() > 6 || GamelogManagerCountdown.GetCurrentCooldown() < 1)
                return false;

            if (NinjaRoutine.UsedMudras.Count >= 3)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Suiton, Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrePullSuitonUse()
        {

            if (!Spells.Suiton.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (!GamelogManagerCountdown.IsCountdownRunning())
                return false;

            if (GamelogManagerCountdown.GetCurrentCooldown() > 1)
                return false;

            if (NinjaRoutine.UsedMudras.Count != 3)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Suiton, Core.Me.CurrentTarget);

        }

        public static async Task<bool> PrePullSuitonUseCheck()
        {
            if (Combat.CombatTime.ElapsedMilliseconds > Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds - 770)
                return false;

            if (!Spells.Ninjutsu.IsKnown())
                return false;

            if (!Spells.Suiton.IsKnownAndReadyAndCastable())
                return false;

            // Only the ramp's own three mudras qualify. The client reports Suiton castable whenever the
            // Ninjutsu button is, so pressing it with fewer mudras up executes Fuma Shuriken instead and
            // burns the charge; on a field pull that happened twice in the first two seconds of most fights.
            if (NinjaRoutine.UsedMudras.Count < 3 || !Core.Me.HasMyAura(Auras.Mudra))
                return false;

            return await Spells.Suiton.Cast(Core.Me.CurrentTarget);

        }

        #endregion

        #region TenChiJin

        public static async Task<bool> TenChiJin()
        {

            if (Core.Me.HasAura(Auras.TenriJindoReady))
                return false;

            //Dont use TCJ when under the affect of kassatsu or in process building a ninjutsu
            if ((Core.Me.HasMyAura(Auras.Kassatsu) || (Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell == Spells.Kassatsu))
                || Core.Me.HasMyAura(Auras.Mudra) || NinjaRoutine.UsedMudras.Count() > 0)
                return false;

            if (!Spells.TenChiJin.IsKnown())
                return false;

            if (!NinjaSettings.Instance.UseTenChiJin)
                return false;

            if (Spells.TrickAttack.Cooldown < new TimeSpan(0, 0, 45))
                return false;

            // Spend a charge on a Raiton first when the mudras are within six seconds of the twenty-second
            // recharge cap (0.3 of a charge); the old integer division made this line refuse only at exactly
            // full charges.
            if (Spells.Chi.Charges >= Spells.Chi.MaxCharges - 0.3)
                return false;

            return await Spells.TenChiJin.CastAura(Core.Me, Auras.TenChiJin);
        }

        public static async Task<bool> TenChiJin_FumaShuriken()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            // Each step is chosen by how many the chain has recorded, exactly - not "at most" - so a
            // press that failed this pulse is retried instead of the next step starting the chain.
            if (NinjaRoutine.UsedMudras.Count() != 0)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.FumaShuriken, Core.Me.CurrentTarget);
        }

        public static async Task<bool> TenChiJin_Raiton()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            if (NinjaRoutine.UsedMudras.Count() != 1)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Raiton, Core.Me.CurrentTarget);
        }

        public static async Task<bool> TenChiJin_Suiton()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            if (NinjaRoutine.UsedMudras.Count() != 2)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Suiton, Core.Me.CurrentTarget);
        }

        #endregion

        #region Kassatsu

        //Missing target count logic
        public static async Task<bool> HyoshoRanryu()
        {

            if (!Spells.HyoshoRanryu.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.Kassatsu))
                return false;

            // Goka Mekkyaku at two targets beats Hyosho since the 7.4 buff (850 x 1.3 on two vs 1300 x 1.3 on one).
            if (AoeControl.Enabled && Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= NinjaSettings.Instance.GokaMekkyakuEnemies)
                return false;

            // Only before the first mudra; a started chain is finished (see Suiton).
            if (NinjaRoutine.UsedMudras.Count == 0 && Cooldown.HoldKassatsuNinjutsuForKunaisBane(Core.Me.CurrentTarget))
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.HyoshoRanryu, Core.Me.CurrentTarget);

        }

        public static async Task<bool> GokaMekkyaku()
        {

            if (!Spells.GokaMekkyaku.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.Kassatsu))
                return false;

            if (!AoeControl.Enabled || Core.Me.CurrentTarget.EnemiesNearby(5).Count() < NinjaSettings.Instance.GokaMekkyakuEnemies)
                return false;

            // Only before the first mudra; a started chain is finished (see Suiton).
            if (NinjaRoutine.UsedMudras.Count == 0 && Cooldown.HoldKassatsuNinjutsuForKunaisBane(Core.Me.CurrentTarget))
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.GokaMekkyaku, Core.Me.CurrentTarget);

        }

        #endregion

        public static async Task<bool> Raiton()
        {

            if (!Spells.Raiton.IsKnown())
                return false;
            if (!Spells.Chi.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || Core.Me.HasAura(Auras.Kassatsu) && Spells.HyoshoRanryu.IsKnown())
                return false;

            if (Spells.Chi.Charges < Spells.Chi.MaxCharges - (Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds / 20000)
                && NinjaRoutine.UsedMudras.Count() == 0
                && Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 45))
                return false;

            if (Core.Me.Auras.Where(x => x.Id == Auras.RaijuReady && x.Value == 2).Count() != 0)
                return false;

            if (Spells.TenChiJin.Cooldown >= new TimeSpan(0, 1, 10) && Core.Me.Auras.Where(x => x.Id == Auras.RaijuReady && x.Value == 1).Count() != 0)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Raiton, Core.Me.CurrentTarget);

        }

        public static async Task<bool> Katon()
        {

            if (!Spells.Katon.IsKnown())
                return false;
            if (!Spells.Ten.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || Core.Me.HasAura(Auras.Kassatsu) && Spells.HyoshoRanryu.IsKnown())
                return false;

            if (Spells.Chi.Charges < Spells.Chi.MaxCharges - (Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds / 20000)
                && NinjaRoutine.UsedMudras.Count() == 0
                && Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 45))
                return false;

            // HARDCODED: Level 90+ rotation adjusts Katon usage based on Mug timing.
            // HARDCODED: Level 90+ rotation adjusts Katon usage based on Mug timing.
            if (Core.Me.ClassLevel >= 90
                && Spells.Mug.Cooldown >= new TimeSpan(0, 1, 40))
                return false;

            if (!AoeControl.Enabled || Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Katon, Core.Me.CurrentTarget);

        }

        public static async Task<bool> Doton()
        {
            if (!Spells.Doton.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.TenChiJin) || Core.Me.HasAura(Auras.Kassatsu) && Spells.HyoshoRanryu.IsKnown())
                return false;

            if (Spells.Chi.Charges < Spells.Chi.MaxCharges - (Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds / 20000)
                && NinjaRoutine.UsedMudras.Count() == 0
                && Spells.TrickAttack.Cooldown <= new TimeSpan(0, 0, 45))
                return false;

            if (!AoeControl.Enabled || Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.Doton))
                return false;

            if (Combat.IsMoving(Core.Me.CurrentTarget))
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Doton, Core.Me);

        }

        public static async Task<bool> FumaShuriken()
        {

            if (!Spells.FumaShuriken.IsKnown())
                return false;

            if (!Spells.Ten.IsKnown())
                return false;

            if (Spells.Raiton.IsKnown())
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.FumaShuriken, Core.Me.CurrentTarget);

        }

    }
}

using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
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

            if (NinjaRoutine.AoeEnemies5Yards <= 2)
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

            if (NinjaRoutine.AoeEnemies5Yards > 2)
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

            if (Spells.TrickAttack.Cooldown < new TimeSpan(0, 0, 45))
                return false;

            if (Spells.Chi.Charges >= (Spells.Chi.MaxCharges - (6000 / 20000)))
                return false;

            return await Spells.TenChiJin.CastAura(Core.Me, Auras.TenChiJin);
        }

        public static async Task<bool> TenChiJin_FumaShuriken()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            if (NinjaRoutine.UsedMudras.Count() >= 1)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.FumaShuriken, Core.Me.CurrentTarget);
        }

        public static async Task<bool> TenChiJin_Raiton()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            if (NinjaRoutine.UsedMudras.Count() >= 2)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.Raiton, Core.Me.CurrentTarget);
        }

        public static async Task<bool> TenChiJin_Suiton()
        {

            if (!Core.Me.HasMyAura(Auras.TenChiJin))
                return false;

            if (NinjaRoutine.UsedMudras.Count() >= 3)
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

            if (!Core.Me.HasAura(Auras.Kassatsu) && (Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell != Spells.Kassatsu))
                return false;

            if (Spells.TrickAttack.Cooldown < new TimeSpan(0, 0, 45))
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= 3)
                return false;

            return await NinjaRoutine.PrepareNinjutsu(Spells.HyoshoRanryu, Core.Me.CurrentTarget);

        }

        public static async Task<bool> GokaMekkyaku()
        {

            if (!Spells.GokaMekkyaku.IsKnown())
                return false;

            if (!Spells.Jin.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.Kassatsu) && (Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell != Spells.Kassatsu))
                return false;

            if (Spells.TrickAttack.Cooldown < new TimeSpan(0, 0, 45))
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
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

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
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

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
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

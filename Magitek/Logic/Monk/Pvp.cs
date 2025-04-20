using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Monk;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Magitek.Logic.Roles;

namespace Magitek.Logic.Monk
{
    internal static class Pvp
    {
        public static async Task<bool> DragonKickPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DragonKickPvp.CanCast())
                return false;

            return await Spells.DragonKickPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> TwinSnakesPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.TwinSnakesPvp.CanCast())
                return false;

            return await Spells.TwinSnakesPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> DemolishPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.DemolishPvp.CanCast())
                return false;

            return await Spells.DemolishPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> LeapingOpoPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.LeapingOpoPvp.CanCast())
                return false;

            return await Spells.LeapingOpoPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> RisingRaptorPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RisingRaptorPvp.CanCast())
                return false;

            return await Spells.RisingRaptorPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> PouncingCoeurlPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PouncingCoeurlPvp.CanCast())
                return false;

            return await Spells.PouncingCoeurlPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> PhantomRushPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PhantomRushPvp.CanCast())
                return false;

            return await Spells.PhantomRushPvp.CastPvpCombo(Spells.PhantomRushPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> FlintsReplyPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            var spell = Spells.FlintsReplyPvp.Masked();

            if (!spell.CanCast())
                return false;

            if (!MonkSettings.Instance.Pvp_FlintsReply)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(spell.Range))
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> WindsReplyPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.WindsReplyPvp.CanCast())
                return false;

            if (!MonkSettings.Instance.Pvp_WindsReply)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.WindsReplyPvp.Range))
                return false;

            return await Spells.WindsReplyPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RisingPhoenixPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RisingPhoenixPvp.CanCast())
                return false;

            if (!MonkSettings.Instance.Pvp_RisingPhoenix)
                return false;

            if (Core.Me.HasAura(Auras.PvpFireResonance))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(6)) < 1)
                return false;

            return await Spells.RisingPhoenixPvp.Cast(Core.Me);
        }

        public static async Task<bool> RiddleofEarthPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.RiddleofEarthPvp.CanCast() && Spells.RiddleofEarthPvp.Masked() == Spells.RiddleofEarthPvp)
                return false;

            if (!MonkSettings.Instance.Pvp_RiddleofEarth)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(10)) < 2)
                return false;

            return await Spells.RiddleofEarthPvp.Cast(Core.Me);
        }

        public static async Task<bool> ThunderclapPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ThunderclapPvp.CanCast())
                return false;

            if (!MonkSettings.Instance.Pvp_Thunderclap)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ThunderclapPvp.Range))
                return false;

            return await Spells.ThunderclapPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EarthsReplyPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.EarthReplyPvp.CanCast())
                return false;

            if (!MonkSettings.Instance.Pvp_EarthReply)
                return false;

            if (!Core.Me.HasAura(Auras.PvpEarthResonance))
                return false;

            if (Core.Me.HasAura(Auras.PvpEarthResonance, true, 5555))
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(6)) < 1)
                return false;

            return await Spells.EarthReplyPvp.Cast(Core.Me);
        }

        public static async Task<bool> MeteodrivePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!MonkSettings.Instance.Pvp_Meteodrive)
                return false;

            if (!Spells.MeteodrivePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.MeteodrivePvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > MonkSettings.Instance.Pvp_MeteodriveHealthPercent)
                return false;

            if (CommonPvp.TooManyAlliesTargeting(MonkSettings.Instance))
                return false;

            return await Spells.MeteodrivePvp.Cast(Core.Me.CurrentTarget);
        }
    }
}

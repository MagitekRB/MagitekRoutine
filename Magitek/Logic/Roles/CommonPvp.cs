using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Roles;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    internal class CommonPvp
    {
        public static bool Attackable()
        {
            return Attackable(Core.Me.CurrentTarget);
        }

        public static bool Attackable(GameObject target)
        {
            if (target == null)
                return false;

            return target.ValidAttackUnit() && target.InLineOfSight();
        }

        public static async Task<bool> CommonTasks<T>(T settings) where T : JobSettings
        {
            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            if (Core.Me.HasAura(Auras.PvpGuard))
                return true;

            if (await Guard(settings))
                return true;

            if (await Purify(settings))
                return true;

            if (await Recuperate(settings))
                return true;

            if (await Sprint(settings))
                return true;

            return false;
        }

        public static async Task<bool> Sprint<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_SprintWithoutTarget)
                return false;

            if (Core.Me.HasAnyAura(Auras.Invincibility))
                return false;

            if (Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasTarget
                && Core.Me.CurrentTarget.CanAttack
                && Core.Me.CurrentTarget.InLineOfSight()
                && (Core.Me.IsMeleeDps() || Core.Me.IsTank() ? Core.Me.CurrentTarget.Distance() < 7 : Core.Me.CurrentTarget.Distance() < 25))
                return false;

            if (Core.Me.HasAura(Auras.PvpSprint))
                return false;

            if (!Spells.SprintPvp.CanCast())
                return false;

            if (WorldManager.ZoneId == 250)
                return false;

            return await Spells.SprintPvp.CastAura(Core.Me, Auras.PvpSprint);
        }

        public static async Task<bool> Guard<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UseGuard)
                return false;

            if (!Spells.Guard.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > settings.Pvp_GuardHealthPercent)
                return false;

            if (Core.Me.HasAnyAura(new uint[] { Auras.PvpHallowedGround, Auras.PvpUndeadRedemption }))
                return false;

            if (!await Spells.Guard.CastAura(Core.Me, Auras.PvpGuard))
                return false;

            return await Coroutine.Wait(1500, () => Core.Me.HasAura(Auras.PvpGuard, true));
        }

        public static bool GuardCheck<T>(T settings, bool checkGuard = true, bool checkInvuln = true) where T : JobSettings
        {
            return GuardCheck(settings, Core.Me.CurrentTarget, checkGuard, checkInvuln);
        }

        public static bool GuardCheck<T>(T settings, GameObject target, bool checkGuard = true, bool checkInvuln = true) where T : JobSettings
        {
            if (target == null)
                return true;

            return !Attackable(target)
                || (checkGuard && settings.Pvp_GuardCheck && target.HasAura(Auras.PvpGuard))
                || (checkInvuln && settings.Pvp_InvulnCheck && target.HasAnyAura(new uint[] { Auras.PvpHallowedGround, Auras.PvpUndeadRedemption }));
        }

        public static async Task<bool> Purify<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UsePurify)
                return false;

            if (!Spells.Purify.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura("Stun") && !Core.Me.HasAura("Heavy") && !Core.Me.HasAura("Bind") && !Core.Me.HasAura("Silence") && !Core.Me.HasAura("Half-asleep") && !Core.Me.HasAura("Sleep") && !Core.Me.HasAura("Deep Freeze"))
                return false;

            return await Spells.Purify.Cast(Core.Me);
        }


        public static async Task<bool> Recuperate<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UseRecuperate)
                return false;

            if (!Spells.Recuperate.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > settings.Pvp_RecuperateHealthPercent)
                return false;

            if (Core.Me.HasAura(Auras.PvpHallowedGround))
                return false;

            if (Core.Me.HasAura(Auras.PvpUndeadRedemption, true) && !Core.Me.HasAura(Auras.PvpUndeadRedemption, true, 3500))
                return false;

            return await Spells.Recuperate.Cast(Core.Me);
        }

    }
}

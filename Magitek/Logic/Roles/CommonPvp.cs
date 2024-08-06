﻿using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Roles;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    internal class CommonPvp
    {
        public static bool Attackable()
        {
            return Core.Me.CurrentTarget.ValidAttackUnit() && Core.Me.CurrentTarget.InLineOfSight();
        }

        public static async Task<bool> CommonTasks<T>(T settings) where T : JobSettings
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return true;

            if (await Sprint(settings))
                return true;

            if (await Guard(settings))
                return true;

            if (await Purify(settings))
                return true;

            if (await Recuperate(settings))
                return true;

            return false;
        }

        public static async Task<bool> Sprint<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_SprintWithoutTarget)
                return false;

            if (Core.Me.HasAnyAura(Auras.Invincibility))
                return false;

            if (Core.Me.HasTarget 
                && Core.Me.CurrentTarget.CanAttack 
                && Core.Me.CurrentTarget.InLineOfSight()
                && Core.Me.CurrentTarget.Distance() < 26)
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

            if (!await Spells.Guard.CastAura(Core.Me, Auras.PvpGuard))
                return false;

            return await Coroutine.Wait(1500, () => Core.Me.HasAura(Auras.PvpGuard, true));
        }

        public static bool GuardCheck<T>(T settings, bool checkGuard = true, bool checkInvuln = true) where T : JobSettings
        {
            return !Attackable()
                || (checkGuard && settings.Pvp_GuardCheck && Core.Me.CurrentTarget.HasAura(Auras.PvpGuard))
                || (checkInvuln && settings.Pvp_InvulnCheck && Core.Me.CurrentTarget.HasAnyAura(new uint[] {Auras.PvpHallowedGround, Auras.PvpUndeadRedemption}));
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

            return await Spells.Recuperate.Cast(Core.Me);
        }
    }
}

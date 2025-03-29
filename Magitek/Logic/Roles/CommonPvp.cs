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

            if (await RoleAction(settings))
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

        public static async Task<bool> RoleAction<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UseRoleActions)
                return false;

            if (!Spells.PvPRoleAction.CanCast())
                return false;

            var selectedAction = Spells.PvPRoleAction.Masked();

            // However ideally, most role actions would be better integrated into
            // the rotation logic, such as bravery being combined with chain saw.
            // This is just a quick and easy way to get role actions working.
            
            if (selectedAction == Spells.RoleDervishPvp)
                return await CastDervish(settings);
                
            if (selectedAction == Spells.RoleBraveryPvp)
                return await CastBravery(settings);
                
            if (selectedAction == Spells.RoleEageEyeShot)
                return await CastEagleEyeShot(settings);

            if (selectedAction == Spells.RoleRampage)
                return await CastRampage(settings);

            if (selectedAction == Spells.RoleRampart)
                return await CastRampart(settings);

            if (selectedAction == Spells.RoleFullSwing)
                return await CastFullSwing(settings);

            if (selectedAction == Spells.RoleHaelan)
                return await CastHaelan(settings);

            if (selectedAction == Spells.RoleStoneskinII)
                return await CastStoneskinII(settings);

            if (selectedAction == Spells.RoleDiabrosis)
                return await CastDiabrosis(settings);

            if (selectedAction == Spells.RoleBloodbath)
                return await CastBloodbath(settings);

            if (selectedAction == Spells.RoleSwift)
                return await CastSwift(settings);

            if (selectedAction == Spells.RoleSmite)
                return await CastSmite(settings);

            if (selectedAction == Spells.RoleComet)
                return await CastComet(settings);

            if (selectedAction == Spells.RolePhantomDart)
                return await CastPhantomDart(settings);

            if (selectedAction == Spells.RoleRust)
                return await CastRust(settings);

            return false;
        }
        
        private static async Task<bool> CastDervish<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > 60)
                return false;

            return await Spells.RoleDervishPvp.Cast(Core.Me);
        }
        
        private static async Task<bool> CastBravery<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 25)
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > 60)
                return false;

            return await Spells.RoleBraveryPvp.Cast(Core.Me);
        }
        
        private static async Task<bool> CastEagleEyeShot<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Spells.PvPRoleAction.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 40)
                return false;

            return await Spells.PvPRoleAction.Cast(Core.Me.CurrentTarget);
        }    

        private static async Task<bool> CastRampage<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleRampage.Radius)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleRampage.Radius) < 3)
                return false;

            return await Spells.RoleRampage.Cast(Core.Me);
        }

        private static async Task<bool> CastRampart<T>(T settings) where T : JobSettings
        {
            if (Core.Me.CurrentHealthPercent > 70)
                return false;

            return await Spells.RoleRampart.Cast(Core.Me);
        }

        private static async Task<bool> CastFullSwing<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleFullSwing.Range)
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RoleFullSwing.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastHaelan<T>(T settings) where T : JobSettings
        {
            if (!Spells.RoleHaelan.CanCast())
                return false;

            // Save mana for self-recuperate
            if (Core.Me.CurrentMana <= 4500)
                return false;

            var healTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 65);

            if (healTarget == null)
                return false;

            return await Spells.RoleHaelan.Cast(healTarget);
        }

        private static async Task<bool> CastStoneskinII<T>(T settings) where T : JobSettings
        {
            var target = Group.CastableAlliesWithin15.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 80);

            if (target == null)
            {
                if (Core.Me.CurrentHealthPercent <= 80)
                    return await Spells.RoleStoneskinII.Cast(Core.Me);
                return false;
            }

            return await Spells.RoleStoneskinII.Cast(Core.Me);
        }

        private static async Task<bool> CastDiabrosis<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleDiabrosis.Range)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent <= 35)
                return await Spells.RoleDiabrosis.Cast(Core.Me.CurrentTarget);

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleDiabrosis.Radius) < 3)
                return false;

            return await Spells.RoleDiabrosis.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastBloodbath<T>(T settings) where T : JobSettings
        {
            if (Core.Me.CurrentHealthPercent > 85)
                return false;
                
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 15)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RoleBloodbath.Cast(Core.Me);
        }

        private static async Task<bool> CastSwift<T>(T settings) where T : JobSettings
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 10)
                return false;

            if (Core.Me.HasAura(Auras.PvpSprint))
                return false;

            return await Spells.RoleSwift.Cast(Core.Me);
        }

        private static async Task<bool> CastSmite<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleSmite.Range)
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > 30)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RoleSmite.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastComet<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleComet.Range)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleComet.Radius) < 4)
                return false;

            return await Spells.RoleComet.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastPhantomDart<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RolePhantomDart.Range)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RolePhantomDart.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastRust<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.RoleRust.Range)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleRust.Radius) < 3)
                return false;

            return await Spells.RoleRust.Cast(Core.Me.CurrentTarget);
        }
    }
}

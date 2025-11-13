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
using System.Collections.Generic;
using System;

namespace Magitek.Logic.Roles
{
    internal class CommonPvp
    {
        // Target tracking for shared high-impact abilities
        private static Dictionary<uint, int> TargetCounts { get; set; } = new Dictionary<uint, int>();
        private static DateTime LastTargetCountUpdate { get; set; } = DateTime.MinValue;
        private static readonly TimeSpan TargetCountUpdateInterval = TimeSpan.FromMilliseconds(500); // Update every 500ms

        // Mount/Dismount debounce tracking with exponential backoff
        private static DateTime LastMountStateChange { get; set; } = DateTime.MinValue;
        private static int ConsecutiveMountStateChanges { get; set; } = 0;
        private static readonly TimeSpan BaseMountDebounceInterval = TimeSpan.FromSeconds(2); // Base 2 second cooldown
        private static readonly TimeSpan MaxMountDebounceInterval = TimeSpan.FromSeconds(15); // Max 15 second cooldown
        private static readonly TimeSpan ResetThreshold = TimeSpan.FromSeconds(30); // Reset counter if stable for 30 seconds
        private static readonly float EmergencyDismountRange = 30f; // Emergency dismount if enemy within 30 yalms (ignores debounce)

        public static void UpdateTargetCounts()
        {
            var now = DateTime.Now;
            if (now - LastTargetCountUpdate < TargetCountUpdateInterval)
                return;

            TargetCounts.Clear();

            // Count all allies' targets
            // Process party members
            foreach (var ally in Group.CastableParty)
            {
                if (ally.TargetGameObject != null && ally.TargetGameObject.CanAttack)
                {
                    uint targetId = ally.TargetGameObject.ObjectId;
                    if (TargetCounts.ContainsKey(targetId))
                        TargetCounts[targetId]++;
                    else
                        TargetCounts[targetId] = 1;
                }
            }

            // Process alliance members
            foreach (var ally in Group.AllianceMembers)
            {
                if (ally.TargetGameObject != null && ally.TargetGameObject.CanAttack)
                {
                    uint targetId = ally.TargetGameObject.ObjectId;
                    if (TargetCounts.ContainsKey(targetId))
                        TargetCounts[targetId]++;
                    else
                        TargetCounts[targetId] = 1;
                }
            }

            // Add our own target to the count
            if (Core.Me.TargetGameObject != null && Core.Me.TargetGameObject.CanAttack)
            {
                uint targetId = Core.Me.TargetGameObject.ObjectId;
                if (TargetCounts.ContainsKey(targetId))
                    TargetCounts[targetId]++;
                else
                    TargetCounts[targetId] = 1;
            }

            LastTargetCountUpdate = now;
        }

        /// <summary>
        /// Checks if too many allies are targeting the specified GameObject.
        /// </summary>
        /// <typeparam name="T">The JobSettings type</typeparam>
        /// <param name="settings">The job settings</param>
        /// <param name="target">The target to check</param>
        /// <returns>True if too many allies are targeting this enemy, false otherwise</returns>
        public static bool TooManyAlliesTargeting<T>(T settings, GameObject target) where T : JobSettings
        {
            if (target == null)
                return true;

            UpdateTargetCounts();

            uint targetId = target.ObjectId;
            int targetCount = TargetCounts.ContainsKey(targetId) ? TargetCounts[targetId] : 0;

            return targetCount > settings.Pvp_MaxAlliesTargetingLimit;
        }

        /// <summary>
        /// Checks if too many allies are targeting the current target.
        /// </summary>
        /// <typeparam name="T">The JobSettings type</typeparam>
        /// <param name="settings">The job settings</param>
        /// <returns>True if too many allies are targeting the current target, false otherwise</returns>
        public static bool TooManyAlliesTargeting<T>(T settings) where T : JobSettings
        {
            return TooManyAlliesTargeting(settings, Core.Me.CurrentTarget);
        }

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

            if (await Mount(settings))
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

            if (!MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAnyAura(Auras.Invincibility))
                return false;

            if (Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (Core.Me.HasTarget
                && Core.Me.CurrentTarget.CanAttack
                && Core.Me.CurrentTarget.InLineOfSight()
                && Core.Me.CurrentTarget.InActualView()
                && (Core.Me.IsMeleeDps() || Core.Me.IsTank() ? Core.Me.CurrentTarget.WithinSpellRange(7) : Core.Me.CurrentTarget.WithinSpellRange(30)))
                return false;

            if (Core.Me.HasAura(Auras.PvpSprint))
                return false;

            if (!Spells.SprintPvp.CanCast())
                return false;

            if (WorldManager.ZoneId == 250)
                return false;

            return await Spells.SprintPvp.CastAura(Core.Me, Auras.PvpSprint);
        }

        public static async Task<bool> Mount<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UseMount)
                return false;

            if (Core.Me.HasAnyAura(Auras.Invincibility) && !MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpHidden))
                return false;

            if (WorldManager.ZoneId == 250)
                return false;

            var now = DateTime.Now;

            // Reset consecutive changes counter if we've been stable for a while
            if (now - LastMountStateChange > ResetThreshold)
            {
                ConsecutiveMountStateChanges = 0;
            }

            // EMERGENCY DISMOUNT: If enemy is dangerously close while mounted, dismount immediately
            // This ignores debounce to prevent getting mount-stunned and killed
            if (Core.Me.IsMounted && Combat.Enemies.Any(x => x.WithinSpellRange(EmergencyDismountRange)))
            {
                ActionManager.Dismount();
                LastMountStateChange = now;
                ConsecutiveMountStateChanges++;
                return true;
            }

            // Calculate current debounce interval with exponential backoff
            // Formula: BaseInterval * 2^(consecutiveChanges) capped at MaxInterval
            var currentDebounceInterval = TimeSpan.FromSeconds(
                Math.Min(
                    BaseMountDebounceInterval.TotalSeconds * Math.Pow(2, ConsecutiveMountStateChanges),
                    MaxMountDebounceInterval.TotalSeconds
                )
            );

            // Check debounce - prevent rapid mount/dismount spam with exponential backoff
            if (now - LastMountStateChange < currentDebounceInterval)
                return false;

            // If we're already mounted, check if we need to dismount (normal range)
            if (Core.Me.IsMounted)
            {
                if (Combat.Enemies.Any(x => x.WithinSpellRange(45)))
                {
                    ActionManager.Dismount();
                    LastMountStateChange = now;
                    ConsecutiveMountStateChanges++;
                    return true;
                }
                return false;
            }

            // If we're not mounted and conditions are good, mount up
            if (!Core.Me.IsMounted
                && !MovementManager.IsOccupied
                && ActionManager.CanMount == 0
                && MovementManager.IsMoving)
            {
                if (Combat.Enemies.Any(x => x.WithinSpellRange(70)))
                    return false;

                ActionManager.Mount();
                LastMountStateChange = now;
                ConsecutiveMountStateChanges++;
                return true;
            }

            return false;
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > 60)
                return false;

            return await Spells.RoleDervishPvp.Cast(Core.Me);
        }

        private static async Task<bool> CastBravery<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(40))
                return false;

            return await Spells.PvPRoleAction.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastRampage<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleRampage.Radius))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleFullSwing.Range))
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

            // Check party first
            var healTarget = Group.CastableParty
                .Where(r => r.Distance(Core.Me) <= 30)
                .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 65);

            // If no suitable target in party, check alliance
            if (healTarget == null)
            {
                healTarget = Group.AllianceMembers
                    .Where(r => r.Distance(Core.Me) <= 30)
                    .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 65);
            }

            if (healTarget == null)
                return false;

            return await Spells.RoleHaelan.Cast(healTarget);
        }

        private static async Task<bool> CastStoneskinII<T>(T settings) where T : JobSettings
        {
            // Check party first
            var target = Group.CastableParty
                .Where(r => r.Distance(Core.Me) <= 15)
                .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 80);

            // If no suitable target in party, check alliance
            if (target == null)
            {
                target = Group.AllianceMembers
                    .Where(r => r.Distance(Core.Me) <= 15)
                    .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= 80);
            }

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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleDiabrosis.Range))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(15))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RoleBloodbath.Cast(Core.Me);
        }

        private static async Task<bool> CastSwift<T>(T settings) where T : JobSettings
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(10))
                return false;

            if (Core.Me.HasAura(Auras.PvpSprint))
                return false;

            return await Spells.RoleSwift.Cast(Core.Me);
        }

        private static async Task<bool> CastSmite<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleSmite.Range))
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

            if (MovementManager.IsMoving)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleComet.Range))
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

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RolePhantomDart.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.RolePhantomDart.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastRust<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleRust.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleRust.Radius) < 3)
                return false;

            return await Spells.RoleRust.Cast(Core.Me.CurrentTarget);
        }
    }
}

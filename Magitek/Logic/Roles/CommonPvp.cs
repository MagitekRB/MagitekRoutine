using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
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
            // Update PvP aggro count overlay
            PvpAggroCountTracker.UpdateAggroCount();

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            if (Core.Me.HasAura(Auras.PvpGuard))
                return true;

            if (await AutoGuard(settings))
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
            if (!Models.Account.BaseSettings.Instance.Pvp_SprintWithoutTarget)
                return false;

            if (!MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAnyAura(Auras.Invincibility))
                return false;

            if (Core.Me.HasAura(Auras.PvpHidden))
                return false;

            // Don't sprint if current target is nearby and in LoS (prevents interrupting melee rotations during brawls)
            // Check regardless of whether we're "looking at them" - if they're close and visible, don't sprint
            if (Core.Me.HasTarget
                && Core.Me.CurrentTarget.CanAttack
                && Core.Me.CurrentTarget.InLineOfSight()
                // && Core.Me.CurrentTarget.InActualView() removed b/c it was causing issues with brawls
                && (Core.Me.IsMeleeDps() || Core.Me.IsTank() ? Core.Me.CurrentTarget.WithinSpellRange(15) : Core.Me.CurrentTarget.WithinSpellRange(25)))
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
            if (!Models.Account.BaseSettings.Instance.Pvp_UseMount)
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

        public static async Task<bool> AutoGuard<T>(T settings) where T : JobSettings
        {
            if (!settings.Pvp_UseGuard)
                return false;

            if (!Spells.Guard.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAnyAura(new uint[] { Auras.PvpHallowedGround, Auras.PvpUndeadRedemption }))
                return false;

            bool shouldAutoGuard = false;

            // NOTE: Marksman's Spite auto-guard is not implemented
            // The combat log doesn't log until after damage is applied, making it too late to guard.
            // Detection would require network packet analysis (like ACT) which isn't available in RBB.
            // or Detect AVFX file ID if we can identify which one corresponds to Marksman's Spite (penumbra).

            // Check if we have WildfirePvp debuff with 1.5 seconds or less remaining
            if (!shouldAutoGuard && settings.Pvp_AutoGuardWildfire)
            {
                if (Core.Me.HasAuraExpiringWithin(Auras.PvpWildfire, msRemaining: 1500))
                    shouldAutoGuard = true;
            }

            // Check if we have Kuzushi debuff (about to be hit by Zantetsuken)
            // Zantetsuken ignores Guard, so our only recourse is to heal to full HP
            // Recuperate if there's an enemy Samurai within Zantetsuken range targeting us
            if (!shouldAutoGuard && settings.Pvp_AutoGuardKuzushi)
            {
                if (Core.Me.HasAura(Auras.PvpKuzushi))
                {
                    var enemySamurai = Combat.Enemies.FirstOrDefault(e =>
                    {
                        try
                        {
                            return e != null &&
                                   e.IsValid &&
                                   e is BattleCharacter bc &&
                                   bc.CurrentJob == ClassJobType.Samurai &&
                                   e.WithinSpellRange(Spells.ZantetsukenPvp.Range) &&
                                   e.TargetGameObject == Core.Me;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    if (enemySamurai != null)
                    {
                        // Zantetsuken ignores Guard - our only defense is to heal to full HP
                        // Recuperate if not at max HP
                        if (Core.Me.CurrentHealthPercent < 100.0f)
                        {
                            if (settings.Pvp_UseRecuperate && Spells.Recuperate.CanCast())
                            {
                                return await Spells.Recuperate.Cast(Core.Me);
                            }
                        }
                        // Already at max HP or can't recuperate - nothing we can do
                        return false;
                    }
                }
            }

            // Add more auto-guard conditions here in the future

            if (!shouldAutoGuard)
                return false;

            if (!await Spells.Guard.CastAura(Core.Me, Auras.PvpGuard))
                return false;

            return await Coroutine.Wait(1500, () => Core.Me.HasAura(Auras.PvpGuard, true));
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
                || (checkGuard && Models.Account.BaseSettings.Instance.Pvp_GuardCheck && target.HasAura(Auras.PvpGuard))
                || (checkInvuln && Models.Account.BaseSettings.Instance.Pvp_InvulnCheck && target.HasAnyAura(new uint[] { Auras.PvpHallowedGround, Auras.PvpUndeadRedemption, Auras.PvpHardenedScales, Auras.PvpArmoredScales }));
        }

        public static bool ShouldUseBurst()
        {
            // Check global "Hold Burst" toggle
            if (Models.Account.BaseSettings.Instance.Pvp_HoldBurst)
                return false;

            // Check if target is Warmachina and global setting says don't burst on them
            if (Core.Me.CurrentTarget != null &&
                Core.Me.CurrentTarget.IsWarMachina() &&
                !Models.Account.BaseSettings.Instance.Pvp_UseBurstOnWarmachina)
                return false;

            return true;
        }

        /// <summary>
        /// Check if a target is mounted in PvP (Warmachina mount status)
        /// </summary>
        public static bool IsPvpMounted(GameObject target)
        {
            if (target == null)
                return false;

            return target.HasAura(Auras.MountedPvp);
        }

        private static readonly Dictionary<uint, double> TargetAuras = new Dictionary<uint, double>
        {
            // Auras on target that increase damage they take (vulnerability debuffs)
            { Auras.PvpGuard, 0.10 }, // Guard - reduces damage taken by 90%
            { Auras.PvpChainSaw, 1.2 }, // MCH - increases damage taken by 20%
            { Auras.PvpRampage, 1.25 }, // Tank role action - increases damage taken by 25%
            { Auras.PvpPhantomDart, 1.25 }, // BLM - increases damage taken by 25%
            { Auras.PvpLordOfCrowns, 1.10 }, // AST - increases damage taken by 10%
            { Auras.PvpMonomachy, 1.10 }, // RDM Corps-a-Corps - increases damage target receives from you by 10%
            
            // Auras on target that reduce damage they take (mitigation)
            { Auras.PvpBravery, 0.75 }, // Role action - reduces damage taken by 25%
            { Auras.PvpLadyOfCrowns, 0.90 }, // AST - reduces damage taken by 10%
        };

        private static readonly Dictionary<uint, double> SelfAuras = new Dictionary<uint, double>
        {
            // Auras on self that increase damage dealt
            { Auras.PvpBravery, 1.25 }, // Role action - increases damage dealt by 25%
            { Auras.PvpDisplacement, 1.15 }, // RDM - increases next spell's damage by 15%
            { Auras.PvpCelestialRiver, 1.30 }, // AST LB - increases damage dealt by 30%
        };

        /// <summary>
        /// Checks if an attack with the given potency would kill the target.
        /// </summary>
        /// <param name="potency">The potency of the attack</param>
        /// <param name="target">Target to check (defaults to CurrentTarget)</param>
        /// <param name="damageMultiplier">Optional additional multiplier from caller</param>
        /// <param name="ignoreGuard">If true, ignores Guard aura damage reduction (for spells that ignore Guard)</param>
        /// <returns>True if estimated damage would kill the target</returns>
        public static bool WouldKillWithPotency(double potency, GameObject target = null, double damageMultiplier = 1.0, bool ignoreGuard = false)
        {
            if (target == null)
                target = Core.Me.CurrentTarget;

            if (target == null || !target.ValidAttackUnit())
                return false;

            const double PvpDamageConversionFactor = 1.0;
            double estimatedDamage = potency * PvpDamageConversionFactor;

            estimatedDamage *= damageMultiplier;

            foreach (var aura in TargetAuras)
            {
                // Skip Guard aura if ignoreGuard is true
                if (ignoreGuard && aura.Key == Auras.PvpGuard)
                    continue;

                if (target.HasAura(aura.Key))
                {
                    estimatedDamage *= aura.Value;
                }
            }

            foreach (var aura in SelfAuras)
            {
                if (Core.Me.HasAura(aura.Key))
                {
                    estimatedDamage *= aura.Value;
                }
            }

            if (target.IsTank())
            {
                estimatedDamage *= 0.8;
            }

            return target.CurrentHealth <= estimatedDamage;
        }

        /// <summary>
        /// Finds a killable target within spell range that would be killed by the given potency.
        /// </summary>
        /// <typeparam name="T">The JobSettings type</typeparam>
        /// <param name="settings">The job settings</param>
        /// <param name="spell">The spell to cast</param>
        /// <param name="potency">The potency of the attack</param>
        /// <param name="range">The range of the spell</param>
        /// <param name="ignoreGuard">If true, ignores Guard aura damage reduction</param>
        /// <param name="checkGuard">If false, skips Guard check when filtering targets</param>
        /// <param name="searchAllTargets">If true, searches all enemies in range, not just current target</param>
        /// <param name="potencyCalculator">Optional function to calculate potency per target (for scaling potency)</param>
        /// <returns>The killable target, or null if none found</returns>
        public static GameObject FindKillableTargetInRange<T>(
            T settings,
            double potency,
            float range,
            bool ignoreGuard = false,
            bool checkGuard = true,
            bool searchAllTargets = false,
            Func<GameObject, double> potencyCalculator = null) where T : JobSettings
        {
            // First check current target if valid
            if (Core.Me.CurrentTarget != null && Core.Me.CurrentTarget.ValidAttackUnit() && Core.Me.CurrentTarget.InLineOfSight())
            {
                if (Core.Me.CurrentTarget.WithinSpellRange(range))
                {
                    // Check Guard if required
                    if (checkGuard && GuardCheck(settings, Core.Me.CurrentTarget))
                        return null; // Skip guarded target

                    double targetPotency = potencyCalculator != null ? potencyCalculator(Core.Me.CurrentTarget) : potency;
                    if (WouldKillWithPotency(targetPotency, Core.Me.CurrentTarget, ignoreGuard: ignoreGuard))
                    {
                        return Core.Me.CurrentTarget;
                    }
                }
            }

            // Only search all targets if the setting is enabled
            if (!searchAllTargets)
                return null;

            // Search for any killable target in range
            var nearby = Combat.Enemies
                .Where(e => e.WithinSpellRange(range)
                        && e.ValidAttackUnit()
                        && e.InLineOfSight()
                        && !e.IsWarMachina()
                        && (!checkGuard || !GuardCheck(settings, e)))
                .OrderBy(e => e.Distance(Core.Me));

            foreach (var target in nearby)
            {
                double targetPotency = potencyCalculator != null ? potencyCalculator(target) : potency;
                if (WouldKillWithPotency(targetPotency, target, ignoreGuard: ignoreGuard))
                {
                    return target;
                }
            }

            return null;
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

            if (Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_DervishTargetHealthPercent)
                return false;

            return await Spells.RoleDervishPvp.Cast(Core.Me);
        }

        private static async Task<bool> CastBravery<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            if (Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_BraveryTargetHealthPercent)
                return false;

            return await Spells.RoleBraveryPvp.Cast(Core.Me);
        }

        private static async Task<bool> CastEagleEyeShot<T>(T settings) where T : JobSettings
        {
            if (!Spells.PvPRoleAction.CanCast())
                return false;

            // Eagle Eye Shot: 12,000 potency, ignores Guard, 40y range
            const double potency = 12000;
            const float range = 40f;

            // Find killable target in range (Eagle Eye Shot ignores Guard)
            var killableTarget = FindKillableTargetInRange(
                settings,
                potency,
                range,
                ignoreGuard: true,
                checkGuard: false, // Eagle Eye Shot ignores Guard
                searchAllTargets: Models.Account.BaseSettings.Instance.Pvp_EagleEyeShotAnyTarget);

            if (killableTarget != null)
            {
                return await Spells.PvPRoleAction.Cast(killableTarget);
            }

            // Fallback to HP threshold if WouldKill is disabled or target not killable
            if (!Models.Account.BaseSettings.Instance.Pvp_UseEagleEyeShotForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(range))
                    return false;

                if (Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_EagleEyeShotTargetHealthPercent)
                    return false;

                return await Spells.PvPRoleAction.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        private static async Task<bool> CastRampage<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleRampage.Radius))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleRampage.Radius) < Models.Account.BaseSettings.Instance.Pvp_RampageAoeCount)
                return false;

            return await Spells.RoleRampage.Cast(Core.Me);
        }

        private static async Task<bool> CastRampart<T>(T settings) where T : JobSettings
        {
            if (Core.Me.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_RampartHealthPercent)
                return false;

            if (!Combat.Enemies.Any(x => x.WithinSpellRange(Models.Account.BaseSettings.Instance.Pvp_RampartEnemyRange)))
                return false;

            return await Spells.RoleRampart.Cast(Core.Me);
        }

        private static async Task<bool> CastFullSwing<T>(T settings) where T : JobSettings
        {
            if (!Core.Me.HasTarget)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleFullSwing.Range))
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.PvpGuard) && Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_FullSwingTargetHealthPercent)
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
            if (Core.Me.CurrentMana <= Models.Account.BaseSettings.Instance.Pvp_HaelanMinimumMana)
                return false;

            // Check party first
            var healTarget = Group.CastableParty
                .Where(r => r.Distance(Core.Me) <= 30)
                .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_HaelanTargetHealthPercent);

            // If no suitable target in party, check alliance
            if (healTarget == null)
            {
                healTarget = Group.AllianceMembers
                    .Where(r => r.Distance(Core.Me) <= 30)
                    .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_HaelanTargetHealthPercent);
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
                .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_StoneskinIITargetHealthPercent);

            // If no suitable target in party, check alliance
            if (target == null)
            {
                target = Group.AllianceMembers
                    .Where(r => r.Distance(Core.Me) <= 15)
                    .FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_StoneskinIITargetHealthPercent);
            }

            if (target == null)
            {
                if (Core.Me.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_StoneskinIITargetHealthPercent)
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

            if (Core.Me.CurrentTarget.CurrentHealthPercent <= Models.Account.BaseSettings.Instance.Pvp_DiabrosisTargetHealthPercent)
                return await Spells.RoleDiabrosis.Cast(Core.Me.CurrentTarget);

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleDiabrosis.Radius) < Models.Account.BaseSettings.Instance.Pvp_DiabrosisAoeCount)
                return false;

            return await Spells.RoleDiabrosis.Cast(Core.Me.CurrentTarget);
        }

        private static async Task<bool> CastBloodbath<T>(T settings) where T : JobSettings
        {
            if (Core.Me.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_BloodbathHealthPercent)
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
            if (!Spells.RoleSmite.CanCast())
                return false;

            // Smite: 6,000 base potency, scales up to 18,000 at 25% HP or less
            // Potency calculator function for scaling based on target HP
            Func<GameObject, double> potencyCalculator = (target) =>
            {
                if (target.CurrentHealthPercent <= 25)
                {
                    return 18000; // Max potency at 25% HP or less
                }
                else
                {
                    // Linear scaling from 100% HP (6000) to 25% HP (18000)
                    double hpPercent = target.CurrentHealthPercent;
                    double scaleFactor = (100 - hpPercent) / 75.0; // 0 at 100% HP, 1 at 25% HP
                    return 6000 + (scaleFactor * 12000); // Scale from 6000 to 18000
                }
            };

            // Find killable target in range (handles target validation internally)
            var killableTarget = FindKillableTargetInRange(
                settings,
                6000, // Base potency (will be calculated per target)
                (float)Spells.RoleSmite.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: Models.Account.BaseSettings.Instance.Pvp_SmiteAnyTarget,
                potencyCalculator: potencyCalculator);

            if (killableTarget != null)
            {
                return await Spells.RoleSmite.Cast(killableTarget);
            }

            // Fallback to HP threshold if WouldKill is disabled or target not killable
            if (!Models.Account.BaseSettings.Instance.Pvp_SmiteForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.RoleSmite.Range))
                    return false;

                if (Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_SmiteTargetHealthPercent)
                    return false;

                return await Spells.RoleSmite.Cast(Core.Me.CurrentTarget);
            }

            return false;
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

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleComet.Radius) < Models.Account.BaseSettings.Instance.Pvp_CometAoeCount)
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

            if (Core.Me.CurrentTarget.CurrentHealthPercent > Models.Account.BaseSettings.Instance.Pvp_PhantomDartTargetHealthPercent)
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

            if (Combat.Enemies.Count(x => x.Distance(Core.Me.CurrentTarget) < Spells.RoleRust.Radius) < Models.Account.BaseSettings.Instance.Pvp_RustAoeCount)
                return false;

            return await Spells.RoleRust.Cast(Core.Me.CurrentTarget);
        }
    }
}

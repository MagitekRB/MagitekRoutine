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
        /// <summary>
        /// PvP mode types based on party composition
        /// </summary>
        public enum PvpMode
        {
            CrystallineConflict,  // 5 players total (4 other allies) - no modifiers
            HiddenGorgeRivalWings, // 4 players total (3 other allies) - uses Frontline modifiers for now
            Frontline            // 8+ players (more than 4 other allies) - applies job-specific modifiers
        }

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

        // PvP invulnerability auras that should be avoided (PLD Hallowed Ground, Viper Hardened/Armored Scales, SAM Chiten)
        private static readonly uint[] InvulnerabilityAuras = new uint[]
        {
            Auras.PvpHallowedGround,
            Auras.PvpUndeadRedemption,
            Auras.PvpHardenedScales,
            Auras.PvpArmoredScales,
            Auras.PvpChiten
        };

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
                || (checkInvuln && Models.Account.BaseSettings.Instance.Pvp_InvulnCheck && target.HasAnyAura(InvulnerabilityAuras));
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

        /// <summary>
        /// Detects the current PvP mode based on party size.
        /// </summary>
        /// <returns>The detected PvP mode</returns>
        public static PvpMode GetPvpMode()
        {
            // Count allies (excluding self)
            int allyCount = (int)PartyManager.NumMembers - 1;

            if (allyCount == 4)
                return PvpMode.CrystallineConflict; // 5 players total (4 other allies)
            else if (allyCount == 3)
                return PvpMode.HiddenGorgeRivalWings; // 4 players total (3 other allies)
            else if (allyCount > 4)
                return PvpMode.Frontline; // 8+ players (more than 4 other allies)

            return PvpMode.CrystallineConflict;
        }

        /// <summary>
        /// Frontline job modifiers (Patch 7.4).
        /// Damage Dealt: percentage modifier (e.g., -10% = 0.90 multiplier, 0% = 1.0 multiplier)
        /// Damage Taken: percentage modifier (e.g., -50% = 0.50 multiplier, 0% = 1.0 multiplier)
        /// Values are stored as multipliers (1.0 = no change, 0.90 = -10%, 0.50 = -50%)
        /// </summary>
        private static readonly Dictionary<ClassJobType, (double DamageDealtMultiplier, double DamageTakenMultiplier)> FrontlineJobModifiers = new Dictionary<ClassJobType, (double, double)>
        {
            // Tanks
            { ClassJobType.Paladin, (1.00, 0.50) },      // 0% dealt, -50% taken
            { ClassJobType.Warrior, (0.90, 0.50) },      // -10% dealt, -50% taken
            { ClassJobType.DarkKnight, (0.85, 0.60) },  // -15% dealt, -40% taken
            { ClassJobType.Gunbreaker, (1.00, 0.45) },  // 0% dealt, -55% taken

            // Melee DPS
            { ClassJobType.Monk, (1.00, 0.50) },        // 0% dealt, -50% taken
            { ClassJobType.Dragoon, (0.85, 0.50) },      // -15% dealt, -50% taken
            { ClassJobType.Ninja, (1.00, 0.55) },       // 0% dealt, -45% taken
            { ClassJobType.Samurai, (0.90, 0.50) },     // -10% dealt, -50% taken
            { ClassJobType.Reaper, (1.00, 0.50) },      // 0% dealt, -50% taken
            { ClassJobType.Viper, (1.00, 0.40) },       // 0% dealt, -60% taken

            // Ranged Physical DPS
            { ClassJobType.Bard, (1.00, 0.70) },        // 0% dealt, -30% taken
            { ClassJobType.Machinist, (1.00, 0.70) },   // 0% dealt, -30% taken
            { ClassJobType.Dancer, (1.00, 0.65) },      // 0% dealt, -35% taken

            // Ranged Magical DPS
            { ClassJobType.BlackMage, (0.95, 0.70) },   // -5% dealt, -30% taken
            { ClassJobType.Summoner, (0.90, 0.70) },    // -10% dealt, -30% taken
            { ClassJobType.RedMage, (1.00, 0.62) },     // 0% dealt, -38% taken
            { ClassJobType.Pictomancer, (0.90, 0.70) }, // -10% dealt, -30% taken

            // Healers
            { ClassJobType.WhiteMage, (0.90, 0.75) },   // -10% dealt, -25% taken
            { ClassJobType.Scholar, (0.90, 0.70) },     // -10% dealt, -30% taken
            { ClassJobType.Astrologian, (0.85, 0.75) }, // -15% dealt, -25% taken
            { ClassJobType.Sage, (1.00, 0.65) },        // 0% dealt, -35% taken
        };

        /// <summary>
        /// Rival Wings (Astragalos/Hidden Gorge) job modifiers (Patch 7.4).
        /// Rival Wings only applies damage-taken modifiers; there are no per-job damage-dealt or limit-gauge adjustments.
        /// Damage Taken: percentage modifier (e.g., -20% = 0.80 multiplier, -10% = 0.90 multiplier)
        /// Values are stored as multipliers (1.0 = no change, 0.80 = -20%, 0.90 = -10%)
        /// </summary>
        private static readonly Dictionary<ClassJobType, double> RivalWingsJobModifiers = new Dictionary<ClassJobType, double>
        {
            // Tanks: -20% damage taken
            { ClassJobType.Paladin, 0.80 },
            { ClassJobType.Warrior, 0.80 },
            { ClassJobType.DarkKnight, 0.80 },
            { ClassJobType.Gunbreaker, 0.80 },

            // Melee DPS: -20% damage taken
            { ClassJobType.Monk, 0.80 },
            { ClassJobType.Dragoon, 0.80 },
            { ClassJobType.Ninja, 0.80 },
            { ClassJobType.Samurai, 0.80 },
            { ClassJobType.Reaper, 0.80 },
            { ClassJobType.Viper, 0.80 },

            // Ranged Physical DPS: -10% damage taken
            { ClassJobType.Bard, 0.90 },
            { ClassJobType.Machinist, 0.90 },
            { ClassJobType.Dancer, 0.90 },

            // Ranged Magical DPS: -10% damage taken
            { ClassJobType.BlackMage, 0.90 },
            { ClassJobType.Summoner, 0.90 },
            { ClassJobType.RedMage, 0.90 },
            { ClassJobType.Pictomancer, 0.90 },

            // Healers: -10% damage taken
            { ClassJobType.WhiteMage, 0.90 },
            { ClassJobType.Scholar, 0.90 },
            { ClassJobType.Astrologian, 0.90 },
            { ClassJobType.Sage, 0.90 },
        };

        // PvP Damage Modifiers
        // 
        // TANKS:
        // PLD - 4283 Shield Smite: increases damage taken by 10%
        // PLD - 1991 Sword Oath: increases damage dealt by 15%
        // PLD - 3188 Shield Oath: reduces damage taken by 15%
        // PLD - 3210 Phalanx: reduces damage taken by 33%
        // PLD - 3026 Holy Sheltron: absorbs potency of 8000
        // WAR - 3029 Onslaught: increases damage taken by 10%
        // War - 3256 Orogeny: Damage dealth is reduced by 10%
        // War - 3031 Stem The Tide: aborbs potency of <20% of 66000 (20% of 66000 = 13200)>
        // DRK - 3037 Salted Earth reducer: reduces damage taken by 20%
        // DRK - 1308 Blackest Night: absorbs potency of 8000
        // GNB - 3052 Relentless Rush: reduces damage taken by 25%
        // GNB - 3053 Relentless Shrapnel: increases damage taken by 5% 
        // GNB - 4295 Heart of Corundum: reduces damage taken by 10%
        // GNB - 3042 NoMercy : increases damage dealt by 10%
        // GNB - 3051 Nebula : aborbs potency of 4000
        // TANK ROLE - 1978 Rampart: reduces damage taken by 50%
        // TANK ROLE - 4476 Rampage: increases target's damage taken by 25%
        //
        // HEALERS:
        // WHM - 1415 Seraph Strike Protect: reduces damage taken by 10%
        // WHM - 3086 Aquaveil: absorbs potency of 8000
        // SCH - 3087 Galvanize: absorbs potency of 4000
        // SCH - 3088 Catalyze: reduces damage taken by 10%
        // SCH - 1406 Chain Strategem: increases damage taken by 10%
        // SCH - 3093 Desperate Measures: Increases damage dealt by 10%
        // SCH - 3097 Seraphic Veil: absorbs potency of 6000
        // SCH - 3098 Consolation: absorbs potency of 8000
        // AST - 1451 Lord of Crowns: increases damage taken by 10%
        // AST - 1452 Lady of Crowns: reduces damage taken by 10%
        // AST - 4330 Epicycle: absorbs potency of 6000
        // AST - 3105 Celestial River (LB): increases damage dealt by 30% (decreases by 10% every 5s, 15 s total duration), 
        // AST - 3106 Celestial Tide: reduces damage dealt by 30% (decreases by 10% every 5s, 15 s total duration)
        // AST - 3100 Nocturnal Benefic: absorbs potency of 4000
        // SGE - 3110 Haima: absorbs potency of 4000
        // SGE - 3113 Toxicon: increases damage taken by 10%
        // SGE - 3119 Mesotes (on target): If target has this buff, then we can't do damage unless we have Lype on ourselves
        // SGE - 3120 Lype (on self): If we have this buff, then we can do damage to the target even if they have Mesotes
        // HEALER ROLE - 4481 Stoneskin: absorbs potency of 12000
        //
        // MELEE DPS:
        // MNK - 3172 Pressure Point (on target): adds 12,000 more potency to the next attack against the char afflicted with pressure point
        // MNK - 3173 Thunderclap: absorbs potency of 8000
        // MNK - 3170 Fire Resonance: increases damage dealt by next weaponskill by 50%
        // DRG - 3177 Life of the Dragon (on self): increases damage dealt by 25%
        // DRG - 3177 Life of the Dragon (on target): increases damage dealt by 15%
        // DRG - 3179 Horrid Roar (on self if target is a dragoon): reduces damage dealt by 50% 
        // NIN - 2011 Shade Shift: absorbs potency of 8000
        // NIN - 3186 Huton: absorbs potency of 16000
        // SAM - 1240 Chiten: reduces damage taken by 33%
        // SAM - Auras.PvpKuzushi: (if we are sam against target with kuzushi) increases damage dealt to target by 25%
        // SAM - 3200 Kaeshi: Namikiri: absorbs potency of 10000
        // SAM - 4306 Debana: (if we are sam against target with debana) increases damage dealt to target by 15%
        // RPR - 2861 Crest of time borrowed: absorbs potency of 12000
        // VPR - 4099 Noxious Gnash (if we are vpr against target with noxious gnash) increases damage dealt to target by 25%
        // VPR - 4096 Hardened Scales: reduces damage taken by 50%
        // VPR - 4097 Armored Scales: Absorbs potency of 4000
        // MELEE ROLE - 1982 Bloodbath: increases damage dealt by 10%
        //
        // RANGED PHYSICAL DPS:
        // BRD - 2178 Warden's Grace: reduces damage taken by 25%
        // MCH - 3154 Chain Saw: increases damage taken by 20%
        // MCH - 3156 Aether Mortar: absorbs potency of 7500
        // DNC - 2052 Fan Dance: reduces damage taken by 20%
        // RANGED PHYS ROLE - Bravery: increases damage dealt by 25%, reduces damage taken by 25%
        //
        // RANGED MAGICAL DPS:
        // BLM - 4316 Wreath of Ice reduces damage taken by 20%
        // BLM - 3221 Burst: Absorbs potency of 15000
        // SMN - 3231 Scarlet Flame: reduces damage dealt by 50%
        // SMN - 3224 Radiant Aegis: absorbs potency by 12000 and reduces damage taken by 25%
        // RDM - 3242 Monomachy: increases damage dealt to target by 10%
        // RDM - 4320 Forte: Reduces damage taken by 50% and absorbs potency of 4000
        // RDM - 3234 Enchanted Riposte: absorbs potency of 4000
        // RDM - 3235 Enchanted Zwerchhau: absorbs potency of 4000
        // RDM - 3236 Enchanted Redoublement: absorbs potency of 4000
        // RDM - 2285 Embolden (self): increases damage dealt by 8%
        // RDM - 2285 Embolden (target): reduces damage taken by 8%
        // RDM - 3243 Displacement: Increases next spell's damage by 15%
        // PCT - 4111 Clawed Muse: increases damage taken by 10%
        // PCT - 4114 Tempera Coat: absorbs potency of 12000
        // PCT - 4115 Tempera Grassa: absorbs potency of 8000
        // PCT - 4109 Pom Muse:increases damage dealt by 20%
        // PCT - 4119 Star Prism: increases damage dealt by 15%
        // PCT - 4117 Chocobastion: reduces damage taken by 25%
        // RANGED ROLE - 4480 Rust: reduces damage dealt by 33%

        // COMMON:
        // Guard: reduces damage taken by 90%
        // Battle High I-V: increases damage dealt and healing potency by 10/20/30/40/50%
        // 

        private static readonly Dictionary<uint, double> TargetAuras = new Dictionary<uint, double>
        {
            // Auras on target that increase damage they take (vulnerability debuffs)
            { Auras.PvpGuard, 0.10 }, // Guard - reduces damage taken by 90%
            { Auras.PvpShieldSmite, 1.10 }, // PLD - increases damage taken by 10%
            { Auras.PvpOnslaught, 1.10 }, // WAR - increases damage taken by 10%
            { Auras.PvpRelentlessShrapnel, 1.05 }, // GNB - increases damage taken by 5%
            { Auras.PvpRampage, 1.25 }, // Tank role action - increases damage taken by 25%
            { Auras.PvpChainStrategem, 1.10 }, // SCH - increases damage taken by 10%
            { Auras.PvpLordOfCrowns, 1.10 }, // AST - increases damage taken by 10%
            { Auras.PvpToxicon, 1.10 }, // SGE - increases damage taken by 10%
            { Auras.PvpChainSaw, 1.20 }, // MCH - increases damage taken by 20%
            { Auras.PvpPhantomDart, 1.25 }, // BLM - increases damage taken by 25%
            { Auras.PvpMonomachy, 1.10 }, // RDM Corps-a-Corps - increases damage target receives from you by 10%
            { Auras.PvpClawedMuse, 1.10 }, // PCT - increases damage taken by 10%
            
            // Auras on target that reduce damage they take (mitigation)
            { Auras.PvpBravery, 0.75 }, // Role action - reduces damage taken by 25%
            { Auras.PvpShieldOath, 0.85 }, // PLD - reduces damage taken by 15%
            { Auras.PvpPhalanx, 0.67 }, // PLD - reduces damage taken by 33%
            { Auras.PvpRampart, 0.50 }, // Tank role - reduces damage taken by 50%
            { Auras.PvpSeraphStrike, 0.90 }, // WHM - reduces damage taken by 10%
            { Auras.PvpCatalyze, 0.90 }, // SCH - reduces damage taken by 10%
            { Auras.PvpLadyOfCrowns, 0.90 }, // AST - reduces damage taken by 10%
            { Auras.PvpHeartOfCorundum, 0.90 }, // GNB - reduces damage taken by 10%
            { Auras.PvpChiten, 0.67 }, // SAM - reduces damage taken by 33%
            { Auras.PvpWardensGrace, 0.75 }, // BRD - reduces damage taken by 25%
            { Auras.PvpFanDance, 0.80 }, // DNC - reduces damage taken by 20%
            { Auras.PvpWreathOfIce, 0.80 }, // BLM - reduces damage taken by 20%
            { Auras.PvpRadiantAegis, 0.75 }, // SMN - reduces damage taken by 25%
            { Auras.PvpForte, 0.50 }, // RDM - reduces damage taken by 50%
            { Auras.PvpChocobastion, 0.75 }, // PCT - reduces damage taken by 25%
            { Auras.PvpEmbolden, 0.92 }, // RDM - reduces damage taken by 8% (when on target)
            { Auras.PvpRelentlessRush, 0.75 }, // GNB - reduces damage taken by 25%
            { Auras.PvpSaltedEarth, 0.80 }, // DRK - reduces damage taken by 20%
            { Auras.PvpLifeoftheDragon, 1.15 }, // DRG - increases damage dealt by 15% (when on target)
        };

        private static readonly Dictionary<uint, double> SelfAuras = new Dictionary<uint, double>
        {
            // Auras on self that increase damage dealt
            { Auras.PvpBravery, 1.25 }, // Role action - increases damage dealt by 25%
            { Auras.PvpSwordOath, 1.15 }, // PLD - increases damage dealt by 15%
            { Auras.PvpNoMercy, 1.10 }, // GNB - increases damage dealt by 10%
            { Auras.PvpDesperateMeasures, 1.10 }, // SCH - increases damage dealt by 10%
            { Auras.PvpFireResonance, 1.50 }, // MNK - increases damage dealt by next weaponskill by 50%
            { Auras.PvpLifeoftheDragon, 1.25 }, // DRG - increases damage dealt by 25% (on self)
            { Auras.PvpBloodbath, 1.10 }, // Melee role - increases damage dealt by 10%
            { Auras.PvpDisplacement, 1.15 }, // RDM - increases next spell's damage by 15%
            { Auras.PvpEmbolden, 1.08 }, // RDM - increases damage dealt by 8% (on self)
            { Auras.PvpPomMuse, 1.20 }, // PCT - increases damage dealt by 20%
            { Auras.PvpStarPrism, 1.15 }, // PCT - increases damage dealt by 15%
            { Auras.PvpCelestialRiver, 1.30 }, // AST LB - increases damage dealt by 30%
            { Auras.PvpBattleHigh1, 1.10 }, // Battle High I - increases damage dealt and healing potency by 10%
            { Auras.PvpBattleHigh2, 1.20 }, // Battle High II - increases damage dealt and healing potency by 20%
            { Auras.PvpBattleHigh3, 1.30 }, // Battle High III - increases damage dealt and healing potency by 30%
            { Auras.PvpBattleHigh4, 1.40 }, // Battle High IV - increases damage dealt and healing potency by 40%
            { Auras.PvpBattleHigh5, 1.50 }, // Battle High V - increases damage dealt and healing potency by 50%
            
            // Auras on self that reduce damage dealt
            { Auras.PvpOrogeny, 0.90 }, // WAR - reduces damage dealt by 10%
            { Auras.PvpScarletFlame, 0.50 }, // SMN - reduces damage dealt by 50%
            { Auras.PvpRust, 0.67 }, // Ranged role - reduces damage dealt by 33%
            { Auras.PvpHorridRoar, 0.50 }, // DRG - reduces damage dealt by 50% (if target is dragoon)
        };

        // Absorb auras: subtract from final damage (capped at 0)
        private static readonly Dictionary<uint, double> AbsorbAuras = new Dictionary<uint, double>
        {
            { Auras.PvpHolySheltron, 8000 }, // PLD - absorbs potency of 8000
            { Auras.PvpStemTheTide, 13200 }, // WAR - absorbs potency of 13200 (20% of 66000)
            { Auras.PvpBlackestNight, 8000 }, // DRK PvP - absorbs potency of 8000
            { Auras.PvpNebula, 4000 }, // GNB - absorbs potency of 4000
            { Auras.PvpAquaveil, 8000 }, // WHM - absorbs potency of 8000
            { Auras.PvpGalvanize, 4000 }, // SCH - absorbs potency of 4000
            { Auras.PvpSeraphicVeil, 6000 }, // SCH - absorbs potency of 6000
            { Auras.PvpConsolation, 8000 }, // SCH - absorbs potency of 8000
            { Auras.PvpEpicycle, 6000 }, // AST - absorbs potency of 6000
            { Auras.PvpNocturnalBenefic, 4000 }, // AST - absorbs potency of 4000
            { Auras.PvpHaima, 4000 }, // SGE - absorbs potency of 4000
            { Auras.PvpStoneskin, 12000 }, // Healer role - absorbs potency of 12000
            { Auras.PvpThunderclap, 8000 }, // MNK - absorbs potency of 8000
            { Auras.PvpShadeShift, 8000 }, // NIN - absorbs potency of 8000
            { Auras.PvpHuton, 16000 }, // NIN - absorbs potency of 16000
            { Auras.PvpKaeshiNamikiri, 10000 }, // SAM - absorbs potency of 10000
            { Auras.PvpCrestOfTimeBorrowed, 12000 }, // RPR - absorbs potency of 12000
            { Auras.PvpArmoredScales, 4000 }, // VPR - absorbs potency of 4000
            { Auras.PvpAetherMortar, 7500 }, // MCH - absorbs potency of 7500
            { Auras.PvpBurst, 15000 }, // BLM - absorbs potency of 15000
            { Auras.PvpRadiantAegis, 12000 }, // SMN - absorbs potency of 12000
            { Auras.PvpForte, 4000 }, // RDM - absorbs potency of 4000
            { Auras.PvpEnchantedRiposte, 4000 }, // RDM - absorbs potency of 4000
            { Auras.PvpEnchantedZwerchhau, 4000 }, // RDM - absorbs potency of 4000
            { Auras.PvpEnchantedRedoublement, 4000 }, // RDM - absorbs potency of 4000
            { Auras.PvpTemperaCoat, 12000 }, // PCT - absorbs potency of 12000
            { Auras.PvpTemperaGrassa, 8000 }, // PCT - absorbs potency of 8000
            { Auras.PvpSnowFort, 25000 }, // Environmental buff in Worqor Chirteh
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

            // Special case: Mesotes/Lype interaction
            // If target has Mesotes, we can't damage them unless we have Lype
            if (target.HasAura(Auras.PvpMesotes) && !Core.Me.HasAura(Auras.PvpLype))
            {
                return false; // Can't damage target with Mesotes without Lype
            }

            const double PvpDamageConversionFactor = 1.0;
            double estimatedDamage = potency * PvpDamageConversionFactor;

            // Special case: Pressure Point adds 12,000 potency to next attack
            if (target.HasAura(Auras.PvpPressurePoint))
            {
                estimatedDamage += 12000;
            }

            estimatedDamage *= damageMultiplier;

            // Apply target auras (affect damage target takes)
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

            // Apply self auras (affect damage we deal)
            foreach (var aura in SelfAuras)
            {
                // Special case: Horrid Roar only applies if target is a dragoon
                if (aura.Key == Auras.PvpHorridRoar)
                {
                    if (Core.Me.HasAura(aura.Key) && target is BattleCharacter bc && bc.CurrentJob == ClassJobType.Dragoon)
                    {
                        estimatedDamage *= aura.Value;
                    }
                }
                else if (Core.Me.HasAura(aura.Key))
                {
                    estimatedDamage *= aura.Value;
                }
            }

            // Special case: Debana (SAM) - only applies if we're SAM and we applied it
            // Note: We can't easily verify if we applied it, so we check if we're SAM
            if (target.HasAura(Auras.PvpDebana) && Core.Me.CurrentJob == ClassJobType.Samurai)
            {
                estimatedDamage *= 1.15; // Increases damage dealt to target by 15%
            }

            // Special case: Noxious Gnash (VPR) - only applies if we're VPR and we applied it
            // Note: We can't easily verify if we applied it, so we check if we're VPR
            if (target.HasAura(Auras.PvpNoxiousGnash) && Core.Me.CurrentJob == ClassJobType.Viper)
            {
                estimatedDamage *= 1.25; // Increases damage dealt to target by 25%
            }

            // Special case: Kuzushi (SAM) - increases damage dealt to target by 25% if we're SAM
            if (target.HasAura(Auras.PvpKuzushi) && Core.Me.CurrentJob == ClassJobType.Samurai)
            {
                estimatedDamage *= 1.25; // Increases damage dealt to target by 25%
            }

            // Apply absorbs (subtract from final damage, capped at 0)
            double totalAbsorb = 0;
            foreach (var absorb in AbsorbAuras)
            {
                if (target.HasAura(absorb.Key))
                {
                    totalAbsorb += absorb.Value;
                }
            }
            estimatedDamage = Math.Max(0, estimatedDamage - totalAbsorb);

            // Apply job modifiers based on PvP mode
            PvpMode currentMode = GetPvpMode();

            if (currentMode == PvpMode.Frontline)
            {
                // Frontline: Apply both damage dealt and damage taken modifiers
                // Apply player's damage dealt modifier
                if (FrontlineJobModifiers.TryGetValue(Core.Me.CurrentJob, out var playerModifiers))
                {
                    estimatedDamage *= playerModifiers.DamageDealtMultiplier;
                }

                // Apply target's damage taken modifier
                if (target is BattleCharacter targetBc && FrontlineJobModifiers.TryGetValue(targetBc.CurrentJob, out var targetModifiers))
                {
                    estimatedDamage *= targetModifiers.DamageTakenMultiplier;
                }
            }
            else if (currentMode == PvpMode.HiddenGorgeRivalWings)
            {
                // Rival Wings: Only apply damage taken modifiers (no damage dealt modifiers)
                // Apply target's damage taken modifier
                if (target is BattleCharacter targetBc && RivalWingsJobModifiers.TryGetValue(targetBc.CurrentJob, out var damageTakenMultiplier))
                {
                    estimatedDamage *= damageTakenMultiplier;
                }
            }

            // I dont' think there are any tank modifiers in CC anymore. 
            // // Tank damage reduction (20% reduction for tanks) - only applies if not in Frontline mode
            // // Frontline modifiers already account for tank damage reduction
            // if (currentMode == PvpMode.CrystallineConflict && target.IsTank())
            // {
            //     estimatedDamage *= 0.8;
            // }

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
        /// <param name="maxAlliesTargetingLimit">If set (value > 0), skips targets with more than this many allies targeting them. Defaults to 0 (disabled).</param>
        /// <returns>The killable target, or null if none found</returns>
        public static GameObject FindKillableTargetInRange<T>(
            T settings,
            double potency,
            float range,
            bool ignoreGuard = false,
            bool checkGuard = true,
            bool searchAllTargets = false,
            Func<GameObject, double> potencyCalculator = null,
            int maxAlliesTargetingLimit = 0) where T : JobSettings
        {
            // First check current target if valid
            if (Core.Me.CurrentTarget != null && Core.Me.CurrentTarget.ValidAttackUnit() && Core.Me.CurrentTarget.InLineOfSight())
            {
                if (Core.Me.CurrentTarget.WithinSpellRange(range))
                {
                    // Check Guard if required
                    if (checkGuard && GuardCheck(settings, Core.Me.CurrentTarget))
                        return null; // Skip guarded target

                    // Check ally targeting limit if enabled
                    if (maxAlliesTargetingLimit > 0 && TooManyAlliesTargeting(settings, Core.Me.CurrentTarget))
                        return null; // Skip target with too many allies

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
                        && (!checkGuard || !GuardCheck(settings, e))
                        && (maxAlliesTargetingLimit <= 0 || !TooManyAlliesTargeting(settings, e)))
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

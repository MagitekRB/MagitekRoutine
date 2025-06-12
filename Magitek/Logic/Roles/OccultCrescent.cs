using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models;
using Magitek.Models.OccultCrescent;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using System.Collections.Generic;
using System;
using Clio.Utilities;

namespace Magitek.Logic.Roles
{
    internal static class OCAuras
    {
        public const int
            OffensiveAria = 4247,
            RomeosBallad = 4244,
            Pray = 4232,
            EnduringFortitude = 4233,
            Slow = 3493,
            HerosRime = 4249,
            OccultMageMasher = 4259,
            OccultQuick = 4260,
            SilverSickness = 4264,
            Fleetfooted = 4239,
            Counterstance = 4238,
            OccultSprint = 4276,
            Vigilance = 4277,
            ForeseenOffense = 4278,
            WeaponPilfered = 4279,
            Shirahadori = 4245,
            BattleBell = 4251,
            FalsePrediction = 4269,
            PredictionOfBlessing = 4267,
            PredictionOfStarfall = 4268,
            PredictionOfCleansing = 4266,
            PredictionOfJudgment = 4265,
            PhantomRejuvenation = 4274,
            RingingRespite = 4257,
            Suspend = 4258;

        // Dispellable enemy auras - add known beneficial enemy auras here
        public static readonly uint[] DispellableAuras = new uint[]
        {

        };
    }

    internal static class OCSpells
    {
        // Bard Spells
        public static readonly SpellData OffensiveAria = DataManager.GetSpellData(41608);
        public static readonly SpellData RomeosBallad = DataManager.GetSpellData(41609);
        public static readonly SpellData MightyMarch = DataManager.GetSpellData(41607);
        public static readonly SpellData HerosRime = DataManager.GetSpellData(41610);

        // Knight Spells
        public static readonly SpellData PhantomGuard = DataManager.GetSpellData(41588);
        public static readonly SpellData Pray = DataManager.GetSpellData(41589);
        public static readonly SpellData OccultHeal = DataManager.GetSpellData(41590);
        public static readonly SpellData Pledge = DataManager.GetSpellData(41591);

        // Monk Spells
        public static readonly SpellData PhantomKick = DataManager.GetSpellData(41595);
        public static readonly SpellData OccultCounter = DataManager.GetSpellData(41596);
        public static readonly SpellData Counterstance = DataManager.GetSpellData(41597);
        public static readonly SpellData OccultChakra = DataManager.GetSpellData(41598);

        // Berserker Spells
        public static readonly SpellData Rage = DataManager.GetSpellData(41592);
        public static readonly SpellData DeadlyBlow = DataManager.GetSpellData(41594);

        // Chemist Spells
        public static readonly SpellData OccultPotion = DataManager.GetSpellData(41631);
        public static readonly SpellData OccultEther = DataManager.GetSpellData(41633);
        public static readonly SpellData Revive = DataManager.GetSpellData(41634);
        public static readonly SpellData OccultElixir = DataManager.GetSpellData(41635);

        // Cannoneer Spells
        public static readonly SpellData PhantomFire = DataManager.GetSpellData(41626);
        public static readonly SpellData HolyCannon = DataManager.GetSpellData(41627);
        public static readonly SpellData DarkCannon = DataManager.GetSpellData(41628);
        public static readonly SpellData ShockCannon = DataManager.GetSpellData(41629);
        public static readonly SpellData SilverCannon = DataManager.GetSpellData(41630);

        // Time Mage Spells
        public static readonly SpellData OccultSlowga = DataManager.GetSpellData(41621);
        public static readonly SpellData OccultComet = DataManager.GetSpellData(41623);
        public static readonly SpellData OccultMageMasher = DataManager.GetSpellData(41624);
        public static readonly SpellData OccultDispel = DataManager.GetSpellData(41622);
        public static readonly SpellData OccultQuick = DataManager.GetSpellData(41625);

        // Ranger Spells
        public static readonly SpellData PhantomAim = DataManager.GetSpellData(41599);
        public static readonly SpellData OccultFalcon = DataManager.GetSpellData(41601);
        public static readonly SpellData OccultUnicorn = DataManager.GetSpellData(41602);

        // Phantom Thief Spells
        public static readonly SpellData OccultSprint = DataManager.GetSpellData(41646);
        public static readonly SpellData Steal = DataManager.GetSpellData(41645);
        public static readonly SpellData Vigilance = DataManager.GetSpellData(41647);
        public static readonly SpellData PilferWeapon = DataManager.GetSpellData(41649);

        // Phantom Samurai Spells
        public static readonly SpellData Mineuchi = DataManager.GetSpellData(41603);
        public static readonly SpellData Shirahadori = DataManager.GetSpellData(41604);
        public static readonly SpellData Iainuki = DataManager.GetSpellData(41605);
        public static readonly SpellData Zeninage = DataManager.GetSpellData(41606);

        // Phantom Oracle Spells
        public static readonly SpellData Predict = DataManager.GetSpellData(41636);
        public static readonly SpellData PhantomJudgment = DataManager.GetSpellData(41637);
        public static readonly SpellData Cleansing = DataManager.GetSpellData(41638);
        public static readonly SpellData Blessing = DataManager.GetSpellData(41639);
        public static readonly SpellData Starfall = DataManager.GetSpellData(41640);
        public static readonly SpellData PhantomRejuvenation = DataManager.GetSpellData(41643);
        public static readonly SpellData Invulnerability = DataManager.GetSpellData(41644);

        // Geomancer Spells
        public static readonly SpellData BattleBell = DataManager.GetSpellData(41611);
        public static readonly SpellData Sunbath = DataManager.GetSpellData(41613);
        public static readonly SpellData CloudyCaress = DataManager.GetSpellData(41614);
        public static readonly SpellData BlessedRain = DataManager.GetSpellData(41615);
        public static readonly SpellData MistyMirage = DataManager.GetSpellData(41616);
        public static readonly SpellData HastyMirage = DataManager.GetSpellData(41617);
        public static readonly SpellData AetherialGain = DataManager.GetSpellData(41618);
        public static readonly SpellData RingingRespite = DataManager.GetSpellData(41619);
        public static readonly SpellData Suspend = DataManager.GetSpellData(41620);

        // NIN Spells (for gold farming)
        public static readonly SpellData Dokumori = DataManager.GetSpellData(36957);
    }

    internal class OccultCrescent
    {
        private static readonly uint KnowledgeCrystal = 2007457;

        // Known Knowledge Crystal locations that never change
        private static readonly Vector3[] KnowledgeCrystalLocations = new[]
        {
            new Vector3(835.9902f, 75.12211f, -709.3925f),
            new Vector3(-165.9937f, 8.5f, -616.4979f),
            new Vector3(-347.2297f, 102.3273f, -124.1305f),
            new Vector3(-393.0761f, 99.51316f, 278.7158f),
            new Vector3(302.5914f, 105f, 313.6591f)
        };

        // Respawn point location
        private static readonly Vector3 RespawnPoint = new Vector3(851.87665f, 73.13358f, -704.79004f);

        // Throttling for knowledge crystal checks
        private static DateTime _lastCrystalCheck = DateTime.MinValue;
        private static bool _lastCrystalResult = false;
        private static readonly TimeSpan CrystalCheckInterval = TimeSpan.FromSeconds(1.0);

        // Throttling for non-party resurrection checks
        private static DateTime _lastNonPartyResCheck = DateTime.MinValue;
        private static readonly TimeSpan NonPartyResCheckInterval = TimeSpan.FromSeconds(1.0);

        // Cannoneer alternating cannon tracking
        private static bool _lastUsedShockCannon = false;

        // Oracle prediction tracking
        private static bool _predictCasted = false;
        private static DateTime _predictCastTime = DateTime.MinValue;
        private static readonly List<uint> _seenPredictions = new List<uint>();

        private static readonly Dictionary<uint, PhantomJob> PhantomJobAuras = new()
        {
            // { auraId, PhantomJob.JobName }
            { 4363, PhantomJob.Bard },
            { 4358, PhantomJob.Knight },
            { 4360, PhantomJob.Monk },
            { 4359, PhantomJob.Berserker },
            { 4367, PhantomJob.Chemist },
            { 4366, PhantomJob.Cannoneer },
            { 4365, PhantomJob.TimeMage },
            { 4361, PhantomJob.Ranger },
            { 4369, PhantomJob.PhantomThief },
            { 4362, PhantomJob.Samurai },
            { 4368, PhantomJob.Oracle },
            { 4364, PhantomJob.Geomancer }
        };

        public enum PhantomJob
        {
            None,
            Bard,
            Knight,
            Monk,
            Berserker,
            Chemist,
            Cannoneer,
            TimeMage,
            Ranger,
            PhantomThief,
            Samurai,
            Oracle,
            Geomancer
        }

        /// <summary>
        /// Check if we are near a Knowledge Crystal at a known crystal location
        /// Throttled to only check once per second for performance
        /// </summary>
        /// <param name="maxDistance">Maximum distance to consider "near" (default 5)</param>
        /// <returns>True if a Knowledge Crystal is found within range at a valid location</returns>
        public static bool IsNearKnowledgeCrystal(float maxDistance = 5.0f)
        {
            var now = DateTime.Now;

            // Return cached result if we checked recently
            if (now - _lastCrystalCheck < CrystalCheckInterval)
                return _lastCrystalResult;

            // Time to do a fresh check
            _lastCrystalCheck = now;
            _lastCrystalResult = PerformCrystalCheck(maxDistance);
            return _lastCrystalResult;
        }

        /// <summary>
        /// Performs the actual crystal proximity check
        /// </summary>
        /// <param name="maxDistance">Maximum distance to consider "near"</param>
        /// <returns>True if near a valid crystal</returns>
        private static bool PerformCrystalCheck(float maxDistance)
        {
            // Simply check if player is near any known crystal location
            // No need for expensive NPC searches since crystal locations are fixed
            var loc = Core.Me.Location;
            foreach (var crystalLocation in KnowledgeCrystalLocations)
            {
                if (loc.DistanceSqr(crystalLocation) <= maxDistance * maxDistance)
                    return true;
            }
            return false;

            /* OLD APPROACH - NPC LOOKUP METHOD (preserved for reference)
             * This was the original implementation that verified actual NPC existence
             * 
            // First, quickly check if player is near any known crystal location
            // This avoids expensive NPC searches when we're nowhere near crystals
            bool nearAnyLocation = false;
            foreach (var crystalLocation in KnowledgeCrystalLocations)
            {
                if (Core.Me.Location.Distance(crystalLocation) <= maxDistance + 2.0f) // Add small buffer
                {
                    nearAnyLocation = true;
                    break;
                }
            }

            // If not near any crystal location, don't bother searching for NPCs
            if (!nearAnyLocation)
                return false;

            // Only now do the expensive NPC search since we're near a crystal location
            var targetNpc = GameObjectManager.GetObjectByNPCId(KnowledgeCrystal);
            if (targetNpc == null || !targetNpc.IsValid || !targetNpc.IsVisible)
                return false;

            // Check if player is within range of the NPC
            if (targetNpc.Distance(Core.Me) > maxDistance)
                return false;

            // Final verification: ensure NPC is at the expected crystal location
            const float locationTolerance = 2.0f;
            foreach (var crystalLocation in KnowledgeCrystalLocations)
            {
                if (targetNpc.Location.Distance(crystalLocation) <= locationTolerance)
                    return true;
            }

            return false; // NPC found but not at a valid crystal location
            */
        }

        /// <summary>
        /// Main entry point for Occult Crescent Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        public static async Task<bool> Execute()
        {
            // Check if OC is enabled
            if (!OccultCrescentSettings.Instance.Enable)
                return false;

            // Check if we're in Occult Crescent content
            if (!Core.Me.OnOccultCrescent())
                return false;

            // Get the current phantom job
            var phantomJob = GetCurrentPhantomJob();
            if (phantomJob == PhantomJob.None)
                return false;

            // First, try automatic phantom job switching for knowledge crystal buffs
            if (await PhantomJobSwitcher.AutoSwitchForKnowledgeCrystalBuffs())
                return true;

            // Execute phantom job specific logic
            var phantomJobResult = phantomJob switch
            {
                PhantomJob.Bard => await ExecuteBardPhantomJob(),
                PhantomJob.Knight => await ExecuteKnightPhantomJob(),
                PhantomJob.Monk => await ExecuteMonkPhantomJob(),
                PhantomJob.Berserker => await ExecuteBerserkerPhantomJob(),
                PhantomJob.Chemist => await ExecuteChemistPhantomJob(),
                PhantomJob.Cannoneer => await ExecuteCannoneerPhantomJob(),
                PhantomJob.TimeMage => await ExecuteTimeMagePhantomJob(),
                PhantomJob.Ranger => await ExecuteRangerPhantomJob(),
                PhantomJob.PhantomThief => await ExecutePhantomThiefJob(),
                PhantomJob.Samurai => await ExecuteSamuraiPhantomJob(),
                PhantomJob.Oracle => await ExecuteOraclePhantomJob(),
                PhantomJob.Geomancer => await ExecuteGeomancerPhantomJob(),
                _ => false
            };

            // If phantom job didn't do anything, try non-party resurrection
            if (!phantomJobResult)
            {
                var nonPartyResResult = await ExecuteNonPartyResurrection();
                if (nonPartyResResult)
                    return true;
            }

            // If no phantom job and no resurrection, try Ninja Dokumori for gold farming
            return await ExecuteNinjaDokumori();
        }

        /// <summary>
        /// Main entry point for non-party resurrection system
        /// Works for all jobs with resurrection spells when in Occult Crescent content
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        public static async Task<bool> ExecuteNonPartyResurrection()
        {
            // Check if OC is enabled
            if (!OccultCrescentSettings.Instance.Enable)
                return false;

            // Check if we're in Occult Crescent content
            if (!Core.Me.OnOccultCrescent())
                return false;

            // Check if non-party resurrection is enabled
            if (!OccultCrescentSettings.Instance.ReviveNonPartyPlayers)
                return false;

            // Throttle non-party resurrection checks to every 2 seconds for performance
            var now = DateTime.Now;
            if (now - _lastNonPartyResCheck < NonPartyResCheckInterval)
                return false;
            _lastNonPartyResCheck = now;

            // Check if we're Phantom Chemist (free resurrection) or need MP check for regular jobs
            var phantomJob = GetCurrentPhantomJob();
            bool isPhantomChemist = phantomJob == PhantomJob.Chemist;

            // Only check MP for non-Chemist resurrections (Chemist Revive doesn't cost MP)
            if (!isPhantomChemist && Core.Me.CurrentManaPercent < OccultCrescentSettings.Instance.ReviveNonPartyMinimumManaPercent)
                return false;

            // Check combat preferences
            if (Core.Me.InCombat && !OccultCrescentSettings.Instance.ReviveNonPartyInCombat)
                return false;

            if (!Core.Me.InCombat && !OccultCrescentSettings.Instance.ReviveNonPartyOutOfCombat)
                return false;

            // Update alliance to get dead players using optimized Group system
            Group.UpdateAlliance(
                IgnoreAlliance: false,
                HealAllianceDps: false,
                HealAllianceHealers: false,
                HealAllianceTanks: false,
                ResAllianceDps: true,
                ResAllianceHealers: true,
                ResAllianceTanks: true
            );

            return await RaiseNonPartyPlayer();
        }

        /// <summary>
        /// Attempts to raise a non-party player using the appropriate resurrection spell for current job
        /// Handles swiftcast/slowcast preferences and special cases like RDM dualcast
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> RaiseNonPartyPlayer()
        {
            // Get dead non-party players from the optimized Group.CastableAlliance
            // Filter out party members since CastableAlliance includes everyone
            var deadNonPartyPlayers = Group.CastableAlliance.Where(u => u.CurrentHealth == 0 &&
                                                       !u.HasAura(Auras.Raise) &&
                                                       u.Distance(Core.Me) <= 30 &&
                                                       u.IsVisible &&
                                                       u.InLineOfSight() &&
                                                       u.IsTargetable &&
                                                       u.Location.DistanceSqr(RespawnPoint) >= 900);

            if (!deadNonPartyPlayers.Any())
                return false;

            // Select the best candidate (prioritize by job role like normal resurrection)
            var resurrectTarget = deadNonPartyPlayers
                .OrderByDescending(player => player.GetResurrectionWeight())
                .FirstOrDefault();

            if (resurrectTarget == null)
                return false;

            // Get the current phantom job first
            var phantomJob = GetCurrentPhantomJob();
            if (phantomJob == PhantomJob.Chemist)
            {
                // Phantom Chemist: Instant resurrection
                if (!OCSpells.Revive.CanCast(resurrectTarget))
                    return false;
                return await OCSpells.Revive.CastAura(resurrectTarget, Auras.Raise);
            }

            // Handle regular job resurrections
            return Core.Me.CurrentJob switch
            {
                ClassJobType.WhiteMage => await RaiseWithSwiftcastOptions(Spells.Raise, resurrectTarget),
                ClassJobType.Scholar => await RaiseWithSwiftcastOptions(Spells.Resurrection, resurrectTarget),
                ClassJobType.Astrologian => await RaiseWithSwiftcastOptions(Spells.Ascend, resurrectTarget),
                ClassJobType.Sage => await RaiseWithSwiftcastOptions(Spells.Egeiro, resurrectTarget),
                ClassJobType.Summoner => await RaiseWithSwiftcastOptions(Spells.Resurrection, resurrectTarget),
                ClassJobType.RedMage => await RaiseRedMage(resurrectTarget),
                _ => false
            };
        }

        /// <summary>
        /// Handles resurrection for jobs that can use Swiftcast
        /// Always tries swiftcast first, then slowcast if out of combat
        /// </summary>
        private static async Task<bool> RaiseWithSwiftcastOptions(SpellData resurrectionSpell, GameObject target)
        {
            if (!resurrectionSpell.CanCast(target))
                return false;

            var inCombat = Core.Me.InCombat;

            // Always try swiftcast first if available
            if (Spells.Swiftcast.IsKnownAndReady())
            {
                if (await Healer.Swiftcast())
                {
                    while (Core.Me.HasAura(Auras.Swiftcast))
                    {
                        if (await resurrectionSpell.CastAura(target, Auras.Raise))
                            return true;
                        await Coroutine.Yield();
                    }
                }
            }

            // If out of combat and swiftcast didn't work, try slowcast
            if (!inCombat)
            {
                return await resurrectionSpell.Cast(target);
            }

            // In combat with no swiftcast available - don't slowcast
            return false;
        }

        /// <summary>
        /// Handles resurrection for Red Mage, preferring Dualcast procs
        /// Falls back to regular swiftcast/slowcast logic if no dualcast
        /// </summary>
        private static async Task<bool> RaiseRedMage(GameObject target)
        {
            if (!Spells.Verraise.CanCast())
                return false;

            // First check for dualcast (best option for RDM)
            if (Core.Me.HasAura(Auras.Dualcast))
            {
                return await Spells.Verraise.Cast(target);
            }

            // No dualcast, use regular swiftcast/slowcast logic
            return await RaiseWithSwiftcastOptions(Spells.Verraise, target);
        }

        /// <summary>
        /// Determine the current phantom job based on player auras
        /// </summary>
        /// <returns>The current phantom job, or None if no phantom job is active</returns>
        public static PhantomJob GetCurrentPhantomJob()
        {
            foreach (var kvp in PhantomJobAuras)
            {
                if (Core.Me.HasAura(kvp.Key))
                    return kvp.Value;
            }
            return PhantomJob.None;
        }

        /// <summary>
        /// Execute Bard Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBardPhantomJob()
        {
            // Hero's Rime - party damage/healing buff, priority over Aria (can't stack)
            if (await HerosRime())
                return true;

            // Offensive Aria - damage buff that lasts 70 seconds, only cast in combat
            if (await OffensiveAria())
                return true;

            // Mighty March - party regen, high cooldown utility
            if (await MightyMarch())
                return true;

            // Romeo's Ballad - interrupt ability
            if (await RomeosBallad())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Knight Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteKnightPhantomJob()
        {
            // Phantom Guard - defensive cooldown like Rampart
            if (await PhantomGuard())
                return true;

            // Pray - regen effect, buff near knowledge crystal
            if (await Pray())
                return true;

            // Occult Heal - heal spell for party members
            if (await OccultHeal())
                return true;

            // Pledge - invulnerability stacks for party members
            if (await Pledge())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Monk Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteMonkPhantomJob()
        {
            // OccultCounter - attack after parry, highest priority
            if (await OccultCounter())
                return true;

            // Counterstance - parry rate buff / movement speed near crystal
            if (await Counterstance())
                return true;

            // OccultChakra - healing ability
            if (await OccultChakra())
                return true;

            // Phantom Kick - leap attack with stacking damage buff
            if (await PhantomKick())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Berserker Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBerserkerPhantomJob()
        {
            // Deadly Blow - high damage attack based on missing HP, 30s cooldown
            if (await DeadlyBlow()) return true;

            // Rage - auto attack current target with high damage
            if (await Rage())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Chemist Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteChemistPhantomJob()
        {
            // Revive - resurrect dead party members first
            if (await Revive())
                return true;

            // OccultElixir - party-wide HP/MP restoration (most expensive)
            if (await OccultElixir())
                return true;

            // OccultPotion - HP restoration (expensive)
            if (await OccultPotion())
                return true;

            // OccultEther - MP restoration
            if (await OccultEther())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Cannoneer Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteCannoneerPhantomJob()
        {
            // Silver Cannon - ranged attack that reduces damage target deals and takes
            if (await SilverCannon())
                return true;

            // Holy Cannon - ranged attack, more damage vs undead, shares cooldown with Silver Cannon
            if (await HolyCannon())
                return true;

            // Handles alternating between Shock Cannon and Dark Cannon when both are available
            // If only Dark Cannon is available (Shock not learned yet), uses Dark Cannon.
            // If both are available (Shock learned), alternates for optimal debuff coverage.
            if (await AlternatingCannons())
                return true;

            // Phantom Fire - standard ranged attack
            if (await PhantomFire())
                return true;

            return false;
        }

        /// <summary>
        /// Handles alternating between Shock Cannon and Dark Cannon when both are available
        /// If only Dark Cannon is available (Shock not learned yet), uses Dark Cannon.
        /// If both are available (Shock learned), alternates for optimal debuff coverage.
        /// </summary>
        /// <returns>True if a cannon spell was cast, false otherwise</returns>
        private static async Task<bool> AlternatingCannons()
        {
            var canShock = OCSpells.ShockCannon.CanCast();
            var canDark = OCSpells.DarkCannon.CanCast();

            // If neither can be cast, return false
            if (!canShock && !canDark)
                return false;

            // If Shock Cannon isn't available (not learned yet), just use Dark Cannon
            if (!canShock)
            {
                if (await DarkCannon())
                {
                    _lastUsedShockCannon = false;
                    return true;
                }
            }
            // If Shock Cannon is available, player definitely has Dark too (Dark learned first)
            // Alternate between them for optimal debuff coverage
            else
            {
                if (_lastUsedShockCannon)
                {
                    // Last used Shock, try Dark first
                    if (await DarkCannon())
                    {
                        _lastUsedShockCannon = false;
                        return true;
                    }
                    else if (await ShockCannon())
                    {
                        _lastUsedShockCannon = true;
                        return true;
                    }
                }
                else
                {
                    // Last used Dark (or first time), try Shock first
                    if (await ShockCannon())
                    {
                        _lastUsedShockCannon = true;
                        return true;
                    }
                    else if (await DarkCannon())
                    {
                        _lastUsedShockCannon = false;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Execute Time Mage Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteTimeMagePhantomJob()
        {
            // OccultQuick - buff party members or self (high priority utility)
            if (await OccultQuick())
                return true;

            // OccultDispel - remove beneficial effects from enemies
            if (await OccultDispel())
                return true;

            // OccultSlowga - apply slow debuff to enemies  
            if (await OccultSlowga())
                return true;

            // OccultMageMasher - magic attack power debuff
            if (await OccultMageMasher())
                return true;

            // OccultComet - AoE damage attack (8s cast time, use carefully)
            if (await OccultComet())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Ranger Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteRangerPhantomJob()
        {
            // Occult Unicorn - barrier for party (high priority defensive utility)
            if (await OccultUnicorn())
                return true;

            // Phantom Aim - damage buff (120s cooldown, use on cooldown)
            if (await PhantomAim())
                return true;

            // Occult Falcon - area attack
            if (await OccultFalcon())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Phantom Thief Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecutePhantomThiefJob()
        {
            // Occult Sprint - buff that reduces cast/recast time and increases movement speed
            if (await OccultSprint())
                return true;

            // Steal - steal an item from an enemy
            if (await Steal())
                return true;

            // Vigilance - defensive cooldown that reduces damage by 60% for 10s
            if (await Vigilance())
                return true;

            // Pilfer Weapon - steal a weapon from an enemy
            if (await PilferWeapon())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Samurai Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteSamuraiPhantomJob()
        {
            // Mineuchi - stuns target for 6 seconds
            if (await Mineuchi())
                return true;

            // Shirahadori - attack with high damage
            if (await Shirahadori())
                return true;

            // Iainuki - attack with high damage
            if (await Iainuki())
                return true;

            // Zeninage - attack with high damage
            if (await Zeninage())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Oracle Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteOraclePhantomJob()
        {
            // Update prediction tracking
            UpdateOraclePredictionTracking();

            // If we're in a prediction cycle, handle it intelligently
            if (_predictCasted && GetCurrentActivePrediction() != 0)
            {
                return await HandlePredictionCycle();
            }

            // Cast Predict to start a new cycle if not in one
            if (await Predict())
                return true;

            // Non-prediction Oracle abilities (can be used anytime)
            if (await PhantomRejuvenation())
                return true;

            if (await Invulnerability())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Geomancer Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteGeomancerPhantomJob()
        {
            // Ringing Respite - healing when taking damage, priority protective buff
            if (await RingingRespite())
                return true;

            // Sunbath - healing spell, can be used anytime when needed
            if (await Sunbath())
                return true;

            // Battle Bell - damage boost that stacks when taking damage (in combat only)
            if (await BattleBell())
                return true;

            // Suspend - utility buff for jumping over obstacles
            if (await Suspend())
                return true;

            // Weather buff spells - cast in combat when available
            if (await CloudyCaress())
                return true;

            if (await BlessedRain())
                return true;

            if (await MistyMirage())
                return true;

            if (await HastyMirage())
                return true;

            if (await AetherialGain())
                return true;

            return false;
        }

        /// <summary>
        /// Updates Oracle prediction tracking state
        /// </summary>
        private static void UpdateOraclePredictionTracking()
        {
            var now = DateTime.Now;

            // Check if we're currently in a prediction cycle
            var activePrediction = GetCurrentActivePrediction();

            if (activePrediction != 0)
            {
                // We have an active prediction - add to seen predictions if not already present
                if (!_seenPredictions.Contains(activePrediction))
                {
                    // New prediction detected
                    _seenPredictions.Add(activePrediction);

                    var predictionName = activePrediction switch
                    {
                        OCAuras.PredictionOfJudgment => "Phantom Judgment",
                        OCAuras.PredictionOfCleansing => "Cleansing",
                        OCAuras.PredictionOfBlessing => "Blessing",
                        OCAuras.PredictionOfStarfall => "Starfall",
                        _ => "Unknown"
                    };

                    Logger.WriteInfo($"[Oracle] New prediction detected: {predictionName} ({_seenPredictions.Count}/4)");
                }
            }

            // Check if we should reset the cycle (no predictions for a while and predict was cast)
            if (_predictCasted && (now - _predictCastTime).TotalSeconds > 20)
            {
                Logger.WriteInfo("[Oracle] Prediction cycle timeout - resetting tracking");
                ResetOraclePredictionTracking();
            }
        }

        /// <summary>
        /// Gets the currently active prediction aura, or 0 if none
        /// </summary>
        /// <returns>Active prediction aura ID</returns>
        private static uint GetCurrentActivePrediction()
        {
            if (Core.Me.HasAura(OCAuras.PredictionOfJudgment))
                return OCAuras.PredictionOfJudgment;
            if (Core.Me.HasAura(OCAuras.PredictionOfCleansing))
                return OCAuras.PredictionOfCleansing;
            if (Core.Me.HasAura(OCAuras.PredictionOfBlessing))
                return OCAuras.PredictionOfBlessing;
            if (Core.Me.HasAura(OCAuras.PredictionOfStarfall))
                return OCAuras.PredictionOfStarfall;
            return 0;
        }

        /// <summary>
        /// Gets the currently active prediction aura object
        /// </summary>
        /// <returns>Active prediction aura object, or null if none</returns>
        private static Aura GetCurrentActivePredictionAura()
        {
            var character = Core.Me as Character;
            if (character == null)
                return null;

            return character.CharacterAuras.FirstOrDefault(aura =>
                aura.Id == OCAuras.PredictionOfJudgment ||
                aura.Id == OCAuras.PredictionOfCleansing ||
                aura.Id == OCAuras.PredictionOfBlessing ||
                aura.Id == OCAuras.PredictionOfStarfall);
        }

        /// <summary>
        /// Resets Oracle prediction tracking
        /// </summary>
        private static void ResetOraclePredictionTracking()
        {
            _predictCasted = false;
            _predictCastTime = DateTime.MinValue;
            _seenPredictions.Clear();
            // _currentPrediction = 0; // Removed - unreliable tracking
        }

        /// <summary>
        /// Handles the prediction cycle with intelligent decision making
        /// </summary>
        /// <returns>True if a prediction spell was cast</returns>
        private static async Task<bool> HandlePredictionCycle()
        {
            var predictionCount = _seenPredictions.Count;
            var isLastPrediction = predictionCount >= 4;
            var isThirdPrediction = predictionCount == 3;

            // Get the current prediction aura and its remaining time
            var currentPredictionAura = GetCurrentActivePredictionAura();
            if (currentPredictionAura == null)
                return false;

            var timeLeft = currentPredictionAura.TimespanLeft.TotalSeconds;
            var aboutToExpire = timeLeft <= 1.0;

            // Special case: if this is the 3rd prediction and Starfall hasn't been seen yet
            // (meaning it will be the forced 4th prediction), and we're not at 100% HP,
            // we should cast the current prediction now to avoid being forced into Starfall
            if (isThirdPrediction &&
                aboutToExpire &&
                !_seenPredictions.Contains(OCAuras.PredictionOfStarfall) &&
                (Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.StarfallHealthPercent ||
                 !OccultCrescentSettings.Instance.UseStarfall))
            {
                Logger.WriteWarning($"[Oracle] STARFALL AVOIDANCE: Casting 3rd prediction to avoid Starfall as 4th (HP: {Core.Me.CurrentHealthPercent:F1}%)");
                var success = await CastCurrentPrediction("Avoiding Starfall as 4th prediction");
                if (success)
                {
                    ResetOraclePredictionTracking();
                    return true;
                }
            }

            // Force cast on 4th prediction to avoid False Prediction
            if (isLastPrediction && aboutToExpire)
            {
                if (currentPredictionAura.Id == OCAuras.PredictionOfStarfall && Core.Me.CurrentHealthPercent < 90)
                {
                    Logger.WriteWarning($"[Oracle] STARFALL AVOIDANCE: Starfall would kill self if cast, so we are forced to take False Prediction (HP: {Core.Me.CurrentHealthPercent:F1}%)");
                    return false;
                }

                Logger.WriteWarning($"[Oracle] FORCED CAST: 4th prediction to avoid False Prediction (Time left: {timeLeft:F1}s)");
                var success = await CastCurrentPrediction("Forced to avoid False Prediction");
                if (success)
                {
                    ResetOraclePredictionTracking();
                    return true;
                }
            }

            // Intelligent decision making based on current needs
            if (ShouldCastCurrentPrediction())
            {
                var success = await CastCurrentPrediction("Intelligent decision");
                if (success)
                {
                    ResetOraclePredictionTracking();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if we should cast the current prediction based on party needs
        /// </summary>
        /// <returns>True if we should cast the current prediction</returns>
        private static bool ShouldCastCurrentPrediction()
        {
            var currentPrediction = GetCurrentActivePrediction();
            switch (currentPrediction)
            {
                case OCAuras.PredictionOfJudgment:
                    // Cast if we/party needs moderate healing and want damage
                    return GetLowestPartyHealthPercent() <= OccultCrescentSettings.Instance.PhantomJudgmentHealthPercent;

                case OCAuras.PredictionOfBlessing:
                    // Cast if we/party needs significant healing
                    return GetLowestPartyHealthPercent() <= OccultCrescentSettings.Instance.BlessingHealthPercent;

                case OCAuras.PredictionOfCleansing:
                    // Cast if party is healthy and we want damage
                    return GetLowestPartyHealthPercent() >= OccultCrescentSettings.Instance.CleansingHealthPercent;

                case OCAuras.PredictionOfStarfall:
                    // Starfall does massive damage to self - be very careful
                    var hasEnemiesTargeting = HasEnemiesTargetingUs();
                    var canCastInvulnerability = OCSpells.Invulnerability.CanCast();

                    // If tanking enemies, only cast Starfall if we can cast Invulnerability (Invuln + Starfall combo available)
                    if (hasEnemiesTargeting && !canCastInvulnerability && Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.StarfallHealthPercent)
                        return false;

                    // If not tanking, only cast if we're at safe HP
                    if (!hasEnemiesTargeting && Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.StarfallHealthPercent)
                        return false;

                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the lowest health percentage in the party (including self)
        /// </summary>
        /// <returns>Lowest health percentage</returns>
        private static float GetLowestPartyHealthPercent()
        {
            var partyHealth = Group.CastableAlliesWithin20
                .Select(ally => ally.CurrentHealthPercent)
                .DefaultIfEmpty(100f);

            var lowestPartyHealth = partyHealth.Min();
            return Math.Min(Core.Me.CurrentHealthPercent, lowestPartyHealth);
        }

        /// <summary>
        /// Checks if any enemies are targeting us
        /// </summary>
        /// <returns>True if enemies are targeting us</returns>
        private static bool HasEnemiesTargetingUs()
        {
            return Combat.Enemies.Any(enemy =>
                enemy.TargetGameObject == Core.Me &&
                enemy.ValidAttackUnit());
        }

        /// <summary>
        /// Casts the current prediction spell
        /// </summary>
        /// <param name="reason">Reason for casting (for logging)</param>
        /// <returns>True if spell was cast successfully</returns>
        private static async Task<bool> CastCurrentPrediction(string reason)
        {
            var currentPrediction = GetCurrentActivePrediction();

            // Get prediction name for logging
            var predictionName = currentPrediction switch
            {
                OCAuras.PredictionOfJudgment => "Phantom Judgment",
                OCAuras.PredictionOfCleansing => "Cleansing",
                OCAuras.PredictionOfBlessing => "Blessing",
                OCAuras.PredictionOfStarfall => "Starfall",
                _ => "Unknown"
            };

            Logger.WriteInfo($"[Oracle] Casting {predictionName} - Reason: {reason} (Seen: {_seenPredictions.Count}/4)");

            switch (currentPrediction)
            {
                case OCAuras.PredictionOfJudgment:
                    return await PhantomJudgment();
                case OCAuras.PredictionOfCleansing:
                    return await Cleansing();
                case OCAuras.PredictionOfBlessing:
                    return await Blessing();
                case OCAuras.PredictionOfStarfall:
                    return await Starfall();
                default:
                    Logger.WriteWarning($"[Oracle] No valid prediction found to cast - Current: {currentPrediction}");
                    return false;
            }
        }

        /// <summary>
        /// Cast Offensive Aria - a damage buff that lasts 70 seconds
        /// Only cast when in combat and buff is not already active
        /// Don't cast if Hero's Rime is active (they don't stack)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OffensiveAria()
        {
            if (!OccultCrescentSettings.Instance.UseOffensiveAria)
                return false;

            // Must be in combat to use this ability
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(OCAuras.OffensiveAria, msLeft: 500))
                return false;

            // Don't cast if Hero's Rime is active (they don't stack)
            if (Core.Me.HasAura(OCAuras.HerosRime))
                return false;

            if (!OCSpells.OffensiveAria.CanCast())
                return false;

            return await OCSpells.OffensiveAria.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Romeo's Ballad - interrupt ability (combat only)
        /// Knowledge crystal casting is now handled by PhantomJobSwitcher
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> RomeosBallad()
        {
            if (!OccultCrescentSettings.Instance.UseRomeosBallad)
                return false;

            // Only used in combat for interrupts, knowledge crystal usage handled by PhantomJobSwitcher
            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.RomeosBallad.CanCast())
                return false;

            // TODO: Implement interrupt logic if needed
            // In combat: only cast if a monster is casting (to interrupt)
            // var castingEnemy = Combat.Enemies.FirstOrDefault(enemy =>
            //     enemy.IsCasting &&
            //     enemy.ValidAttackUnit() &&
            //     enemy.InLineOfSight() &&
            //     enemy.WithinSpellRange(OCSpells.RomeosBallad.Radius));
            //
            // if (castingEnemy != null)
            //     return await OCSpells.RomeosBallad.Cast(castingEnemy);

            return false;
        }

        /// <summary>
        /// Cast Phantom Guard - defensive cooldown that reduces damage by 60% for 10s
        /// Works like Rampart for tanks
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomGuard()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomGuard)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.PhantomGuard.CanCast())
                return false;

            // Cast when health is below configured percentage
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.PhantomGuardHealthPercent)
                return false;

            return await OCSpells.PhantomGuard.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Pray - regen effect (combat only)
        /// Knowledge crystal party buff casting is now handled by PhantomJobSwitcher
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Pray()
        {
            if (!OccultCrescentSettings.Instance.UsePray)
                return false;

            // Only used in combat for regen, knowledge crystal party buff handled by PhantomJobSwitcher
            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Pray.CanCast())
                return false;

            // In combat: cast if we don't have the regen effect and HP is below threshold
            if (Core.Me.HasAura(OCAuras.Pray))
                return false;

            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.PrayHealthPercent)
                return false;

            return await OCSpells.Pray.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Occult Heal - healing spell for party members
        /// Similar to Clemency or Cure
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultHeal()
        {
            if (!OccultCrescentSettings.Instance.UseOccultHeal)
                return false;

            if (!OCSpells.OccultHeal.CanCast())
                return false;

            if (Core.Me.CurrentManaPercent < 65)
                return false;

            GameObject healTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultHealCastOnAllies)
            {
                // Find party member who needs healing
                healTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need healing, check self
                if (healTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    healTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    healTarget = Core.Me;
            }

            if (healTarget == null)
                return false;

            return await OCSpells.OccultHeal.Cast(healTarget);
        }

        /// <summary>
        /// Cast Pledge - grants invulnerability stacks to party members
        /// Renders target invulnerable to autoattacks
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Pledge()
        {
            if (!OccultCrescentSettings.Instance.UsePledge)
                return false;

            if (!OCSpells.Pledge.CanCast())
                return false;

            GameObject pledgeTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.PledgeCastOnAllies)
            {
                // Prioritize party members who need protection (low HP)
                pledgeTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no low HP targets, cast on self if we need it
                if (pledgeTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    pledgeTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    pledgeTarget = Core.Me;
            }

            if (pledgeTarget == null)
                return false;

            return await OCSpells.Pledge.Cast(pledgeTarget);
        }

        /// <summary>
        /// Cast Phantom Kick - leap attack that grants stacking damage buff
        /// 100 potency AoE, grants up to 3 stacks for increased damage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomKick()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomKick)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PhantomKick.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
                return false;

            // Check melee range restriction if enabled
            if (OccultCrescentSettings.Instance.PhantomKickMeleeRangeOnly && !Core.Me.CurrentTarget.WithinSpellRange(3.0f))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PhantomKick.Range))
                return false;

            return await OCSpells.PhantomKick.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultCounter - attack that can only be used after a parry
        /// If it can cast, we should use it immediately
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultCounter()
        {
            if (!OccultCrescentSettings.Instance.UseOccultCounter)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultCounter.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultCounter.Range))
                return false;

            return await OCSpells.OccultCounter.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Counterstance - increases parry rate by 100% for 60s (combat only)
        /// Knowledge crystal movement speed buff casting is now handled by PhantomJobSwitcher
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Counterstance()
        {
            if (!OccultCrescentSettings.Instance.UseCounterstance)
                return false;

            // Only used in combat for parry rate, knowledge crystal movement buff handled by PhantomJobSwitcher
            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Counterstance.CanCast())
                return false;

            // In combat: cast for parry rate buff
            // Check if we already have the Counterstance parry buff
            if (Core.Me.HasAura(OCAuras.Counterstance))
                return false;

            return await OCSpells.Counterstance.Cast(Core.Me);
        }

        /// <summary>
        /// Cast OccultChakra - healing ability
        /// Restores 30% HP normally, or 70% HP if current HP < 30%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultChakra()
        {
            if (!OccultCrescentSettings.Instance.UseOccultChakra)
                return false;

            if (!OCSpells.OccultChakra.CanCast())
                return false;

            // Cast when health is below configured percentage
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.OccultChakraHealthPercent)
                return false;

            return await OCSpells.OccultChakra.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Mighty March - regen to self and all party members
        /// High cooldown (120s) utility spell
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> MightyMarch()
        {
            if (!OccultCrescentSettings.Instance.UseMightyMarch)
                return false;

            if (!OCSpells.MightyMarch.CanCast())
                return false;

            GameObject marchTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.MightyMarchCastOnAllies)
            {
                // Find party member who needs regen
                marchTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need regen, check self
                if (marchTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    marchTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    marchTarget = Core.Me;
            }

            if (marchTarget == null)
                return false;

            return await OCSpells.MightyMarch.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Hero's Rime - increases damage and healing potency of all party by 10%
        /// High priority due to 120s cooldown and party-wide benefit
        /// Can't stack with Offensive Aria
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> HerosRime()
        {
            if (!OccultCrescentSettings.Instance.UseHerosRime)
                return false;

            // Must be in combat to use this ability
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(OCAuras.HerosRime))
                return false;

            if (!OCSpells.HerosRime.CanCast())
                return false;

            return await OCSpells.HerosRime.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Deadly Blow - high damage attack based on missing HP, 30s cooldown
        /// Cast on cooldown when we have a valid target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> DeadlyBlow()
        {
            if (!OccultCrescentSettings.Instance.UseDeadlyBlow)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.DeadlyBlow.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.DeadlyBlow.Range))
                return false;

            return await OCSpells.DeadlyBlow.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Rage - auto attack current target with high damage
        /// Only use in melee range if enabled, and only if enemy is not casting
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Rage()
        {
            if (!OccultCrescentSettings.Instance.UseRage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Rage.CanCast())
                return false;

            // Need a valid attackable target that's not casting
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
                return false;

            // Check melee range restriction if enabled
            if (OccultCrescentSettings.Instance.RageMeleeRangeOnly && !Core.Me.CurrentTarget.WithinSpellRange(3.0f))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.Rage.Range))
                return false;

            return await OCSpells.Rage.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Phantom Fire - standard ranged attack
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomFire()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomFire)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PhantomFire.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PhantomFire.Range))
                return false;

            return await OCSpells.PhantomFire.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Holy Cannon - ranged attack, more damage vs undead, shares cooldown with Silver Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> HolyCannon()
        {
            if (!OccultCrescentSettings.Instance.UseHolyCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.HolyCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.HolyCannon.Range))
                return false;

            return await OCSpells.HolyCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Dark Cannon - ranged attack that inflicts blind
        /// Cannot be used at same time as Shock Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> DarkCannon()
        {
            if (!OccultCrescentSettings.Instance.UseDarkCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.DarkCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.DarkCannon.Range))
                return false;

            return await OCSpells.DarkCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Shock Cannon - ranged attack that inflicts paralysis
        /// Cannot be used at same time as Dark Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> ShockCannon()
        {
            if (!OccultCrescentSettings.Instance.UseShockCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.ShockCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.ShockCannon.Range))
                return false;

            return await OCSpells.ShockCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Silver Cannon - ranged attack that reduces damage target deals and takes
        /// Shares cooldown with Holy Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> SilverCannon()
        {
            if (!OccultCrescentSettings.Instance.UseSilverCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.SilverCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Don't cast if target has Silver Sickness unless it expires in 20 seconds or less
            if (Core.Me.CurrentTarget.HasAura(OCAuras.SilverSickness, msLeft: 20000))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.SilverCannon.Range))
                return false;

            return await OCSpells.SilverCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultPotion - completely restores HP of self or target
        /// Costs 100k gil per cast - very restrictive usage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultPotion()
        {
            if (!OccultCrescentSettings.Instance.UseOccultPotion)
                return false;

            if (!OCSpells.OccultPotion.CanCast())
                return false;

            GameObject potionTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultPotionCastOnAllies)
            {
                // Find party member who desperately needs healing
                potionTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need healing, check self
                if (potionTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    potionTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    potionTarget = Core.Me;
            }

            if (potionTarget == null)
                return false;

            return await OCSpells.OccultPotion.Cast(potionTarget);
        }

        /// <summary>
        /// Cast OccultEther - completely restores MP of self or target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultEther()
        {
            if (!OccultCrescentSettings.Instance.UseOccultEther)
                return false;

            if (!OCSpells.OccultEther.CanCast())
                return false;

            GameObject etherTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultEtherCastOnAllies)
            {
                // Find party member who needs MP
                etherTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    .OrderBy(ally => ally.CurrentManaPercent)
                    .FirstOrDefault();

                // If no allies need MP, check self
                if (etherTarget == null && Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    etherTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    etherTarget = Core.Me;
            }

            if (etherTarget == null)
                return false;

            return await OCSpells.OccultEther.Cast(etherTarget);
        }

        /// <summary>
        /// Cast Revive - resurrects a dead party member
        /// Instant cast, no swiftcast needed for Chemist
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Revive()
        {
            if (!OccultCrescentSettings.Instance.UseRevive)
                return false;

            // Find dead allies using the same logic as Healer.Raise
            var deadList = Group.DeadAllies.Where(u => u.CurrentHealth == 0 &&
                                                       !u.HasAura(Auras.Raise) &&
                                                       u.Distance(Core.Me) <= 30 &&
                                                       u.IsVisible &&
                                                       u.InLineOfSight() &&
                                                       u.IsTargetable &&
                                                       Group.GetDeathTime(u)?.AddSeconds(OccultCrescentSettings.Instance.ReviveDelay) <= DateTime.Now)
                .OrderByDescending(r => r.GetResurrectionWeight());

            var deadTarget = deadList.FirstOrDefault();

            if (deadTarget == null)
                return false;

            if (!deadTarget.IsTargetable)
                return false;

            if (!OCSpells.Revive.CanCast(deadTarget))
                return false;

            // Check combat restrictions - only check out of combat restriction
            if (!Core.Me.InCombat && !OccultCrescentSettings.Instance.ReviveOutOfCombat)
                return false;

            // Chemist Revive is instant - no swiftcast needed
            return await OCSpells.Revive.CastAura(deadTarget, Auras.Raise);
        }

        /// <summary>
        /// Cast OccultElixir - completely restores HP and MP of all party members
        /// Costs 300k gil per cast - extremely restrictive usage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultElixir()
        {
            if (!OccultCrescentSettings.Instance.UseOccultElixir)
                return false;

            if (!OCSpells.OccultElixir.CanCast())
                return false;

            // Check if multiple party members (including self) need healing/MP
            var partyMembersNeedingHelp = Group.CastableAlliesWithin30.Where(ally =>
                ally.IsValid &&
                ally.IsAlive &&
                (ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent ||
                 ally.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent))
                .Count();

            // Include self in the count
            if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent ||
                Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent)
                partyMembersNeedingHelp++;

            // Only cast if multiple people need help (justify the 300k cost)
            if (partyMembersNeedingHelp < 2)
                return false;

            return await OCSpells.OccultElixir.Cast(Core.Me);
        }

        /// <summary>
        /// Cast OccultSlowga - afflicts target with Slow (aura 3493)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultSlowga()
        {
            if (!OccultCrescentSettings.Instance.UseOccultSlowga)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultSlowga.CanCast())
                return false;

            // Need a valid attackable target that doesn't already have slow
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Don't cast if target already has slow debuff
            if (Core.Me.CurrentTarget.HasAura(OCAuras.Slow))
                return false;

            // Check if target is a BattleCharacter for additional checks
            if (Core.Me.CurrentTarget is BattleCharacter battleTarget)
            {
                // Check difficulty - high difficulty enemies are often immune to CC
                if (battleTarget.RawDifficulty >= 2)
                    return false;

                if (battleTarget.DifficultyEstimate != DifficultyEstimate.Normal)
                    return false;

                // FATE enemies might have different immunity rules
                if (battleTarget.IsFate)
                    return false;
            }

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultSlowga.Range))
                return false;

            return await OCSpells.OccultSlowga.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultComet - AoE damage with 8s cast time
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultComet()
        {
            if (!OccultCrescentSettings.Instance.UseOccultComet)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultComet.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultComet.Range))
                return false;

            // Long cast time ability - be careful about using it
            return await OCSpells.OccultComet.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultMageMasher - lowers target magic attack power by 10% for 60s
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultMageMasher()
        {
            if (!OccultCrescentSettings.Instance.UseOccultMageMasher)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultMageMasher.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Don't cast if target already has magic attack debuff
            if (Core.Me.CurrentTarget.HasAura(OCAuras.OccultMageMasher))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultMageMasher.Range))
                return false;

            return await OCSpells.OccultMageMasher.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultDispel - removes one beneficial status from target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultDispel()
        {
            if (!OccultCrescentSettings.Instance.UseOccultDispel)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultDispel.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.HasDispellableAura())
                return false;

            // if (!Core.Me.CurrentTarget.HasAnyAura(OCAuras.DispellableAuras))
            //     return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultDispel.Range))
                return false;

            return await OCSpells.OccultDispel.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultQuick - buff that reduces cast/recast time and increases movement speed
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultQuick()
        {
            if (!OccultCrescentSettings.Instance.UseOccultQuick)
                return false;

            if (!OCSpells.OccultQuick.CanCast())
                return false;

            GameObject quickTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultQuickCastOnAllies)
            {
                // Prioritize casters or low-mobility party members who could benefit from speed
                quickTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    !ally.HasAura(OCAuras.OccultQuick))
                    .OrderBy(ally => ally.IsDps() ? 0 : ally.IsTank() ? 1 : ally.IsHealer() ? 2 : 3)
                    .FirstOrDefault();

                // If no allies need buff, consider self
                if (quickTarget == null && !Core.Me.HasAura(OCAuras.OccultQuick))
                    quickTarget = Core.Me;
            }
            else
            {
                // Self-only mode
                if (!Core.Me.HasAura(OCAuras.OccultQuick))
                    quickTarget = Core.Me;
            }

            if (quickTarget == null)
                return false;

            return await OCSpells.OccultQuick.Cast(quickTarget);
        }

        /// <summary>
        /// Cast Phantom Aim - increases damage for 30s (120s recast)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomAim()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomAim)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.PhantomAim.CanCast())
                return false;

            // Cast on cooldown in combat for damage boost
            return await OCSpells.PhantomAim.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Occult Falcon - area attack that also triggers traps
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultFalcon()
        {
            // I don't know what a trap is, so disable this ability for now. 
            return false;

            if (!OccultCrescentSettings.Instance.UseOccultFalcon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultFalcon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultFalcon.Range))
                return false;

            return await OCSpells.OccultFalcon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Occult Unicorn - creates barrier around self and party that absorbs 40k damage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultUnicorn()
        {
            if (!OccultCrescentSettings.Instance.UseOccultUnicorn)
                return false;

            if (!OCSpells.OccultUnicorn.CanCast())
                return false;

            GameObject unicornTarget = null;

            // Check if we should consider allies
            if (OccultCrescentSettings.Instance.OccultUnicornCastOnAllies)
            {
                // Find party member who needs barrier protection
                unicornTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultUnicornHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need barrier, check self
                if (unicornTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultUnicornHealthPercent)
                    unicornTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultUnicornHealthPercent)
                    unicornTarget = Core.Me;
            }

            if (unicornTarget == null)
                return false;

            // Cast on self but affects whole party
            return await OCSpells.OccultUnicorn.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Occult Sprint - greatly increases movement speed for 10s
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultSprint()
        {
            if (!OccultCrescentSettings.Instance.UseOccultSprint)
                return false;

            if (!OCSpells.OccultSprint.CanCast())
                return false;

            // Check combat-only setting
            if (OccultCrescentSettings.Instance.OccultSprintOnlyInCombat && !Core.Me.InCombat)
                return false;

            // Only cast when moving - no point in speed buff when standing still
            if (!MovementManager.IsMoving)
                return false;

            // Don't cast if we already have the sprint buff
            if (Core.Me.HasAura(OCAuras.OccultSprint))
                return false;

            return await OCSpells.OccultSprint.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Steal - increases chance of additional items being dropped if cast before finishing blow
        /// Cast when any enemy within range is below configured HP threshold
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Steal()
        {
            if (!OccultCrescentSettings.Instance.UseSteal)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Steal.CanCast())
                return false;

            // Find any enemy within spell range that's below the HP threshold
            var stealTarget = Combat.Enemies.Where(enemy =>
                enemy.ValidAttackUnit() &&
                enemy.InLineOfSight() &&
                enemy.WithinSpellRange(OCSpells.Steal.Range) &&
                enemy.CurrentHealthPercent <= OccultCrescentSettings.Instance.StealHealthPercent)
                .OrderBy(enemy => enemy.CurrentHealthPercent) // Prioritize lowest HP for finishing blow
                .FirstOrDefault();

            if (stealTarget == null)
                return false;

            return await OCSpells.Steal.Cast(stealTarget);
        }

        /// <summary>
        /// Cast Vigilance - grants Vigilance aura that changes to Foreseen Offense when entering combat
        /// Can only be cast out of combat when we have a valid target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Vigilance()
        {
            if (!OccultCrescentSettings.Instance.UseVigilance)
                return false;

            // Cannot be executed while in combat
            if (Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Vigilance.CanCast())
                return false;

            // Need a valid attackable target (but not in combat)
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within configured distance
            if (Core.Me.CurrentTarget.Distance(Core.Me) > OccultCrescentSettings.Instance.VigilanceTargetDistance)
                return false;

            // Don't cast if we already have Vigilance
            if (Core.Me.HasAura(OCAuras.Vigilance, msLeft: 1000))
                return false;

            return await OCSpells.Vigilance.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Pilfer Weapon - lowers target's physical attack power by 10% for 60s
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PilferWeapon()
        {
            if (!OccultCrescentSettings.Instance.UsePilferWeapon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PilferWeapon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Don't cast if target already has weapon pilfered debuff
            if (Core.Me.CurrentTarget.HasAura(OCAuras.WeaponPilfered))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PilferWeapon.Range))
                return false;

            return await OCSpells.PilferWeapon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Mineuchi - stuns target for 6 seconds
        /// Uses Magitek's interrupt/stun system with configurable strategy
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Mineuchi()
        {
            if (!OccultCrescentSettings.Instance.UseMineuchi)
                return false;

            // Use Magitek's interrupt/stun system
            List<SpellData> stunSpells = new List<SpellData>() { OCSpells.Mineuchi };
            List<SpellData> interruptSpells = new List<SpellData>(); // Empty list since Mineuchi is stun-only

            return await InterruptAndStunLogic.StunOrInterrupt(stunSpells, interruptSpells, OccultCrescentSettings.Instance.MineuchiStrategy);
        }

        /// <summary>
        /// Cast Shirahadori - renders you impervious to physical damage for one attack
        /// Cast when health is below configured percentage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Shirahadori()
        {
            if (!OccultCrescentSettings.Instance.UseShirahadori)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Shirahadori.CanCast())
                return false;

            // Don't cast if we already have the buff
            if (Core.Me.HasAura(OCAuras.Shirahadori))
                return false;

            // Cast when health is below configured percentage
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.ShirahadoriHealthPercent)
                return false;

            return await OCSpells.Shirahadori.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Iainuki - cone attack with potency 300/500, chance to instantly kill
        /// AoE attack with 8y range
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Iainuki()
        {
            if (!OccultCrescentSettings.Instance.UseIainuki)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Iainuki.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.Iainuki.Range))
                return false;

            return await OCSpells.Iainuki.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Zeninage - consumes Occult Coffer for guaranteed strike with 1,500 potency
        /// Only cast if we have an Occult Coffer (can check by spell availability)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Zeninage()
        {
            if (!OccultCrescentSettings.Instance.UseZeninage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Zeninage.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.Zeninage.Range))
                return false;

            return await OCSpells.Zeninage.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Predict - cast a spell on a random party member
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Predict()
        {
            if (!OccultCrescentSettings.Instance.UsePredict)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Predict.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.Cleansing.Radius))
                return false;

            // Cast Predict and start tracking
            if (await OCSpells.Predict.Cast(Core.Me))
            {
                Logger.WriteInfo("[Oracle] Predict cast - starting new prediction cycle");
                _predictCasted = true;
                _predictCastTime = DateTime.Now;
                _seenPredictions.Clear();
                // _currentPrediction = 0; // Removed - unreliable tracking
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cast Phantom Judgment - cast a judgment on a random party member
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomJudgment()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomJudgment)
                return false;

            if (!OCSpells.PhantomJudgment.CanCast())
                return false;

            // Check if we have the correct prediction aura
            if (!Core.Me.HasAura(OCAuras.PredictionOfJudgment))
                return false;

            // Cast on self - Phantom Judgment affects area around caster
            return await OCSpells.PhantomJudgment.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Cleansing - cast a cleansing spell on a random party member
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Cleansing()
        {
            if (!OccultCrescentSettings.Instance.UseCleansing)
                return false;

            if (!OCSpells.Cleansing.CanCast())
                return false;

            // Check if we have the correct prediction aura
            if (!Core.Me.HasAura(OCAuras.PredictionOfCleansing))
                return false;

            // Cast on self - Cleansing affects area around caster
            return await OCSpells.Cleansing.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Blessing - cast a blessing spell on a random party member
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Blessing()
        {
            if (!OccultCrescentSettings.Instance.UseBlessing)
                return false;

            if (!OCSpells.Blessing.CanCast())
                return false;

            // Check if we have the correct prediction aura
            if (!Core.Me.HasAura(OCAuras.PredictionOfBlessing))
                return false;

            // Cast on self - Blessing affects self and nearby party members
            return await OCSpells.Blessing.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Starfall - cast a starfall spell on a random party member
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Starfall()
        {
            if (!OccultCrescentSettings.Instance.UseStarfall)
                return false;

            if (!OCSpells.Starfall.CanCast())
                return false;

            // Check if we have the correct prediction aura
            if (!Core.Me.HasAura(OCAuras.PredictionOfStarfall))
                return false;

            if (Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.StarfallHealthPercent)
                return false;

            // Cast on self - Starfall affects self and nearby enemies
            return await OCSpells.Starfall.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Phantom Rejuvenation - restores HP and MP of self or target
        /// Prioritizes tanks, then self, then other party members
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomRejuvenation()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomRejuvenation)
                return false;

            if (!OCSpells.PhantomRejuvenation.CanCast())
                return false;

            GameObject rejuvenationTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.PhantomRejuvenationCastOnAllies)
            {
                // First priority: Find tank who needs healing
                rejuvenationTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.IsTank() &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.PhantomRejuvenationHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // Second priority: Self if we need healing
                if (rejuvenationTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PhantomRejuvenationHealthPercent)
                    rejuvenationTarget = Core.Me;

                // Third priority: Any other party member who needs healing
                if (rejuvenationTarget == null)
                {
                    rejuvenationTarget = Group.CastableAlliesWithin30.Where(ally =>
                        ally.IsValid &&
                        ally.IsAlive &&
                        !ally.IsTank() &&
                        ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.PhantomRejuvenationHealthPercent)
                        .OrderBy(ally => ally.CurrentHealthPercent)
                        .FirstOrDefault();
                }
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PhantomRejuvenationHealthPercent)
                    rejuvenationTarget = Core.Me;
            }

            if (rejuvenationTarget == null)
                return false;

            return await OCSpells.PhantomRejuvenation.Cast(rejuvenationTarget);
        }

        /// <summary>
        /// Cast Invulnerability - grants invulnerability to party members only (cannot target self)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Invulnerability()
        {
            if (!OccultCrescentSettings.Instance.UseInvulnerability)
                return false;

            if (!OCSpells.Invulnerability.CanCast())
                return false;

            // Invulnerability can only be cast on party members, not self
            if (!OccultCrescentSettings.Instance.InvulnerabilityCastOnAllies)
                return false;

            // Find party member who desperately needs protection
            var invulnerabilityTarget = Group.CastableAlliesWithin30.Where(ally =>
                ally.IsValid &&
                ally.IsAlive &&
                ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.InvulnerabilityHealthPercent)
                .OrderBy(ally => ally.CurrentHealthPercent)
                .FirstOrDefault();

            if (invulnerabilityTarget == null)
                return false;

            return await OCSpells.Invulnerability.Cast(invulnerabilityTarget);
        }

        /// <summary>
        /// Cast Battle Bell - damage boost that stacks when taking damage
        /// Prioritizes tanks first (who take most damage), then self, then other party members
        /// Buff lasts 60 seconds, spell has 30 second cooldown
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> BattleBell()
        {
            if (!OccultCrescentSettings.Instance.UseBattleBell)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.BattleBell.CanCast())
                return false;

            GameObject battleBellTarget = null;

            // Always include self option - if enabled and we don't have the buff, prioritize self
            if (OccultCrescentSettings.Instance.BattleBellAlwaysIncludeSelf && !Core.Me.HasAura(OCAuras.BattleBell, msLeft: 1000))
            {
                battleBellTarget = Core.Me;
            }
            else
            {
                // Normal priority system
                // First priority: Find tank who doesn't have Battle Bell buff
                battleBellTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.IsTank() &&
                    !ally.HasAura(OCAuras.BattleBell, msLeft: 1000))
                    .OrderBy(ally => ally.CurrentHealthPercent) // Prioritize tank taking more damage
                    .FirstOrDefault();

                // Second priority: Self if we don't have the buff
                if (battleBellTarget == null && !Core.Me.HasAura(OCAuras.BattleBell, msLeft: 1000))
                    battleBellTarget = Core.Me;

                // Third priority: Any other party member who doesn't have the buff
                if (battleBellTarget == null)
                {
                    battleBellTarget = Group.CastableAlliesWithin30.Where(ally =>
                        ally.IsValid &&
                        ally.IsAlive &&
                        !ally.IsTank() &&
                        !ally.HasAura(OCAuras.BattleBell, msLeft: 1000))
                        .OrderBy(ally => ally.IsHealer() ? 1 : 0) // Prefer DPS over healers
                        .FirstOrDefault();
                }
            }

            if (battleBellTarget == null)
                return false;

            return await OCSpells.BattleBell.Cast(battleBellTarget);
        }

        /// <summary>
        /// Cast Sunbath - healing spell that restores HP
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Sunbath()
        {
            if (!OccultCrescentSettings.Instance.UseSunbath)
                return false;

            if (!OCSpells.Sunbath.CanCast())
                return false;

            GameObject healTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.SunbathCastOnAllies)
            {
                // Find party member who needs healing most
                healTarget = Group.CastableAlliesWithin15.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.SunbathHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need healing, check self
                if (healTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.SunbathHealthPercent)
                    healTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.SunbathHealthPercent)
                    healTarget = Core.Me;
            }

            if (healTarget == null)
                return false;

            return await OCSpells.Sunbath.Cast(healTarget);
        }

        /// <summary>
        /// Cast Cloudy Caress - increases healing potency by 30%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> CloudyCaress()
        {
            if (!OccultCrescentSettings.Instance.UseCloudyCaress)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.CloudyCaress.CanCast())
                return false;

            return await OCSpells.CloudyCaress.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Blessed Rain - erects a magical barrier which nullifies damage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> BlessedRain()
        {
            if (!OccultCrescentSettings.Instance.UseBlessedRain)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.BlessedRain.CanCast())
                return false;

            return await OCSpells.BlessedRain.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Misty Mirage - increases evasion by 40%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> MistyMirage()
        {
            if (!OccultCrescentSettings.Instance.UseMistyMirage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.MistyMirage.CanCast())
                return false;

            return await OCSpells.MistyMirage.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Hasty Mirage - increases movement speed by 20%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> HastyMirage()
        {
            if (!OccultCrescentSettings.Instance.UseHastyMirage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.HastyMirage.CanCast())
                return false;

            return await OCSpells.HastyMirage.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Aetherial Gain - increases damage dealt by 10%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> AetherialGain()
        {
            if (!OccultCrescentSettings.Instance.UseAetherialGain)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.AetherialGain.CanCast())
                return false;

            return await OCSpells.AetherialGain.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Ringing Respite - heals target when they take damage
        /// Similar to Battle Bell but focused on healing instead of damage boost
        /// Prioritizes tanks first (who take most damage), then self, then other party members
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> RingingRespite()
        {
            if (!OccultCrescentSettings.Instance.UseRingingRespite)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.RingingRespite.CanCast())
                return false;

            GameObject ringingRespiteTarget = null;

            // Always include self option - if enabled and we don't have the buff, prioritize self
            if (OccultCrescentSettings.Instance.RingingRespiteAlwaysIncludeSelf && !Core.Me.HasAura(OCAuras.RingingRespite, msLeft: 1000))
            {
                ringingRespiteTarget = Core.Me;
            }
            // Check if we should cast on allies
            else if (OccultCrescentSettings.Instance.RingingRespiteCastOnAllies)
            {
                // First priority: Find tank who doesn't have Ringing Respite buff
                ringingRespiteTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.IsTank() &&
                    !ally.HasAura(OCAuras.RingingRespite, msLeft: 1000))
                    .OrderBy(ally => ally.CurrentHealthPercent) // Prioritize tank taking more damage
                    .FirstOrDefault();

                // Second priority: Self if we don't have the buff
                if (ringingRespiteTarget == null && !Core.Me.HasAura(OCAuras.RingingRespite, msLeft: 1000))
                    ringingRespiteTarget = Core.Me;

                // Third priority: Any other party member who doesn't have the buff
                if (ringingRespiteTarget == null)
                {
                    ringingRespiteTarget = Group.CastableAlliesWithin30.Where(ally =>
                        ally.IsValid &&
                        ally.IsAlive &&
                        !ally.HasAura(OCAuras.RingingRespite, msLeft: 1000))
                        .OrderBy(ally => ally.IsHealer() ? 1 : 0) // Prefer DPS over healers
                        .FirstOrDefault();
                }
            }
            else
            {
                // Self-only mode: only check self
                if (!Core.Me.HasAura(OCAuras.RingingRespite))
                    ringingRespiteTarget = Core.Me;
            }

            if (ringingRespiteTarget == null)
                return false;

            return await OCSpells.RingingRespite.Cast(ringingRespiteTarget);
        }

        /// <summary>
        /// Cast Suspend - utility buff for jumping over obstacles
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Suspend()
        {
            if (!OccultCrescentSettings.Instance.UseSuspend)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Suspend.CanCast())
                return false;

            GameObject suspendTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.SuspendCastOnAllies)
            {
                // Find party member who doesn't have Suspend buff
                suspendTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    !ally.HasAura(OCAuras.Suspend, msLeft: 1000))
                    .FirstOrDefault();

                // If no allies need suspend, check self
                if (suspendTarget == null && !Core.Me.HasAura(OCAuras.Suspend, msLeft: 1000))
                    suspendTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (!Core.Me.HasAura(OCAuras.Suspend, msLeft: 1000))
                    suspendTarget = Core.Me;
            }

            if (suspendTarget == null)
                return false;

            return await OCSpells.Suspend.Cast(suspendTarget);
        }

        /// <summary>
        /// Main entry point for Ninja Dokumori gold farming
        /// Works for NIN jobs when in Occult Crescent content
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        public static async Task<bool> ExecuteNinjaDokumori()
        {
            // Check if we're Ninja
            if (Core.Me.CurrentJob != ClassJobType.Ninja)
                return false;

            // Check if Dokumori is enabled
            if (!OccultCrescentSettings.Instance.UseDokumori)
                return false;

            return await Dokumori();
        }

        /// <summary>
        /// Cast Dokumori - AoE steal ability for Ninja gold farming
        /// Similar to Phantom Thief's steal but affects multiple enemies
        /// Cast when any enemy within range is below configured HP threshold
        /// Only used for multi-target scenarios (2+ enemies) - single target uses normal rotation
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Dokumori()
        {
            if (!OccultCrescentSettings.Instance.UseDokumori)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Dokumori.CanCast())
                return false;

            // Check if we should skip single target usage when the setting is enabled
            var nearbyEnemies = Combat.Enemies.Count();
            if (nearbyEnemies < 2 && OccultCrescentSettings.Instance.DokumoriOnlyMultipleTargets)
                return false;

            // Find any enemy within spell range that's below the HP threshold
            var dokumoriTarget = Combat.Enemies.Where(enemy =>
                enemy.ValidAttackUnit() &&
                enemy.InLineOfSight() &&
                enemy.WithinSpellRange(OCSpells.Dokumori.Range) &&
                enemy.CurrentHealthPercent <= OccultCrescentSettings.Instance.DokumoriHealthPercent)
                .OrderBy(enemy => enemy.CurrentHealthPercent) // Prioritize lowest HP for finishing blow
                .FirstOrDefault();

            if (dokumoriTarget == null)
                return false;

            return await OCSpells.Dokumori.Cast(dokumoriTarget);
        }
    }
}
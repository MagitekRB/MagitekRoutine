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
            Suspend = 4258,
            PoisedToSwordDance = 4794,
            TemptedToTango = 4795,
            Jitterbugged = 4796,
            WillingToWaltz = 4797,
            Quickstep = 4798,
            QuickerStep = 4799,
            SteadfastStance = 4800,
            Enamored = 4801,
            Mesmerized = 4802,
            MagicShell = 4788,
            HonedSpellblade = 4789,
            FinishingFervor = 4793,
            Defend = 4792,
            // Elemental weaknesses revealed on an enemy by Occult Libra (North Horn). A matching
            // elemental attack casts at bonus potency while the aura is up.
            FireWeakness = 5322,
            IceWeakness = 5323,
            LightningWeakness = 5324,
            WindWeakness = 5325,
            // Occult Toad (Phantom Black Mage). 20s; the target's damage dealt drops by 99% and
            // it cannot use any action but its auto-attack. Plenty of enemies are flatly immune -
            // OccultDebuffImmunityTracker learns which ones.
            OccultToad = 5317,
            // Phantom Blue Mage. Occult Mighty Guard is -20% damage taken for 15s.
            OccultMightyGuard = 5321,
            // Phantom Ninja. Smoke is +20% evasion for 90s.
            Smoke = 5327,
            // Image grants three stacks, each nullifying one physical attack, for 30s. Confirmed
            // in game; note it sits outside the 5316-5335 band the rest of North Horn uses.
            Image = 4873,
            // Phantom Necromancer. Drain Touch lasts 6s, floors our HP at 1, and raises every
            // other Necromancer action from 300 to 400 potency. It is also the switch that decides
            // whether an attack Dooms us, so the rotation reads it as a safety gate, not just a buff.
            // Necromancer's self-Doom needs no constant here: it is the game's ordinary Doom,
            // already declared as Auras.Doom (1769).
            DrainTouch = 5326;

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

        // Phantom Dancer Spells (Job ID: 4805)
        public static readonly SpellData Dance = DataManager.GetSpellData(46598);
        public static readonly SpellData PhantomSwordDance = DataManager.GetSpellData(46599);
        public static readonly SpellData TemptingTango = DataManager.GetSpellData(46600);
        public static readonly SpellData Jitterbug = DataManager.GetSpellData(46601);
        public static readonly SpellData MysteryWaltz = DataManager.GetSpellData(46602);
        public static readonly SpellData Quickstep = DataManager.GetSpellData(46603);
        public static readonly SpellData SteadfastStance = DataManager.GetSpellData(46604);
        public static readonly SpellData Mesmerize = DataManager.GetSpellData(46605);

        // Freelancer Spells
        public static readonly SpellData InquiringMind = DataManager.GetSpellData(46606);

        // Phantom Mystic Knight Spells (Job ID: 4803)
        public static readonly SpellData MagicShell = DataManager.GetSpellData(46590);
        public static readonly SpellData SunderingSpellblade = DataManager.GetSpellData(46591);
        public static readonly SpellData HolySpellblade = DataManager.GetSpellData(46592);
        public static readonly SpellData BlazingSpellblade = DataManager.GetSpellData(46593);

        // Phantom Gladiator Spells (Job ID: 4804)
        public static readonly SpellData Finisher = DataManager.GetSpellData(46594);
        public static readonly SpellData Defend = DataManager.GetSpellData(46595);
        public static readonly SpellData LongReach = DataManager.GetSpellData(46596);
        public static readonly SpellData Bladeblitz = DataManager.GetSpellData(46597);

        // Phantom Red Mage Spells (Job ID: 5334)
        // The three nukes share a single 30s recast timer.
        public static readonly SpellData OccultFireII = DataManager.GetSpellData(49092);
        // Phantom White Mage has its own, different "Occult Cure II" action (49067), so this
        // field carries the job name to keep the two apart.
        public static readonly SpellData RedMageOccultCureII = DataManager.GetSpellData(49093);
        public static readonly SpellData OccultLibra = DataManager.GetSpellData(49094);
        public static readonly SpellData OccultBlizzardII = DataManager.GetSpellData(49095);
        public static readonly SpellData OccultThunderII = DataManager.GetSpellData(49096);

        // Phantom Blue Mage Spells (Job ID: 5333)
        // Every action except Occult Aero has to be LEARNED from a specific enemy, gated behind
        // the Occult Learning I/II/III traits, so CanCast does the real work here - a freshly
        // unlocked Blue Mage knows nothing but Aero. The Aero line is an upgrade chain: each
        // replaces the previous on learning, and all three share one 30s recast.
        //
        // The client also carries enemy-cast twins of four of these (50570, 50611, 50627, 50628).
        // Verified live: those have Range 0 and a 1.5s recast, they are what the monsters cast for
        // Blue Mage to learn from, and GetMaskedAction never resolves a player action to one.
        // Do not use them.
        public static readonly SpellData OccultAero = DataManager.GetSpellData(49085);
        public static readonly SpellData OccultMissile = DataManager.GetSpellData(49086);
        public static readonly SpellData OccultAquaBreath = DataManager.GetSpellData(49087);
        public static readonly SpellData OccultMightyGuard = DataManager.GetSpellData(49088);
        public static readonly SpellData OccultAeroII = DataManager.GetSpellData(49089);
        public static readonly SpellData OccultWhiteWind = DataManager.GetSpellData(49090);
        public static readonly SpellData OccultAeroIII = DataManager.GetSpellData(49091);

        // Phantom Summoner Spells (Job ID: 5332)
        // Hellfire, Judgment Bolt and Thunderstorm share a single 60s recast timer. Thunderstorm
        // is the only Wind attack any implemented phantom job has. No traits on this job.
        public static readonly SpellData Hellfire = DataManager.GetSpellData(49080);
        public static readonly SpellData JudgmentBolt = DataManager.GetSpellData(49081);
        public static readonly SpellData EarthenWall = DataManager.GetSpellData(49082);
        public static readonly SpellData Thunderstorm = DataManager.GetSpellData(49083);
        public static readonly SpellData Megaflare = DataManager.GetSpellData(49084);

        // Phantom Dragoon Spells (Job ID: 5331)
        // Step Forth (49078) is deliberately not defined - it is a pure positioning dash
        // (ground-targeted, 10 yalms in a chosen direction) with no combat value the routine can
        // judge. Level 4 is the Enhanced Occult Jump trait, which is passive.
        public static readonly SpellData OccultJump = DataManager.GetSpellData(49077);
        public static readonly SpellData Lance = DataManager.GetSpellData(49079);

        // Phantom Ninja Spells (Job ID: 5328)
        // Lightning Scroll and Flame Scroll share a single 60s recast timer. Level 5 is the
        // First Strike trait, which is passive and needs no rotation code.
        public static readonly SpellData FumaShuriken = DataManager.GetSpellData(49062);
        public static readonly SpellData Smoke = DataManager.GetSpellData(49063);
        public static readonly SpellData LightningScroll = DataManager.GetSpellData(49064);
        public static readonly SpellData FlameScroll = DataManager.GetSpellData(49065);
        public static readonly SpellData Image = DataManager.GetSpellData(49066);

        // Phantom White Mage Spells (Job ID: 5329)
        // Phantom Red Mage has its own, different "Occult Cure II" action (49093), so both carry
        // their job name. Occult Blink (49069) is deliberately not defined - it grants one
        // magic-damage immunity and is only useful against specific scripted mechanics, which
        // the routine has no way to anticipate.
        public static readonly SpellData WhiteMageOccultCureII = DataManager.GetSpellData(49067);
        public static readonly SpellData WhiteMageOccultCureIII = DataManager.GetSpellData(49068);
        public static readonly SpellData OccultRaise = DataManager.GetSpellData(49070);
        public static readonly SpellData OccultHoly = DataManager.GetSpellData(49071);

        // Phantom Black Mage Spells (Job ID: 5330)
        // Fire III / Blizzard III / Thunder III share a single 40s recast timer, the same way
        // Red Mage's II-tier nukes share a 30s one.
        public static readonly SpellData OccultFireIII = DataManager.GetSpellData(49072);
        public static readonly SpellData OccultBlizzardIII = DataManager.GetSpellData(49073);
        public static readonly SpellData OccultThunderIII = DataManager.GetSpellData(49074);
        public static readonly SpellData OccultToad = DataManager.GetSpellData(49075);
        public static readonly SpellData OccultFlare = DataManager.GetSpellData(49076);

        // Phantom Necromancer Spells (Job ID: 5335)
        // Deep Freeze / Hell Wind / Chaos Drive share a single 40s recast. Doomsday has its own
        // 120s timer, which is why it needs the Drain Touch guard separately rather than inheriting
        // it from the shared window. All four cost 10% of maximum HP; Drain Touch costs none.
        public static readonly SpellData DrainTouch = DataManager.GetSpellData(49097);
        public static readonly SpellData DeepFreeze = DataManager.GetSpellData(49098);
        public static readonly SpellData HellWind = DataManager.GetSpellData(49099);
        public static readonly SpellData ChaosDrive = DataManager.GetSpellData(49100);
        public static readonly SpellData Doomsday = DataManager.GetSpellData(49101);
    }

    internal class OccultCrescent
    {
        private const ushort SouthHornZoneId = 1252;
        private const ushort NorthHornZoneId = 1346;

        private static readonly HashSet<ushort> OccultCrescentZoneIds = new()
        {
            SouthHornZoneId,
            NorthHornZoneId
        };

        /// <summary>
        /// Each Horn must be added here by zone id when it ships. Matching on the zone name instead is
        /// not an option: WorldManager.CurrentZoneName returns the map's own name ("South Horn",
        /// "North Horn") and never contains "Occult Crescent", so a name check can never match.
        /// </summary>
        public static bool IsInOccultCrescent()
        {
            return OccultCrescentZoneIds.Contains(WorldManager.ZoneId);
        }

        // Known Knowledge Crystal locations per zone. These never change, and each Horn has its own
        // coordinate space, so the lists are keyed by zone id.
        private static readonly Dictionary<ushort, Vector3[]> KnowledgeCrystalLocations = new()
        {
            [SouthHornZoneId] = new[]
            {
                new Vector3(835.9902f, 75.12211f, -709.3925f),
                new Vector3(-165.9937f, 8.5f, -616.4979f),
                new Vector3(-347.2297f, 102.3273f, -124.1305f),
                new Vector3(-393.0761f, 99.51316f, 278.7158f),
                new Vector3(302.5914f, 105f, 313.6591f)
            },
            [NorthHornZoneId] = new[]
            {
                // Overworld crystals, each read off the live crystal object in game.
                new Vector3(884.896f, 259.5558f, 875.1169f),    // base camp
                new Vector3(-542.5715f, 68.6256f, 598.2891f),
                new Vector3(-382.6042f, 41.22726f, -442.7764f),
                new Vector3(456.5246f, 71.46682f, 524.4749f),
                new Vector3(350.6741f, 46.45173f, -558.5289f),
                new Vector3(-18.32449f, 3.79342f, -37.40308f),
                // Forked Tower shares this zone id but sits far below the overworld. These came from
                // session logs rather than a live reading, so they are approximate and unconfirmed.
                new Vector3(597.8f, -700f, 927f),               // Forked Tower entrance
                new Vector3(-893f, -984.7401f, 780f),           // Forked Tower
                new Vector3(-900f, -986.1f, 782.2488f),
                new Vector3(103f, -706.7383f, 678f),
                new Vector3(0f, -722.6936f, -367f),
                new Vector3(603.5453f, -672.6606f, 640.6041f),
                new Vector3(599.4f, -700.0f, 927.8f),           // Forked Tower, Lower Vestibule
                new Vector3(603.7968f, -670.6514f, -125.1157f)
            }
        };

        // Respawn point locations per zone. North Horn has more than one: the open field returns
        // players to base camp, while the Forked Tower returns them to its own entrance points.
        private static readonly Dictionary<ushort, Vector3[]> RespawnPoints = new()
        {
            [SouthHornZoneId] = new[] { new Vector3(851.87665f, 73.13358f, -704.79004f) },
            [NorthHornZoneId] = new[]
            {
                new Vector3(905.57f, 259.88f, 905.97f),   // base camp
                new Vector3(600.2f, -700f, 975f),         // Forked Tower
                new Vector3(706f, -709.8f, 184f),
                new Vector3(800.2f, -600f, -677.6f),
                new Vector3(100.1f, -691.5f, 496.9f),
                new Vector3(600.2f, -674f, 703.1f)
            }
        };

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
            { 4364, PhantomJob.Geomancer },
            { 4805, PhantomJob.Dancer },
            { 4803, PhantomJob.MysticKnight },
            { 4804, PhantomJob.Gladiator },
            // Added with North Horn. Without these the current job reads as None and every Occult
            // Crescent feature bails out, which is why nothing worked while playing one of them.
            { 5328, PhantomJob.Ninja },
            { 5329, PhantomJob.WhiteMage },
            { 5330, PhantomJob.BlackMage },
            { 5331, PhantomJob.Dragoon },
            { 5332, PhantomJob.Summoner },
            { 5333, PhantomJob.BlueMage },
            { 5334, PhantomJob.RedMage },
            { 5335, PhantomJob.Necromancer }
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
            Geomancer,
            Dancer,
            MysticKnight,
            Gladiator,
            Ninja,
            WhiteMage,
            BlackMage,
            Dragoon,
            Summoner,
            BlueMage,
            RedMage,
            Necromancer
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
            if (!KnowledgeCrystalLocations.TryGetValue(WorldManager.ZoneId, out var crystalLocations))
                return false;

            foreach (var crystalLocation in crystalLocations)
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
            if (!IsInOccultCrescent())
                return false;

            // Adjudicate any debuff we cast on a previous pulse. This sits above every path that
            // can return true, so a pending Occult Toad or Slowga still gets a verdict on the
            // pulses where an ability fires.
            OccultDebuffImmunityTracker.Update();

            // First, try automatic phantom job switching for knowledge crystal buffs. This runs before
            // the phantom-job guard below because Freelancer grants no phantom job aura, so a player
            // standing at a crystal as Freelancer reads as None — and they are exactly who the
            // Inquiring Mind path serves. The switcher carries its own gating.
            if (await PhantomJobSwitcher.AutoSwitchForKnowledgeCrystalBuffs())
                return true;

            // Get the current phantom job
            var phantomJob = GetCurrentPhantomJob();
            if (phantomJob == PhantomJob.None)
                return false;

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
                PhantomJob.Dancer => await ExecuteDancerPhantomJob(),
                PhantomJob.MysticKnight => await ExecuteMysticKnightPhantomJob(),
                PhantomJob.Gladiator => await ExecuteGladiatorPhantomJob(),
                PhantomJob.RedMage => await ExecuteRedMagePhantomJob(),
                PhantomJob.BlackMage => await ExecuteBlackMagePhantomJob(),
                PhantomJob.WhiteMage => await ExecuteWhiteMagePhantomJob(),
                PhantomJob.Ninja => await ExecuteNinjaPhantomJob(),
                PhantomJob.Dragoon => await ExecuteDragoonPhantomJob(),
                PhantomJob.Summoner => await ExecuteSummonerPhantomJob(),
                PhantomJob.BlueMage => await ExecuteBlueMagePhantomJob(),
                PhantomJob.Necromancer => await ExecuteNecromancerPhantomJob(),
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
            if (!IsInOccultCrescent())
                return false;

            // Check if non-party resurrection is enabled
            if (!OccultCrescentSettings.Instance.ReviveNonPartyPlayers)
                return false;

            // Throttle non-party resurrection checks to every 2 seconds for performance
            var now = DateTime.Now;
            if (now - _lastNonPartyResCheck < NonPartyResCheckInterval)
                return false;
            _lastNonPartyResCheck = now;

            // Don't resurrect someone outside the party while a party member is down — the standard
            // raise path owns party rez and gets priority for any instant-cast sources.
            if (Group.DeadAllies.Any())
                return false;

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
            //
            // Raise range is a raw 3D distance on purpose, and is the exception to the
            // "always use WithinSpellRange" rule in AGENTS.md. A corpse has no usable
            // CombatReach for the game to measure against, so raise range is centre to
            // centre. WithinSpellRange would subtract both hitboxes and, because it uses
            // Distance2D, ignore height entirely — which matters a lot here, where the
            // Forked Tower sits roughly a thousand yalms below the overworld in the same
            // zone. Leave this as Distance.
            var deadNonPartyPlayers = Group.CastableAlliance.Where(u => u.CurrentHealth == 0 &&
                                                       !u.HasAura(Auras.Raise) &&
                                                       u.Distance(Core.Me) <= 30 &&
                                                       u.IsVisible &&
                                                       u.InLineOfSight() &&
                                                       u.IsTargetable &&
                                                       // Skip anyone stood at one of the zone's respawn points, since
                                                       // they have chosen to return rather than wait for a raise.
                                                       (!RespawnPoints.TryGetValue(WorldManager.ZoneId, out var respawnPoints)
                                                        || respawnPoints.All(p => u.Location.DistanceSqr(p) >= 900)));

            if (!deadNonPartyPlayers.Any())
                return false;

            // Select the best candidate (prioritize by job role like normal resurrection)
            var resurrectTarget = deadNonPartyPlayers
                .OrderByDescending(player => player.GetResurrectionWeight())
                .FirstOrDefault();

            if (resurrectTarget == null)
                return false;

            // Get the current phantom job first.
            //
            // Both phantom raises are instant, so they need no Swiftcast handling, and Occult
            // Raise additionally works on targets flagged Resurrection Restricted - so prefer
            // them when they are actually available.
            //
            // These must FALL THROUGH rather than return false when the phantom raise cannot be
            // cast. Phantom abilities unlock by phantom job level (Occult Raise at 4), so a real
            // White Mage running phantom White Mage below that level would otherwise lose their
            // own Raise entirely: the branch matches, CanCast fails, and the real-job switch below
            // is never reached. The same applies while the phantom raise sits on its short recast.
            var phantomJob = GetCurrentPhantomJob();

            if (phantomJob == PhantomJob.Chemist && OCSpells.Revive.CanCast(resurrectTarget))
                return await OCSpells.Revive.CastAura(resurrectTarget, Auras.Raise);

            if (phantomJob == PhantomJob.WhiteMage && OCSpells.OccultRaise.CanCast(resurrectTarget))
                return await OCSpells.OccultRaise.CastAura(resurrectTarget, Auras.Raise);

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
                    // Re-validate the target each iteration: another player can rez/LoS-break the same
                    // corpse mid-loop, which would otherwise spin here (CastAura keeps failing without
                    // consuming Swiftcast) until the aura expires ~10s later, stalling the whole routine.
                    // Being alive again is the case the other checks miss: someone else's raise can be
                    // accepted mid-loop, leaving the target valid, targetable, in range and carrying no
                    // pending Raise aura — so without this the loop spins until Swiftcast expires.
                    while (Core.Me.HasAura(Auras.Swiftcast)
                           && target != null && target.IsValid && target.IsTargetable
                           && (target as Character)?.CurrentHealth == 0
                           && !target.HasAura(Auras.Raise)
                           && target.WithinSpellRange(30) && target.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Don't cast if target has Silver Sickness unless it expires in 20 seconds or less
            if (Core.Me.CurrentTarget.HasAura(OCAuras.SilverSickness, msLeft: 20000))
                return false;

            // Don't cast if target has Enamored or Mesmerized (conflicts with SilverSickness)
            if (Core.Me.CurrentTarget.HasAura(OCAuras.Mesmerized))
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

            // Find dead allies using the same logic as Healer.Raise — including the raw 3D
            // distance, which is deliberate there and must stay deliberate here. It is the
            // exception to the "always use WithinSpellRange" rule in AGENTS.md: a corpse has
            // no usable CombatReach, so raise range is centre to centre, and WithinSpellRange
            // uses Distance2D so it would ignore height entirely. Leave this as Distance.
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

            // Need a valid attackable target
            if (Core.Me.CurrentTarget is not BattleCharacter battleTarget)
                return false;

            if (!battleTarget.ValidAttackUnit() || !battleTarget.InLineOfSight())
                return false;

            // Check difficulty - high difficulty enemies are often immune to CC
            if (battleTarget.RawDifficulty >= 2)
                return false;

            if (battleTarget.DifficultyEstimate != DifficultyEstimate.Normal)
                return false;

            // FATE enemies might have different immunity rules
            if (battleTarget.IsFate)
                return false;

            // Check if target is within spell range
            if (!battleTarget.WithinSpellRange(OCSpells.OccultSlowga.Range))
                return false;

            // Slow shares an immunity set with Occult Toad as far as we can tell, so an enemy
            // that has already refused either one is skipped here too. This also covers the
            // "target is already slowed" case.
            if (!OccultDebuffImmunityTracker.IsWorthAttempting(battleTarget, OCAuras.Slow))
                return false;

            if (!await OCSpells.OccultSlowga.Cast(battleTarget))
                return false;

            OccultDebuffImmunityTracker.RecordAttempt(battleTarget, OCAuras.Slow, OCSpells.OccultSlowga.AdjustedCastTime);
            return true;
        }

        /// <summary>
        /// Cast OccultComet - AoE damage with 8s cast time
        /// Can be restricted to only cast with job-specific cast time reduction buffs
        /// Will automatically try to use Swiftcast if available and restriction is enabled
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultComet.Range))
                return false;

            // If job-specific buff restriction is enabled, handle it intelligently
            if (OccultCrescentSettings.Instance.OccultCometOnlyWithJobSpecificBuffs)
            {
                return await CastOccultCometWithJobSpecificBuffs();
            }

            // No restrictions - cast normally
            return await OCSpells.OccultComet.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Attempts to cast OccultComet with job-specific cast time reduction buffs
        /// Tries to use Swiftcast if available and allowed by settings
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> CastOccultCometWithJobSpecificBuffs()
        {
            // First check if we already have a job-specific buff active
            if (HasJobSpecificCastTimeReductionBuff())
            {
                return await OCSpells.OccultComet.Cast(Core.Me.CurrentTarget);
            }

            // Try to use Swiftcast if available and allowed by settings
            if (Spells.Swiftcast.IsKnownAndReady() && OccultCrescentSettings.Instance.OccultCometAllowSwiftcast)
            {
                if (await CastSwiftcastThenComet())
                    return true;
            }

            // No suitable buffs available
            return false;
        }

        /// <summary>
        /// Checks if we currently have any job-specific cast time reduction buffs
        /// </summary>
        /// <returns>True if we have a relevant buff active</returns>
        private static bool HasJobSpecificCastTimeReductionBuff()
        {
            // Check for Dualcast (RDM)
            if (Core.Me.HasAura(Auras.Dualcast))
                return true;

            // Check for Requiescat (PLD)
            if (Core.Me.HasAura(Auras.Requiescat))
                return true;

            // Check for Occult Quick (Time Mage)
            if (Core.Me.HasAura(OCAuras.OccultQuick))
                return true;

            // Check for Swiftcast (any job that has it)
            if (Core.Me.HasAura(Auras.Swiftcast))
                return true;

            return false;
        }

        /// <summary>
        /// Casts Swiftcast then OccultComet (similar to resurrection logic)
        /// </summary>
        /// <returns>True if both spells were cast successfully</returns>
        private static async Task<bool> CastSwiftcastThenComet()
        {
            // Cast Swiftcast first
            if (!await Spells.Swiftcast.Cast(Core.Me))
                return false;

            // Wait for Swiftcast buff to apply
            await Coroutine.Wait(2000, () => Core.Me.HasAura(Auras.Swiftcast));

            // Cast OccultComet while we have Swiftcast
            while (Core.Me.HasAura(Auras.Swiftcast))
            {
                if (await OCSpells.OccultComet.Cast(Core.Me.CurrentTarget))
                    return true;
                await Coroutine.Yield();
            }

            return false;
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

            if (!Core.Me.InCombat)
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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
            if (!Core.Me.CurrentTarget.ValidDamageTarget() || !Core.Me.CurrentTarget.InLineOfSight())
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

        #region Phantom Dancer (Job ID: 4805)
        /// <summary>
        /// Execute Phantom Dancer phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteDancerPhantomJob()
        {
            // Steadfast Stance - barrier for self or party members
            if (await SteadfastStance())
                return true;

            // Mesmerize - debuff enemy (conflicts with SilverSickness)
            if (await Mesmerize())
                return true;

            // Quickstep - evasion buff, or knowledge crystal buff if near crystal
            if (await Quickstep())
                return true;

            // Dance - masked action that becomes one of four abilities based on aura
            if (await Dance())
                return true;

            return false;
        }

        /// <summary>
        /// Cast Dance - masked action that becomes Phantom Sword Dance, Tempting Tango, Jitterbug, or Mystery Waltz based on aura
        /// Dance grants one of four statuses (Poised to Sword Dance, Tempted to Tango, Jitterbugged, Willing to Waltz),
        /// then becomes the corresponding attack ability when cast again
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Dance()
        {
            if (!OccultCrescentSettings.Instance.UseDance)
                return false;

            if (!Core.Me.InCombat)
                return false;

            // Check if we have a target
            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidAttackUnit() || !target.InLineOfSight())
                return false;

            // Check if target is within spell range (all dance abilities have 30y range)
            if (!target.WithinSpellRange(30.0f))
                return false;

            // Check which aura we have and cast the corresponding ability
            // Since Masked() doesn't work reliably, we check each aura and cast the corresponding spell directly
            if (Core.Me.HasAura(OCAuras.PoisedToSwordDance))
            {
                if (OCSpells.PhantomSwordDance.CanCast(target))
                    return await OCSpells.PhantomSwordDance.Cast(target);
            }
            else if (Core.Me.HasAura(OCAuras.TemptedToTango))
            {
                if (OCSpells.TemptingTango.CanCast(target))
                    return await OCSpells.TemptingTango.Cast(target);
            }
            else if (Core.Me.HasAura(OCAuras.Jitterbugged))
            {
                if (OCSpells.Jitterbug.CanCast(target))
                    return await OCSpells.Jitterbug.Cast(target);
            }
            else if (Core.Me.HasAura(OCAuras.WillingToWaltz))
            {
                if (OCSpells.MysteryWaltz.CanCast(target))
                    return await OCSpells.MysteryWaltz.Cast(target);
            }
            else
            {
                // No aura active, cast Dance to get an aura
                if (OCSpells.Dance.CanCast(target))
                    return await OCSpells.Dance.Cast(target);
            }

            return false;
        }

        /// <summary>
        /// Cast Quickstep - increases evasion by 20% for 90s (combat only)
        /// Knowledge crystal party buff casting is now handled by PhantomJobSwitcher
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Quickstep()
        {
            if (!OccultCrescentSettings.Instance.UseQuickstep)
                return false;

            // Only used in combat for evasion buff, knowledge crystal party buff handled by PhantomJobSwitcher
            if (!Core.Me.InCombat)
                return false;

            // Check if we already have the Quickstep evasion aura (don't recast unnecessarily)
            if (Core.Me.HasAura(OCAuras.Quickstep, msLeft: 1000))
                return false;

            if (!OCSpells.Quickstep.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidAttackUnit())
                return false;

            // Check if target is targeting us
            if (!Core.Me.BeingTargetedBy(target))
                return false;

            // Check if we're at configured HP% or lower
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.QuickstepHealthPercent)
                return false;

            return await OCSpells.Quickstep.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Steadfast Stance - grants barrier that absorbs damage equal to 10% of max HP for 30s
        /// Can be cast on self or party members
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> SteadfastStance()
        {
            if (!OccultCrescentSettings.Instance.UseSteadfastStance)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.SteadfastStance.CanCast())
                return false;

            GameObject stanceTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.SteadfastStanceCastOnAllies)
            {
                // Find party member who doesn't have Steadfast Stance buff and is below HP threshold
                stanceTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    !ally.HasAura(OCAuras.SteadfastStance, msLeft: 1000) &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.SteadfastStanceHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent) // Prioritize lower HP
                    .FirstOrDefault();

                // If no allies need it, check self
                if (stanceTarget == null &&
                    !Core.Me.HasAura(OCAuras.SteadfastStance, msLeft: 1000) &&
                    Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.SteadfastStanceHealthPercent)
                    stanceTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self if below HP threshold
                if (!Core.Me.HasAura(OCAuras.SteadfastStance, msLeft: 1000) &&
                    Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.SteadfastStanceHealthPercent)
                    stanceTarget = Core.Me;
            }

            if (stanceTarget == null)
                return false;

            // Check if target is within spell range
            if (!stanceTarget.WithinSpellRange(OCSpells.SteadfastStance.Range))
                return false;

            return await OCSpells.SteadfastStance.Cast(stanceTarget);
        }

        /// <summary>
        /// Cast Mesmerize - afflicts target with Enamored (40% damage reduction for 4s) and Mesmerized (10% damage reduction, 5% damage taken increase for 100s)
        /// Cannot be cast if target has SilverSickness (conflicts)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Mesmerize()
        {
            if (!OccultCrescentSettings.Instance.UseMesmerize)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Mesmerize.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidAttackUnit() || !target.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!target.WithinSpellRange(OCSpells.Mesmerize.Range))
                return false;

            // Don't cast if target has SilverSickness (conflicts with Mesmerized)
            if (target.HasAura(OCAuras.SilverSickness))
                return false;

            // Don't recast if target already has Mesmerized (the long duration debuff)
            if (target.HasAura(OCAuras.Mesmerized, msLeft: 5000))
                return false;

            return await OCSpells.Mesmerize.Cast(target);
        }
        #endregion

        #region Phantom Mystic Knight (Job ID: 4803)
        /// <summary>
        /// Magic Shell - Creates a barrier absorbing damage equal to 20% of max HP for 60s
        /// When depleted, grants Honed Spellblade for 30s
        /// Can be cast on self or party members
        /// </summary>
        private static async Task<bool> MagicShell()
        {
            if (!OccultCrescentSettings.Instance.UseMagicShell)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.MagicShell.CanCast())
                return false;

            // Don't cast if we have Honed Spellblade (barrier was just depleted)
            if (Core.Me.HasAura(OCAuras.HonedSpellblade))
                return false;

            GameObject shellTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.MagicShellCastOnAllies)
            {
                // Prioritize tanks, then lowest HP party member who needs it
                shellTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    !ally.HasAura(OCAuras.MagicShell) &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.MagicShellHealthPercent)
                    .OrderByDescending(ally => ally.IsTank()) // Tanks first
                    .ThenBy(ally => ally.CurrentHealthPercent) // Then lowest HP
                    .FirstOrDefault();

                // If no allies need it, check self if below HP threshold
                if (shellTarget == null &&
                    !Core.Me.HasAura(OCAuras.MagicShell) &&
                    Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MagicShellHealthPercent)
                    shellTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self if below HP threshold
                if (!Core.Me.HasAura(OCAuras.MagicShell) &&
                    Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MagicShellHealthPercent)
                    shellTarget = Core.Me;
            }

            if (shellTarget == null)
                return false;

            // Check if target is within spell range
            if (!shellTarget.WithinSpellRange(OCSpells.MagicShell.Range))
                return false;

            return await OCSpells.MagicShell.Cast(shellTarget);
        }

        /// <summary>
        /// Sundering Spellblade - Deals 200 potency (300 with Honed Spellblade)
        /// 20% chance to Petrify target
        /// </summary>
        private static async Task<bool> SunderingSpellblade()
        {
            if (!OccultCrescentSettings.Instance.UseSunderingSpellblade)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.SunderingSpellblade.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.SunderingSpellblade.Range))
                return false;

            return await OCSpells.SunderingSpellblade.Cast(target);
        }

        /// <summary>
        /// Holy Spellblade - Deals 300 potency (500 vs undead)
        /// 400 potency (600 vs undead) with Honed Spellblade
        /// </summary>
        private static async Task<bool> HolySpellblade()
        {
            if (!OccultCrescentSettings.Instance.UseHolySpellblade)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.HolySpellblade.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.HolySpellblade.Range))
                return false;

            return await OCSpells.HolySpellblade.Cast(target);
        }

        /// <summary>
        /// Blazing Spellblade - Deals 200 potency (300 with Honed Spellblade)
        /// Increases target's damage taken by 5% and caster's damage dealt by 5% for 70s
        /// </summary>
        private static async Task<bool> BlazingSpellblade()
        {
            if (!OccultCrescentSettings.Instance.UseBlazingSpellblade)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.BlazingSpellblade.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.BlazingSpellblade.Range))
                return false;

            // Don't recast if target already has Vulnerability Up (70s duration)
            if (target.HasAura(Auras.VulnerabilityUp))
                return false;

            return await OCSpells.BlazingSpellblade.Cast(target);
        }

        /// <summary>
        /// Execute Phantom Mystic Knight phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteMysticKnightPhantomJob()
        {
            // Magic Shell - defensive barrier, priority over offensive spells
            if (await MagicShell())
                return true;

            // Blazing Spellblade - damage buff (5% damage dealt, 5% vulnerability), high priority
            if (await BlazingSpellblade())
                return true;

            // Holy Spellblade - high potency, especially vs undead
            if (await HolySpellblade())
                return true;

            // Sundering Spellblade - lower potency but has petrify chance
            if (await SunderingSpellblade())
                return true;

            return false;
        }
        #endregion

        #region Phantom Gladiator (Job ID: 4804)
        /// <summary>
        /// Defend - Reduces damage taken by 50% for 5s
        /// Grants Finishing Fervor stack when damage taken (max 4 stacks, 120s)
        /// </summary>
        private static async Task<bool> Defend()
        {
            if (!OccultCrescentSettings.Instance.UseDefend)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Defend.CanCast())
                return false;

            // Don't recast if Defend buff is already active
            if (Core.Me.HasAura(OCAuras.Defend))
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidAttackUnit())
                return false;

            // Always cast if target is targeting us (to build Finishing Fervor stacks)
            if (Core.Me.BeingTargetedBy(target))
                return await OCSpells.Defend.Cast(Core.Me);

            // Otherwise cast as defensive when below HP threshold
            if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.DefendHealthPercent)
                return await OCSpells.Defend.Cast(Core.Me);

            return false;
        }

        /// <summary>
        /// Finisher - Multi-outcome attack (600/1000 potency, 25% instakill chance)
        /// Improved by Finishing Fervor stacks from Defend
        /// </summary>
        private static async Task<bool> Finisher()
        {
            if (!OccultCrescentSettings.Instance.UseFinisher)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Finisher.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.Finisher.Range))
                return false;

            return await OCSpells.Finisher.Cast(target);
        }

        /// <summary>
        /// Long Reach - Long-range attack with 400 potency
        /// </summary>
        private static async Task<bool> LongReach()
        {
            if (!OccultCrescentSettings.Instance.UseLongReach)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.LongReach.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.LongReach.Range))
                return false;

            return await OCSpells.LongReach.Cast(target);
        }

        /// <summary>
        /// Bladeblitz - Deals 600 potency damage to all nearby enemies (radius-based AoE)
        /// </summary>
        private static async Task<bool> Bladeblitz()
        {
            if (!OccultCrescentSettings.Instance.UseBladeblitz)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Bladeblitz.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.Bladeblitz.Radius))
                return false;

            return await OCSpells.Bladeblitz.Cast(Core.Me);
        }

        /// <summary>
        /// Execute Phantom Gladiator phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteGladiatorPhantomJob()
        {
            // Defend - defensive, builds Finishing Fervor stacks
            if (await Defend())
                return true;

            // Bladeblitz - AoE attack (3+ enemies)
            if (await Bladeblitz())
                return true;

            // Long Reach - filler/ranged attack
            if (await LongReach())
                return true;

            // Finisher - main single-target (benefits from Finishing Fervor stacks)
            if (await Finisher())
                return true;

            return false;
        }
        #endregion

        #region Shared phantom job helpers
        /// <summary>
        /// Casts one of a set of elemental attacks that share a single recast timer. Phantom Red
        /// Mage, Black Mage and Ninja all work this way, differing only in tier and in how many
        /// elements they cover, so each caller passes its own candidates.
        /// </summary>
        private static async Task<bool> SharedRecastElementalNukes(params (SpellData Spell, bool Enabled, uint WeaknessAura)[] candidates)
        {
            if (!Core.Me.InCombat)
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            var spell = PickElementalNuke(target, candidates);
            if (spell == null)
                return false;

            if (!target.WithinSpellRange(spell.Range))
                return false;

            return await spell.Cast(target);
        }

        /// <summary>
        /// Decides which elemental attack to spend the shared recast window on, or null to hold it.
        /// A weakness revealed on the target by anyone's Occult Libra shows up as one of
        /// OCAuras.FireWeakness / IceWeakness / LightningWeakness / WindWeakness, and the matching
        /// element hits for bonus potency - so a matched element wins, but the user's toggles
        /// always veto. Failing a match, the first usable candidate wins, so callers list theirs
        /// in fallback preference order. Wind is covered only by Phantom Summoner's Thunderstorm
        /// (and Blue Mage's Aero line, once that job is implemented).
        /// </summary>
        private static SpellData PickElementalNuke(GameObject target, (SpellData Spell, bool Enabled, uint WeaknessAura)[] candidates)
        {
            // CanCast covers both "learned at this phantom job level" and the shared recast timer.
            // Picking a spell the player has not learned yet would silently cast nothing at low
            // phantom levels.
            var usable = candidates.Where(c => c.Enabled && c.Spell.CanCast()).ToList();

            var matched = usable.FirstOrDefault(c => target.HasAura(c.WeaknessAura));
            if (matched.Spell != null)
                return matched.Spell;

            // No exploitable weakness (not yet revealed, Wind, or the matched element is toggled
            // off): don't hold the shared window - cast the first usable candidate.
            return usable.FirstOrDefault().Spell;
        }
        #endregion

        #region Phantom Red Mage (Job ID: 5334)
        /// <summary>
        /// Occult Cure II - Restores target's HP (cure potency 40,000), 1.5s cast, 2.5s recast
        /// Costs 1,500 MP; note this is a different action from Phantom White Mage's Occult Cure II
        /// </summary>
        private static async Task<bool> RedMageOccultCureII()
        {
            if (!OccultCrescentSettings.Instance.UseRedMageOccultCureII)
                return false;

            if (!OCSpells.RedMageOccultCureII.CanCast())
                return false;

            // Same MP floor as Occult Heal: keep enough MP for the real job's own spells
            if (Core.Me.CurrentManaPercent < 65)
                return false;

            GameObject healTarget = null;

            if (OccultCrescentSettings.Instance.RedMageOccultCureIICastOnAllies)
            {
                healTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.RedMageOccultCureIIHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                if (healTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.RedMageOccultCureIIHealthPercent)
                    healTarget = Core.Me;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.RedMageOccultCureIIHealthPercent)
                    healTarget = Core.Me;
            }

            if (healTarget == null)
                return false;

            return await OCSpells.RedMageOccultCureII.Cast(healTarget);
        }

        /// <summary>
        /// Occult Libra - Instant, 5s recast. Reveals the target's elemental affinity for 120s,
        /// increasing the potency of elemental attacks that exploit its weakness (for everyone,
        /// not just the caster). The revealed weakness shows up as one of the *Weakness auras.
        /// </summary>
        private static async Task<bool> OccultLibra()
        {
            if (!OccultCrescentSettings.Instance.UseOccultLibra)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultLibra.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidAttackUnit() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultLibra.Range))
                return false;

            // Already revealed - the weakness aura lasts 120s, no point recasting
            if (target.HasAnyAura(WeaknessAuras))
                return false;

            return await OCSpells.OccultLibra.Cast(target);
        }

        private static readonly uint[] WeaknessAuras =
        {
            OCAuras.FireWeakness,
            OCAuras.IceWeakness,
            OCAuras.LightningWeakness,
            OCAuras.WindWeakness
        };

        /// <summary>
        /// Occult Fire II / Blizzard II / Thunder II - one shared 30s recast window, so each
        /// window spends exactly one of them. 300 potency splash (5y), or 390 when the element
        /// matches the target's revealed weakness. Wind has no matching Red Mage spell.
        /// Fire II unlocks at phantom job level 1, Blizzard II at 4, Thunder II at 5.
        /// </summary>
        private static Task<bool> RedMageElementalNukes()
        {
            var settings = OccultCrescentSettings.Instance;

            // Listed highest-unlock first, which is the order the fallback has always used when
            // no weakness is revealed. All three hit for the same potency, so it only tie-breaks.
            return SharedRecastElementalNukes(
                (OCSpells.OccultThunderII, settings.UseOccultThunderII, OCAuras.LightningWeakness),
                (OCSpells.OccultBlizzardII, settings.UseOccultBlizzardII, OCAuras.IceWeakness),
                (OCSpells.OccultFireII, settings.UseOccultFireII, OCAuras.FireWeakness));
        }

        /// <summary>
        /// Execute Phantom Red Mage phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteRedMagePhantomJob()
        {
            // Occult Cure II - keep ourselves and allies healthy first
            if (await RedMageOccultCureII())
                return true;

            // Occult Libra - reveal the weakness before spending the shared nuke window
            if (await OccultLibra())
                return true;

            // Fire II / Blizzard II / Thunder II - one shared 30s recast window
            if (await RedMageElementalNukes())
                return true;

            return false;
        }
        #endregion

        #region Phantom Black Mage (Job ID: 5330)
        /// <summary>
        /// Occult Toad - 1.5s cast, 2.5s recast. Applies Occult Toad for 20s: the target's damage
        /// dealt drops by 99% and it cannot use any action other than its auto-attack.
        ///
        /// A lot of enemies are simply immune. Rather than guess from difficulty flags, we cast
        /// once and let OccultDebuffImmunityTracker watch whether the aura lands, so the routine stops
        /// wasting casts on enemy types that have already refused it.
        /// </summary>
        private static async Task<bool> OccultToad()
        {
            if (!OccultCrescentSettings.Instance.UseOccultToad)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultToad.CanCast())
                return false;

            if (Core.Me.CurrentTarget is not BattleCharacter target)
                return false;

            if (!target.ValidAttackUnit() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultToad.Range))
                return false;

            // Covers "already toaded", "an attempt is still in flight" and "this enemy type has
            // refused it before".
            if (!OccultDebuffImmunityTracker.IsWorthAttempting(target, OCAuras.OccultToad))
                return false;

            if (!await OCSpells.OccultToad.Cast(target))
                return false;

            // Cast() returns once casting has STARTED, so the tracker is told the cast time and
            // works out for itself when the aura should have appeared by.
            OccultDebuffImmunityTracker.RecordAttempt(target, OCAuras.OccultToad, OCSpells.OccultToad.AdjustedCastTime);
            return true;
        }

        /// <summary>
        /// Occult Flare - 2.3s cast, 60s recast. 500 potency of unaspected damage to the target
        /// and everything within 8y of it. Unaspected, so elemental weaknesses do not apply.
        /// </summary>
        private static async Task<bool> OccultFlare()
        {
            if (!OccultCrescentSettings.Instance.UseOccultFlare)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultFlare.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultFlare.Range))
                return false;

            return await OCSpells.OccultFlare.Cast(target);
        }

        /// <summary>
        /// Occult Fire III / Blizzard III / Thunder III - one shared 40s recast window, so each
        /// window spends exactly one of them. 400 potency splash (5y), or 520 when the element
        /// matches the target's revealed weakness. Phantom Black Mage has no Occult Libra of its
        /// own, but the weakness auras are readable whoever revealed them. Wind has no Black Mage
        /// spell. Fire III unlocks at phantom job level 1, Blizzard III at 2, Thunder III at 3.
        /// </summary>
        private static Task<bool> BlackMageElementalNukes()
        {
            var settings = OccultCrescentSettings.Instance;

            return SharedRecastElementalNukes(
                (OCSpells.OccultThunderIII, settings.UseOccultThunderIII, OCAuras.LightningWeakness),
                (OCSpells.OccultBlizzardIII, settings.UseOccultBlizzardIII, OCAuras.IceWeakness),
                (OCSpells.OccultFireIII, settings.UseOccultFireIII, OCAuras.FireWeakness));
        }

        /// <summary>
        /// Execute Phantom Black Mage phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBlackMagePhantomJob()
        {
            // Occult Toad - neutralise the target first; it lasts 20s so this is roughly one cast
            // per target, not per GCD
            if (await OccultToad())
                return true;

            // Occult Flare - 60s recast, biggest single hit
            if (await OccultFlare())
                return true;

            // Fire III / Blizzard III / Thunder III - one shared 40s recast window
            if (await BlackMageElementalNukes())
                return true;

            return false;
        }
        #endregion

        #region Phantom White Mage (Job ID: 5329)
        /// <summary>
        /// Occult Cure II - Restores target's HP (cure potency 40,000), 1.5s cast, 2.5s recast.
        /// Costs 1,500 MP; note this is a different action from Phantom Red Mage's Occult Cure II.
        /// </summary>
        private static async Task<bool> WhiteMageOccultCureII()
        {
            if (!OccultCrescentSettings.Instance.UseWhiteMageOccultCureII)
                return false;

            if (!OCSpells.WhiteMageOccultCureII.CanCast())
                return false;

            // Same MP floor as Occult Heal: keep enough MP for the real job's own spells
            if (Core.Me.CurrentManaPercent < 65)
                return false;

            GameObject healTarget = null;

            if (OccultCrescentSettings.Instance.WhiteMageOccultCureIICastOnAllies)
            {
                healTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.WhiteMageOccultCureIIHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                if (healTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.WhiteMageOccultCureIIHealthPercent)
                    healTarget = Core.Me;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.WhiteMageOccultCureIIHealthPercent)
                    healTarget = Core.Me;
            }

            if (healTarget == null)
                return false;

            return await OCSpells.WhiteMageOccultCureII.Cast(healTarget);
        }

        /// <summary>
        /// Occult Cure III - Restores 30,000 HP to the target and every party member within 15y
        /// of THE TARGET (not of us), 2.3s cast, 2.5s recast, 3,000 MP.
        /// </summary>
        private static async Task<bool> WhiteMageOccultCureIII()
        {
            if (!OccultCrescentSettings.Instance.UseWhiteMageOccultCureIII)
                return false;

            if (!OCSpells.WhiteMageOccultCureIII.CanCast())
                return false;

            // Same MP floor as Occult Heal: keep enough MP for the real job's own spells
            if (Core.Me.CurrentManaPercent < 65)
                return false;

            var threshold = OccultCrescentSettings.Instance.WhiteMageOccultCureIIIHealthPercent;

            var injured = Group.CastableAlliesWithin30
                .Where(ally => ally.IsValid && ally.IsAlive && ally.CurrentHealthPercent <= threshold)
                .ToList();

            if (Core.Me.CurrentHealthPercent <= threshold)
                injured.Add(Core.Me);

            if (injured.Count == 0)
                return false;

            // The heal circle is centred on whoever we target, so pick the casualty with the most
            // other casualties around them rather than simply the lowest HP - covering the group
            // is the only reason to spend 3,000 MP here instead of Cure II's 1,500.
            var healTarget = injured
                .OrderByDescending(ally => injured.Count(other => other.Location.Distance(ally.Location) <= OCSpells.WhiteMageOccultCureIII.Radius))
                .ThenBy(ally => ally.CurrentHealthPercent)
                .First();

            var covered = injured.Count(other => other.Location.Distance(healTarget.Location) <= OCSpells.WhiteMageOccultCureIII.Radius);
            if (covered < OccultCrescentSettings.Instance.WhiteMageOccultCureIIIAllyCount)
                return false;

            return await OCSpells.WhiteMageOccultCureIII.Cast(healTarget);
        }

        /// <summary>
        /// Occult Holy - 2.3s cast, 60s recast. 500 potency of unaspected damage to the target and
        /// everything within 8y of it, rising to 750 against undead.
        /// </summary>
        private static async Task<bool> OccultHoly()
        {
            if (!OccultCrescentSettings.Instance.UseOccultHoly)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultHoly.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultHoly.Range))
                return false;

            return await OCSpells.OccultHoly.Cast(target);
        }

        /// <summary>
        /// Execute Phantom White Mage phantom job rotation
        ///
        /// Occult Raise is not called from here - it is instant, so it slots into the existing
        /// resurrection path beside Phantom Chemist's Revive (see RaiseNonPartyPlayer).
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteWhiteMagePhantomJob()
        {
            // Occult Cure III - group heal first, so a raid-wide hit is answered in one cast
            if (await WhiteMageOccultCureIII())
                return true;

            // Occult Cure II - single target top-up
            if (await WhiteMageOccultCureII())
                return true;

            // Occult Holy - 60s recast, the job's only damage
            if (await OccultHoly())
                return true;

            return false;
        }
        #endregion

        #region Phantom Ninja (Job ID: 5328)
        /// <summary>
        /// Fuma Shuriken - instant, 60s recast. 230 potency to a single target, on its own timer
        /// rather than the scrolls' shared one.
        /// </summary>
        private static async Task<bool> FumaShuriken()
        {
            if (!OccultCrescentSettings.Instance.UseFumaShuriken)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.FumaShuriken.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.FumaShuriken.Range))
                return false;

            return await OCSpells.FumaShuriken.Cast(target);
        }

        /// <summary>
        /// Smoke - instant, 5s recast, raises our evasion by 20% for 90s. Long duration against a
        /// short recast, so this is pure upkeep: cast it whenever it has fallen off in combat.
        /// </summary>
        private static async Task<bool> Smoke()
        {
            if (!OccultCrescentSettings.Instance.UseSmoke)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Smoke.CanCast())
                return false;

            if (Core.Me.HasAura(OCAuras.Smoke))
                return false;

            return await OCSpells.Smoke.Cast(Core.Me);
        }

        /// <summary>
        /// Image - instant, 120s recast. Three stacks, each nullifying one physical attack, for
        /// 30s. Defensive, so it waits for a health threshold rather than being spent on cooldown.
        /// </summary>
        private static async Task<bool> Image()
        {
            if (!OccultCrescentSettings.Instance.UseImage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Image.CanCast())
                return false;

            if (Core.Me.HasAura(OCAuras.Image))
                return false;

            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.ImageHealthPercent)
                return false;

            return await OCSpells.Image.Cast(Core.Me);
        }

        /// <summary>
        /// Lightning Scroll / Flame Scroll - one shared 60s recast window, so each window spends
        /// exactly one of them. 150 potency splash (5y), or 195 when the element matches the
        /// target's revealed weakness. Phantom Ninja has no Occult Libra of its own, but the
        /// weakness auras are readable whoever revealed them. Both hit for the same potency, so
        /// the fallback order below only tie-breaks. Lightning unlocks at phantom job level 3,
        /// Flame at 4.
        /// </summary>
        private static Task<bool> NinjaElementalScrolls()
        {
            var settings = OccultCrescentSettings.Instance;

            return SharedRecastElementalNukes(
                (OCSpells.FlameScroll, settings.UseFlameScroll, OCAuras.FireWeakness),
                (OCSpells.LightningScroll, settings.UseLightningScroll, OCAuras.LightningWeakness));
        }

        /// <summary>
        /// Execute Phantom Ninja phantom job rotation
        ///
        /// Level 5 is the First Strike trait - passive, so there is nothing to call for it.
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteNinjaPhantomJob()
        {
            // Image - physical damage nullification, held for when we are actually hurt
            if (await Image())
                return true;

            // Smoke - cheap evasion upkeep
            if (await Smoke())
                return true;

            // Fuma Shuriken - 230 potency single target on its own 60s timer
            if (await FumaShuriken())
                return true;

            // Lightning Scroll / Flame Scroll - one shared 60s recast window
            if (await NinjaElementalScrolls())
                return true;

            return false;
        }
        #endregion

        #region Phantom Dragoon (Job ID: 5331)
        /// <summary>
        /// Occult Jump - instant, 60s recast, 30y. 400 potency, rising to 500 with the level 4
        /// Enhanced Occult Jump trait, and cuts damage taken by 60% (90% with the trait) for 2s
        /// on landing.
        ///
        /// This is a gap closer - it moves the character to the target - so it has to respect the
        /// bot's movement capability flags. Beyond that it jumps freely by default; anyone who
        /// would rather not be pulled out of ranged position can turn on OccultJumpMeleeRangeOnly.
        /// </summary>
        private static async Task<bool> OccultJump()
        {
            if (!OccultCrescentSettings.Instance.UseOccultJump)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultJump.CanCast())
                return false;

            // The bot base or another plugin can forbid repositioning - cutscenes, mechanics,
            // fates that punish moving. CanUseGapCloser covers the GapCloser and Movement flags
            // together, which is exactly what a jump needs.
            if (!Movement.CanUseGapCloser())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            // Check melee range restriction if enabled
            if (OccultCrescentSettings.Instance.OccultJumpMeleeRangeOnly && !target.WithinSpellRange(3.0f))
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultJump.Range))
                return false;

            return await OCSpells.OccultJump.Cast(target);
        }

        /// <summary>
        /// Lance - instant, 30s recast, 30y. 300 potency, and the damage dealt is absorbed back as
        /// HP; anything over our maximum becomes a barrier lasting 60s. No movement involved, so
        /// unlike Occult Jump this needs no capability or range gating beyond the usual.
        /// </summary>
        private static async Task<bool> Lance()
        {
            if (!OccultCrescentSettings.Instance.UseLance)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Lance.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.Lance.Range))
                return false;

            return await OCSpells.Lance.Cast(target);
        }

        /// <summary>
        /// Execute Phantom Dragoon phantom job rotation
        ///
        /// Step Forth is not implemented - it is pure repositioning. Level 4 is a passive trait.
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteDragoonPhantomJob()
        {
            // Occult Jump - the bigger hit, and the 2s damage cut on landing
            if (await OccultJump())
                return true;

            // Lance - 300 potency that comes back as HP, and a barrier with the overflow
            if (await Lance())
                return true;

            return false;
        }
        #endregion

        #region Phantom Summoner (Job ID: 5332)
        /// <summary>
        /// Earthen Wall - 2.5s cast, 120s recast. Barriers us and every party member within 20y
        /// for the equivalent of a 40,000 potency heal, lasting 10s.
        ///
        /// The barrier is really meant to be pre-cast into a raidwide, which the routine cannot
        /// see coming, so it fires reactively once someone has actually been hurt.
        /// </summary>
        private static async Task<bool> EarthenWall()
        {
            if (!OccultCrescentSettings.Instance.UseEarthenWall)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.EarthenWall.CanCast())
                return false;

            // A 2.5s cast breaks the moment we move, so do not bother starting it.
            if (MovementManager.IsMoving)
                return false;

            var threshold = OccultCrescentSettings.Instance.EarthenWallHealthPercent;

            var anyoneHurt = Core.Me.CurrentHealthPercent <= threshold
                             || Group.CastableAlliesWithin30.Any(ally => ally.IsValid
                                                                         && ally.IsAlive
                                                                         && ally.CurrentHealthPercent <= threshold
                                                                         && ally.Distance(Core.Me) <= OCSpells.EarthenWall.Radius);

            if (!anyoneHurt)
                return false;

            return await OCSpells.EarthenWall.Cast(Core.Me);
        }

        /// <summary>
        /// Megaflare - 6s cast, 90s recast. 1,000 potency of unaspected damage to the target and
        /// everything within 15y. Unaspected, so elemental weaknesses do not apply, and it sits on
        /// its own timer rather than the shared elemental one.
        /// </summary>
        private static async Task<bool> Megaflare()
        {
            if (!OccultCrescentSettings.Instance.UseMegaflare)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Megaflare.CanCast())
                return false;

            // The longest cast in Occult Crescent bar Occult Comet - never start it on the move.
            if (MovementManager.IsMoving)
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.Megaflare.Range))
                return false;

            return await OCSpells.Megaflare.Cast(target);
        }

        /// <summary>
        /// Hellfire / Judgment Bolt / Thunderstorm - one shared 60s recast window, so each window
        /// spends exactly one of them. 600 potency, or 780 when the element matches the target's
        /// revealed weakness. Hellfire unlocks at phantom job level 1, Judgment Bolt at 2,
        /// Thunderstorm at 4.
        ///
        /// Thunderstorm is this routine's only way to exploit Wind Weakness.
        /// </summary>
        private static Task<bool> SummonerElementalNukes()
        {
            // All three are 4s casts, so moving means the cast simply breaks.
            if (MovementManager.IsMoving)
                return Task.FromResult(false);

            var settings = OccultCrescentSettings.Instance;

            // Listed highest-unlock first, matching the other shared-recast jobs. Thunderstorm
            // being a cone rather than a circle needs no special handling: it still reaches 30y
            // and we cast facing the target, so the helper's usual range check covers it.
            return SharedRecastElementalNukes(
                (OCSpells.Thunderstorm, settings.UseThunderstorm, OCAuras.WindWeakness),
                (OCSpells.JudgmentBolt, settings.UseJudgmentBolt, OCAuras.LightningWeakness),
                (OCSpells.Hellfire, settings.UseHellfire, OCAuras.FireWeakness));
        }

        /// <summary>
        /// Execute Phantom Summoner phantom job rotation
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteSummonerPhantomJob()
        {
            // Earthen Wall - party barrier first
            if (await EarthenWall())
                return true;

            // Megaflare - 1,000 potency on its own 90s timer
            if (await Megaflare())
                return true;

            // Hellfire / Judgment Bolt / Thunderstorm - one shared 60s recast window
            if (await SummonerElementalNukes())
                return true;

            return false;
        }
        #endregion

        #region Phantom Blue Mage (Job ID: 5333)
        /// <summary>
        /// Occult White Wind - 1.5s cast, 150s recast. Heals us and every party member within 15y
        /// by an amount equal to OUR CURRENT HP.
        ///
        /// That last part drives the gating: at 20% health it heals for almost nothing, so it is
        /// held until we are healthy enough for it to be worth its 150s recast, even though the
        /// people it is saving are the hurt ones.
        /// </summary>
        private static async Task<bool> OccultWhiteWind()
        {
            if (!OccultCrescentSettings.Instance.UseOccultWhiteWind)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultWhiteWind.CanCast())
                return false;

            // The heal is our current HP, so casting it while we are low wastes the whole recast.
            if (Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.OccultWhiteWindMinimumOwnHealthPercent)
                return false;

            var threshold = OccultCrescentSettings.Instance.OccultWhiteWindHealthPercent;

            var anyoneHurt = Core.Me.CurrentHealthPercent <= threshold
                             || Group.CastableAlliesWithin30.Any(ally => ally.IsValid
                                                                         && ally.IsAlive
                                                                         && ally.CurrentHealthPercent <= threshold
                                                                         && ally.Distance(Core.Me) <= OCSpells.OccultWhiteWind.Radius);

            if (!anyoneHurt)
                return false;

            return await OCSpells.OccultWhiteWind.Cast(Core.Me);
        }

        /// <summary>
        /// Occult Mighty Guard - instant, 120s recast. Cuts damage taken by 20% for us and party
        /// members within 20y, for 15s.
        /// </summary>
        private static async Task<bool> OccultMightyGuard()
        {
            if (!OccultCrescentSettings.Instance.UseOccultMightyGuard)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultMightyGuard.CanCast())
                return false;

            if (Core.Me.HasAura(OCAuras.OccultMightyGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.OccultMightyGuardHealthPercent)
                return false;

            return await OCSpells.OccultMightyGuard.Cast(Core.Me);
        }

        /// <summary>
        /// Occult Missile - 1.5s cast, 30s recast. A flat 35% chance to deal 75% of the target's
        /// current HP, "with some exceptions". Nothing to time or hold: on anything it works
        /// against it is worth casting the moment it is up.
        ///
        /// Those exceptions are the catch. They are almost certainly what Occult Slowga and
        /// Occult Toad run into - bosses and other high difficulty enemies ignore effects of this
        /// shape - and unlike Toad there is no aura to watch, so OccultDebuffImmunityTracker
        /// cannot learn this one. A difficulty check is the best available guess.
        /// </summary>
        private static async Task<bool> OccultMissile()
        {
            if (!OccultCrescentSettings.Instance.UseOccultMissile)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultMissile.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            // Skip the enemies the effect is expected to do nothing to. Note this deliberately
            // does NOT exclude every FATE enemy the way Occult Slowga does: in Occult Crescent
            // that would rule out most of the content, and FATE trash is exactly where a 35%
            // chance at 75% of current HP pays off.
            if (Combat.IsBoss())
                return false;

            if (target is BattleCharacter missileTarget && missileTarget.RawDifficulty >= 2)
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultMissile.Range))
                return false;

            return await OCSpells.OccultMissile.Cast(target);
        }

        /// <summary>
        /// Occult Aqua Breath - 1.5s cast, 60s recast. 300 potency of unaspected damage to the
        /// target and everything within 5y of it.
        /// </summary>
        private static async Task<bool> OccultAquaBreath()
        {
            if (!OccultCrescentSettings.Instance.UseOccultAquaBreath)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.OccultAquaBreath.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.OccultAquaBreath.Range))
                return false;

            return await OCSpells.OccultAquaBreath.Cast(target);
        }

        /// <summary>
        /// Occult Aero / Aero II / Aero III - an upgrade chain sharing one 30s recast. Learning the
        /// next one replaces the last, so at most one is ever castable and there is nothing to
        /// choose between: take the best we have. 150/200/250 potency, or 195/260/325 against a
        /// wind-weak target. Aero III also splashes 5y, where the first two are single target.
        ///
        /// This is the only Blue Mage action available on a freshly unlocked job.
        /// </summary>
        private static async Task<bool> OccultAero()
        {
            if (!OccultCrescentSettings.Instance.UseOccultAero)
                return false;

            if (!Core.Me.InCombat)
                return false;

            var spell = OCSpells.OccultAeroIII.CanCast() ? OCSpells.OccultAeroIII
                      : OCSpells.OccultAeroII.CanCast() ? OCSpells.OccultAeroII
                      : OCSpells.OccultAero.CanCast() ? OCSpells.OccultAero
                      : null;

            if (spell == null)
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(spell.Range))
                return false;

            return await spell.Cast(target);
        }

        /// <summary>
        /// Execute Phantom Blue Mage phantom job rotation
        ///
        /// Occult Learning I/II/III are traits and need no code. Every action but Occult Aero has
        /// to be learned from a particular enemy first, so on a newly unlocked Blue Mage only the
        /// Aero line will fire - the rest stay silent until they are learned, which is correct.
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBlueMagePhantomJob()
        {
            // Occult White Wind - the big party heal
            if (await OccultWhiteWind())
                return true;

            // Occult Mighty Guard - party mitigation
            if (await OccultMightyGuard())
                return true;

            // Occult Missile - the execute gamble, on its own 30s timer
            if (await OccultMissile())
                return true;

            // Occult Aqua Breath - 300 potency splash on 60s
            if (await OccultAquaBreath())
                return true;

            // Occult Aero line - the job's bread and butter, one shared 30s recast
            if (await OccultAero())
                return true;

            return false;
        }
        #endregion

        #region Phantom Necromancer (Job ID: 5335)
        /// <summary>
        /// Whether we are willing to pay the HP for an attack right now.
        ///
        /// Every Necromancer attack except Drain Touch costs 10% of our maximum HP, and that cost
        /// is unconditional - unlike Doom, it applies whether or not Drain Touch is up. The floor
        /// stops the rotation chipping us to death over a long fight.
        ///
        /// Doom, by contrast, only lands while Drain Touch is up (confirmed in game). Nothing in
        /// Magitek can promise a return to full HP inside its 10s, so the rotation never attacks
        /// through the buff at all - this is the gate that guarantees it.
        /// </summary>
        private static bool NecromancerCanSpendHealth()
        {
            if (Core.Me.CurrentHealthPercent < OccultCrescentSettings.Instance.NecromancerMinimumHealthPercent)
                return false;

            // Drain Touch is up, so this attack would Doom us.
            return !Core.Me.HasAura(OCAuras.DrainTouch);
        }

        /// <summary>
        /// Drain Touch - 150 potency, instant, 40s recast. Costs no HP and never Dooms us. Absorbs
        /// the damage dealt back as HP (~32,000 against a 109,000 pool in testing, comfortably more
        /// than the 10% an attack costs) and leaves the 6s buff behind.
        ///
        /// Free damage and free healing, so it goes out on cooldown for every job. It is ordered
        /// after the attacks in ExecuteNecromancerPhantomJob rather than gated here, so the buff it
        /// leaves behind never catches one of them and turns it into a Doom.
        /// </summary>
        private static async Task<bool> DrainTouch()
        {
            if (!OccultCrescentSettings.Instance.UseDrainTouch)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.DrainTouch.CanCast())
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.DrainTouch.Range))
                return false;

            return await OCSpells.DrainTouch.Cast(target);
        }

        /// <summary>
        /// Deep Freeze / Hell Wind / Chaos Drive - one shared 40s recast, so each window spends
        /// exactly one of them. Ice / Wind / Lightning, 300 potency or 390 on a matched weakness,
        /// rising to 400 / 520 under Drain Touch. Each costs 10% of our maximum HP.
        ///
        /// This is the routine's second source of Wind damage after Phantom Summoner's Thunderstorm.
        /// Deep Freeze unlocks at phantom job level 2, Hell Wind at 3, Chaos Drive at 4, so
        /// PickElementalNuke's CanCast filter is what stops a low-level Necromancer holding the
        /// shared window for something it has not learned.
        /// </summary>
        private static Task<bool> NecromancerElementalNukes()
        {
            if (!NecromancerCanSpendHealth())
                return Task.FromResult(false);

            // 1.5s casts - do not start one we are only going to break by moving.
            if (MovementManager.IsMoving)
                return Task.FromResult(false);

            var settings = OccultCrescentSettings.Instance;

            // Listed highest-unlock first, matching the other shared-recast jobs.
            return SharedRecastElementalNukes(
                (OCSpells.ChaosDrive, settings.UseChaosDrive, OCAuras.LightningWeakness),
                (OCSpells.HellWind, settings.UseHellWind, OCAuras.WindWeakness),
                (OCSpells.DeepFreeze, settings.UseDeepFreeze, OCAuras.IceWeakness));
        }

        /// <summary>
        /// Doomsday - 1.5s cast, 120s recast, 30y line. 350 potency, or 500 under Drain Touch where
        /// it also strips one beneficial status off the target. Costs 10% of our maximum HP.
        ///
        /// It sits on its own timer rather than the elemental one, which is exactly why it needs
        /// the health check of its own: it can come up inside the 6s buff window left behind by a
        /// Drain Touch we cast as a heal, and would Doom us for it.
        /// </summary>
        private static async Task<bool> Doomsday()
        {
            if (!OccultCrescentSettings.Instance.UseDoomsday)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.Doomsday.CanCast())
                return false;

            if (!NecromancerCanSpendHealth())
                return false;

            if (MovementManager.IsMoving)
                return false;

            var target = Core.Me.CurrentTarget;
            if (target == null || !target.ValidDamageTarget() || !target.InLineOfSight())
                return false;

            if (!target.WithinSpellRange(OCSpells.Doomsday.Range))
                return false;

            return await OCSpells.Doomsday.Cast(target);
        }

        /// <summary>
        /// Execute Phantom Necromancer phantom job rotation
        ///
        /// The whole shape of this job comes from one fact confirmed in game: the HP-cost attacks
        /// Doom us only while Drain Touch is up. Doom runs 10s, clears only at full HP, and cannot
        /// be dispelled - so we attack first and use Drain Touch afterwards purely as the heal that
        /// pays back the 10%, and are never Doomed at all. This ordering is what produces that; the
        /// Drain Touch check inside NecromancerCanSpendHealth is what keeps it safe when the timers
        /// drift and the ordering alone would not.
        ///
        /// It also leaves the buff unspent - 300/390 potency where attacking through it would give
        /// 400/520. Taking that trade needs a heal that can reliably reach full HP inside 10s,
        /// which is not a Necromancer problem: Doom is an ordinary game mechanic that any job can
        /// be hit with in any content. It waits on the routine-wide Doom response rather than being
        /// solved once, here, for this job.
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteNecromancerPhantomJob()
        {
            // Attack unbuffed and let Drain Touch follow as the heal that repays the cost. Both
            // attacks refuse to fire while Drain Touch is up.
            if (await Doomsday())
                return true;

            if (await NecromancerElementalNukes())
                return true;

            if (await DrainTouch())
                return true;

            return false;
        }
        #endregion
    }
}
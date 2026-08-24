using System.Collections.Generic;

namespace Magitek.Utilities
{
    /// <summary>
    /// Every status and encounter involved in the game nullifying our damage. Declarative twin of
    /// <see cref="FightLogicEncounters"/>: this file holds only data, <see cref="ImmunityLogic"/> holds
    /// the rules that read it.
    ///
    /// These ids live here rather than in <see cref="Auras"/> because nothing else consumes them —
    /// splitting one feature's data across two files buys nothing. The generic
    /// <c>Auras.Invincibility</c> list stays where it is: it feeds <c>NotInvulnerable()</c>, which is
    /// used all over the routine.
    ///
    /// The tables are declared before <see cref="Encounters"/> because static field initializers run in
    /// textual order and the encounter list references them.
    /// </summary>
    internal static class ImmunityEncounters
    {
        #region Aura-keyed rules — these apply in every zone

        // "Hero" / "Villain" duels — Jeuno's Ark Angels and the Occult Crescent.
        // Each Ark Angel can be dubbed a Villain, whose tooltip reads "Damage from those who have not
        // been dubbed <X> Hero is nullified". This is the mirror of the Meso Terminal mark: there OUR
        // mark chose which enemy we could hit, here the ENEMY's status dictates which players may hit it.
        //
        // Note 4198 (Mighty Strikes) sits in the middle of this id range but is unrelated — it is an Ark
        // Angel self-buff that crits its melee attacks, and must NOT be treated as a duel status.
        public const int
            EpicHero = 4192,
            EpicVillain = 4193,
            FatedHero = 4194,
            FatedVillain = 4195,
            VauntedHero = 4196,
            VauntedVillain = 4197;

        // The Occult Crescent's Blue Head and Green Head run the same duel, but the game gives that
        // encounter its own copies of the Villain statuses while reusing the very same Hero statuses
        // above. Only two exist because there are only two heads — there is no second Vaunted Villain.
        public const int
            EpicVillainOccult = 5400,
            FatedVillainOccult = 5401;

        /// <summary>Villain status an enemy carries -> Hero status a player needs to damage it.</summary>
        public static readonly Dictionary<uint, uint> DuelVillainRequiredHeroAura = new Dictionary<uint, uint>
        {
            { EpicVillain, EpicHero },
            { FatedVillain, FatedHero },
            { VauntedVillain, VauntedHero },
            { EpicVillainOccult, EpicHero },
            { FatedVillainOccult, FatedHero },
        };

        // Damage-type immunity (e.g. The Void Ark — Sawtooth / Irminsul). Some encounters hand a boss
        // immunity to one damage type instead of full invulnerability, so whether we can hurt it depends
        // on our job. In The Void Ark the pair take one each at random, which is why this keys off the
        // status rather than the enemy.
        //
        // "Magic Resistance" — invulnerable to magic attacks: blocks healers and casters.
        // "Ranged Resistance" — invulnerable to ranged attacks: blocks PHYSICAL ranged only. Magical
        // ranged still lands, so casters must NOT be excluded by it.
        public static readonly uint[] MagicImmunity =
        {
            942,  // Magic Resistance
            3621, // Magic Resistance (same status, reused by later content)
        };

        public static readonly uint[] RangedImmunity =
        {
            941, // Ranged Resistance
        };

        // Buffs that nullify our damage without being full invulnerability. Deliberately NOT in
        // Auras.Invincibility: that list feeds NotInvulnerable(), which Tracking.Update uses to build
        // Combat.Enemies, so anything added there vanishes from the ~200 sites that read that collection
        // — including Provoke, the interrupt strategies and IsBoss(). These last long enough to make that
        // blackout matter, so they gate at the cast instead and leave the enemy visible to every
        // defensive path.
        public static readonly uint[] DamageNullifying =
        {
            4175, // Burning Ward — Tangata (Halatali, Occult Crescent) self-applies it via action 40596
        };

        #endregion

        #region Statuses consumed by a single encounter below

        // The Meso Terminal. Each of the four Headsmen carries a Guard on Duty α/β/γ/δ status and tethers
        // one player, marking them with the matching Cell Block letter. Same-letter pairing.
        public const int
            CellBlockAlpha = 4542,
            CellBlockBeta = 4543,
            CellBlockGamma = 4544,
            CellBlockDelta = 4545,
            GuardOnDutyAlpha = 4546,
            GuardOnDutyBeta = 4547,
            GuardOnDutyGamma = 4548,
            GuardOnDutyDelta = 4549;

        // RebornBuddy exposes these BNpc name-row identifiers through GameObject.NpcId. Unlike display
        // names they are stable across client languages, and the untargetable helper actors used by the
        // Headsmen's cell attacks retain their owner's id, so the same mark restriction still covers the
        // entire mechanic without accidentally including Hellmakers.
        public const uint
            BloodyHeadsmanNpcId = 14047,
            RavenousHeadsmanNpcId = 14048,
            PaleHeadsmanNpcId = 14049,
            PestilentHeadsmanNpcId = 14050;

        /// <summary>Our Cell Block mark -> the Guard on Duty status an enemy needs to take our damage.</summary>
        public static readonly Dictionary<uint, uint> MarkMatchDamageableEnemyAura = new Dictionary<uint, uint>
        {
            { CellBlockAlpha, GuardOnDutyAlpha },
            { CellBlockBeta, GuardOnDutyBeta },
            { CellBlockGamma, GuardOnDutyGamma },
            { CellBlockDelta, GuardOnDutyDelta },
        };

        // The Labyrinth of the Ancients. Player buff whose tooltip reads "Existentially aligned to the
        // astral realm. Damage dealt is reduced, but can attack ghostly beings."
        public const int AstralRealignment = 398;

        #endregion

        #region Zone-keyed encounters

        internal static readonly List<ImmunityEncounter> Encounters = new List<ImmunityEncounter>
        {
            #region A Realm Reborn: Alliance Raids

            new ImmunityEncounter {
                ZoneId = ZoneId.TheLabyrinthOfTheAncients,
                Name = "Alliance Raid: The Labyrinth of the Ancients (Thanatos)",
                Expansion = FfxivExpansion.ARealmReborn,
                // Without Astral Realignment our damage against Thanatos does nothing. There is no status
                // on the target to key off, which is why this has to be matched by name. The exact match
                // keeps Eureka Orthos' "Orthos Thanatos" out of it.
                RequiresSelfAura = new Dictionary<string, uint> {
                    { "Thanatos", AstralRealignment }
                }
            },

            #endregion

            #region Dawntrail: Dungeons

            new ImmunityEncounter {
                ZoneId = 1292,
                Name = "The Meso Terminal (Headsman mark mechanic)",
                Expansion = FfxivExpansion.Dawntrail,
                // While marked, a player can ONLY damage the Headsman whose Guard on Duty letter matches
                // their Cell Block — "Attacks against other targets are nullified." Hellmakers can spawn
                // during the same cell phase without a Guard on Duty status and must still be attacked, so
                // the mark rule is deliberately scoped to the four marked Headsmen rather than the zone.
                MarkMatch = MarkMatchDamageableEnemyAura,
                MarkMatchEnemyIds = new HashSet<uint> {
                    BloodyHeadsmanNpcId,
                    RavenousHeadsmanNpcId,
                    PaleHeadsmanNpcId,
                    PestilentHeadsmanNpcId,
                }
            },

            #endregion

            #region Dawntrail: The Occult Crescent

            new ImmunityEncounter {
                ZoneId = 1346,
                Name = "The Occult Crescent: South Horn — Forked Tower: Blood (Arbatel)",
                Expansion = FfxivExpansion.Dawntrail,
                // Arbatel spawns numbered Pages the routine should never spend actions on: the game
                // nullifies damage against them (combat logs across multiple pulls: 153 player hits, 151
                // dealt zero, no Page ever died), so attacking one is pure waste. Stray AoE splash onto
                // them is harmless, which is why this gates the cast rather than the enemy collection.
                IgnoredEnemies = new HashSet<string> {
                    "Page 512",
                    "Page 64",
                    "Page 16",
                    "Page 8",
                }
            },

            #endregion
        };

        #endregion
    }
}

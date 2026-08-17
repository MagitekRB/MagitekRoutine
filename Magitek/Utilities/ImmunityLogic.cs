using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities
{
    /// <summary>
    /// Whether our damage can reach a unit at all right now — the composed answer to every "the game
    /// nullifies your hits against this" mechanic. Rules that need to know which fight we are in read
    /// <see cref="ImmunityEncounters"/>; rules keyed by an aura apply everywhere.
    ///
    /// Reached through <c>GameObjectExtensions.CanBeDamagedByMe()</c>, which is what rotations and the
    /// Occult Crescent offensive paths actually call.
    ///
    /// Deliberately NOT wired into <c>NotInvulnerable()</c>: <c>Tracking.Update</c> builds
    /// <c>Combat.Enemies</c> from that, so widening it would silently refilter the collection under all
    /// ~200 of its readers, including Provoke, the interrupt strategies and <c>IsBoss()</c>. Gating at
    /// the cast leaves an undamageable enemy fully visible to every defensive path.
    /// </summary>
    internal static class ImmunityLogic
    {
        // Built once. Everywhere outside a listed fight this costs a single dictionary probe, which is
        // what keeps the encounter rules off the hot path.
        private static readonly Dictionary<ushort, ImmunityEncounter> EncountersByZone =
            ImmunityEncounters.Encounters.ToDictionary(e => e.ZoneId);

        public static bool CanBeDamagedBy(GameObject unit, Character me)
        {
            if (unit == null)
                return false;

            if (me == null)
                return true;

            return EncounterRulesAllow(unit, me) && AuraRulesAllow(unit, me);
        }

        /// <summary>
        /// Fight-specific rules, gated behind one dictionary probe. EnglishName is only ever read inside
        /// a matched encounter, and the player's aura list is only walked by the mark mechanic — so in
        /// every zone but the listed few this does no work at all.
        /// </summary>
        private static bool EncounterRulesAllow(GameObject unit, Character me)
        {
            if (!EncountersByZone.TryGetValue(WorldManager.ZoneId, out var encounter))
                return true;

            var name = unit.EnglishName;

            if (encounter.IgnoredEnemies != null && encounter.IgnoredEnemies.Contains(name))
                return false;

            if (encounter.RequiresSelfAura != null
                && encounter.RequiresSelfAura.TryGetValue(name, out var requiredSelfAura)
                && !me.HasAura(requiredSelfAura))
                return false;

            if (encounter.MarkMatch != null && !DamageableByMyMark(unit, me, encounter.MarkMatch))
                return false;

            return true;
        }

        /// <summary>
        /// Rules answered from the target's own statuses, so the aura list is walked exactly ONCE.
        /// RB aura collections are lazy cross-process facades — each enumeration is a fresh sequence of
        /// memory reads rather than a cached list, so asking these three questions with three separate
        /// HasAnyAura calls would cost three times as much.
        /// </summary>
        private static bool AuraRulesAllow(GameObject unit, Character me)
        {
            if (unit is not Character target || !target.IsValid)
                return true;

            uint requiredHeroAura = 0;
            var magicImmune = false;
            var rangedImmune = false;
            var damageNullified = false;

            foreach (var aura in target.CharacterAuras)
            {
                if (aura == null)
                    continue;

                var id = aura.Id;

                if (requiredHeroAura == 0 && ImmunityEncounters.DuelVillainRequiredHeroAura.TryGetValue(id, out var hero))
                    requiredHeroAura = hero;

                if (!magicImmune && ImmunityEncounters.MagicImmunity.Contains(id))
                    magicImmune = true;

                if (!rangedImmune && ImmunityEncounters.RangedImmunity.Contains(id))
                    rangedImmune = true;

                if (!damageNullified && ImmunityEncounters.DamageNullifying.Contains(id))
                    damageNullified = true;
            }

            if (damageNullified)
                return false;

            // A target carrying no Villain status is damageable by anyone, which is the common case.
            if (requiredHeroAura != 0 && !me.HasAura(requiredHeroAura))
                return false;

            if (!magicImmune && !rangedImmune)
                return true;

            var myDamage = GetMyDamageType(me.CurrentJob);

            if (magicImmune && myDamage == MyDamageType.Magical)
                return false;

            if (rangedImmune && myDamage == MyDamageType.PhysicalRanged)
                return false;

            return true;
        }

        private static bool DamageableByMyMark(GameObject unit, Character me, Dictionary<uint, uint> markMatch)
        {
            uint requiredEnemyAura = 0;

            foreach (var aura in me.CharacterAuras)
            {
                if (aura != null && markMatch.TryGetValue(aura.Id, out requiredEnemyAura))
                    break;
            }

            // Not marked -> normal targeting.
            if (requiredEnemyAura == 0)
                return true;

            // Marked -> only the enemy with the matching letter takes our damage.
            return unit.HasAura(requiredEnemyAura);
        }

        private enum MyDamageType
        {
            /// <summary>Healers and casters. Blocked by Magic Resistance.</summary>
            Magical,

            /// <summary>Physical ranged only. Blocked by Ranged Resistance.</summary>
            PhysicalRanged,

            /// <summary>Blocked by neither: melee, and Blue Mage (see below).</summary>
            Unaffected
        }

        // A switch rather than List.Contains: our job cannot change mid-pulse, so scanning a twelve-entry
        // list per call was per-call work for a per-pulse constant.
        private static MyDamageType GetMyDamageType(ClassJobType job)
        {
            switch (job)
            {
                case ClassJobType.Arcanist:
                case ClassJobType.Scholar:
                case ClassJobType.Conjurer:
                case ClassJobType.WhiteMage:
                case ClassJobType.Astrologian:
                case ClassJobType.Sage:
                case ClassJobType.Thaumaturge:
                case ClassJobType.BlackMage:
                case ClassJobType.Summoner:
                case ClassJobType.RedMage:
                case ClassJobType.Pictomancer:
                    return MyDamageType.Magical;

                case ClassJobType.Archer:
                case ClassJobType.Bard:
                case ClassJobType.Machinist:
                case ClassJobType.Dancer:
                    return MyDamageType.PhysicalRanged;

                // Blue Mage is blocked by neither, and is the reason this cannot be a two-list check.
                // Ranged Resistance must not stop it because its ranged attacks are magical; Magic
                // Resistance must not stop it either, because it is the one job carrying genuinely
                // physical attacks — Sharpened Knife is slashing, Triple Trident piercing, and both land
                // through Magic Resistance. Blanket-blocking it would shut down the whole rotation over
                // the spells it cannot use while discarding the ones it can.
                case ClassJobType.BlueMage:
                default:
                    return MyDamageType.Unaffected;
            }
        }
    }

    internal class ImmunityEncounter
    {
        internal ushort ZoneId { get; set; }
        internal string Name { get; set; }
        internal FfxivExpansion Expansion { get; set; }

        /// <summary>Enemy name -> the aura we must carry to damage it.</summary>
        internal Dictionary<string, uint> RequiresSelfAura { get; set; }

        /// <summary>Enemies the game nullifies all our damage against; never worth an action.</summary>
        internal HashSet<string> IgnoredEnemies { get; set; }

        /// <summary>Our mark -> the aura an enemy must carry to take our damage while we are marked.</summary>
        internal Dictionary<uint, uint> MarkMatch { get; set; }
    }
}

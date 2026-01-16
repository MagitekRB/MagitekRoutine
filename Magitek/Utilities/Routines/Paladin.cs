using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Utilities.Routines
{
    internal static class Paladin
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Paladin, Spells.FastBlade);
        public static double GCDTimeMilliseconds = Spells.FastBlade.AdjustedCooldown.TotalMilliseconds;

        public static SpellData RoyalAuthority => Spells.RoyalAuthority.IsKnown()
                                                    ? Spells.RoyalAuthority
                                                    : Spells.RageofHalone;
        public static SpellData Expiacion => Spells.Expiacion.IsKnown()
                                            ? Spells.Expiacion
                                            : Spells.SpiritsWithin;

        public static readonly SpellData[] DefensiveFastSpells = new SpellData[]
        {
            Spells.HolySheltron,
            Spells.Sheltron
        };

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.Rampart,
            Spells.Guardian,
            Spells.Sentinel,
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.HallowedGround,
            Auras.Rampart,
            Auras.Sentinel,
            Auras.Guardian,
            Auras.HolySheltron,
            Auras.Sheltron
        };

        public static int RequiescatStackCount => Core.Me.CharacterAuras.GetAuraStacksById(Auras.Requiescat);

        public static bool ToggleAndSpellCheck(bool Toggle, SpellData Spell)
        {
            if (!Toggle)
                return false;

            if (!ActionManager.HasSpell(Spell.Id))
                return false;

            return true;
        }

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        /// <summary>
        /// Returns the number of GCDs until Royal Authority combo will be ready.
        /// Returns 0 if RA is ready now, 1 if we need Riot Blade, 2 if we need Fast Blade + Riot Blade.
        /// Returns -1 if combo is broken or unknown state.
        /// </summary>
        public static int GCDsUntilRoyalAuthorityReady()
        {
            // If we can continue combo after Riot Blade, RA is ready now
            if (CanContinueComboAfter(Spells.RiotBlade))
                return 0;

            // If we can continue combo after Fast Blade, need 1 GCD (Riot Blade)
            if (CanContinueComboAfter(Spells.FastBlade))
                return 1;

            // If combo is broken or we're at start, need 2 GCDs (Fast Blade + Riot Blade)
            if (ActionManager.ComboTimeLeft <= 0 || ActionManager.LastSpell.Id == Spells.RoyalAuthority.Id)
                return 2;

            // Unknown state - assume we need to restart combo
            return 2;
        }

        /// <summary>
        /// Checks if we should bank resources (Atonement stacks, Supplication, Sepulchre, Divine Might)
        /// until the next Royal Authority combo is ready.
        /// </summary>
        public static bool ShouldBankResourcesForRoyalAuthority()
        {
            int gcdsUntilRA = GCDsUntilRoyalAuthorityReady();

            // Bank resources if RA is 1-2 GCDs away (allows us to prime resources for burst)
            // Don't bank if RA is ready now (gcdsUntilRA == 0) - we can use resources immediately
            return gcdsUntilRA > 0 && gcdsUntilRA <= 2;
        }

        /// <summary>
        /// Checks if we have any high-priority resources available for FoF window.
        /// Returns true if we have AtonementReady, SupplicationReady, SepulchreReady, or DivineMight.
        /// </summary>
        public static bool HasHighPriorityResourcesForFoF()
        {
            return Core.Me.HasAnyAura(new uint[]
            {
                Auras.AtonementReady,
                Auras.SupplicationReady,
                Auras.SepulchreReady,
                Auras.DivineMight
            });
        }

        /// <summary>
        /// Checks if we can reach Sepulchre before Fight or Flight ends.
        /// Returns true if we can reach Sepulchre in time, false otherwise.
        /// </summary>
        public static bool CanReachSepulchreBeforeFoFEnds()
        {
            if (!Core.Me.HasAura(Auras.FightOrFlight))
                return false;

            if (!Spells.Sepulchre.IsKnown())
                return false;

            // If we already have Sepulchre ready, we can use it immediately
            if (Core.Me.HasAura(Auras.SepulchreReady))
                return true;

            // Calculate GCDs needed to reach Sepulchre based on current stacks
            int gcdsToSepulchre = 0;
            if (Core.Me.HasAura(Auras.AtonementReady))
                gcdsToSepulchre = 2; // Atonement -> Supplication -> Sepulchre
            else if (Core.Me.HasAura(Auras.SupplicationReady))
                gcdsToSepulchre = 1; // Supplication -> Sepulchre
            else
            {
                // No stacks available - need RA first to get stacks
                // If RA combo is ready, that's 3 GCDs total (RA -> Atonement -> Supplication -> Sepulchre)
                if (CanContinueComboAfter(Spells.RiotBlade))
                    gcdsToSepulchre = 3; // RA -> Atonement -> Supplication -> Sepulchre
                else
                    // Can't RA yet - too many GCDs needed, can't reach Sepulchre
                    return false;
            }

            // Calculate time needed and check if FoF has enough time remaining
            int timeNeededForSepulchre = (int)(gcdsToSepulchre * GCDTimeMilliseconds);

            // Return true if FoF has at least enough time remaining to reach Sepulchre
            return Core.Me.HasAura(Auras.FightOrFlight, msLeft: timeNeededForSepulchre);
        }
    }
}

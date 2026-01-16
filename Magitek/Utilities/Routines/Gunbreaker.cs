using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;

namespace Magitek.Utilities.Routines
{
    internal static class Gunbreaker

    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Gunbreaker, Spells.KeenEdge);

        // Track Gnashing Fang usage in current burst window (reset on No Mercy, increment on each GF cast)
        public static int GnashingFangUsesThisBurst = 0;

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.Rampart,
            Spells.Camouflage,
            Spells.Nebula,
            Spells.GreatNebula,
            Spells.HeartOfCorundum,
            Spells.HeartofStone
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.Camouflage,
            Auras.Nebula,
            Auras.Aurora,
            Auras.Superbolide,
            Auras.HeartofLight,
            Auras.HeartOfCorundum
        };

        public static SpellData HeartOfCorundum => Spells.HeartOfCorundum.IsKnown()
                                            ? Spells.HeartOfCorundum
                                            : Spells.HeartofStone;

        public static SpellData BlastingZone => Spells.BlastingZone.IsKnown()
                                            ? Spells.BlastingZone
                                            : Spells.DangerZone;

        // HARDCODED: Level 88 trait increases max cartridges and Bloodfest generation.
        public static int MaxCartridge => Core.Me.ClassLevel < 88 ? 2 : 3;
        // HARDCODED: Level 88 trait increases max cartridges and Bloodfest generation.
        public static int AmountCartridgeFromBloodfest => Core.Me.ClassLevel < 88 ? 2 : 3;
        public static int RequiredCartridgeForGnashingFang => 1;
        public static int RequiredCartridgeForDoubleDown => 2;
        public static int RequiredCartridgeForBurstStrike => 1;
        public static int RequiredCartridgeForFatedCircle => 1;

        public static bool IsAurasForComboActive()
        {
            return (Core.Me.HasAura(Auras.ReadytoRip)
                || Core.Me.HasAura(Auras.ReadytoTear)
                || Core.Me.HasAura(Auras.ReadytoGouge)
                || Core.Me.HasAura(Auras.ReadytoBlast)
                );
        }

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

    }
}

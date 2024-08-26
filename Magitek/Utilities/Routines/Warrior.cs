using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using System.Collections.Generic;

namespace Magitek.Utilities.Routines
{
    internal static class Warrior
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Warrior, Spells.HeavySwing);

        public static SpellData FellCleave => Core.Me.ClassLevel < 54
                                            ? Spells.InnerBeast
                                            : Spells.FellCleave;

        public static SpellData Decimate => Core.Me.ClassLevel < 60
                                            ? Spells.SteelCyclone
                                            : Spells.Decimate;

        public static SpellData InnerRelease => Core.Me.ClassLevel < 70
                                            ? Spells.Berserk
                                            : Spells.InnerRelease;

        public static SpellData Bloodwhetting => Core.Me.ClassLevel < 82
                                            ? Spells.RawIntuition
                                            : Spells.Bloodwhetting;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.Rampart,
            Spells.Damnation,
            Spells.Vengeance,
            Spells.ThrillofBattle,
            Spells.Bloodwhetting,
            Spells.RawIntuition
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.RawIntuition,
            Auras.Bloodwhetting,
            Auras.Vengeance,
            Auras.Holmgang,
            Auras.ThrillOfBattle,
            Auras.Damnation
        };

        public static readonly List<uint> Heal = new List<uint>()
        {
            Auras.Equilibrium,
            Auras.ThrillOfBattle
        };
    }
}

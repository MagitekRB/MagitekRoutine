using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;

namespace Magitek.Utilities.Routines
{
    internal static class Warrior
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Warrior, Spells.HeavySwing);

        public static SpellData FellCleave => Spells.FellCleave.IsKnown()
                                            ? Spells.FellCleave
                                            : Spells.InnerBeast;

        public static SpellData Decimate => Spells.Decimate.IsKnown()
                                            ? Spells.Decimate
                                            : Spells.SteelCyclone;

        public static SpellData InnerRelease => Spells.InnerRelease.IsKnown()
                                            ? Spells.InnerRelease
                                            : Spells.Berserk;

        public static SpellData Bloodwhetting => Spells.Bloodwhetting.IsKnown()
                                            ? Spells.Bloodwhetting
                                            : Spells.RawIntuition;

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

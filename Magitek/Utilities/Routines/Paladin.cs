using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;

namespace Magitek.Utilities.Routines
{
    internal static class Paladin
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Paladin, Spells.FastBlade);
        public static double GCDTimeMilliseconds = Spells.FastBlade.AdjustedCooldown.TotalMilliseconds;

        public static SpellData RoyalAuthority => Core.Me.ClassLevel < 60
                                                    ? Spells.RageofHalone
                                                    : Spells.RoyalAuthority;
        public static SpellData Expiacion => Core.Me.ClassLevel < 86
                                            ? Spells.SpiritsWithin
                                            : Spells.Expiacion;

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
    }
}
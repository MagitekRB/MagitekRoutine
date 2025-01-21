using ff14bot.Enums;
using ff14bot.Objects;

namespace Magitek.Utilities.Routines
{
    internal static class DarkKnight
    {
        public static WeaveWindow GlobalCooldown
            = new WeaveWindow(ClassJobType.DarkKnight, Spells.HardSlash);

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.TheBlackestNight,
            Spells.Rampart,
            Spells.ShadowWall,
            Spells.DarkMind,
            Spells.Oblation,
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.LivingDead,
            Auras.ShadowWall,
            Auras.Rampart,
            Auras.BlackestNight,
            Auras.Oblation
        };
    }
}
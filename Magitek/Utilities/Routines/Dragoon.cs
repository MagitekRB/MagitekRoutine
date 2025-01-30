using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using System.Collections.Generic;


namespace Magitek.Utilities.Routines
{
    internal static class Dragoon
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Dragoon, Spells.TrueThrust);

        public static SpellData HighJump => Core.Me.ClassLevel < 74
                                            ? Spells.Jump
                                            : Spells.HighJump;

        public static SpellData HeavensThrust => Core.Me.ClassLevel < 86
                                            ? Spells.FullThrust
                                            : Spells.HeavensThrust;

        public static SpellData ChaoticSpring => Core.Me.ClassLevel < 86
                                            ? Spells.ChaosThrust
                                            : Spells.ChaoticSpring;

        public static SpellData Disembowel => Core.Me.ClassLevel < Spells.SpiralBlow.LevelAcquired
                                            ? Spells.Disembowel
                                            : Spells.SpiralBlow;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static List<SpellData> JumpsList = new List<SpellData>()
        {
            HighJump,
            Spells.DragonfireDive,
            Spells.MirageDive,
            Spells.Stardiver
        };

        public static List<SpellData> SingleWeaveJumpsList = new List<SpellData>()
        {
            HighJump,
            Spells.DragonfireDive,
            Spells.Stardiver
        };
    }
}

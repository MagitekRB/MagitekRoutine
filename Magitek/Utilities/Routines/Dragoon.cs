using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;


namespace Magitek.Utilities.Routines
{
    internal static class Dragoon
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Dragoon, Spells.TrueThrust);

        public static SpellData HighJump => Spells.HighJump.IsKnown()
                                            ? Spells.HighJump
                                            : Spells.Jump;

        public static SpellData HeavensThrust => Spells.HeavensThrust.IsKnown()
                                            ? Spells.HeavensThrust
                                            : Spells.FullThrust;

        public static SpellData ChaoticSpring => Spells.ChaoticSpring.IsKnown()
                                            ? Spells.ChaoticSpring
                                            : Spells.ChaosThrust;

        public static SpellData Disembowel => !Spells.SpiralBlow.IsKnown()
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

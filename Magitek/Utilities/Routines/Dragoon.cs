using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;
using Magitek.Models.Account;
using ff14bot.Managers;


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

        public static SpellData VorpalThrust => !Spells.LanceBarrage.IsKnown()
                                            ? Spells.VorpalThrust
                                            : Spells.LanceBarrage;
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
            Spells.Jump,
            Spells.HighJump,
            Spells.DragonfireDive,
            Spells.MirageDive,
            Spells.Stardiver
        };

        public static List<SpellData> SingleWeaveJumpsList = new List<SpellData>()
        {
            Spells.Jump,
            Spells.HighJump,
            Spells.DragonfireDive,
            Spells.Stardiver
        };
        public static bool CanWeaveJump()
        {
            // Jumps have a longer animation lock than generic oGCDs.
            // Add 150ms buffer to the standard animation lock check.
            return GlobalCooldown.CanWeave() &&
                   Spells.TrueThrust.Cooldown.TotalMilliseconds > 
                   (Globals.AnimationLockMs + 150 + BaseSettings.Instance.UserLatencyOffset);
        }
    }
}

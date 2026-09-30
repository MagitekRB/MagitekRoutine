using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;

namespace Magitek.Utilities
{
    /// <summary>
    /// Controls AoE rotation selection and casting using one shared permission check.
    /// </summary>
    /// <remarks>
    /// Enable/Disable persist the user's preference. Botbase restrictions temporarily
    /// override it without changing saved settings. Casting checks include masked
    /// finishers and damaging heals, even when reached through a single-target rotation.
    /// </remarks>
    public static class AoeControl
    {
        private static ushort _preparedTerritory;

        public static bool Enabled
        {
            get => BaseSettings.Instance.EnableAoe && !RestrictedByBotbase;
            private set => BaseSettings.Instance.EnableAoe = value;
        }

        public static void Enable() => Enabled = true;

        public static void Disable() => Enabled = false;

        // Toggle the saved preference, not the temporarily restricted value.
        public static void Toggle() => Enabled = !BaseSettings.Instance.EnableAoe;

        public static void Set(bool enabled) => Enabled = enabled;

        private static bool RestrictedByBotbase => !WorldManager.InPvP
            && RoutineManager.IsAnyDisallowed(CapabilityFlags.Aoe);

        // Suppress lasting effects from route entry: an earlier summon or ground
        // effect cannot be recalled when the botbase later disables AoE for a pack.
        internal static void Prepare(ushort territory) => _preparedTerritory = territory;

        private static bool Preparing => !WorldManager.InPvP && _preparedTerritory != 0
            && DutyManager.InInstance && WorldManager.ZoneId == _preparedTerritory;

        // Called on the bot thread by readiness, casting and cast tracking. Keeping
        // the rule here prevents individual rotations from bypassing the AoE switch.
        internal static bool Allows(SpellData spell, GameObject target)
        {
            var enabled = Enabled;
            var preparing = Preparing;
            if (enabled && !preparing)
                return true;

            if (spell == null || target == null || !target.IsValid)
                return false;

            var effective = spell.Masked();
            if (effective == null)
                return false;

            if (HasPersistentDamage(spell.Id) || HasPersistentDamage(effective.Id))
                return false;

            if (enabled)
                return true;

            // RB cast type 1 with zero radius/effect range describes one actor.
            // Reject unknown geometry (including NaN) instead of letting a masked
            // finisher through. This conservatively also suppresses area support.
            if (spell.Id == 0 || effective.Id == 0 || effective.RawCastType != 1
                || effective.Radius != 0 || effective.EffectRange != 0 || effective.GroundTarget)
                return false;

            // An ordered-kill restriction also forbids multi-dotting another enemy.
            // The routine must never change the target selected by the botbase.
            return !RestrictedByBotbase || !(target is BattleCharacter enemy && enemy.CanAttack)
                || target.ObjectId == Core.Me.CurrentTarget?.ObjectId;
        }

        // Only advertise ordinary PvE jobs whose dispatch paths use this control.
        // Limited-job autonomous familiars are outside that contract.
        internal static bool Supports(ClassJobType job) => job is
            ClassJobType.Paladin or ClassJobType.Warrior or ClassJobType.DarkKnight or ClassJobType.Gunbreaker
            or ClassJobType.WhiteMage or ClassJobType.Scholar or ClassJobType.Astrologian or ClassJobType.Sage
            or ClassJobType.Monk or ClassJobType.Dragoon or ClassJobType.Ninja or ClassJobType.Samurai
            or ClassJobType.Reaper or ClassJobType.Viper or ClassJobType.Bard or ClassJobType.Machinist
            or ClassJobType.Dancer or ClassJobType.BlackMage or ClassJobType.Summoner or ClassJobType.RedMage
            or ClassJobType.Pictomancer;

        // Spells.cs and Global action metadata captured 2026-09-30. These actions
        // can keep dealing damage after the next cast is blocked; summon and
        // retaliation buttons have single-actor geometry, so shape alone is unsafe.
        private static bool HasPersistentDamage(uint id) => id is
            44 or 36923 // Vengeance / Damnation: retaliation
            or 16472 // Living Shadow
            or 2864 or 16501 // Rook Autoturret / Automaton Queen
            or 7427 or 25831 or 36992 // Bahamut / Phoenix / Solar Bahamut
            or 25802 or 25803 or 25804 // Ruby / Topaz / Emerald summon buttons
            or 25805 or 25806 or 25807 or 25838 or 25839 or 25840 // Egi upgrades
            or 25800 or 3581 // Aethercharge / Trance can mask to a demi summon
            or 3639 // Salted Earth
            or 2270 // Doton
            or 25837 // Slipstream
            or 7439 or 8324 // Earthly Star / Stellar Detonation
            or 7418; // Flamethrower
    }
}

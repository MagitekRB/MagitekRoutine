using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Models;
using System.Collections.Generic;
using System.Linq;

namespace Magitek.Utilities
{
    internal static class PvpAggroCountTracker
    {
        // Frame-cached collection of enemy players targeting the player
        // Automatically refreshes once per frame when accessed
        // Note: FrameCachedObject requires reference types, so we cache IEnumerable<BattleCharacter>
        private static readonly FrameCachedObject<IEnumerable<BattleCharacter>> _enemiesTargetingMe = new(() =>
        {
            // Return empty if not in PvP (the overlay will show "0")
            if (!Globals.OnPvpMap)
                return Enumerable.Empty<BattleCharacter>();

            try
            {
                // Get all enemy players who are targeting us
                return GameObjectManager.GetObjectsOfType<BattleCharacter>()
                    .Where(enemy =>
                        enemy != null &&
                        enemy.IsValid &&
                        enemy.IsTargetable &&
                        enemy.CanAttack &&
                        enemy.Type == ff14bot.Enums.GameObjectType.Pc && // Only players
                        enemy.TargetGameObject != null &&
                        enemy.TargetGameObject.ObjectId == Core.Me.ObjectId) // Targeting me
                    .ToList(); // Materialize to avoid multiple enumerations
            }
            catch
            {
                // Silently fail if there's any issue querying game objects
                return Enumerable.Empty<BattleCharacter>();
            }
        });

        /// <summary>
        /// Gets the number of enemy players currently targeting you.
        /// Frame-cached - safe to call multiple times per frame.
        /// </summary>
        public static int EnemiesTargetingMe => _enemiesTargetingMe.Value.Count();

        /// <summary>
        /// Gets the collection of enemy players currently targeting you.
        /// Frame-cached - safe to call multiple times per frame.
        /// Use this for advanced logic that needs to know WHO is targeting you, not just how many.
        /// </summary>
        public static IEnumerable<BattleCharacter> EnemiesTargetingMeCollection => _enemiesTargetingMe.Value;

        /// <summary>
        /// Updates the aggro count model with the current frame-cached enemy count.
        /// This method should be called once per pulse to update the UI overlay.
        /// The actual query is frame-cached and won't run multiple times per frame.
        /// </summary>
        public static void UpdateAggroCount()
        {
            // Count the frame-cached collection
            PvpAggroCountModel.Instance.EnemiesTargetingMe = _enemiesTargetingMe.Value.Count();
        }
    }
}


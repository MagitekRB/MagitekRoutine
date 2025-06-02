using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Utilities;
using Magitek.ViewModels;
using System.Collections.Generic;
using System.Linq;


namespace Magitek.Extensions
{
    internal static class PlayerExtensions
    {
        public static bool HasAetherflow(this LocalPlayer me)
        {
            return ActionResourceManager.Scholar.Aetherflow > 0;
        }

        public static bool HasDarkArts(this LocalPlayer me)
        {
            return ActionResourceManager.DarkKnight.DarkArts;
        }

        public static bool OnPvpMap(this LocalPlayer player)
        {
            if (!WorldManager.InPvP)
            {
                BaseSettings.Instance.InPvp = false;
                return false;
            }

            BaseSettings.Instance.InPvp = true;
            return true;
        }

        public static bool OnOccultCrescent(this LocalPlayer player)
        {
            return WorldManager.ZoneId == 1252;
        }

        /// <summary>
        /// Checks if the player is in a sanctuary (game's sanctuary status) or in a zone that should be treated as a sanctuary
        /// to prevent instant casting of buffs and other actions.
        /// </summary>
        public static bool InSanctuaryOrSafeZone(this LocalPlayer player)
        {
            // Check the game's built-in sanctuary status first
            if (WorldManager.InSanctuary)
                return true;

            // Check additional zones that should be treated as sanctuaries
            return CustomSanctuaryZones.Contains(WorldManager.ZoneId);
        }

        private static readonly HashSet<ushort> PvpMaps = new HashSet<ushort>()
        {
            149,
            175,
            184,
            186,
            250,
            336,
            337,
            352,
            376,
            422,
            431,
            502,
            506,
            518,
            525,
            526,
            527,
            528,
            537,
            538,
            539,
            540,
            541,
            542,
            543,
            544,
            545,
            546,
            547,
            548,
            549,
            550,
            551,
            552,
            554
        };

        /// <summary>
        /// Zone IDs that should be treated as sanctuaries even if the game doesn't mark them as such.
        /// Add zone IDs here to prevent instant casting of buffs and other actions.
        /// </summary>
        private static readonly HashSet<ushort> CustomSanctuaryZones = new HashSet<ushort>()
        {
            // Add zone IDs here that should be treated as sanctuaries
            1269, // Phantom Village
            1237, // Sinus Adorum
        };

        public static int EnemiesInCone(this LocalPlayer player, float maxdistance)
        {
            return Combat.Enemies.Count(r => r.Distance(Core.Me) <= maxdistance + r.CombatReach && r.RadiansFromPlayerHeading() < 0.9599f);
        }
    }
}

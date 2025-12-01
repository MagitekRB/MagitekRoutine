using Magitek.Enumerations;
using Magitek.Logic.Gunbreaker;
using Latest = Magitek.Logic.Gunbreaker.Latest;
using V49 = Magitek.Logic.Gunbreaker.V49;
using Experimental = Magitek.Logic.Gunbreaker.Experimental;
using Magitek.Models.Gunbreaker;
using System.Threading.Tasks;

namespace Magitek.Rotations
{
    /// <summary>
    /// Gunbreaker rotation proxy router - selects implementation based on settings.
    /// </summary>
    public static class Gunbreaker
    {
        public static Task<bool> Rest()
        {
            return GetImplementation().Rest();
        }

        public static Task<bool> PreCombatBuff()
        {
            return GetImplementation().PreCombatBuff();
        }

        public static Task<bool> Pull()
        {
            return GetImplementation().Pull();
        }

        public static Task<bool> Heal()
        {
            return GetImplementation().Heal();
        }

        public static Task<bool> CombatBuff()
        {
            return GetImplementation().CombatBuff();
        }

        public static Task<bool> Combat()
        {
            return GetImplementation().Combat();
        }

        public static Task<bool> PvP()
        {
            return GetImplementation().PvP();
        }

        /// <summary>
        /// Router method - selects implementation based on settings.
        /// </summary>
        private static IGunbreakerImplementation GetImplementation()
        {
            return GunbreakerSettings.Instance.Implementation switch
            {
                GunbreakerImplementation.V49 => V49.Implementation.Instance,
                GunbreakerImplementation.Experimental => Experimental.Implementation.Instance,
                _ => Latest.Implementation.Instance  // Default to Latest
            };
        }
    }
}

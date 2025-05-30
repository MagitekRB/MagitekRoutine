using ff14bot;
using ff14bot.Directors;
using ff14bot.Managers;
using System.Collections.Generic;

namespace Magitek.Utilities
{
    public static class Duty
    {
        public static HashSet<uint> PvpZoneIds = new HashSet<uint>()
            {
                1032, // the palaistra
                1033, // the volcanic heart
                1058, // the palaistra 2
                1059, // the volcanic heart 2
                1034, // cloud nine
                1060, // cloud nine 2
            };

        /// <summary>
        /// Provides Duty State information. InProgress, NotInDuty, NotStarted, and Ended.
        /// </summary>
        /// <returns></returns>
        public static States State()
        {
            if (Core.Me.InCombat) return States.InProgress; //If we're in Combat, don't even continue, it doesn't matter if we're in a duty or not, just unblock our actions and tell us we're in Progress

            // In Occult Crescent
            if (WorldManager.ZoneId == 1252) return States.InProgress;

            if (DirectorManager.ActiveDirector == null) return States.NotInDuty;

            if (DirectorManager.ActiveDirector.DirectorType != DirectorType.InstanceContent) return States.NotInDuty;

            var instanceDirector = (InstanceContentDirector)DirectorManager.ActiveDirector;

            if (instanceDirector.InstanceEnded) return States.Ended;

            return instanceDirector.InstanceStarted ? States.InProgress : States.NotStarted;
        }

        public enum States
        {
            NotInDuty,
            NotStarted,
            InProgress,
            Ended
        }
    }
}

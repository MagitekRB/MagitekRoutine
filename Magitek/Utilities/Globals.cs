using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using System;
using System.Linq;

namespace Magitek.Utilities
{
    internal static class Globals
    {
        public static bool InParty => PartyManager.IsInParty;
        public static bool PartyInCombat => Core.Me.InCombat ||
            (InParty && Group.RawPartyMembers.Any(ally => 
                ally.IsAlive && ally.InCombat
            ));
        public static bool InGcInstance => RaptureAtkUnitManager.Controls.Any(r => r.Name == "GcArmyOrder");
        public static bool OnPvpMap => Core.Me.OnPvpMap();
        public static bool InActiveDuty => DutyManager.InInstance && Duty.State() == Duty.States.InProgress;
        public static GameObject HealTarget;
        // the game has a base animation lock of 610ms for most weaponskills & spells.
        // Some weaved abilities that are instant have a shorter animation lock of ~360-410ms
        // that is not known until the ability is used. This is a safe value to use for all abilities
        // to calculate what can be achieved under normal circumstances. 
        public static int AnimationLockMs
        {
            get {
                // a lower threshold should be safe to assume if the routine is going to wait for the game to release the lock
                if (BaseSettings.Instance.DebugActionLockWait)
                    return 625;
                else
                    return 770;
            }
        }
        public static TimeSpan AnimationLockTimespan = TimeSpan.FromMilliseconds(AnimationLockMs);
    }
}

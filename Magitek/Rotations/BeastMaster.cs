using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.BeastMaster;
using Magitek.Logic.Roles;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Threading.Tasks;
using BeastMasterRoutine = Magitek.Utilities.Routines.BeastMaster;

namespace Magitek.Rotations
{
    /// <summary>
    /// Beastmaster (7.56, level 1-50). First version: the bot does not expose the TP gauges or the Inner Compass
    /// yet, so the instinctual skills are gated the way the client gates them (castable from 100 TP) and the compass
    /// is read from the Heart statuses. The familiar is the player's pet; its Trick affinity comes from the
    /// bestiary catalogue by name.
    /// </summary>
    public static class BeastMaster
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(Core.Me.CurrentHealthPercent < BeastMasterSettings.Instance.RestHealthPercent);
        }

        public static async Task<bool> PreCombatBuff()
        {
            await Casting.CheckForSuccessfulCast();
            BeastMasterRoutine.RefreshVars();

            // A familiar out before the pull: the horn's cooldown only runs from a retreat in combat.
            return await Familiar.Summon();
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return false;
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            BeastMasterRoutine.RefreshVars();

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            // A familiar first: everything else keys off it.
            if (await Familiar.Summon()) return true;

            if (BeastMasterRoutine.GlobalCooldown.CanWeave())
            {
                if (await Familiar.SpendOneWithNature()) return true;
                if (await Familiar.KinshipAction()) return true;
                if (await Familiar.Trick()) return true;
                if (await Familiar.PartingBlow()) return true;
                if (await Familiar.Rally()) return true;
                if (await Familiar.RallyingCheer()) return true;
                if (await SingleTarget.ShieldCharge()) return true;
                if (await PhysicalDps.SecondWind(BeastMasterSettings.Instance)) return true;
                if (await PhysicalDps.Bloodbath(BeastMasterSettings.Instance)) return true;
            }

            if (await SingleTarget.InstinctualSkill()) return true;
            if (await Familiar.QuellingWave()) return true;
            return await SingleTarget.Combo();
        }

        public static Task<bool> PvP()
        {
            return Task.FromResult(false);
        }
    }
}

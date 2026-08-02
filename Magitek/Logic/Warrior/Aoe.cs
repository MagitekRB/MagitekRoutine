using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Warrior;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using WarriorRoutine = Magitek.Utilities.Routines.Warrior;

namespace Magitek.Logic.Warrior
{
    internal static class Aoe
    {
        public static async Task<bool> ChaoticCyclone()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (!WarriorSettings.Instance.UseChaoticCyclone)
                return false;

            if (!Core.Me.HasAura(Auras.NascentChaos))
                return false;

            if (!Core.Me.HasAura(Auras.SurgingTempest))
                return false;

            if (!Core.Me.HasAura(Auras.InnerRelease) && !WarriorSettings.Instance.UseBeastGauge)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ChaoticCyclone.Radius)) < WarriorSettings.Instance.ChaoticCycloneMinimumEnemies)
                return false;

            return await Spells.ChaoticCyclone.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> Decimate()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (!WarriorSettings.Instance.UseDecimate)
                return false;

            if (!Core.Me.HasAura(Auras.SurgingTempest))
                return false;

            if (Core.Me.HasAura(Auras.NascentChaos))
                return false;

            if (!Core.Me.HasAura(Auras.InnerRelease) && !WarriorSettings.Instance.UseBeastGauge)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(WarriorRoutine.Decimate.Radius)) < WarriorSettings.Instance.DecimateMinimumEnemies)
                return false;

            return await WarriorRoutine.Decimate.Cast(Core.Me.CurrentTarget);
        }


        public static async Task<bool> Overpower()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.Overpower.Radius)) < WarriorSettings.Instance.OverpowerMinimumEnemies)
                return false;

            return await Spells.Overpower.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MythrilTempest()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (!WarriorRoutine.CanContinueComboAfter(Spells.Overpower))
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.MythrilTempest.Radius)) < WarriorSettings.Instance.MythrilTempestMinimumEnemies)
                return false;

            return await Spells.MythrilTempest.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Orogeny()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (!Spells.Orogeny.IsReady())
                return false;

            if (!Core.Me.HasAura(Auras.SurgingTempest))
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.Orogeny.Radius)) < WarriorSettings.Instance.OrogenyMinimumEnemies)
                return false;

            return await Spells.Orogeny.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrimalRend()
        {
            if (!WarriorSettings.Instance.UsePrimalRend)
                return false;

            // Primal Rend jumps to the target, so it is a gap closer in everything but name and has to
            // answer to the same gate — otherwise a movement-punishing mechanic parks navigation and this
            // moves the character anyway. UsePrimalRendWhenNotMoving below is a separate, opt-in DPS
            // preference reading MovementManager; it is not a substitute for this.
            if (!Movement.CanUseGapCloser())
                return false;

            if (!Core.Me.HasAura(Auras.PrimalRendReady))
                return false;

            if (!Core.Me.HasAura(Auras.SurgingTempest))
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.PrimalRend.Radius)) < WarriorSettings.Instance.PrimalRendMinimumEnemies)
                return false;

            if (WarriorSettings.Instance.UsePrimalRendWhenNotMoving && MovementManager.IsMoving)
                return false;

            return await Spells.PrimalRend.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrimalRuination()
        {
            if (!WarriorSettings.Instance.UsePrimalRend)
                return false;

            if (!Core.Me.HasAura(Auras.PrimalRuinationReady))
                return false;

            if (!Core.Me.HasAura(Auras.SurgingTempest))
                return false;

            return await Spells.PrimalRuination.Cast(Core.Me.CurrentTarget);
        }
    }
}
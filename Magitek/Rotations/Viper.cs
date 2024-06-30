using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Roles;
using Magitek.Logic.Viper;
using Magitek.Models.Account;
using Magitek.Models.Viper;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using ViperRoutine = Magitek.Utilities.Routines.Viper;
using System.Linq;
using System.Threading.Tasks;


namespace Magitek.Rotations
{

    public static class Viper
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(Core.Me.CurrentHealthPercent < 75 || Core.Me.CurrentManaPercent < 50);
        }

        public static async Task<bool> PreCombatBuff()
        {
            await Casting.CheckForSuccessfulCast();

            if (WorldManager.InSanctuary)
                return false;

            return false;
        }

        public static async Task<bool> Pull()
        {
            if (BotManager.Current.IsAutonomous)
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, Core.Me.CurrentTarget.CombatReach);

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            if (await Casting.TrackSpellCast()) 
                return true;

            await Casting.CheckForSuccessfulCast();

            return await GambitLogic.Gambit();
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            ViperRoutine.RefreshVars();

            if (BotManager.Current.IsAutonomous)
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 2 + Core.Me.CurrentTarget.CombatReach);

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (!SpellQueueLogic.SpellQueue.Any())
                SpellQueueLogic.InSpellQueue = false;

            if (Core.Me.CurrentTarget.HasAnyAura(Auras.Invincibility))
                return false;

            if (await CustomOpenerLogic.Opener())
                return true;

            if (SpellQueueLogic.SpellQueue.Any())
                if (await SpellQueueLogic.SpellQueueMethod())
                    return true;

            //LimitBreak
            //if (SingleTarget.ForceLimitBreak()) return true;

            //Utility
            /*
            if (await PhysicalDps.Interrupt(SamuraiSettings.Instance)) return true;
            if (await PhysicalDps.SecondWind(SamuraiSettings.Instance)) return true;
            if (await PhysicalDps.Bloodbath(SamuraiSettings.Instance)) return true;
            */

            if (ViperRoutine.GlobalCooldown.CanWeave())
            {
                //Utility
                //if (await Utility.TrueNorth()) return true;
                //if (await Buff.UsePotion()) return true;

                //if (await SingleTarget.HissatsuYaten()) return true; //dash backward
            }


            //if (await SingleTarget.SteelMaw()) return true;

            return await SingleTarget.SteelMaw();
        }

        public static void RegisterCombatMessages()
        {

        }

        public static async Task<bool> PvP()
        {
            if(!BaseSettings.Instance.ActivePvpCombatRoutine)
                return await Combat();

            return (await SingleTarget.SteelMaw());
        }
    }
}

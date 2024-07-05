using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Roles;
using Magitek.Logic.Pictomancer;
using Magitek.Models.Account;
using Magitek.Models.Pictomancer;
using Magitek.Utilities;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using PictomancerRoutine = Magitek.Utilities.Routines.Pictomancer;

namespace Magitek.Rotations
{
    public static class Pictomancer
    {
        public static async Task<bool> Rest()
        {
            if (Core.Me.CurrentHealthPercent > 70 || Core.Me.ClassLevel < 4)
                return false;

            if (WorldManager.InSanctuary)
                return false;

            return false;
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (Core.Me.IsCasting)
                return true;

            await Casting.CheckForSuccessfulCast();
            SpellQueueLogic.SpellQueue.Clear();

            if (WorldManager.InSanctuary)
                return false;

            if (DutyManager.InInstance && !Globals.InActiveDuty)
                return false;

            if (await Palette.PrePaintPalettes(true)) return true;

            return false;
        }

        public static async Task<bool> Pull()
        {
            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);
            }

            return await Combat();
        }
        public static async Task<bool> Heal()
        {
            if (Core.Me.IsMounted)
                return true;

            if (await Casting.TrackSpellCast()) return true;
            await Casting.CheckForSuccessfulCast();

            Casting.DoHealthChecks = false;

            if (await GambitLogic.Gambit()) return true;
            return false;

        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);
            }

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack()) {
                // Paint up the palettes during "downtime".
                if (await Palette.PrePaintPalettes(false)) return true;
                return false;
            }

            if (await CustomOpenerLogic.Opener())
                return true;

            if (Core.Me.CurrentTarget.HasAura(Auras.MagicResistance))
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(Auras.Invincibility))
                return false;

            if (PictomancerRoutine.GlobalCooldown.CanWeave(1)) 
            {
                if (await Buff.FightLogic_TemperaCoat()) return true;
                if (await Buff.FightLogic_Addle()) return true;
                if (await Healer.LucidDreaming(PictomancerSettings.Instance.UseLucidDreaming, PictomancerSettings.Instance.LucidDreamingMinimumManaPercent)) return true;
            }

            // palettes
            if (await Palette.StarPrism()) return true;
            if (await Palette.RainbowDrip()) return true;
            if (await Palette.ScenicMuse()) return true;

            if (Core.Me.HasAura(Auras.Inspiration) ||
                PictomancerRoutine.GlobalCooldown.CanWeave(1))
            {
                if (await Palette.MogoftheAges()) return true;
                if (await Palette.StrikingMuse()) return true;
                if (await Palette.CreatureMuse()) return true;
                if (await Buff.SubtractivePalette()) return true;
            }

            if (await Palette.HammerStamp()) return true;

            // inspiration is on a timer, need to consume those stacks first.
            // don't waste time painting more palettes
            if (PictomancerSettings.Instance.PaletteDuringStarry || !Core.Me.HasAura(Auras.Hyperphantasia) || Spells.Swiftcast.IsKnownAndReady())
            {
                if (await Palette.LandscapeMotif()) return true;
                if (await Palette.CreatureMotif()) return true;
                if (await Palette.WeaponMotif()) return true;
            }

            // attacks
            if (await AOE.CometinBlack()) return true;
            if (await AOE.HolyinWhite()) return true;
            if (await SingleTarget.HolyinWhite()) return true;
            if (await AOE.Paint()) return true;
            if (await SingleTarget.CometinBlack()) return true;
            if (await SingleTarget.Paint()) return true;
            return false;
        }

        public static async Task<bool> PvP()
        {
            return false;
        }
    }
}

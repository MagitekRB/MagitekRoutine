using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Pictomancer;
using Magitek.Logic.Roles;
using Magitek.Models.Pictomancer;
using Magitek.Utilities;
using System.Threading.Tasks;
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
            return false;
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
            {
                // Paint up the palettes during "downtime".
                if (await Palette.PrePaintPalettes(false)) return true;
                return false;
            }

            if (MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.ChromaticFantasy, Spells.FireinRed)) return true;

            if (Core.Me.CurrentTarget.HasAura(Auras.MagicResistance))
                return false;

            PictomancerRoutine.DetectSmudge();

            if (!Core.Me.HasAura(Auras.StarryMuse, true))
            {
                if (await Buff.FightLogic_TemperaGrassa()) return true;
                if (await Buff.FightLogic_TemperaCoat()) return true;
                if (await MagicDps.FightLogic_Addle(PictomancerSettings.Instance)) return true;
            }

            if (await CommonFightLogic.FightLogic_Knockback(PictomancerSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            if (PictomancerRoutine.GlobalCooldown.CanWeave())
            {
                if (await Healer.LucidDreaming(PictomancerSettings.Instance.UseLucidDreaming, PictomancerSettings.Instance.LucidDreamingMinimumManaPercent)) return true;

                if (await Buff.UsePotion()) return true;
            }

            if (PictomancerRoutine.GlobalCooldown.CanWeave() || MovementManager.IsMoving)
            {
                if (await Palette.ScenicMuse()) return true;
                if (await Palette.CreatureMuse()) return true;
                if (await Palette.StrikingMuse()) return true;
                if (await Palette.MogoftheAges()) return true;
            }

            // palettes
            if (await SingleTarget.CometinBlack()) return true;
            if (await Buff.SubtractivePalette()) return true;

            if (await Palette.StarPrism()) return true;
            if (await Palette.RainbowDrip()) return true;
            if (await Palette.HammerStamp()) return true;

            // inspiration is on a timer, need to consume those stacks first.
            // don't waste time painting more palettes
            if (PictomancerSettings.Instance.PaletteDuringStarry
                || !Core.Me.HasAura(Auras.StarryMuse, true))
            {
                if (await Palette.LandscapeMotif()) return true;
                if (await Palette.WeaponMotif()) return true;
                if (await Palette.CreatureMotif()) return true;
            }

            // attacks
            if (await AOE.CometinBlack()) return true;
            if (await AOE.HolyinWhite()) return true;
            if (await AOE.Paint()) return true;
            if (await SingleTarget.HolyinWhite()) return true;
            if (await SingleTarget.Paint()) return true;
            return false;
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(PictomancerSettings.Instance)) return true;

            if (!CommonPvp.GuardCheck(PictomancerSettings.Instance))
            {
                if (await Pvp.TemperaCoat()) return true;
                if (await Pvp.TemperaGrassa()) return true;

                if (await Pvp.Starstruck()) return true;
                if (await Pvp.AdventofChocobastion()) return true;
                if (await Pvp.SubtractivePalette()) return true;

                if (await Pvp.PaintB()) return true;
                if (await Pvp.PaintW()) return true;

                if (await Pvp.MogoftheAges()) return true;
                if (await Pvp.LivingMuse()) return true;
            }

            if (await Pvp.CreatureMotif()) return true;

            if (!CommonPvp.GuardCheck(PictomancerSettings.Instance, checkGuard: false))
            {
                if (await Pvp.PaintRGB()) return true;
            }

            return false;
        }
    }
}

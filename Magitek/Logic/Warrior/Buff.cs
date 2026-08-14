using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Warrior;
using Magitek.Utilities;
using Magitek.Utilities.Managers;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using WarriorRoutine = Magitek.Utilities.Routines.Warrior;

namespace Magitek.Logic.Warrior
{
    internal static class Buff
    {
        //Beast Gauge cost of Inner Beast / Fell Cleave
        private const int InnerBeastBeastGaugeCost = 50;

        public static async Task<bool> Defiance()
        {
            if (WarriorSettings.Instance.ManuallyControlTankStance)
                return false;

            if (!WarriorSettings.Instance.UseDefiance)
            {
                if (Core.Me.HasAura(Auras.Defiance))
                {
                    return await Spells.Defiance.Cast(Core.Me);
                }

                return false;
            }

            if (Core.Me.HasAura(Auras.Defiance))
                return false;

            return await Spells.Defiance.Cast(Core.Me);
        }

        //Berserk Becomes Inner Release
        public static async Task<bool> InnerRelease()
        {
            if (!WarriorSettings.Instance.UseInnerRelease)
                return false;

            if (Combat.Enemies.Count(r => r.WithinSpellRange(Spells.HeavySwing.Range)) < 1)
                return false;

            //Surging Tempest only comes from Storm's Eye (50) or the Mythril Tempest combo (40), so only
            //demand it when the rotation we're actually running can apply it. Below Storm's Eye the single
            //target combo never grants it, which left Berserk unused from 40 to 49 outside of AoE.
            if (Spells.StormsEye.IsKnown() || RunningMythrilTempestCombo())
            {
                if (!Core.Me.HasAura(Auras.SurgingTempest, true, 12000))
                    return false;
            }
            else if (!BerserkStacksAreWorthSpending())
            {
                return false;
            }

            if (Core.Me.HasAura(Auras.NascentChaos))
                return false;

            return await WarriorRoutine.InnerRelease.Cast(Core.Me);
        }

        //Mirrors Aoe.MythrilTempest: is the Overpower combo the rotation we're running? That combo is the
        //only source of Surging Tempest before Storm's Eye unlocks.
        private static bool RunningMythrilTempestCombo()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!WarriorSettings.Instance.UseAoe)
                return false;

            if (!Spells.MythrilTempest.IsKnown())
                return false;

            return Combat.Enemies.Count(r => r.WithinSpellRange(Spells.MythrilTempest.Radius)) >= WarriorSettings.Instance.MythrilTempestMinimumEnemies;
        }

        //Without Surging Tempest to protect, Berserk is only worth timing around its three guaranteed crit
        //direct hits. Any three consecutive combo GCDs come out to the same potency, so the only placement
        //that gains anything is one where a Beast Gauge spender displaces a Heavy Swing inside the window.
        private static bool BerserkStacksAreWorthSpending()
        {
            //With no Beast Gauge spender the combo is a flat three GCD cycle, so any three consecutive
            //weaponskills are worth the same and holding Berserk only delays the next use.
            if (!WarriorSettings.Instance.UseFellCleave || !WarriorRoutine.FellCleave.IsKnown())
                return true;

            //Enough gauge for Inner Beast to eat one of the stacks.
            if (ActionResourceManager.Warrior.BeastGauge >= InnerBeastBeastGaugeCost)
                return true;

            //Combo isn't running, so we're at the head of a fresh combo and the window covers all of it.
            if (ActionManager.ComboTimeLeft <= 0)
                return true;

            //Storm's Path is the next GCD, and the 20 gauge it grants can still buy an Inner Beast in time.
            return WarriorRoutine.CanContinueComboAfter(Spells.Maim);
        }

        public static async Task<bool> Infuriate()
        {
            if (!WarriorSettings.Instance.UseInfuriate)
                return false;

            if (Casting.LastSpell == Spells.InnerRelease)
                return false;

            if (Core.Me.HasAura(Auras.InnerRelease))
                return false;

            if (ActionResourceManager.Warrior.BeastGauge >= WarriorSettings.Instance.UseInfuriateAtBeastGauge)
                return false;

            return await Spells.Infuriate.Cast(Core.Me);
        }

        public static async Task<bool> NascentFlash()
        {
            if (!WarriorSettings.Instance.UseNascentFlash)
                return false;

            if (!Globals.InParty)
                return false;

            if (!Spells.NascentFlash.IsReady())
                return false;


            var canNascentTargets = Group.CastableAlliesWithin30.Where(CanNascentFlash);

            if (!BaseSettings.Instance.UseWeightedHealingPriority)
                canNascentTargets = canNascentTargets.OrderByDescending(DispelManager.GetWeight).ThenBy(c => c.CurrentHealthPercent);

            var nascentTarget = canNascentTargets.FirstOrDefault();

            if (nascentTarget == null)
                return false;

            return await Spells.NascentFlash.Cast(nascentTarget);

            bool CanNascentFlash(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.IsMe)
                    return false;

                if (unit.HasAura(Auras.NascentGlint))
                    return false;

                if (unit.CurrentHealthPercent > WarriorSettings.Instance.NascentFlashHealthPercent)
                    return false;

                if (WarriorSettings.Instance.NascentFlashTank && unit.IsTank())
                    return true;

                if (WarriorSettings.Instance.NascentFlashHealer && unit.IsHealer())
                    return true;

                if (WarriorSettings.Instance.NascentFlashDps && unit.IsDps())
                    return true;

                return false;
            }
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.InnerRelease.IsKnown() && !Spells.InnerRelease.IsReady(3000))
                return false;

            return await Tank.UsePotion(WarriorSettings.Instance);
        }
    }
}
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Monk;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Monk;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using System.Threading.Tasks;
using MonkRoutine = Magitek.Utilities.Routines.Monk;

namespace Magitek.Rotations
{
    public static class Monk
    {
        public static Task<bool> Rest()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (await Buff.Meditate()) return true;

            return false;
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            if (await Buff.Mantra()) return true;

            return false;
        }

        public static async Task<bool> CombatBuff()
        {
            return await Buff.Meditate();
        }

        public static async Task<bool> Combat()
        {
            MonkRoutine.RefreshVars();

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
            {
                if (await Buff.Meditate())
                    return true;
                return false;
            }

            //Limit Break
            if (SingleTarget.ForceLimitBreak())
                return true;

            if (await CommonFightLogic.FightLogic_PartyShield(MonkSettings.Instance.FightLogicMantra, Spells.Mantra, true, aura: Auras.Mantra)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(MonkSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(MonkSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, Auras.ArmsLength)) return true;

            //Buff
            if (await Buff.Meditate())
                return true;

            if (MonkRoutine.GlobalCooldown.CountOGCDs() < 2 && Spells.Bootshine.Cooldown.TotalMilliseconds > 750 + BaseSettings.Instance.UserLatencyOffset)
            {
                if (await PhysicalDps.Interrupt(MonkSettings.Instance)) return true;
                if (await PhysicalDps.SecondWind(MonkSettings.Instance)) return true;
                if (await PhysicalDps.Bloodbath(MonkSettings.Instance)) return true;
                if (await PhysicalDps.Feint(MonkSettings.Instance)) return true;
                if (await Buff.UsePotion()) return true;
                if (await Buff.TrueNorth()) return true;

                if (await Buff.EarthReply()) return true;
                if (await Buff.RiddleOfFire()) return true;
                if (await Buff.Brotherhood()) return true;
                if (await Buff.RiddleOfWind()) return true;

                if (await Aoe.Enlightenment()) return true;
                if (await SingleTarget.TheForbiddenChakra()) return true;
                if (await Buff.PerfectBalance()) return true;
                if (await Buff.Mantra()) return true;
            }

            if (await Aoe.MasterfulBlitz()) return true;
            if (await Aoe.FireReply()) return true;
            if (await Aoe.WindReply()) return true;
            if (await Aoe.PerfectBalance()) return true;
            if (await Aoe.Rockbreaker()) return true;
            if (await Aoe.FourPointStrike()) return true;
            if (await Aoe.ArmOfDestroyer()) return true;

            if (await SingleTarget.PerfectBalance()) return true;
            if (await SingleTarget.DragonKick()) return true;
            if (await SingleTarget.Bootshine()) return true;
            if (await SingleTarget.TwinSnakes()) return true;
            if (await SingleTarget.Demolish()) return true;
            if (await SingleTarget.TrueStrike()) return true;
            if (await SingleTarget.SnapPunch()) return true;

            return await Buff.FormShiftIC();
        }

        public static void RegisterCombatMessages()
        {

            //Highest priority: Don't show anything if we're not in combat
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(100,
                                          "",
                                          () => !Core.Me.InCombat || !Core.Me.HasTarget));

            //Second priority: Don't show anything if positional requirements are Nulled
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(200,
                                          "",
                                          () => MonkSettings.Instance.HidePositionalMessage && Core.Me.HasAura(Auras.TrueNorth) || MonkSettings.Instance.EnemyIsOmni));

            //third priority (tie): Demolish
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Demolish: Get behind Enemy", "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury == 0 && !Core.Me.HasAura(Auras.PerfectBalance) && MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies));

            //fourth priority (tie): Snap punch
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(400,
                                          "Pouncing Coeurl: Side of Enemy", "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => Core.Me.ClassLevel >= Spells.PouncingCoeurl.LevelAcquired && Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury >= 0 && !Core.Me.HasAura(Auras.PerfectBalance) && MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies));

            //fourth priority (tie): Snap punch
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(400,
                                          "Snap punch: Side of Enemy", "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => Core.Me.HasAura(Auras.CoeurlForm) && ActionResourceManager.Monk.CoeurlFury >= 1 && !Core.Me.HasAura(Auras.PerfectBalance) && MonkRoutine.AoeEnemies5Yards < MonkSettings.Instance.AoeEnemies));
        }

        public static async Task<bool> PvP()
        {
            MonkRoutine.RefreshVars();

            if (await CommonPvp.CommonTasks(MonkSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except basic combo
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.MeteodrivePvp()) return true;
                if (await Pvp.EarthsReplyPvp()) return true;
                if (await Pvp.RiddleofEarthPvp()) return true;

                if (!CommonPvp.GuardCheck(MonkSettings.Instance))
                {
                    if (await Pvp.RisingPhoenixPvp()) return true;
                    if (await Pvp.FlintsReplyPvp()) return true;

                    if (await Pvp.WindsReplyPvp()) return true;
                    if (await Pvp.ThunderclapPvp()) return true;
                }

                // if (await Pvp.EnlightenmentPvp()) return true;
            }

            // Basic Combo (ungated) - reverse order for priority
            if (await Pvp.PhantomRushPvp()) return true;
            if (await Pvp.PouncingCoeurlPvp()) return true;
            if (await Pvp.RisingRaptorPvp()) return true;
            if (await Pvp.LeapingOpoPvp()) return true;
            if (await Pvp.DemolishPvp()) return true;
            if (await Pvp.TwinSnakesPvp()) return true;
            return await Pvp.DragonKickPvp();
        }
    }
}

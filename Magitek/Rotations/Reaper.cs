using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Reaper;
using Magitek.Logic.Roles;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using System.Threading.Tasks;
using Enshroud = Magitek.Logic.Reaper.Enshroud;
using ReaperRoutine = Magitek.Utilities.Routines.Reaper;

namespace Magitek.Rotations
{
    public static class Reaper
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < ReaperSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (await Utility.Soulsow())
                return true;

            return false;
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
            ReaperRoutine.RefreshVars();

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
            {
                if (await Utility.Soulsow()) return true;
                return false;
            }

            if (await CommonFightLogic.FightLogic_SelfShield(ReaperSettings.Instance.FightLogicArcaneCrest, Spells.ArcaneCrest, false, castTimeRemainingMs: 3000)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(ReaperSettings.Instance.FightLogicFeint, Spells.Feint, true, Auras.Feint)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(ReaperSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            if (SingleTarget.ForceLimitBreak()) return true;

            if (Core.Me.HasAura(Auras.Enshrouded)) //Enshroud Mode
            {
                if (ReaperRoutine.GlobalCooldown.CanWeave(1))
                {
                    if (await Enshroud.AoE.LemuresScythe()) return true;
                    if (await Enshroud.SingleTarget.LemuresSlice()) return true;
                }

                if (await Enshroud.AoE.Sacrificium()) return true;
                if (await Enshroud.AoE.Communio()) return true;
                if (await Enshroud.AoE.GrimReaping()) return true;
                if (await Enshroud.SingleTarget.VoidReaping()) return true;
                if (await Enshroud.SingleTarget.CrossReaping()) return true;
                if (await Enshroud.AoE.LemuresScytheOffWeave()) return true;
                if (await Enshroud.SingleTarget.LemuresSliceOfFWeave()) return true;
            }
            else
            {
                if (ReaperRoutine.GlobalCooldown.CanWeave())
                {
                    if (await Utility.UsePotion()) return true;
                    if (await PhysicalDps.Interrupt(ReaperSettings.Instance)) return true;
                    if (await PhysicalDps.SecondWind(ReaperSettings.Instance)) return true;
                    if (await PhysicalDps.Bloodbath(ReaperSettings.Instance)) return true;

                    if (await Cooldown.ArcaneCircle()) return true;
                    if (await Cooldown.Enshroud()) return true;
                    if (await Cooldown.Gluttony()) return true;
                    if (await AoE.GrimSwathe()) return true;
                    if (await SingleTarget.BloodStalk()) return true;
                    if (await Utility.TrueNorth()) return true;
                }

                if (await SingleTarget.HarvestMoon()) return true;
                if (await SingleTarget.EnhancedHarpe()) return true;
                if (await Utility.Soulsow()) return true;

                if (await AoE.Perfectio()) return true;
                if (await AoE.WhorlofDeath()) return true;
                if (await SingleTarget.ShadowOfDeath()) return true;
                if (await AoE.HarvestMoon()) return true;
                if (await AoE.PlentifulHarvest()) return true;
                if (await AoE.Guillotine()) return true;
                if (await SingleTarget.GibbetAndGallows()) return true;
                if (await AoE.SoulScythe()) return true;
                if (await SingleTarget.SoulSlice()) return true;

                if (await AoE.NightmareScythe()) return true;
                if (await SingleTarget.InfernalSlice()) return true;
                if (await SingleTarget.WaxingSlice()) return true;
                if (await AoE.WhorlofDeathIdle()) return true;
                if (await AoE.SpinningScythe()) return true;
                if (await SingleTarget.ShadowOfDeathIdle()) return true;
                if (await SingleTarget.Slice()) return true;
                return await SingleTarget.Harpe();
            }

            return false;
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(ReaperSettings.Instance)) return true;

            // Defensive abilities
            if (await Pvp.ArcaneCrestPvp()) return true;

            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.PerfectioPvp()) return true;
                if (await Pvp.GuillotinePvp()) return true;
            }

            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(ReaperSettings.Instance))
            {
                // Enshrouded abilities
                if (await Pvp.TenebraeLemurumPvp()) return true;
                if (await Pvp.LemureSlicePvp()) return true;
                if (await Pvp.CrossReapingePvp()) return true;
                if (await Pvp.VoidReapingPvp()) return true;
                if (await Pvp.CommunioPvp()) return true;

                // Soul Gauge abilities
                if (await Pvp.PlentifulHarvestPvp()) return true;
                if (await Pvp.HarvestMoonPvp()) return true;

                // Debuffs and utility
                if (await Pvp.DeathWarrantPvp()) return true;
                if (await Pvp.GrimSwathePvp()) return true;
                if (await Pvp.SoulSlicePvp()) return true;
            }

            // Basic combo (ungated)
            if (await Pvp.InfernalSlicePvp()) return true;
            if (await Pvp.WaxingSlicePvp()) return true;
            return await Pvp.SlicePvp();
        }

        public static void RegisterCombatMessages()
        {
            //Highest priority: Don't show anything if we're not in combat
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(100,
                                          "",
                                          () => !Core.Me.InCombat || !Core.Me.HasTarget)
                );

            //Second priority: Don't show anything if positional requirements are Nulled
            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(200,
                                          "",
                                          () => ReaperSettings.Instance.HidePositionalMessage || Core.Me.HasAura(Auras.TrueNorth) || ReaperSettings.Instance.EnemyIsOmni || Core.Me.HasAura(Auras.Enshrouded))
                );

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Gibbet => SIDE !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => ReaperSettings.Instance.UseGibbet && Core.Me.HasAura(Auras.SoulReaver) && !Core.Me.HasAura(Auras.EnhancedGibbet) && !Core.Me.HasAura(Auras.EnhancedGallows) && Spells.TrueNorth.Charges < 1 && !Core.Me.CurrentTarget.IsFlanking)
                );

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(300,
                                          "Gallows => BEHIND !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => ReaperSettings.Instance.UseGallows && Core.Me.HasAura(Auras.SoulReaver) && !Core.Me.HasAura(Auras.EnhancedGibbet) && !Core.Me.HasAura(Auras.EnhancedGallows) && Spells.TrueNorth.Charges < 1 && !Core.Me.CurrentTarget.IsBehind)
                );

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(310,
                                          "Gibbet => SIDE !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowSidesHighlighted.png",
                                          () => ReaperSettings.Instance.UseGibbet && Core.Me.HasAura(Auras.EnhancedGibbet) && !Core.Me.HasAura(Auras.EnhancedGallows)
                                           && (Core.Me.HasAura(Auras.SoulReaver) || ActionResourceManager.Reaper.SoulGauge >= 40) && Spells.TrueNorth.Charges < 1 && !Core.Me.CurrentTarget.IsFlanking)
                );

            CombatMessageManager.RegisterMessageStrategy(
                new CombatMessageStrategy(310,
                                          "Gallows => BEHIND !!!",
                                          "/Magitek;component/Resources/Images/General/ArrowDownHighlighted.png",
                                          () => ReaperSettings.Instance.UseGallows && Core.Me.HasAura(Auras.EnhancedGallows) && !Core.Me.HasAura(Auras.EnhancedGibbet)
                                           && (Core.Me.HasAura(Auras.SoulReaver) || ActionResourceManager.Reaper.SoulGauge >= 40) && Spells.TrueNorth.Charges < 1 && !Core.Me.CurrentTarget.IsBehind)
                );
        }
    }
}



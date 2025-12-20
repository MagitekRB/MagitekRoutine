using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Roles;
using Magitek.Toggles;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    public class Tank
    {
        public static async Task<bool> Provoke<T>(T settings) where T : TankSettings
        {
            if (Globals.OnPvpMap) return false;
            if (!settings.UseProvoke) return false;
            if (!Globals.InParty) return false;
            if (Spells.Provoke.Cooldown > TimeSpan.Zero) return false;

            var provokeTarget = Combat.Enemies.FirstOrDefault(r => r.WithinSpellRange(Spells.Provoke.Range) && r.TargetGameObject.IsHealer());

            if (provokeTarget != null)
            {
                if (await Spells.Provoke.Cast(provokeTarget))
                {
                    if (provokeTarget.Distance(Core.Me) > 5)
                    {
                        return (await Spells.ShieldLob.Cast(provokeTarget));
                    }
                    Logger.Write($@"Provoke On {provokeTarget.Name} To Pull Aggro");
                    return true;
                }
            }

            provokeTarget = Combat.Enemies.FirstOrDefault(r => r.WithinSpellRange(Spells.Provoke.Range) && r.TargetGameObject.IsDps());

            if (provokeTarget == null)
                return false;

            if (!await Spells.Provoke.Cast(provokeTarget))
                return false;

            Logger.Write($@"Provoke On {provokeTarget.Name} To Pull Aggro");
            return true;
        }

        public static async Task<bool> Rampart<T>(T settings) where T : TankSettings
        {
            return await BasicHpSetting(settings.UseRampart, settings.RampartHpPercentage, Spells.Rampart, Core.Me, Auras.Rampart);
        }

        private static async Task<bool> BasicHpSetting(bool useSetting, int hpPercentage, SpellData spell, GameObject target, uint aura)
        {
            if (!useSetting)
                return false;

            if (Core.Me.CurrentHealthPercent > hpPercentage)
                return false;

            return await spell.CastAura(target, aura);
        }

        public static async Task<bool> ArmsLength<T>(T settings) where T : TankSettings
        {
            if (!settings.UseArmsLength)
                return false;

            if (Core.Me.CurrentHealthPercent > settings.ArmsLengthPercentage)
                return false;

            if (!(Combat.Enemies.Count(r => r.TargetGameObject == Core.Me) >= settings.ArmsLengthEnemies))
                return false;

            return await Spells.ArmsLength.Cast(Core.Me);
        }

        //If the calling class has stuns or interrupts beyond the default tank abilities (e.g.,
        //Paladin has Shield Bash in addition to Low Blow and Interject), pass them in the
        //extraStuns and extraInterrupts parameters
        public static async Task<bool> Interrupt(TankSettings settings, IEnumerable<SpellData> extraStuns = null, IEnumerable<SpellData> extraInterrupts = null)
        {
            if (!settings.UseStunOrInterrupt)
                return false;

            List<SpellData> stuns = new List<SpellData>() { Spells.LowBlow };
            List<SpellData> interrupts = new List<SpellData>() { Spells.Interject };

            if (extraStuns != null)
            {
                stuns.AddRange(extraStuns);
            }

            if (extraInterrupts != null)
            {
                interrupts.AddRange(extraInterrupts);
            }

            return await InterruptAndStunLogic.StunOrInterrupt(stuns, interrupts, settings.Strategy);
        }

        public static bool ForceLimitBreak(SpellData limitBreak1Spell, SpellData limitBreak2Spell, SpellData limitBreak3Spell, SpellData gcd)
        {
            if (!BaseSettings.Instance.ForceLimitBreak)
                return false;

            //LB 3
            if (PartyManager.NumMembers == 8
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak3Spell)
                && gcd.Cooldown.TotalMilliseconds < 500)
            {
                ActionManager.DoAction(limitBreak3Spell, Core.Me);
                BaseSettings.Instance.ForceLimitBreak = false;
                TogglesManager.ResetToggles();
                return true;
            }

            //LB 2 or LB 1
            if (PartyManager.NumMembers == 4
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak1Spell)
                && !Casting.SpellCastHistory.Any(s => s.Spell == limitBreak2Spell)
                && gcd.Cooldown.TotalMilliseconds < 500)
            {
                if (!ActionManager.DoAction(limitBreak2Spell, Core.Me))
                    ActionManager.DoAction(limitBreak1Spell, Core.Me);

                BaseSettings.Instance.ForceLimitBreak = false;
                TogglesManager.ResetToggles();
                return true;
            }
            return false;
        }

        public static async Task<bool> UsePotion<T>(T settings) where T : TankSettings
        {
            if (!settings.UsePotion)
                return false;

            if (Core.Me.HasAura(Auras.Medicated, true))
                return false;

            return await Potion.UsePotion((int)settings.PotionTypeAndGradeLevel);
        }


        /****************************************************************************************************
         *                                                 PVP
         * **************************************************************************************************/
        public static async Task<bool> Recuperate<T>(T settings) where T : TankSettings
        {
            if (!settings.Pvp_UseRecuperate)
                return false;

            if (!Spells.Recuperate.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.CurrentHealthPercent > settings.Pvp_RecuperateHealthPercent)
                return false;

            return await Spells.Recuperate.Cast(Core.Me);
        }

        public static async Task<bool> Purify<T>(T settings) where T : TankSettings
        {
            if (!settings.Pvp_UsePurify)
                return false;

            if (!Spells.Purify.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura("Stun") && !Core.Me.HasAura("Heavy") && !Core.Me.HasAura("Bind") && !Core.Me.HasAura("Silence") && !Core.Me.HasAura("Half-asleep") && !Core.Me.HasAura("Sleep") && !Core.Me.HasAura("Deep Freeze"))
                return false;

            return await Spells.Purify.Cast(Core.Me);
        }

        public static async Task<bool> Guard<T>(T settings) where T : TankSettings
        {
            if (!settings.Pvp_UseGuard)
                return false;

            if (!Spells.Guard.CanCast())
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.IsAlive)
                return false;

            if (Core.Me.CurrentHealthPercent > settings.Pvp_GuardHealthPercent)
                return false;

            if (!await Spells.Guard.CastAura(Core.Me, Auras.PvpGuard))
                return false;

            return await Coroutine.Wait(1500, () => Core.Me.HasAura(Auras.PvpGuard, true));
        }

        public static bool GuardCheck()
        {
            return Core.Me.CurrentTarget.HasAura(Auras.PvpGuard);
        }

        public static async Task<bool> Dash(
            SpellData dashSpell,
            bool useDash,
            bool useForMobility,
            bool useForDps,
            bool onlyDuringBurst,
            int saveCharges,
            Func<bool> hasBurstAuras,
            Func<bool> canWeave,
            double meleeRange)
        {
            if (!useDash)
                return false;

            if (!dashSpell.IsKnown())
                return false;

            if (Casting.LastSpell == dashSpell)
                return false;

            if ((uint)Math.Floor(dashSpell.Charges) <= saveCharges)
                return false;

            if (!Core.Me.HasTarget || Core.Me.CurrentTarget == null)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(dashSpell.Range))
                return false;

            bool inMeleeRange = Core.Me.CurrentTarget.WithinSpellRange(meleeRange);

            if (inMeleeRange)
            {
                if (!useForDps)
                    return false;

                if (onlyDuringBurst)
                {
                    if (hasBurstAuras == null || !hasBurstAuras())
                        return false;
                    if (!canWeave())
                        return false;
                }
            }
            else
            {
                if (!useForMobility)
                    return false;
            }

            return await dashSpell.Cast(Core.Me.CurrentTarget);
        }
    }
}
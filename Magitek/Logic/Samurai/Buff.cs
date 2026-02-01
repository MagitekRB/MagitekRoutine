using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using SamuraiRoutine = Magitek.Utilities.Routines.Samurai;

namespace Magitek.Logic.Samurai
{
    internal static class Buff
    {
        public static async Task<bool> MeikyoShisuiNotInCombat()
        {
            if (!SamuraiSettings.Instance.UseMeikyoShisui)
                return false;

            if (Spells.MeikyoShisui.Charges < Spells.MeikyoShisui.MaxCharges)
                return false;

            if (Spells.Ikishoten.IsKnown() && !Spells.Ikishoten.IsReady())
                return false;

            if (Spells.HissatsuSenei.IsKnown() && !Spells.HissatsuSenei.IsReady())
                return false;

            if (Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            if (Core.Me.HasAura(Auras.Shifu, true))
                return false;

            if (Core.Me.HasAura(Auras.Jinpu, true))
                return false;

            if (SamuraiRoutine.SenCount > 0)
                return false;

            if (ActionResourceManager.Samurai.Kenki > 0)
                return false;

            if (ActionResourceManager.Samurai.Meditation > 0)
                return false;

            return await Spells.MeikyoShisui.CastAura(Core.Me, Auras.MeikyoShisui);
        }

        public static async Task<bool> MeikyoShisui()
        {
            if (!SamuraiSettings.Instance.UseMeikyoShisui)
                return false;

            if (!Spells.MeikyoShisui.IsKnown() || !Spells.MeikyoShisui.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            // -------------------------------------------------
            // FALLBACK (no Ikishoten or no Kaeshi learned)
            // Default behavior -> cast when ready
            // -------------------------------------------------
            if (!Spells.Ikishoten.IsKnown() || !Spells.KaeshiSetsugekka.IsKnown())
            {
                if (SamuraiSettings.Instance.UseMeikyoShisuiOnlyWithZeroSen && SamuraiRoutine.SenCount != 0)
                    return false;

                return await Spells.MeikyoShisui.CastAura(Core.Me, Auras.MeikyoShisui);
            }

            TimeSpan ikCd = Spells.Ikishoten.Cooldown;
            TimeSpan gcd = Spells.Hakaze.AdjustedCooldown;

            // -------------------------------------------------
            // Burst windows
            // Even: 120..105
            // Odd :  60..45
            // -------------------------------------------------
            bool evenBurst15s = ikCd >= TimeSpan.FromSeconds(105);
            bool oddBurst15s = ikCd <= TimeSpan.FromSeconds(60) && ikCd >= TimeSpan.FromSeconds(45);
            bool inBurst15s = evenBurst15s || oddBurst15s;

            double ch = Spells.MeikyoShisui.Charges;

            // -------------------------------------------------
            // During burst: dump extra charges
            // -------------------------------------------------
            if (inBurst15s && ch > 1.0)
                return await Spells.MeikyoShisui.CastAura(Core.Me, Auras.MeikyoShisui);

            // -------------------------------------------------
            // Determine next burst-window START threshold:
            // - if ikCd > 105 => next burst is entering even window at 105
            // - else if ikCd > 60 => next burst is entering odd window at 60
            // - else: no upcoming burst window start handled here
            // -------------------------------------------------
            TimeSpan evenStart = TimeSpan.FromSeconds(0);
            TimeSpan oddStart = TimeSpan.FromSeconds(60);

            TimeSpan nextStart;
             if (ikCd > oddStart) nextStart = oddStart;
             else if (ikCd > evenStart) nextStart = evenStart;
            else return false;

            TimeSpan timeToBurstStart = ikCd - nextStart;

            // -------------------------------------------------
            // Use Meikyo to help reach 3 Sen by burst start
            // (backward count from entering the burst window base on adjusted gcd)
            // -------------------------------------------------
            int missingSen = Math.Max(0, 3 - SamuraiRoutine.SenCount);
            TimeSpan need = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * missingSen);
            TimeSpan buffer = TimeSpan.FromMilliseconds(770);

            bool shouldPrep = (missingSen > 0) && (timeToBurstStart <= need + buffer);
            if (shouldPrep)
                return await Spells.MeikyoShisui.CastAura(Core.Me, Auras.MeikyoShisui);

            // -------------------------------------------------
            // 2) If already 3 Sen, force Meikyo right before burst start
            // Right before = within 1 GCD before the burst
            // -------------------------------------------------
            bool rightBeforeBurst = (ikCd > nextStart) && (ikCd <= nextStart + gcd);

            if (rightBeforeBurst)
                return await Spells.MeikyoShisui.CastAura(Core.Me, Auras.MeikyoShisui);

            return false;
        }


        public static async Task<bool> ThirdEye()
        {
            return await Spells.ThirdEye.Cast(Core.Me);
        }


        public static async Task<bool> Ikishoten()
        {
            if(!Spells.Ikishoten.IsKnown())
                return false;

            if (!SamuraiSettings.Instance.UseIkishoten || SamuraiSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (Combat.CombatTime.ElapsedMilliseconds < (Spells.Hakaze.AdjustedCooldown.TotalMilliseconds * 2) - 770)
                return false;
            
            //Delay cause the whole burst window to be delayed. 
            //if (ActionResourceManager.Samurai.Kenki >= 50)
            //    return false;

            if (!await Spells.Ikishoten.Cast(Core.Me))
                return false;

            //SamuraiRoutine.InitializeFillerVar(false, false); // Remove Filler after Even Minutes Burst

            return true;
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.HissatsuSenei.IsKnown() && !Spells.HissatsuSenei.IsReady(4000))
                return false;

            if (SamuraiRoutine.SenCount == 0)
                return false;

            return await PhysicalDps.UsePotion(SamuraiSettings.Instance);
        }
    }
}
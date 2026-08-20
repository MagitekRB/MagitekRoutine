using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Dragoon;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using DragoonRoutine = Magitek.Utilities.Routines.Dragoon;

namespace Magitek.Logic.Dragoon
{
    internal static class Buff
    {
        public static async Task<bool> LanceCharge() //Damage +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseLanceCharge)
                return false;

            if (Core.Me.HasAura(Auras.LanceCharge))
                return false;

            //Exec LanceCharge after Disembowel, SpiralBlow or RaidenThrust if only 1 enemy
            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 10 + x.CombatReach) == 1)
            {
                if (ActionManager.LastSpell.Id != Spells.Disembowel.Id
                    && ActionManager.LastSpell.Id != Spells.SpiralBlow.Id
                    && ActionManager.LastSpell.Id != Spells.RaidenThrust.Id)
                    return false;
            }
            return await Spells.LanceCharge.Cast(Core.Me);
        }

        public static async Task<bool> BattleLitany() // Crit +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseBattleLitany)
                return false;

            if (!Core.Me.HasAura(Auras.LanceCharge))
                return false;

            return await Spells.BattleLitany.Cast(Core.Me);
        }

        public static async Task<bool> LifeSurge() // Crit +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseLifeSurge)
                return false;

            if (Casting.LastSpell == Spells.LifeSurge || Core.Me.HasAura(Auras.LifeSurge))
                return false;

            // LanceCharge burst or prevent charge overcapping outside burst
            if (!Core.Me.HasAura(Auras.LanceCharge))
            {
                if (Spells.LanceCharge.IsKnown())
                {
                    // With 2 charges (lv88+), spend a charge if nearing overcap or LanceCharge is far off
                    if (Spells.LifeSurge.MaxCharges >= 2)
                    {
                        // Use the user-configured threshold to ensure we don't overcap charges before the next Lance Charge window
                        if (Spells.LifeSurge.Charges < DragoonSettings.Instance.LifeSurgeChargeThreshold && Spells.LanceCharge.Cooldown.TotalMilliseconds <= 15000)
                            return false;
                    }
                    else
                    {
                        // With 1 charge, save it for LanceCharge
                        return false;
                    }
                }
            }

            // Prioritize highest-potency weaponskills: Heavens' Thrust / Full Thrust > Drakesbane > Fang & Claw / AoE
            if (!Spells.FullThrust.IsKnown())
            {
                return await Spells.LifeSurge.Cast(Core.Me);
            }

            // Single Target: Heavens' Thrust / Full Thrust (after Vorpal Thrust or Lance Barrage)
            if (ActionManager.LastSpell == Spells.VorpalThrust || ActionManager.LastSpell == Spells.LanceBarrage)
                return await Spells.LifeSurge.Cast(Core.Me);

            // Single Target: Drakesbane (after Wheeling Thrust or Fang and Claw)
            if (Spells.Drakesbane.IsKnown() && (ActionManager.LastSpell == Spells.WheelingThrust || ActionManager.LastSpell == Spells.FangAndClaw))
                return await Spells.LifeSurge.Cast(Core.Me);

            // Low level (58-63): Fang and Claw after Heavens' Thrust
            if (!Spells.Drakesbane.IsKnown() && ActionManager.LastSpell == DragoonRoutine.HeavensThrust)
                return await Spells.LifeSurge.Cast(Core.Me);

            // AoE: Coerthan Torment (after Sonic Thrust)
            if (Spells.CoerthanTorment.IsKnown() && ActionManager.LastSpell == Spells.SonicThrust)
                return await Spells.LifeSurge.Cast(Core.Me);

            // AoE low level: Sonic Thrust (after Doom Spike)
            if (!Spells.CoerthanTorment.IsKnown() && ActionManager.LastSpell == Spells.DoomSpike)
                return await Spells.LifeSurge.Cast(Core.Me);

            // AoE: Draconian Fury
            if (Core.Me.HasAura(Auras.DraconianFire, true)
                && AoeControl.Enabled
                && DragoonSettings.Instance.UseAoe
                && Combat.Enemies.Count(x => x.WithinSpellRange(10)) >= DragoonSettings.Instance.AoeEnemies)
            {
                return await Spells.LifeSurge.Cast(Core.Me);
            }

            // Fallback: If no conditions are met, do not cast Life Surge
            return false;
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.BattleLitany.IsKnown() && !Spells.BattleLitany.IsReady(5000))
                return false;

            return await PhysicalDps.UsePotion(DragoonSettings.Instance);
        }

    }

}

using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Logic.Machinist
{
    internal static class Cooldowns
    {
        public static async Task<bool> BarrelStabilizer()
        {
            if (!MachinistSettings.Instance.UseBarrelStabilizer)
                return false;

            if (!Spells.BarrelStabilizer.IsReady())
                return false;

         //   if (ActionResourceManager.Machinist.Heat > 50)
         //       return false;

            if (Spells.Reassemble.IsKnown() && Spells.Reassemble.Charges > 1)
                return false;

            if (Spells.Wildfire.Cooldown.TotalMilliseconds > 0 && Spells.Wildfire.Cooldown.TotalMilliseconds <= 6000)
                return false;

            if (Utilities.Routines.Common.CheckTTDIsEnemyDyingSoon(MachinistSettings.Instance))
                return false;

            return await Spells.BarrelStabilizer.Cast(Core.Me);
        }

        public static async Task<bool> Hypercharge()
        {
            if (Core.Me.ClassLevel < Spells.Hypercharge.LevelAcquired)
                return false;

            if (!MachinistSettings.Instance.UseHypercharge)
                return false;

            if (!Spells.Hypercharge.CanCast())
                return false;

            if (ActionResourceManager.Machinist.Heat < 50 && !Core.Me.HasAura(Auras.Hypercharged, true))
                return false;

            if (ActionResourceManager.Machinist.OverheatRemaining > TimeSpan.Zero)
                return false;

            // Force cast if barrel stabilizer is active and about to expire
            if (Core.Me.HasAura(Auras.Hypercharged, true, 3000))
                return await Spells.Hypercharge.Cast(Core.Me);

            if (Spells.Wildfire.IsKnownAndReady())
                return false;

            // Force cast during wildfire
            if (Spells.Wildfire.IsKnown() && (Casting.LastSpell == Spells.Wildfire || Core.Me.HasAura(Auras.WildfireBuff, true)))
                return await Spells.Hypercharge.Cast(Core.Me);

            if (MachinistSettings.Instance.DelayHypercharge)
            {
                if (Spells.Drill.IsKnown() && Spells.Drill.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayHyperchargeSeconds)
                    return false;

                if (Spells.AirAnchor.IsKnown() && Spells.AirAnchor.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayHyperchargeSeconds)
                    return false;

                if (Spells.ChainSaw.IsKnown() && Spells.ChainSaw.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayHyperchargeSeconds)
                    return false;
            }

            return await Spells.Hypercharge.Cast(Core.Me);
        }

        public static async Task<bool> Wildfire()
        {
            if (!MachinistSettings.Instance.UseWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff, true) || Casting.SpellCastHistory.Any(x => x.Spell == Spells.Wildfire))
                return false;

            if (!Core.Me.HasAura(Auras.Hypercharged, true) && ActionResourceManager.Machinist.Heat < 50 && ActionResourceManager.Machinist.OverheatRemaining == TimeSpan.Zero)
                return false;

            if (ActionResourceManager.Machinist.OverheatRemaining > TimeSpan.Zero)
                return false;

            if (MachinistSettings.Instance.DelayWildfire) { 
                if (Spells.Drill.IsKnown() && Spells.Drill.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayWildfireSeconds)
                    return false;

                if (Spells.AirAnchor.IsKnown() && Spells.AirAnchor.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayWildfireSeconds)
                    return false;

                if (Spells.ChainSaw.IsKnown() && Spells.ChainSaw.Cooldown.TotalSeconds <= MachinistSettings.Instance.DelayWildfireSeconds)
                    return false;
            }

            if (Utilities.Routines.Common.CheckTTDIsEnemyDyingSoon(MachinistSettings.Instance))
                return false;

            return await Spells.Wildfire.CastAura(Core.Me.CurrentTarget, Auras.WildfireBuff, auraTarget: Core.Me);
        }

        public static async Task<bool> Reassemble()
        {
            if (!MachinistSettings.Instance.UseReassemble)
                return false;

            if (Spells.Reassemble.Charges < 1)
                return false;

            if (Core.Me.HasAura(Auras.Reassembled))
                return false;

            if (ActionResourceManager.Machinist.OverheatRemaining > TimeSpan.Zero)
                return false;

            // Added check for cooldown, gets stuck at lower levels otherwise.
            if (Spells.Reassemble.Charges == 0 && !Spells.Reassemble.IsReady())
                return false;

            if (Core.Me.ClassLevel < 58)
            {
                if (ActionManager.LastSpell == MachinistRoutine.HeatedSlugShot)
                    return false;
            }

            if (Core.Me.ClassLevel >= 58 && Core.Me.ClassLevel < 76)
            {
                if (MachinistSettings.Instance.UseDrill && !Spells.Drill.IsKnownAndReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100))
                    return false;
            }

            if (Core.Me.ClassLevel >= 76 && Core.Me.ClassLevel < 90)
            {
                if ((MachinistSettings.Instance.UseDrill && !Spells.Drill.IsKnownAndReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100))
                    && (MachinistSettings.Instance.UseHotAirAnchor && !Spells.AirAnchor.IsKnownAndReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100)))
                    return false;
            }

            if (Core.Me.ClassLevel >= 90)
            {
                if (Spells.Reassemble.Charges >= 1)
                {
                    if (MachinistSettings.Instance.UseDrill && MachinistSettings.Instance.UseReassembleOnDrill && Spells.Drill.IsReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100)
                        || MachinistSettings.Instance.UseHotAirAnchor && MachinistSettings.Instance.UseReassembleOnAA && Spells.AirAnchor.IsReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100)
                        || MachinistSettings.Instance.UseChainSaw && MachinistSettings.Instance.UseReassembleOnChainSaw && Spells.ChainSaw.IsReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100)
                        || MachinistSettings.Instance.UseChainSaw && MachinistSettings.Instance.UseReassembleOnChainSaw && Spells.Excavator.IsReady((int)MachinistRoutine.HeatedSplitShot.Cooldown.TotalMilliseconds - 100))
                    {
                        return await Spells.Reassemble.CastAura(Core.Me, Auras.Reassembled);
                    }
                    else
                        return false;
                }
                else
                    return false;
            }

            return await Spells.Reassemble.CastAura(Core.Me, Auras.Reassembled);
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.BarrelStabilizer.IsKnown() && !Spells.BarrelStabilizer.IsReady(5000))
                return false;

            return await PhysicalDps.UsePotion(MachinistSettings.Instance);
        }
    }
}
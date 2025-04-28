using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Gunbreaker;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;

namespace Magitek.Logic.Gunbreaker
{
    internal static class Buff
    {
        public static async Task<bool> RoyalGuard() //Tank stance
        {
            switch (GunbreakerSettings.Instance.UseRoyalGuard)
            {
                case true:
                    if (!Core.Me.HasAura(Auras.RoyalGuard))
                        return await Spells.RoyalGuard.CastAura(Core.Me, Auras.RoyalGuard);
                    break;

                case false:
                    if (Core.Me.HasAura(Auras.RoyalGuard))
                        return await Spells.RoyalGuard.Cast(Core.Me);
                    break;
            }
            return false;
        }

        public static async Task<bool> NoMercy() // Damage Buff +20%
        {
            if (!GunbreakerSettings.Instance.UseNoMercy)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldNoMercy && !Spells.GnashingFang.IsKnownAndReady(3000) && !Spells.DoubleDown.IsKnownAndReady(5000))
                return false;

            //Force Delay when pulling
            if (Casting.LastSpell == Spells.LightningShot)
                return false;

            if (GunbreakerSettings.Instance.GunbreakerStrategy.Equals(GunbreakerStrategy.SlowGCD) && ((Cartridge == 3) || Casting.LastSpell == Spells.Bloodfest) && Spells.NoMercy.IsKnownAndReady(1000))
            {
                if(Combat.Enemies.Count(r => r.Distance(Core.Me) <= 5 + r.CombatReach) < GunbreakerSettings.Instance.UseAoeEnemies)
                {
                    if (!await UseFatedCircle())
                        return false;
                }
                else
                {
                    if (!await UseBurstStrike())
                        return false;
                }

                return await Spells.NoMercy.Cast(Core.Me);

            }

            if (Casting.LastSpell == Spells.Bloodfest)
                return false;

            if (Cartridge < 2)
                return false;

            return await Spells.NoMercy.Cast(Core.Me);
        }

        public static async Task<bool> Bloodfest() // +2 or +3 cartrige
        {
            if (!GunbreakerSettings.Instance.UseBloodfest)
                return false;

            if (Cartridge > GunbreakerRoutine.MaxCartridge - GunbreakerRoutine.AmountCartridgeFromBloodfest)
                return false;

            if (GunbreakerSettings.Instance.BurstLogicHoldBurst)
                return false;

            return await Spells.Bloodfest.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.NoMercy.IsKnown() && !Spells.NoMercy.IsReady(3000))
                return false;

            return await Tank.UsePotion(GunbreakerSettings.Instance);
        }

        private static async Task<bool> UseBurstStrike()
        {
            if (Core.Me.ClassLevel < Spells.BurstStrike.LevelAcquired)
                return false;

            if (!await Spells.BurstStrike.Cast(Core.Me.CurrentTarget))
                return false;

            return await Coroutine.Wait(1000, Spells.Hypervelocity.CanCast);
        }

        private static async Task<bool> UseFatedCircle()
        {
            if (Core.Me.ClassLevel < Spells.FatedCircle.LevelAcquired)
                return false;

            if (!await Spells.FatedCircle.Cast(Core.Me.CurrentTarget))
                return false;

            return await Coroutine.Wait(1000, Spells.FatedBrand.CanCast);
        }
    }
}
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Summoner;
using Magitek.Toggles;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Summoner
{
    internal static class Heal
    {
        public static async Task<bool> Physick()
        {
            if (!SummonerSettings.Instance.Physick)
                return false;

            if (!Spells.SmnPhysick.IsKnown())
                return false;

            if (Globals.InParty)
                return false;

            if (Core.Me.CurrentHealthPercent > SummonerSettings.Instance.PhysickHPThreshold)
                return false;

            return await Spells.SmnPhysick.Heal(Core.Me);
        }

        public static async Task<bool> ForceRaise()
        {
            if (!SummonerSettings.Instance.ForceResuSwift)
                return false;

            if (!Spells.Resurrection.IsKnown())
                return false;

            if (!ActionManager.HasSpell(Spells.Swiftcast.Id))
                return false;

            if (Spells.Swiftcast.Cooldown != TimeSpan.Zero)
                return false;

            if (!await ForceRaiseLogic()) return false;
            SummonerSettings.Instance.ForceResuSwift = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> ForceHardRaise()
        {
            if (!SummonerSettings.Instance.ForceResu)
                return false;

            if (!Spells.Resurrection.IsKnown())
                return false;

            if (!await HardRaise()) return false;
            SummonerSettings.Instance.ForceResu = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Resurrection()
        {
            if (!Spells.Resurrection.IsKnown())
                return false;

            return await Roles.Healer.Raise(
                Spells.Resurrection,
                SummonerSettings.Instance.SwiftcastRes,
                SummonerSettings.Instance.SlowcastRes,
                SummonerSettings.Instance.ResOutOfCombat,
                SummonerSettings.Instance.ResDelay
            );
        }
        public static async Task<bool> ForceRaiseLogic()
        {
            if (!Spells.Resurrection.IsKnown())
                return false;

            if (!Globals.InParty)
                return false;

            if (Core.Me.CurrentMana < Spells.Resurrection.Cost)
                return false;

            var deadList = Group.DeadAllies.Where(u => !u.HasAura(Auras.Raise) &&
                                                       u.WithinSpellRange(30) &&
                                                       u.InLineOfSight() &&
                                                       u.IsTargetable)
                                           .OrderByDescending(r => r.GetResurrectionWeight());

            var deadTarget = deadList.FirstOrDefault();

            if (deadTarget == null)
                return false;

            if (!deadTarget.IsVisible)
                return false;

            if (!deadTarget.IsTargetable)
                return false;

            if (Core.Me.InCombat || Globals.OnPvpMap)
            {
                if (!Spells.Swiftcast.IsKnown())
                    return false;

                if (Spells.Swiftcast.Cooldown != TimeSpan.Zero)
                    return false;

                if (await Buff.Swiftcast())
                {
                    while (Core.Me.HasAura(Auras.Swiftcast))
                    {
                        if (await Spells.Resurrection.Cast(deadTarget)) return true;
                        await Coroutine.Yield();
                    }
                }
            }

            if (Core.Me.InCombat)
                return false;

            return await Spells.Resurrection.Cast(deadTarget);
        }

        public static async Task<bool> HardRaise()
        {
            if (!Spells.Resurrection.IsKnown())
                return false;

            if (!Globals.InParty)
                return false;

            if (Core.Me.CurrentMana < Spells.Resurrection.Cost)
                return false;

            var deadList = Group.DeadAllies.Where(u => !u.HasAura(Auras.Raise) &&
                                                       u.WithinSpellRange(30) &&
                                                       u.InLineOfSight() &&
                                                       u.IsTargetable)
                                           .OrderByDescending(r => r.GetResurrectionWeight());

            var deadTarget = deadList.FirstOrDefault();

            if (deadTarget == null)
                return false;

            if (!deadTarget.IsVisible)
                return false;

            if (!deadTarget.IsTargetable)
                return false;

            return await Spells.Resurrection.Cast(deadTarget);
        }

        public static async Task<bool> RadiantAegis()
        {
            if (!SummonerSettings.Instance.RadiantAegis)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.RadiantAegis.IsKnownAndReady())
                return false;

            if (Utilities.Routines.Summoner.RadiantAegisUpOrLanding)
                return false;

            if (Core.Me.CurrentHealthPercent >= SummonerSettings.Instance.RadiantAegisHPThreshold)
                return false;

            // All() over an empty list is true: with no enemies tracked at all, the
            // "everything attacking me is casting" panic pattern passed vacuously and the
            // shield was spent against nothing. Demand an enemy before reading the pattern.
            if (!Combat.Enemies.Any())
                return false;

            if (!Combat.Enemies.All(x => x.TargetCharacter == Core.Me && x.IsCasting))
                return false;

            // Cast, not CastAura: CastAura then waits up to 3s for the shield with the player as
            // its caster, but Carbuncle is the caster, so every press here parked the whole
            // rotation for the full 3s - 37.7s without a GCD in one dungeon run.
            return await Spells.RadiantAegis.Cast(Core.Me);
        }

        public static async Task<bool> LuxSolaris()
        {
            if (!SummonerSettings.Instance.LuxSolaris)
                return false;

            if (!Spells.LuxSolaris.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.RefulgentLux))
                return false;

            // Expiry dump: the heal is free and the charge is about to vanish, so spend it
            // regardless of anyone's health rather than let it expire unused. Never on a fresh
            // charge: for about a second after Solar Bahamut lands, the charge's time left reads
            // wrong and can pass for one about to expire (13/13 field casts once came 0.1-1.3s
            // after the summon). The charge lasts 30s from the summon, whose 60s recast (shortened
            // by spell speed) then still has well over 50s to run, while in the charge's last 4s it
            // has 34s or less. So the dump also waits until the recast is down to 45s, about when
            // the demi leaves, which a fresh charge cannot reach whatever its time left reads.
            if (Utilities.Routines.Summoner.DemiSummonCooldownMs <= 45000
                && Core.Me.HasAuraExpiringWithin(Auras.RefulgentLux, msRemaining: 4000))
                return await Spells.LuxSolaris.Cast(Core.Me);

            if (Globals.InParty)
            {
                var needHealing = PartyManager.NumMembers > 4 ? 3 : 2;

                if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SummonerSettings.Instance.LuxSolarisHpPercent) < needHealing)
                    return false;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent > SummonerSettings.Instance.LuxSolarisHpPercent)
                    return false;
            }

            return await Spells.LuxSolaris.Cast(Core.Me);
        }
    }
}

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

            if (!Spells.Physick.IsKnown())
                return false;

            if (!Spells.Physick.IsKnown())
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

            if (Core.Me.HasAura(Auras.RadiantAegis))
                return false;

            if (Core.Me.CurrentHealthPercent >= SummonerSettings.Instance.RadiantAegisHPThreshold)
                return false;

            if (!Combat.Enemies.All(x => x.TargetCharacter == Core.Me && x.IsCasting))
                return false;

            return await Spells.RadiantAegis.CastAura(Core.Me, Auras.RadiantAegis);
        }

        public static async Task<bool> LuxSolaris()
        {
            if (!SummonerSettings.Instance.LuxSolaris)
                return false;

            if (!Spells.LuxSolaris.IsKnown())
                return false;

            if (!Core.Me.HasAura(Auras.RefulgentLux))
                return false;

            if (Globals.InParty)
            {
                var needHealing = PartyManager.NumMembers > 4 ? 3 : 2;

                if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SummonerSettings.Instance.LuxSolarisHpPercent) < needHealing)
                    return false;
            }
            else
            {
                if (Core.Me.CurrentHealthPercent <= SummonerSettings.Instance.LuxSolarisHpPercent)
                    return false;
            }

            return await Spells.LuxSolaris.Cast(Core.Me);
        }
    }
}

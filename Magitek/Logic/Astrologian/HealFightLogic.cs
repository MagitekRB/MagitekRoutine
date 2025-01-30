using ff14bot;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Astrologian;
using static Magitek.Logic.Astrologian.Heals;
using Magitek.Utilities;
using static Magitek.Utilities.Routines.Astrologian;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal class HealFightLogic
    {
        public static async Task<bool> Aoe()
        {
            if (!Globals.InParty)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (!FightLogic.EnemyIsCastingBigAoe() && !FightLogic.EnemyIsCastingAoe())
                return false;

            if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                return false;

            if (AstrologianSettings.Instance.FightLogicNeutralSect
                && Spells.NeutralSect.IsKnownAndReady()
                && Spells.NeutralSect.CanCast())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Neutral Sect");
                return await FightLogic.DoAndBuffer(Spells.NeutralSect.Cast(Core.Me));
            }

            if (AstrologianSettings.Instance.FightLogicEarthlyStar
                && Spells.StellarDetonation.IsKnownAndReady()
                && Spells.StellarDetonation.CanCast())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Earthly Star");

                Character target = Core.Me;

                if (AstrologianSettings.Instance.EarthlyStarCenterParty)
                {
                    var targets = Group.CastableAlliesWithin30.OrderBy(r =>
                        Group.CastableAlliesWithin30.Sum(ot => r.Distance(EarthlyStarLocation))
                    ).ThenBy(t => Core.Me.Distance(t.Location));

                    target = targets.FirstOrDefault(Core.Me);
                }

                return await FightLogic.DoAndBuffer(Spells.StellarDetonation.Cast(target));
            }

            if (AstrologianSettings.Instance.FightLogicCollectiveUnconscious
                && Spells.CollectiveUnconscious.IsKnownAndReady()
                && Spells.CollectiveUnconscious.CanCast())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Collective Unconscious");

                Character target = Core.Me;

                if (AstrologianSettings.Instance.CollectiveUnconsciousCenterParty)
                {
                    var targets = Group.CastableAlliesWithin30.Count();

                    if ( targets >= AstrologianSettings.Instance.CollectiveUnconsciousAllies)
                    {
                        return await FightLogic.DoAndBuffer(Spells.CollectiveUnconscious.Cast(Core.Me));
                    }
                }
                return false;
                
            }

            if (AstrologianSettings.Instance.FightLogicHoroscope
                && Spells.Horoscope.IsKnownAndReady()
                && Spells.Horoscope.CanCast())
            {
                if (!FightLogic.HodlCastTimeRemaining(1500))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Horoscope");

                return await FightLogic.DoAndBuffer(Spells.Horoscope.Cast(Core.Me));
            }

            if (AstrologianSettings.Instance.FightLogicAspectedHelios)
            {
                var spell = Spells.HeliosConjunction.IsKnown() ? Spells.HeliosConjunction : Spells.AspectedHelios;

                if (!spell.IsKnownAndReady() || !spell.CanCast())
                    return false;

                if (Group.CastableAlliesWithin30.Any(x => x.HasAura(Auras.AspectedHelios, true) || x.HasAura(Auras.HeliosConjunction, true)))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(2000))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Aspected Helios");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }

            return false;
        }

        public static async Task<bool> Tankbuster()
        {
            if (!Globals.InParty)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            var target = FightLogic.EnemyIsCastingTankBuster();

            if (target == null)
            {
                target = FightLogic.EnemyIsCastingSharedTankBuster();

                if (target == null)
                    return false;
            }

            if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                return false;

            if (AstrologianSettings.Instance.FightLogicCelestialIntersection
                && Spells.CelestialIntersection.IsKnownAndReady()
                && !target.HasAura(Auras.CelestialIntersection)
                && Spells.CelestialIntersection.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Celestial Intersection on {target.Name}");
                return await FightLogic.DoAndBuffer(Spells.CelestialIntersection.HealAura(target, Auras.CelestialIntersection));
            }

            if (AstrologianSettings.Instance.FightLogicExaltation
                && Spells.Exaltation.IsKnownAndReady()
                && !target.HasAura(Auras.Exaltation)
                && Spells.Exaltation.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Exaltation on {target.Name}");
                return await FightLogic.DoAndBuffer(Spells.Exaltation.HealAura(target, Auras.Exaltation));
            }

            return false;
        }
    }
}

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

            // Only pop a matured star: during Earthly Dominance the burst is the weak version,
            // and with proactive planting a fresh star would otherwise be spent at reduced
            // potency the moment any raidwide is detected.
            if (AstrologianSettings.Instance.FightLogicEarthlyStar
                && Core.Me.HasAura(Auras.GiantDominance)
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

            // When Collective Unconscious declines, fall through to the later responses —
            // an early return here starves Horoscope and Aspected Helios of the mechanic.
            if (AstrologianSettings.Instance.FightLogicCollectiveUnconscious
                && Spells.CollectiveUnconscious.IsKnownAndReady()
                && Spells.CollectiveUnconscious.CanCast()
                && Group.CastableAlliesWithin30.Count() >= AstrologianSettings.Instance.CollectiveUnconsciousAllies)
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Collective Unconscious");

                return await FightLogic.DoAndBuffer(Spells.CollectiveUnconscious.Cast(Core.Me));
            }

            // Macrocosmos stores the hit and returns half of it as healing, so it must go
            // out BEFORE the raidwide lands - and only for the big ones, the same
            // selectivity its old branch in Heals had. The aura guards are load-bearing:
            // with the buff already up the client resolves this id to Microcosmos, so an
            // unguarded press would CONVERT the stored damage before the hit arrives.
            if (AstrologianSettings.Instance.FightLogic_Macrocosmos
                && FightLogic.EnemyIsCastingBigAoe()
                && Spells.Macrocosmos.IsKnownAndReady()
                && !Core.Me.HasMyAura(Auras.Macrocosmos)
                && !Group.CastableAlliesWithin20.Any(x => x.HasAura(Auras.Macrocosmos))
                && Spells.Macrocosmos.CanCast())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Macrocosmos");

                return await FightLogic.DoAndBuffer(Spells.Macrocosmos.HealAura(Core.Me, Auras.Macrocosmos));
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

            // One mitigation per buster: if the victim already carries one of our tools
            // (the card's Bole included), the hit is answered - save the next tool for the
            // next buster instead of stacking diminishing mitigation on this one.
            var alreadyMitigated = target.HasAura(Auras.TheBole)
                || target.HasAura(Auras.CelestialIntersection)
                || target.HasAura(Auras.Exaltation);

            if (AstrologianSettings.Instance.FightLogicCelestialIntersection
                && Spells.CelestialIntersection.IsKnownAndReady()
                && !alreadyMitigated
                && Spells.CelestialIntersection.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Celestial Intersection on {target.Name}");
                return await FightLogic.DoAndBuffer(Spells.CelestialIntersection.HealAura(target, Auras.CelestialIntersection));
            }

            if (AstrologianSettings.Instance.FightLogicExaltation
                && Spells.Exaltation.IsKnownAndReady()
                && !alreadyMitigated
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

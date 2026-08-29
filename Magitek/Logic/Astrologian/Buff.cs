using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using AstroUtils = Magitek.Utilities.Routines.Astrologian;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class Buff
    {
        public static int AoeThreshold => PartyManager.NumMembers > 4 ? AstrologianSettings.Instance.AoeNeedHealingFullParty : AstrologianSettings.Instance.AoeNeedHealingLightParty;

        public static async Task<bool> LucidDreaming()
        {
            return await Roles.Healer.LucidDreaming(AstrologianSettings.Instance.LucidDreaming, AstrologianSettings.Instance.LucidDreamingManaPercent);
        }

        public static async Task<bool> Lightspeed()
        {
            if (!AstrologianSettings.Instance.Lightspeed)
                return false;

            if (Core.Me.HasAura(Auras.Lightspeed, true))
                return false;

            if (Core.Me.HasAura(Auras.Swiftcast, true))
                return false;

            if (Spells.Lightspeed.Cooldown != TimeSpan.Zero
                && Spells.Lightspeed.Charges == 0)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Lightspeed.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.LightspeedWithDivination && Core.Me.HasAura(Auras.Divination, true))
                return await Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed);

            if (AstrologianSettings.Instance.LightspeedWithNeutralSect && Core.Me.HasAura(Auras.NeutralSect, true))
                return await Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed);

            if (Globals.InParty)
            {
                if (AstrologianSettings.Instance.FightLogic_Lightspeed && FightLogic.EnemyIsCastingBigAoe() && !Spells.NeutralSect.IsKnownAndReady() && !Spells.Macrocosmos.IsKnownAndReady())
                    return await FightLogic.DoAndBuffer(Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed));

                if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.LightspeedHealthPercent) >= Heals.AoeThreshold)
                    return await Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed);
            }

            return false;
        }

        public static async Task<bool> Divination()
        {
            if (!AstrologianSettings.Instance.Divination)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.Divination.IsKnownAndReady())
                return false;

            if (Cards.HoldDivinationForDraw())
                return false;

            // Added check to see if more than configured allies are around
            var divinationTargets = Group.CastableAlliesWithin30.Count(r => r.IsAlive);

            if (divinationTargets >= AstrologianSettings.Instance.DivinationAllies)
                return await Spells.Divination.CastAura(Core.Me, Auras.Divination);

            return false;
        }

        public static async Task<bool> Synastry()
        {
            if (!AstrologianSettings.Instance.Synastry)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.Synastry.IsKnownAndReady())
                return false;

            if (Casting.LastSpell == Spells.Synastry)
                return false;

            if (Core.Me.HasAura(Auras.SynastrySource))
                return false;

            // Verified tooltip: the bonded ally recovers 40% of every single-target healing
            // spell we cast on anyone, themselves included - Synastry is a focus-heal
            // amplifier, so the bond belongs on the ally our single-target heals are about
            // to pour into. One endangered ally is the whole trigger; the old ally-count
            // gate blocked exactly the textbook case, a lone tank eating busters.
            GameObject target = Group.CastableAlliesWithin30
                .Where(r => r.CurrentHealth > 0
                    && r.CurrentHealthPercent <= AstrologianSettings.Instance.SynastryHealthPercent)
                .OrderBy(r => r.IsTank() ? 0 : 1)
                .ThenBy(r => r.CurrentHealthPercent)
                .FirstOrDefault();

            if (target == null)
                return false;

            return await Spells.Synastry.CastAura(target, Auras.SynastryDestination);
        }

        public static async Task<bool> NeutralSect()
        {
            if (!AstrologianSettings.Instance.NeutralSect)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.NeutralSect.IsKnownAndReady())
                return false;

            // One fight-logic family: the responder tab's toggle rules this, and the Macrocosmos
            // check is READINESS - Neutral Sect steps in when Macrocosmos cannot, regardless of
            // whether the user enabled Macrocosmos reactions.
            if (AstrologianSettings.Instance.FightLogicNeutralSect && FightLogic.EnemyIsCastingBigAoe() && !Spells.Macrocosmos.IsKnownAndReady() && !Core.Me.HasAnyAura(AstroUtils.ScholarAndSageShieldsNotToOverwrite))
                return await FightLogic.DoAndBuffer(Spells.NeutralSect.CastAura(Core.Me, Auras.NeutralSect));

            var neutral = Group.CastableAlliesWithin15.Count(r => r.CurrentHealth > 0
            && r.CurrentHealthPercent <= AstrologianSettings.Instance.NeutralSectHealthPercent);

            if (neutral < AoeThreshold)
                return false;

            return await Spells.NeutralSect.CastAura(Core.Me, Auras.NeutralSect);
        }

        public static async Task<bool> SunSign()
        {
            if (!AstrologianSettings.Instance.SunSign)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.SunSign.IsKnownAndReady())
                return false;

            if (!Core.Me.HasAura(Auras.Suntouched, true))
                return false;

            return await Spells.SunSign.CastAura(Core.Me, Auras.SunSign);
        }
    }
}
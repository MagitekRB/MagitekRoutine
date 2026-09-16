using Buddy.Coroutines;
using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Summoner;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Magitek.Utilities.Routines.Summoner;
using SmnResources = ff14bot.Managers.ActionResourceManager.Summoner;

namespace Magitek.Logic.Summoner
{
    internal static class Buff
    {
        public static async Task<bool> DreadwyrmTrance()
        {
            if (!SummonerSettings.Instance.DreadwyrmTrance)
                return false;

            if (!Spells.DreadwyrmTrance.IsKnown())
                return false;

            if (!Spells.DreadwyrmTrance.IsKnownAndReady())
                return false;

            if (Spells.SummonBahamut.IsKnown())
                return false;

            if (SmnResources.PetTimer + SmnResources.TranceTimer > 0)
                return false;

            if (Core.Me.SummonedPet() != SmnPets.Carbuncle)
                return false;

            if (Combat.CombatTotalTimeLeft < 15)
                return false;

            return await Spells.DreadwyrmTrance.Cast(Core.Me);
        }

        public static async Task<bool> LucidDreaming()
        {
            if (!Spells.LucidDreaming.IsKnownAndReady())
                return false;

            if (!Spells.LucidDreaming.IsKnown())
                return false;

            if (!SummonerSettings.Instance.LucidDreaming)
                return false;

            if (Core.Me.CurrentManaPercent > SummonerSettings.Instance.LucidDreamingManaPercent)
                return false;

            if (!CanWeave())
                return false;

            return await Spells.LucidDreaming.Cast(Core.Me);
        }


        public static async Task<bool> Swiftcast()
        {
            if (Spells.Swiftcast.Cooldown != TimeSpan.Zero)
                return false;

            if (!Spells.Swiftcast.IsKnown())
                return false;

            if (await Spells.Swiftcast.CastAura(Core.Me, Auras.Swiftcast))
            {
                return await Coroutine.Wait(15000, () => Core.Me.HasAura(Auras.Swiftcast, true, 7000));
            }

            return false;
        }

        public static async Task<bool> Aethercharge()
        {
            if (!Spells.Aethercharge.IsKnown())
                return false;

            if (await Pets.SummonPhoenix()) return true;
            if (await Pets.SummonBahamut()) return true;

            if (Spells.SummonBahamut.IsKnown())
                return false;

            if (await DreadwyrmTrance()) return true;

            if (Spells.DreadwyrmTrance.IsKnown())
                return false;

            if (!SummonerSettings.Instance.Aethercharge)
                return false;

            if (!Spells.Aethercharge.IsKnownAndReady())
                return false;

            return await Spells.Aethercharge.Cast(Core.Me);
        }

        public static async Task<bool> SearingLight()
        {
            if (!SummonerSettings.Instance.SearingLight)
                return false;

            if (!Spells.SearingLight.IsKnown())
                return false;

            if (Spells.SearingLight.Cooldown != TimeSpan.Zero)
            {
                Utilities.Routines.Summoner.SearingLightHoldStartTick = 0;
                return false;
            }

            if (Core.Me.HasAura(Auras.SearingLight))
                return false;

            // It is an oGCD: without a weave gate it fired the moment the 120s recast ended, clipping
            // a ready GCD by an animation lock — worst at the aligned pulse where the next GCD is the
            // demi summon itself. The stall-fallback gate still lets it fire if the GCD cannot roll.
            if (!CanWeave())
                return false;

            // Align with the demi window: fired on plain cooldown, the 20s buff opens on the
            // zero-potency summon GCD and expires one to two GCDs early at the tail — the guides
            // weave it after the first demi GCD instead. TranceTimer is the demi/trance clock
            // (despite the name, PetTimer is the gem attunement timer), so: demi out -> cast;
            // demi imminent (summon cooldown inside a few seconds) -> hold for it; no demi in
            // sight (desynced, downtime recovery) -> cast on cooldown, because staying aligned
            // with the party's two-minute buffs outranks our own placement.
            if (SmnResources.TranceTimer <= 0)
            {
                var summon = Spells.SummonSolarBahamut.IsKnown() ? Spells.SummonSolarBahamut
                    : Spells.SummonBahamut.IsKnown() ? Spells.SummonBahamut
                    : Spells.DreadwyrmTrance.IsKnown() ? Spells.DreadwyrmTrance
                    : Spells.Aethercharge;

                var summonCooldownMs = summon.Cooldown.TotalMilliseconds;

                // Ready counts as imminent too — field-observed: with the summon at zero the
                // buff went out 3.8s before the demi. But a ready summon can also sit parked
                // behind leftover gem phases for tens of seconds, so the hold is BOUNDED:
                // after a few seconds of waiting, alignment with the party's two-minute
                // buffs wins and the cast goes out anyway.
                if (summonCooldownMs <= 5000)
                {
                    if (Utilities.Routines.Summoner.SearingLightHoldStartTick == 0)
                        Utilities.Routines.Summoner.SearingLightHoldStartTick = System.Environment.TickCount64;

                    if (System.Environment.TickCount64 - Utilities.Routines.Summoner.SearingLightHoldStartTick < 8000)
                        return false;
                }
            }

            //In Shadowbringers, Searing Light was cast by your Carbuncle. In modern FFXIV, it is cast directly by the Summoner.
            //if (Core.Me.SummonedPet() != SmnPets.Carbuncle)
            //    return false;

            return await Spells.SearingLight.Cast(Core.Me);
        }
    }
}
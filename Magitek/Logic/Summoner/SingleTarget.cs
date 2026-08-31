using Buddy.Coroutines;
using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Summoner;
using Magitek.Utilities;
using Magitek.Utilities.Routines;
using System.Linq;
using System.Threading.Tasks;
using static Magitek.Utilities.Routines.Summoner;
using ArcResources = ff14bot.Managers.ActionResourceManager.Arcanist;
using Auras = Magitek.Utilities.Auras;
using SmnResources = ff14bot.Managers.ActionResourceManager.Summoner;

namespace Magitek.Logic.Summoner
{
    internal static class SingleTarget
    {
        public static async Task<bool> Ruin()
        {
            if (!SummonerSettings.Instance.Ruin)
                return false;

            if (Core.Me.SummonedPet() == SmnPets.Phoenix)
                return await Spells.FountainofFire.Cast(Core.Me.CurrentTarget);

            if (Core.Me.SummonedPet() == SmnPets.Bahamut)
                return await Spells.AstralImpulse.Cast(Core.Me.CurrentTarget);

            if (Core.Me.SummonedPet() == SmnPets.SolarBahamut)
                return await Spells.UmbralImpulse.Cast(Core.Me.CurrentTarget);

            if (Spells.AstralImpulse.IsKnownAndReady() && SmnResources.TranceTimer > 0 && Core.Me.SummonedPet() == SmnPets.Carbuncle) //It means we're in Dreadwyrm Trance
                return await Spells.AstralImpulse.Cast(Core.Me.CurrentTarget);

            var gemshine = Spells.Gemshine.Masked();

            if (gemshine.IsKnownAndReadyAndCastableAtTarget())
            {
                if (gemshine == Spells.RubyRite)
                {
                    if (!Spells.Swiftcast.IsKnownAndReady())
                        return await Spells.RubyRite.Cast(Core.Me.CurrentTarget);

                    var anyDead = Group.DeadAllies.Any(u => !u.HasAura(Auras.Raise) &&
                                                            u.WithinSpellRange(30) &&
                                                            u.InLineOfSight() &&
                                                            u.IsTargetable);

                    if (anyDead || SmnResources.ElementalAttunement > 1 ||
                        !SummonerSettings.Instance.SwiftRubyRite)
                        return await Spells.RubyRite.Cast(Core.Me.CurrentTarget);

                    if (await Buff.Swiftcast())
                    {
                        while (Core.Me.HasAura(Auras.Swiftcast))
                        {
                            // If the target died or despawned mid-sequence the cast can never succeed,
                            // and without an exit this loop parks the whole routine until the Swiftcast
                            // aura expires (~10s). Bail out so the normal chain resumes next pulse.
                            if (Core.Me.CurrentTarget == null || !Core.Me.CurrentTarget.IsValid || Core.Me.CurrentTarget.CurrentHealth == 0)
                                return false;

                            if (await Spells.RubyRite.Cast(Core.Me.CurrentTarget)) return true;
                            await Coroutine.Yield();
                        }
                    }
                }

                return await gemshine.Cast(Core.Me.CurrentTarget);
            }

            if (Spells.Ruin3.IsKnownAndReadyAndCastableAtTarget())
                return await Spells.Ruin3.Cast(Core.Me.CurrentTarget);

            return Spells.Ruin2.IsKnown()
                ? await Spells.Ruin2.Cast(Core.Me.CurrentTarget)
                : await Spells.Ruin.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> Fester()
        {
            if (!SummonerSettings.Instance.Fester)
                return false;

            if (!Spells.Fester.IsKnown())
                return false;

            if (!Spells.Fester.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow == 0)
                return false;

            if (!CanWeave())
                return false;

            // Defer to Painflare only where Painflare will actually take over: its own gate
            // needs three enemies, so deferring at two left the gauge frozen - Fester held,
            // Painflare declining, Energy Drain never recast - any time exactly two enemies
            // were clustered. And below Painflare's level there is nothing to defer to at all.
            if (AoeControl.Enabled && Spells.Painflare.IsKnown()
                && Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= 3)
                return false;

            return await Spells.Fester.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnergyDrain()
        {
            if (!SummonerSettings.Instance.EnergyDrain)
                return false;

            if (!Spells.EnergyDrain.IsKnown())
                return false;

            if (!Spells.EnergyDrain.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow != 0)
                return false;
            
            //This explicitly forbids the bot from casting Energy Drain unless you are currently in a Bahamut or Phoenix phase. 
            //If it comes off cooldown during Titan/Ifrit/Garuda, it will sit unused for up to 45 seconds, completely destroying your DPS.
            //if (ArcResources.TranceTimer + SmnResources.TranceTimer == 0)
            //    return false;

            if (!CanWeave())
                return false;

            // Only defer to Energy Siphon when it can actually fire: it is Lv52 (Energy Drain is Lv10)
            // and has its own toggle. Deferring while it is unknown or disabled means no Aetherflow is
            // ever generated in 3+ packs, which also kills Painflare and Fester for the whole pull.
            if (AoeControl.Enabled && Spells.EnergySiphon.IsKnown() && SummonerSettings.Instance.EnergySiphon
                && Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= 3)
                return false;

            return await Spells.EnergyDrain.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Enkindle()
        {
            if (!Core.Me.InCombat)
                return false;

            // No top-level Enkindle known-check: base action 184 is the removed Stormblood egi-command
            // (no current job owns it), so IsKnown()/HasSpell is always false and would dead-gate this
            // dispatcher. Each per-pet method guards on its real demi-enkindle (7429/16516/36998) below.
            if (Core.Me.SummonedPet() == SmnPets.Bahamut) return await EnkindleBahamut();
            if (Core.Me.SummonedPet() == SmnPets.SolarBahamut) return await EnkindleSolarBahamut();
            if (Core.Me.SummonedPet() == SmnPets.Phoenix) return await EnkindlePhoenix();

            return false;
        }

        public static async Task<bool> EnkindleBahamut()
        {
            if (!SummonerSettings.Instance.EnkindleBahamut)
                return false;

            if (!Spells.EnkindleBahamut.IsKnown())
                return false;

            if (!Spells.EnkindleBahamut.IsKnownAndReady())
                return false;

            if (!CanWeave())
                return false;

            return await Spells.EnkindleBahamut.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnkindleSolarBahamut()
        {
            if (!SummonerSettings.Instance.EnkindleBahamut)
                return false;

            if (!Spells.EnkindleSolarBahamut.IsKnown())
                return false;

            if (!Spells.EnkindleSolarBahamut.IsKnownAndReady())
                return false;

            if (!CanWeave())
                return false;

            return await Spells.EnkindleSolarBahamut.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnkindlePhoenix()
        {
            if (!SummonerSettings.Instance.EnkindlePhoenix)
                return false;

            if (!Spells.EnkindlePhoenix.IsKnown())
                return false;

            if (!Spells.EnkindlePhoenix.IsKnownAndReady())
                return false;

            if (!CanWeave())
                return false;

            return await Spells.EnkindlePhoenix.Cast(Core.Me.CurrentTarget);
        }
    }
}
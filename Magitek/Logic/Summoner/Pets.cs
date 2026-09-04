using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Summoner;
using Magitek.Utilities;
using Magitek.Utilities.Routines;
using System.Threading.Tasks;
using static Magitek.Utilities.Routines.Summoner;
using ArcResources = ff14bot.Managers.ActionResourceManager.Arcanist;
using SmnResources = ff14bot.Managers.ActionResourceManager.Summoner;
using SpellData = ff14bot.Objects.SpellData;


namespace Magitek.Logic.Summoner
{
    internal static class Pets
    {

        public static async Task<bool> SummonCarbuncle()
        {
            if (!SummonerSettings.Instance.SummonCarbuncle)
                return false;

            if (!Spells.SummonCarbuncle.IsKnown())
                return false;

            if (!Spells.SummonCarbuncle.IsKnown())
                return false;

            if (Core.Me.IsMounted || MovementManager.IsMoving || MovementManager.IsOccupied)
                return false;

            if (Core.Me.SummonedPet() != SmnPets.None)
                return false;

            return await Spells.SummonCarbuncle.Cast(Core.Me);
        }

        public static async Task<bool> SummonPhoenix()
        {
            if (!SummonerSettings.Instance.SummonPhoenix)
                return false;

            if (!Spells.SummonPhoenix.IsKnown())
                return false;

            if (!Spells.SummonPhoenix.IsKnownAndReady())
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (SmnResources.PetTimer + SmnResources.TranceTimer > 0)
                return false;

            // No gem-flag guard here on purpose: the demi is never delayed for unspent
            // primal gems. Both guides are explicit - "do not delay demi-primal" (The
            // Balance downtime flowchart) and "you should skip the remainder of your
            // primal(s)" (Icy Veins) - and the demi re-grants all three gems on summon,
            // so nothing is lost. The old guard also read the per-gem toggles as
            // always-on: one unticked gem summon left its flag permanently set and
            // deadlocked the entire demi cycle for the rest of the session.

            if (SummonerSettings.Instance.ThrottleTranceSummonsWithTTL
                && !(SummonerSettings.Instance.SummonThrottleIgnoreBosses && Core.Me.CurrentTarget.IsBoss())
                && Combat.CombatTotalTimeLeft < SummonerSettings.Instance.ThrottleTranceSummonsSeconds)
                return false;

            return await Spells.SummonPhoenix.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SummonBahamut()
        {
            if (!SummonerSettings.Instance.SummonBahamut)
                return false;

            if (!Spells.SummonBahamut.IsKnown())
                return false;

            SpellData bahamutSpell;

            if (Spells.SummonBahamut.IsKnownAndReady())
                bahamutSpell = Spells.SummonBahamut;
            else if (Spells.SummonSolarBahamut.IsKnownAndReady())
                bahamutSpell = Spells.SummonSolarBahamut;
            else
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Phoenix))
                return false;

            if ((SmnResources.PetTimer + SmnResources.TranceTimer) > 0)
                return false;

            // No gem-flag guard: see SummonPhoenix above - the demi outranks unspent gems.
            if (Core.Me.SummonedPet() != SmnPets.Carbuncle)
                return false;

            if (SummonerSettings.Instance.ThrottleTranceSummonsWithTTL
                && !(SummonerSettings.Instance.SummonThrottleIgnoreBosses && Core.Me.CurrentTarget.IsBoss())
                && Combat.CombatTotalTimeLeft < SummonerSettings.Instance.ThrottleTranceSummonsSeconds)
                return false;

            // Dawntrail Optimization: Decoupled Searing Light from Bahamut. 
            // It is now an independent oGCD that should weave naturally without stalling Demi summons.
            return await bahamutSpell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SummonCarbuncleOrEgi()
        {
            if (!Spells.SummonCarbuncle.IsKnown())
                return false;

            if (Core.Me.SummonedPet() == SmnPets.None)
                return await SummonCarbuncle();

            if (!Core.Me.InCombat)
                return false;

            if ((SmnResources.PetTimer + SmnResources.TranceTimer) > 0)
                return false;

            if (SummonerSettings.Instance.ThrottleEgiSummonsWithTTL
                && !(SummonerSettings.Instance.SummonThrottleIgnoreBosses && Core.Me.CurrentTarget.IsBoss())
                && Combat.CombatTotalTimeLeft < SummonerSettings.Instance.ThrottleEgiSummonsSeconds)
                return false;

            if (SummonerSettings.Instance.SummonTopazTitan)
            {
                if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Titan) &&
                    Spells.SummonTitan.IsKnownAndReady())
                    return Spells.SummonTitan2.IsKnown()
                        ? await Spells.SummonTitan2.Cast(Core.Me.CurrentTarget)
                        : await Spells.SummonTitan.Cast(Core.Me.CurrentTarget);

                if (ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Topaz))
                    return await Spells.SummonTopaz.Cast(Core.Me.CurrentTarget);
            }

            if (SummonerSettings.Instance.SummonEmeraldGaruda)
            {
                if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Garuda) &&
                    Spells.SummonGaruda.IsKnownAndReady())
                    return Spells.SummonGaruda2.IsKnown()
                        ? await Spells.SummonGaruda2.CastAura(Core.Me.CurrentTarget, Auras.GarudasFavor, auraTarget: Core.Me)
                        : await Spells.SummonGaruda.CastAura(Core.Me.CurrentTarget, Auras.GarudasFavor, auraTarget: Core.Me);

                if (ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Emerald))
                    return await Spells.SummonEmerald.Cast(Core.Me.CurrentTarget);
            }

            if (SummonerSettings.Instance.SummonRubyIfrit)
            {
                if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Ifrit) &&
                    Spells.SummonIfrit.IsKnownAndReady())
                    return Spells.SummonIfrit2.IsKnown()
                        ? await Spells.SummonIfrit2.CastAura(Core.Me.CurrentTarget, Auras.IfritsFavor, auraTarget: Core.Me)
                        : await Spells.SummonIfrit.CastAura(Core.Me.CurrentTarget, Auras.IfritsFavor, auraTarget: Core.Me);

                if (ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Ruby))
                    return await Spells.SummonRuby.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }
    }
}
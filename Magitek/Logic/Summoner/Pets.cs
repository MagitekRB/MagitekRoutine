﻿using ff14bot;
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

            if (Core.Me.ClassLevel < Spells.SummonCarbuncle.LevelAcquired)
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

            if (Core.Me.ClassLevel < Spells.SummonPhoenix.LevelAcquired)
                return false;

            if (!Spells.SummonPhoenix.IsKnownAndReady())
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (SmnResources.PetTimer + SmnResources.TranceTimer > 0)
                return false;

            if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Ifrit)
                || SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Titan)
                || SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Garuda)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Ruby)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Topaz)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Emerald))
                return false;

            if ((SmnResources.PetTimer + SmnResources.TranceTimer) > 0)
                return false;

            if (SummonerSettings.Instance.ThrottleTranceSummonsWithTTL && Combat.CombatTotalTimeLeft < 15)
                return false;

            return await Spells.SummonPhoenix.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> SummonBahamut()
        {
            if (!SummonerSettings.Instance.SummonBahamut)
                return false;

            if (Core.Me.ClassLevel < Spells.SummonBahamut.LevelAcquired)
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

            if (SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Ifrit)
                || SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Titan)
                || SmnResources.AvailablePets.HasFlag(SmnResources.AvailablePetFlags.Garuda)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Ruby)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Topaz)
                || ArcResources.AvailablePets.HasFlag(ArcResources.AvailablePetFlags.Emerald))
                return false;

            if (Core.Me.SummonedPet() != SmnPets.Carbuncle)
                return false;

            if (SummonerSettings.Instance.ThrottleTranceSummonsWithTTL && Combat.CombatTotalTimeLeft < 15)
                return false;

            if (!SummonerSettings.Instance.SearingLight)
                return await bahamutSpell.Cast(Core.Me.CurrentTarget);

            if (!Spells.SearingLight.IsReady() && !Core.Me.HasAura(Auras.SearingLight))
                return await bahamutSpell.Cast(Core.Me.CurrentTarget);

            if (Spells.SearingLight.IsReady() && GlobalCooldown.CanWeave())
                return await Buff.SearingLight();

            if (!Core.Me.HasAura(Auras.SearingLight))
                return false;

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

            if (SummonerSettings.Instance.ThrottleEgiSummonsWithTTL && Combat.CombatTotalTimeLeft < 30)
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
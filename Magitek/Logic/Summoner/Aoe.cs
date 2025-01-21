using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
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
    internal static class Aoe
    {
        public static async Task<bool> AstralFlow()
        {
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.SummonedPet() == SmnPets.Bahamut) return await Deathflare();
            if (Core.Me.SummonedPet() == SmnPets.SolarBahamut) return await Sunflare();
            if (ArcResources.TranceTimer > 0 && Core.Me.SummonedPet() == SmnPets.Carbuncle) return await Deathflare();

            if (Core.Me.SummonedPet() == SmnPets.Pheonix) return await Rekindle();

            if (!Spells.MountainBuster.IsKnown()) return false;

            if (await CrimsonCyclone()) return true;
            if (await MountainBuster()) return true;
            return await Slipstream();
        }
        public static async Task<bool> Deathflare()
        {
            if (!SummonerSettings.Instance.Deathflare)
                return false;

            if (!Spells.Deathflare.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            var target = Combat.SmartAoeTarget(Spells.Deathflare, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.Deathflare.Cast(target);
        }

        public static async Task<bool> Sunflare()
        {
            if (!SummonerSettings.Instance.Deathflare)
                return false;

            if (!Spells.Sunflare.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            var target = Combat.SmartAoeTarget(Spells.Sunflare, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.Sunflare.Cast(target);
        }

        public static async Task<bool> Rekindle()
        {
            if (!SummonerSettings.Instance.Rekindle)
                return false;

            if (!Spells.Rekindle.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            var targetNeedsHealing = Group.CastableAlliesWithin30
                .FirstOrDefault(x => x.CurrentHealthPercent < SummonerSettings.Instance.RekindleHPThreshold);

            if (targetNeedsHealing == null)
                return false;

            return await Spells.Rekindle.Heal(targetNeedsHealing, false);
        }

        public static async Task<bool> CrimsonCyclone()
        {
            if (!SummonerSettings.Instance.CrimsonCyclone)
                return false;

            if (!Spells.CrimsonCyclone.IsKnownAndReady())
                return false;

            if (!Core.Me.HasAura(Auras.IfritsFavor))
                return false;

            if (SmnResources.ElementalAttunement > 1)
                return false;

            var target = Combat.SmartAoeTarget(Spells.CrimsonCyclone, SummonerSettings.Instance.SmartAoe);

            if (target == null)
                return false;

            return await Spells.CrimsonCyclone.Cast(target);
        }

        public static async Task<bool> CrimsonStrike()
        {
            if (!SummonerSettings.Instance.CrimsonStrike)
                return false;

            //if (SmnResources.ActivePet != SmnResources.ActivePetType.Ifrit)
            //    return false;

            if (!Spells.CrimsonStrike.IsKnownAndReady()) return false;

            if (!Core.Me.HasAura(Auras.CrimsonStrikeReady))
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (!Core.Me.WithinSpellRange(Spells.CrimsonStrike.Range))
                return false;

            return await Spells.CrimsonStrike.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> MountainBuster()
        {
            if (!SummonerSettings.Instance.MountainBuster)
                return false;

            //if (!Spells.MountainBuster.IsKnownAndReady())
            //    return false;

            if (!Core.Me.HasAura(Auras.TitansFavor))
                return false;

            var target = Combat.SmartAoeTarget(Spells.MountainBuster, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            if (Spells.MountainBuster.IsKnownAndReady())
                return await Spells.MountainBuster.Cast(target);

            return false;
        }

        public static async Task<bool> Slipstream()
        {
            if (!SummonerSettings.Instance.Slipstream)
                return false;

            if (!Spells.Slipstream.IsKnownAndReady())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (!Core.Me.HasAura(Auras.GarudasFavor))
                return false;

            var target = Combat.SmartAoeTarget(Spells.Slipstream, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.Slipstream.Cast(target);
        }
        public static async Task<bool> EnergySiphon()
        {
            if (!SummonerSettings.Instance.EnergySiphon)
                return false;

            if (!Spells.EnergySiphon.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow != 0)
                return false;

            if (ArcResources.TranceTimer + SmnResources.TranceTimer == 0)
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
                return false;

            var target = Combat.SmartAoeTarget(Spells.EnergySiphon, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.EnergySiphon.Cast(target);
        }

        public static async Task<bool> SearingFlash()
        {
            if (!Spells.SearingFlash.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            if (!Core.Me.HasAura(Auras.RubysGlimmer))
                return false;

            var target = Combat.SmartAoeTarget(Spells.SearingFlash, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.SearingFlash.Cast(target);
        }

        public static async Task<bool> Outburst()
        {
            if (!SummonerSettings.Instance.Outburst)
                return false;

            if (!Spells.Outburst.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
                return false;

            BattleCharacter target;

            if (Core.Me.SummonedPet() == SmnPets.Pheonix)
            {
                target = Combat.SmartAoeTarget(Spells.BrandofPurgatory, SummonerSettings.Instance.SmartAoe);

                if (target == null || Core.Me.CurrentTarget == null)
                    return false;

                return await Spells.BrandofPurgatory.Cast(target);
            }

            target = Combat.SmartAoeTarget(Spells.PreciousBrilliance, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            if (Core.Me.SummonedPet() == SmnPets.Bahamut)
                return await Spells.AstralFlare.Cast(target);

            if (Core.Me.SummonedPet() == SmnPets.SolarBahamut)
                return await Spells.UmbralFlare.Cast(target);

            if (Spells.AstralFlare.IsKnownAndReadyAndCastableAtTarget() && SmnResources.TranceTimer > 0 && Core.Me.SummonedPet() == SmnPets.Carbuncle) //It means we're in Dreadwyrm Trance
                return await Spells.AstralFlare.Cast(target);

            var brilliance = Spells.PreciousBrilliance.Masked();

            if (brilliance.IsKnownAndReadyAndCastableAtTarget())
                return await brilliance.Cast(target);

            if (Spells.TriDisaster.IsKnownAndReadyAndCastableAtTarget())
                return await Spells.TriDisaster.Cast(target);

            return await Spells.Outburst.Cast(target);
        }

        public static async Task<bool> Painflare()
        {
            if (!SummonerSettings.Instance.Painflare)
                return false;

            if (!Spells.Painflare.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow == 0)
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() < 3)
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            var target = Combat.SmartAoeTarget(Spells.Painflare, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.Painflare.Cast(target);
        }

        public static async Task<bool> Ruin4()
        {
            if (!SummonerSettings.Instance.Ruin4)
                return false;

            if (!Spells.Ruin4.IsKnownAndReady())
                return false;

            if (!Core.Me.HasAura(Auras.FurtherRuin))
                return false;

            if (Core.Me.SummonedPet() == SmnPets.Bahamut
                || Core.Me.SummonedPet() == SmnPets.SolarBahamut
                || Core.Me.SummonedPet() == SmnPets.Pheonix)
                return false;

            if ((SmnResources.ActivePet == SmnResources.ActivePetType.Garuda
                || SmnResources.ActivePet == SmnResources.ActivePetType.Titan)
                && SmnResources.ElementalAttunement > 0)
                return false;

            if (SmnResources.ActivePet == SmnResources.ActivePetType.Ifrit
                && (SmnResources.ElementalAttunement > 1 || !MovementManager.IsMoving))
                return false;

            var target = Combat.SmartAoeTarget(Spells.Ruin4, SummonerSettings.Instance.SmartAoe);

            if (target == null || Core.Me.CurrentTarget == null)
                return false;

            return await Spells.Ruin4.Cast(target);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Teraflare, Spells.Ruin);
        }
    }
}
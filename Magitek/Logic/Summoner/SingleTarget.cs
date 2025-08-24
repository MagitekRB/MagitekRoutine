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

            if (Core.Me.ClassLevel < Spells.Fester.LevelAcquired)
                return false;

            if (!Spells.Fester.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow == 0)
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= 2)
                return false;

            return await Spells.Fester.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnergyDrain()
        {
            if (!SummonerSettings.Instance.EnergyDrain)
                return false;

            if (Core.Me.ClassLevel < Spells.EnergyDrain.LevelAcquired)
                return false;

            if (!Spells.EnergyDrain.IsKnownAndReady())
                return false;

            if (SmnResources.Aetherflow + ArcResources.Aetherflow != 0)
                return false;

            if (ArcResources.TranceTimer + SmnResources.TranceTimer == 0)
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            if (Core.Me.CurrentTarget.EnemiesNearby(5).Count() >= 3)
                return false;

            return await Spells.EnergyDrain.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Enkindle()
        {
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.ClassLevel < Spells.Enkindle.LevelAcquired)
                return false;

            if (Core.Me.SummonedPet() == SmnPets.Bahamut) return await EnkindleBahamut();
            if (Core.Me.SummonedPet() == SmnPets.SolarBahamut) return await EnkindleSolarBahamut();
            if (Core.Me.SummonedPet() == SmnPets.Phoenix) return await EnkindlePhoenix();

            return false;
        }

        public static async Task<bool> EnkindleBahamut()
        {
            if (!SummonerSettings.Instance.EnkindleBahamut)
                return false;

            if (Core.Me.ClassLevel < Spells.EnkindleBahamut.LevelAcquired)
                return false;

            if (!Spells.EnkindleBahamut.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            return await Spells.EnkindleBahamut.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnkindleSolarBahamut()
        {
            if (!SummonerSettings.Instance.EnkindleBahamut)
                return false;

            if (Core.Me.ClassLevel < Spells.EnkindleSolarBahamut.LevelAcquired)
                return false;

            if (!Spells.EnkindleSolarBahamut.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            return await Spells.EnkindleSolarBahamut.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> EnkindlePhoenix()
        {
            if (!SummonerSettings.Instance.EnkindlePhoenix)
                return false;

            if (Core.Me.ClassLevel < Spells.EnkindlePhoenix.LevelAcquired)
                return false;

            if (!Spells.EnkindlePhoenix.IsKnownAndReady())
                return false;

            if (!GlobalCooldown.CanWeave())
                return false;

            return await Spells.EnkindlePhoenix.Cast(Core.Me.CurrentTarget);
        }
    }
}
using ff14bot.Objects;
using Magitek.Utilities.Managers;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ff14bot;
using ff14bot.Managers;
using Magitek.Utilities;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Extensions
{
    internal static class CharacterExtensions
    {
        public static bool NeedsDispel(this Character unit, bool highPriority = false)
        {
            return unit.CharacterAuras.Select(r => r.Id).Intersect(DispelManager.HighPriorityDispels.Union(DispelManager.NormalDispels)).Any();
        }

        public static bool HasAnyDispellableAura(this Character unit)
        {
            return unit.CharacterAuras.Any(r => r.IsDispellable);
        }

        public static bool HasAnyCardAura(this Character unit)
        {
            // The six arcana are the only card auras an ally can carry; Lord and Lady of
            // Crowns apply no aura to party members.
            return unit.HasAnyAura(new uint[] { Auras.TheBalance,
                                                        Auras.TheBole,
                                                        Auras.TheArrow,
                                                        Auras.TheSpear,
                                                        Auras.TheEwer,
                                                        Auras.TheSpire
            });
        }

        public static bool HasAnyDpsCardAura(this Character unit)
        {
            return unit.HasAnyAura(new uint[] {
                                                    Auras.TheSpear,
                                                    Auras.TheBalance,
            });
        }

        public static bool HasAnyHealerRegen(this Character unit)
        {
            return unit.HasAnyAura(HealerRegens);
        }

        public static float AdjustHealthThresholdByRegen(this Character target, float healthThreshold)
        {

            var regens = HealerRegens;
            var matchingAuras = target.CharacterAuras.Count(r => regens.Contains(r.Id));

            return healthThreshold + (2 * matchingAuras);
        }

        public static uint[] HealerRegens = new uint[] {
                Auras.Regen,
                Auras.Regen2,
                Auras.Medica2,
                Auras.Medica3,
                Auras.AsylumReceiver,
                Auras.SacredSoilReceiver,
                Auras.WhisperingDawn,
                Auras.AngelsWhisper,
                Auras.AspectedBenefic,
                Auras.AspectedHelios,
                Auras.HeliosConjunction,
                Auras.Kerakeia,
                Auras.PhysisII,
                Auras.SeraphismReceiver,
                Auras.CrestOfTimeReturned,
                Auras.Opposition,
                Auras.WheelOfFortune,
                Auras.TheEwer,
        };

        public static uint[] HealerShields = new uint[]
        {
            Auras.NocturnalField,
            Auras.Galvanize,
            Auras.EukrasianDiagnosis,
            Auras.EukrasianPrognosis,
            Auras.Haimatinon,
            Auras.Haima,
            Auras.Panhaima,
            Auras.Panhaimatinon,
            Auras.ShakeItOff,
            Auras.BlackestNight,
            Auras.NeutralSectShield,
            Auras.CelestialIntersection,
            Auras.TheSpire,
        };

        // The subset of HealerShields that OVERWRITE one another rather than stacking, so a target
        // carrying any one of them cannot usefully be given another. Confirmed in game: our own
        // Galvanize was replaced in the same millisecond a Sage's Eukrasian Prognosis landed, with
        // 29s still on it. Cross-class, which is why this cannot be a per-job list.
        public static uint[] PrimaryShields = new uint[]
        {
            Auras.Galvanize,
            Auras.EukrasianDiagnosis,
            Auras.EukrasianPrognosis,
        };

        /// <summary>
        /// True when the unit already carries a primary shield from ANY healer, ours or another's.
        /// Deliberately not own-aura only: these overwrite each other, so someone else's shield
        /// still makes ours redundant.
        /// </summary>
        public static bool HasPrimaryShield(this Character unit, int msLeft = 0)
        {
            return unit != null && unit.HasAnyAura(PrimaryShields, false, msLeft);
        }

        public static uint[] BuffIgnore = new uint[]
        {
            Auras.DancePartner,
            Auras.ClosedPosition,
            Auras.IronWill,
            Auras.Defiance,
            Auras.Grit,
            Auras.RoyalGuard,
            Auras.EyesOpen,
            Auras.Kardia,
            Auras.Kardion,
            Auras.Eukrasia
        };
    }
}

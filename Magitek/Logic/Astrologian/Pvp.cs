using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class Pvp
    {
        public static async Task<bool> FallMaleficPvp()
        {
            if (!Spells.FallMaleficPvp.CanCast())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.FallMaleficPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AspectedBeneficPvp()
        {
            if (!Spells.AspectedBeneficPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_AspectedBenefic)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Globals.HealTarget?.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent)
            {
                return await Spells.AspectedBeneficPvp.Heal(Globals.HealTarget);
            }

            var Target = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent);

            if (Target == null)
                if (Core.Me.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent)
                    return await Spells.AspectedBeneficPvp.Heal(Core.Me);

            return await Spells.AspectedBeneficPvp.Heal(Target);
        }

        public static async Task<bool> GravityIIPvp()
        {
            if (!Spells.GravityIIPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_GravityII)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check for nearby enemies around the target
            var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.GravityIIPvp.Radius));
            if (nearbyEnemies < AstrologianSettings.Instance.Pvp_GravityIIEnemies)
                return false;

            return await Spells.GravityIIPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DoubleCastPvp()
        {
            if (!Spells.DoubleCastPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_DoubleCast)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // Only use Double Cast if we have more than 1 charge
            if (Spells.DoubleCastPvp.Charges <= 1)
                return false;

            // Get the masked spell from Double Cast
            var maskedSpell = Spells.DoubleCastPvp.Masked();
            if (maskedSpell == null)
                return false;

            // Handle each possible masked spell type
            if (maskedSpell.Id == Spells.DoubleFallMaleficPvp.Id)
            {
                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                return await maskedSpell.Cast(Core.Me.CurrentTarget);
            }
            else if (maskedSpell.Id == Spells.DoubleAspectedBeneficPvp.Id)
            {
                if (Globals.HealTarget?.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent)
                {
                    return await maskedSpell.Cast(Globals.HealTarget);
                }

                var Target = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent);

                if (Target == null)
                    if (Core.Me.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent)
                        return await maskedSpell.Cast(Core.Me);

                return await maskedSpell.Cast(Target);
            }
            else if (maskedSpell.Id == Spells.DoubleGravityIIPvp.Id)
            {
                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                // Check for nearby enemies around the target
                var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.GravityIIPvp.Radius));
                if (nearbyEnemies < AstrologianSettings.Instance.Pvp_GravityIIEnemies)
                    return false;

                return await maskedSpell.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> MacrocosmosPvp()
        {
            if (!Spells.MacrocosmosPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_Macrocosmos)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAura(Auras.PvpMacrocosmos))
                return false;

            // Check for nearby enemies around the player
            var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.MacrocosmosPvp.Radius));
            if (nearbyEnemies < AstrologianSettings.Instance.Pvp_MacrocosmosEnemies)
                return false;

            return await Spells.MacrocosmosPvp.Cast(Core.Me);
        }

        public static async Task<bool> MinorArcanaPvp()
        {
            if (!Spells.MinorArcanaPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_MinorArcana)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // Get the masked spell from Minor Arcana
            var maskedSpell = Spells.MinorArcanaPvp.Masked();
            if (maskedSpell == null)
                return false;

            // If Minor Arcana equals itself (not drawn yet) and we have a target in range
            if (maskedSpell.Id == Spells.MinorArcanaPvp.Id)
            {
                var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.LordOfCrownsPvp.Radius));
                if (nearbyEnemies < AstrologianSettings.Instance.Pvp_LordOfCrownsEnemies)
                    return false;

                return await maskedSpell.Cast(Core.Me);
            }

            // Handle each possible masked spell type
            if (maskedSpell.Id == Spells.LadyOfCrownsPvp.Id)
            {
                // Check if any party members need healing
                var Target = Group.CastableAlliesWithin20.FirstOrDefault(r => r.CurrentHealth > 0 && r.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent);

                if (Target == null)
                    if (Core.Me.CurrentHealthPercent > AstrologianSettings.Instance.Pvp_AspectedBeneficHealthPercent)
                        return false;

                return await maskedSpell.Cast(Core.Me);
            }
            else if (maskedSpell.Id == Spells.LordOfCrownsPvp.Id)
            {
                // Check if we have a valid target for damage
                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                var nearbyEnemies = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.LordOfCrownsPvp.Radius));
                if (nearbyEnemies < AstrologianSettings.Instance.Pvp_LordOfCrownsEnemies)
                    return false;

                return await maskedSpell.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> OraclePvp()
        {
            if (!Spells.OraclePvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_Oracle)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Core.Me.HasAura(Auras.PvpDivining))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            return await Spells.OraclePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> CelestialRiverPvp()
        {
            if (!Spells.CelestialRiverPvp.CanCast())
                return false;

            if (!AstrologianSettings.Instance.Pvp_CelestialRiver)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            // Check for nearby allies that need healing
            var alliesNeedingHealing = Group.CastableAlliesWithin15.Count(x =>
                x.IsValid &&
                x.IsAlive &&
                x.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_CelestialRiverHealthPercent);

            // Include self in the count if below health threshold
            if (Core.Me.CurrentHealthPercent <= AstrologianSettings.Instance.Pvp_CelestialRiverHealthPercent)
                alliesNeedingHealing++;

            if (alliesNeedingHealing < AstrologianSettings.Instance.Pvp_CelestialRiverNearbyAllies)
                return false;

            // Check for enemies within Oracle range
            var enemiesInRange = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.OraclePvp.Radius));
            if (enemiesInRange == 0)
                return false;

            return await Spells.CelestialRiverPvp.Cast(Core.Me);
        }
    }
}
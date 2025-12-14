using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Dancer;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Dancer
{
    internal static class Pvp
    {
        public static async Task<bool> EnAvant()
        {
            if (!DancerSettings.Instance.Pvp_UseEnAvant)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (Core.Me.HasAura(Auras.PvpEnAvant))
                return false;

            if (!Spells.EnAvantPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // really a 10 yalm dash, only dash if it would put us within standard attack range which is 15
            if (!Core.Me.CurrentTarget.WithinSpellRange(25))
                return false;

            return await Spells.EnAvantPvp.Cast(Core.Me);
        }

        public static async Task<bool> FountainCombo()
        {
            // Spells: Cascade, Reverse Cascade, Saber Dance, Fountainfall, Dance of the Dawn
            // This method is used to cast the Fountain Combo
            // Using Masked it basically contains all in one.
            if (await Fountain())
                return true;

            if (await Cascade())
                return true;

            return false;
        }

        public static async Task<bool> Cascade()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.CascadePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.CascadePvp.Range))
                return false;

            return await Spells.CascadePvp.Masked().CastPvpCombo(Spells.FountainPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fountain()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.FountainPvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.FountainPvp.Range))
                return false;

            return await Spells.FountainPvp.Masked().CastPvpCombo(Spells.FountainPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> StarfallDance()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!DancerSettings.Instance.Pvp_StarfallDance)
                return false;

            if (!Spells.StarfallDancePvp.CanCast())
                return false;

            // Starfall Dance: 12,000 potency to all enemies in a straight line
            const double potency = 12000;

            // Find killable target in range (handles target validation internally)
            var killableTarget = CommonPvp.FindKillableTargetInRange(
                DancerSettings.Instance,
                potency,
                (float)Spells.StarfallDancePvp.Range,
                ignoreGuard: false,
                checkGuard: true,
                searchAllTargets: DancerSettings.Instance.Pvp_StarfallDanceAnyTarget);

            if (killableTarget != null)
            {
                return await Spells.StarfallDancePvp.Cast(killableTarget);
            }

            // Fallback: cast normally if not kill-only mode
            if (!DancerSettings.Instance.Pvp_StarfallDanceForKillsOnly)
            {
                if (!Core.Me.HasTarget)
                    return false;

                if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                    return false;

                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.StarfallDancePvp.Range))
                    return false;

                if (!Spells.StarfallDancePvp.CanCast(Core.Me.CurrentTarget))
                    return false;

                return await Spells.StarfallDancePvp.Cast(Core.Me.CurrentTarget);
            }

            return false;
        }

        public static async Task<bool> FanDance()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.FanDancePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.FanDancePvp.Range))
                return false;

            return await Spells.FanDancePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HoningDance()
        {
            if (!DancerSettings.Instance.Pvp_UseHoningDance)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HoningDancePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.HoningDancePvp.Radius))
                return false;

            var enemiesAroundTarget = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.HoningDancePvp.Radius));
            if (enemiesAroundTarget < DancerSettings.Instance.Pvp_HoningDanceMinimumEnemies)
                return false;

            return await Spells.HoningDancePvp.Cast(Core.Me);
        }

        public static async Task<bool> Contradance()
        {
            if (!DancerSettings.Instance.Pvp_UseContradance)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ContradancePvp.CanCast())
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.ContradancePvp.Radius))
                return false;

            var enemiesAroundTarget = Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ContradancePvp.Radius));
            if (enemiesAroundTarget < DancerSettings.Instance.Pvp_ContradanceMinimumEnemies)
                return false;

            // Don't use Contradance when target is mounted in Warmachina (ability is centered around target)
            if (CommonPvp.IsPvpMounted(Core.Me.CurrentTarget))
                return false;

            return await Spells.ContradancePvp.Cast(Core.Me);
        }

        public static async Task<bool> ClosedPosition()
        {
            if (!DancerSettings.Instance.Pvp_UseClosedPosition)
                return false;

            if (!Globals.InParty)
                return false;

            if (!Spells.ClosedPositionPvp.CanCast())
                return false;

            // If we have the aura, check if any allies within range have the dance partner aura
            if (Core.Me.HasAura(Auras.PvpClosedPosition))
            {
                var alliesWithDancePartner = Group.CastableAlliesWithin50.Any(a => a.IsAlive && !a.IsMe && a.HasAura(Auras.PvpDancePartner));
                if (alliesWithDancePartner)
                    return false;
                if (!Core.Me.HasTarget)
                    return false;
            }

            IEnumerable<Character> allyList = null;
            switch (DancerSettings.Instance.Pvp_DancePartnerSelectedStrategy)
            {
                case DancePartnerStrategy.ClosestDps:
                    allyList = Group.CastableAlliesWithin30.Where(a => a.IsAlive && !a.IsMe && a.IsDps()).OrderBy(GetWeight);
                    break;

                case DancePartnerStrategy.MeleeDps:
                    allyList = Group.CastableAlliesWithin30.Where(a => a.IsAlive && !a.IsMe && a.IsMeleeDps()).OrderBy(GetWeight);
                    break;

                case DancePartnerStrategy.RangedDps:
                    allyList = Group.CastableAlliesWithin30.Where(a => a.IsAlive && !a.IsMe && a.IsRangedDpsCard()).OrderBy(GetWeight);
                    break;

                case DancePartnerStrategy.Tank:
                    allyList = Group.CastableAlliesWithin30.Where(a => a.IsAlive && !a.IsMe && a.IsTank()).OrderBy(GetWeight);
                    break;

                case DancePartnerStrategy.Healer:
                    allyList = Group.CastableAlliesWithin30.Where(a => a.IsAlive && !a.IsMe && a.IsHealer()).OrderBy(GetWeight);
                    break;
            }

            if (allyList == null)
                return false;

            return await Spells.ClosedPositionPvp.CastAura(allyList.FirstOrDefault(), Auras.PvpDancePartner);
        }

        public static async Task<bool> CuringWaltz()
        {
            if (!DancerSettings.Instance.Pvp_UseCuringWaltz)
                return false;

            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.CuringWaltzPvp.CanCast())
                return false;

            var cureTargets = Group.CastableParty.Count(x => x.IsValid && x.CurrentHealthPercent < DancerSettings.Instance.Pvp_CuringWaltzHP && x.Distance(Core.Me) < 5 + x.CombatReach);

            if (Core.Me.HasAura(Auras.ClosedPosition))
            {
                var DancePartner = Group.CastableParty.FirstOrDefault(x => x.HasMyAura(Auras.DancePartner));

                if (DancePartner != null)
                    cureTargets += Group.CastableParty.Count(x => x.IsValid && x.CurrentHealthPercent < DancerSettings.Instance.Pvp_CuringWaltzHP && x.Distance(DancePartner) < 5 + x.CombatReach);
            }

            if (cureTargets < 1)
                return false;

            return await Spells.CuringWaltzPvp.Cast(Core.Me.CurrentTarget);
        }

        private static int GetWeight(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Reaper:
                    return DancerSettings.Instance.Pvp_RprPartnerWeight;

                case ClassJobType.Sage:
                    return DancerSettings.Instance.Pvp_SgePartnerWeight;

                case ClassJobType.Astrologian:
                    return DancerSettings.Instance.Pvp_AstPartnerWeight;

                case ClassJobType.Monk:
                case ClassJobType.Pugilist:
                    return DancerSettings.Instance.Pvp_MnkPartnerWeight;

                case ClassJobType.BlackMage:
                case ClassJobType.Thaumaturge:
                    return DancerSettings.Instance.Pvp_BlmPartnerWeight;

                case ClassJobType.Dragoon:
                case ClassJobType.Lancer:
                    return DancerSettings.Instance.Pvp_DrgPartnerWeight;

                case ClassJobType.Samurai:
                    return DancerSettings.Instance.Pvp_SamPartnerWeight;

                case ClassJobType.Machinist:
                    return DancerSettings.Instance.Pvp_MchPartnerWeight;

                case ClassJobType.Summoner:
                case ClassJobType.Arcanist:
                    return DancerSettings.Instance.Pvp_SmnPartnerWeight;

                case ClassJobType.Bard:
                case ClassJobType.Archer:
                    return DancerSettings.Instance.Pvp_BrdPartnerWeight;

                case ClassJobType.Ninja:
                case ClassJobType.Rogue:
                    return DancerSettings.Instance.Pvp_NinPartnerWeight;

                case ClassJobType.RedMage:
                    return DancerSettings.Instance.Pvp_RdmPartnerWeight;

                case ClassJobType.Dancer:
                    return DancerSettings.Instance.Pvp_DncPartnerWeight;

                case ClassJobType.Paladin:
                case ClassJobType.Gladiator:
                    return DancerSettings.Instance.Pvp_PldPartnerWeight;

                case ClassJobType.Warrior:
                case ClassJobType.Marauder:
                    return DancerSettings.Instance.Pvp_WarPartnerWeight;

                case ClassJobType.DarkKnight:
                    return DancerSettings.Instance.Pvp_DrkPartnerWeight;

                case ClassJobType.Gunbreaker:
                    return DancerSettings.Instance.Pvp_GnbPartnerWeight;

                case ClassJobType.WhiteMage:
                case ClassJobType.Conjurer:
                    return DancerSettings.Instance.Pvp_WhmPartnerWeight;

                case ClassJobType.Scholar:
                    return DancerSettings.Instance.Pvp_SchPartnerWeight;

                case ClassJobType.Viper:
                    return DancerSettings.Instance.Pvp_VprPartnerWeight;

                case ClassJobType.Pictomancer:
                    return DancerSettings.Instance.Pvp_PctPartnerWeight;
            }

            return c.CurrentJob == ClassJobType.Adventurer ? 70 : 0;
        }
    }
}

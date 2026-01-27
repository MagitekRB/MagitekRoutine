using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Astrologian;
using Magitek.Logic.Roles;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Threading.Tasks;
using static Magitek.Utilities.Routines.Astrologian;

namespace Magitek.Rotations
{
    public static class Astrologian
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < AstrologianSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await Cards.Draw();
        }

        public static async Task<bool> Pull()
        {
            if (Globals.InParty && Utilities.Combat.Enemies.Count > AstrologianSettings.Instance.StopDamageWhenMoreThanEnemies)
                return false;

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            //LimitBreak
            if (Heals.ForceLimitBreak()) return true;

            if (await Heals.Ascend()) return true;
            if (await Dispel.Execute()) return true;

            if (await HealFightLogic.Aoe()) return true;
            if (await HealFightLogic.Tankbuster()) return true;
            if (await CommonFightLogic.FightLogic_Knockback(AstrologianSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            if (AstrologianSettings.Instance.WeaveOGCDHeals && GlobalCooldown.CanWeave(1))
            {
                if (await Buff.Divination()) return true;
                if (await Buff.LucidDreaming()) return true;
                if (await Buff.Lightspeed()) return true;
                if (await Buff.NeutralSect()) return true;
                if (await Cards.Draw()) return true;
                if (await Cards.PlayCards()) return true;
            }

            if (Globals.InActiveDuty || Core.Me.InCombat)
            {
                if (AstrologianSettings.Instance.WeaveOGCDHeals && GlobalCooldown.CanWeave(1))
                {
                    if (await Heals.Macrocosmos()) return true;
                    if (await Heals.EarthlyStar()) return true;
                    if (await Heals.CollectiveUnconscious()) return true;
                    if (await Heals.LadyOfCrowns()) return true;
                    if (await Heals.CelestialOpposition()) return true;
                    if (await Heals.HoroscopePop()) return true;
                    if (await Buff.Synastry()) return true;
                    if (await Heals.EssentialDignity()) return true;
                    if (await Heals.CelestialIntersection()) return true;
                    if (await Heals.Horoscope()) return true;
                    if (await Heals.Exaltation()) return true;
                    if (await Aoe.LordOfCrown()) return true;
                    if (await Cards.Draw()) return true;
                    if (await Cards.PlayCards()) return true;
                }

                if (await Heals.AspectedHelios()) return true;
                if (await Heals.Helios()) return true;
                if (await Heals.AspectedBenefic()) return true;
                if (await Heals.Benefic2()) return true;
                if (await Heals.Benefic()) return true;
                if (await Heals.DontLetTheDrkDie()) return true;
            }

            return await HealAlliance();
        }

        public static async Task<bool> HealAlliance()
        {
            if (Group.CastableAlliance.Count == 0)
                return false;

            Group.SwitchCastableToAlliance();
            var res = await DoHeal();
            Group.SwitchCastableToParty();
            return res;

            async Task<bool> DoHeal()
            {
                if (await Heals.Ascend()) return true;

                if (AstrologianSettings.Instance.HealAllianceOnlyBenefic)
                {
                    return await Heals.Benefic();
                }

                if (await Heals.EssentialDignity()) return true;
                if (await Heals.Benefic2()) return true;
                if (await Heals.Benefic()) return true;
                return await Heals.AspectedBenefic();
            }
        }

        public static async Task<bool> CombatBuff()
        {
            if (AstrologianSettings.Instance.WeaveOGCDHeals && GlobalCooldown.CanWeave(1))

            {
                if (await Buff.LucidDreaming()) return true;
                if (await Buff.Lightspeed()) return true;
                if (await Buff.SunSign()) return true;
                if (await Buff.Divination()) return true;
                if (await Buff.NeutralSect()) return true;
                if (await Aoe.Oracle()) return true;
                if (await Cards.Draw()) return true;
                if (await Cards.PlayCards()) return true;


            }

            if (Globals.InActiveDuty || Core.Me.InCombat)
            {
                if (AstrologianSettings.Instance.WeaveOGCDHeals && GlobalCooldown.CanWeave(1))
                {
                    if (await Heals.Macrocosmos()) return true;
                    if (await Heals.EarthlyStar()) return true;
                    if (await Heals.CollectiveUnconscious()) return true;
                    if (await Heals.LadyOfCrowns()) return true;
                    if (await Heals.CelestialOpposition()) return true;
                    if (await Heals.HoroscopePop()) return true;
                    if (await Heals.Horoscope()) return true;
                    if (await Buff.Synastry()) return true;
                    if (await Heals.EssentialDignity()) return true;
                    if (await Heals.CelestialIntersection()) return true;
                    if (await Heals.Exaltation()) return true;
                    if (await Aoe.LordOfCrown()) return true;
                    if (await Cards.Draw()) return true;
                    if (await Cards.PlayCards()) return true;
                }

            }
            return false;
        }

        public static async Task<bool> Combat()
        {
            await CombatBuff();

            if (Globals.InParty)
            {
                if (Utilities.Combat.Enemies.Count > AstrologianSettings.Instance.StopDamageWhenMoreThanEnemies)
                    return false;

                if (Core.Me.CurrentManaPercent < AstrologianSettings.Instance.MinimumManaPercentToDoDamage
                    && Core.Target.CombatTimeLeft() > AstrologianSettings.Instance.DoDamageIfTimeLeftLessThan)
                    return false;
            }

            if (!AstrologianSettings.Instance.DoDamage)
                return false;

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (await Aoe.AggroAst()) return true;
            //if (await Aoe.LordOfCrown()) return true;
            if (await Aoe.Gravity()) return true;
            if (await SingleTarget.Combust()) return true;
            if (await SingleTarget.CombustMultipleTargets()) return true;
            return await SingleTarget.Malefic();
        }

        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(AstrologianSettings.Instance)) return true;

            // Limit Break
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.CelestialRiverPvp()) return true;
            }

            // Healing
            if (await Pvp.MacrocosmosPvp()) return true;
            if (await Pvp.AspectedBeneficPvp()) return true;

            // Special Actions
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.MinorArcanaPvp()) return true;
                if (await Pvp.OraclePvp()) return true;
            }

            // Damage
            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(AstrologianSettings.Instance))
            {
                if (await Pvp.DoubleCastPvp()) return true;
                if (await Pvp.GravityIIPvp()) return true;
            }

            return await Pvp.FallMaleficPvp();
        }
    }
}

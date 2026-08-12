using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Scholar;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Scholar
{
    internal static class HealFightLogic
    {
        public static async Task<bool> Aoe()
        {
            if (!Globals.InParty)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (FightLogic.EnemyIsCastingBigAoe() || FightLogic.EnemyIsCastingAoe())
            {
                if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (await BigAoe())
                    return true;
                if (await JustAoe())
                    return true;
            }

            return false;
        }

        private static async Task<bool> BigAoe()
        {
            // Dawntrail Fix: Dynamic Succor scaling
            var succorSpell = Spells.Concitation.IsKnown() ? Spells.Concitation : Spells.Succor;
            if (Core.Me.HasAura(Auras.Seraphism) && Spells.Accession.IsKnown())
                succorSpell = Spells.Accession;

            if (!succorSpell.IsKnownAndReady())
                return false;

            var enemyTarget = (Character)Core.Me.CurrentTarget;
            var castTimeRemaining = (int)enemyTarget.SpellCastInfo.RemainingCastTime.TotalMilliseconds;

            if (enemyTarget.SpellCastInfo.RemainingCastTime <= succorSpell.AdjustedCastTime)
                return false;

            if (ScholarSettings.Instance.FightLogicAdloDeployBigAoe &&
                Spells.DeploymentTactics.IsKnownAndReady())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Deploy Adlo");

                var target = Group.CastableParty.FirstOrDefault(x => x.HasAura(Auras.Catalyze, true, castTimeRemaining + 1000));

                if (target == null) target = Group.CastableParty.FirstOrDefault(x => x.HasAura(Auras.Galvanize, true, castTimeRemaining + 1000));

                if (target == null)
                {
                    target = Core.Me;

                    if (Spells.Recitation.IsKnownAndReady())
                    {
                        if (await Spells.Recitation.Cast(Core.Me))
                            await Coroutine.Wait(2500, () => Core.Me.HasAura(Auras.Recitation, true));
                    }

                    var adloSpell = Core.Me.HasAura(Auras.Seraphism) && Spells.Manifestation.IsKnown() ? Spells.Manifestation : Spells.Adloquium;

                    if (!await adloSpell.Cast(target))
                        return false;
                }

                return await FightLogic.DoAndBuffer(Spells.DeploymentTactics.Cast(target));
            }

            if (ScholarSettings.Instance.FightLogicRecitSuccorBigAoe &&
                Spells.Recitation.IsKnownAndReady() &&
                !Core.Me.HasAura(Auras.EmergencyTactics) &&
                Group.CastableParty.Count(x => x.HasAura(Auras.Galvanize)) < AoeThreshold)
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Recitation Succor");

                if (Spells.Recitation.IsKnownAndReady())
                {
                    if (await Spells.Recitation.Cast(Core.Me))
                        await Coroutine.Wait(2500, () => Core.Me.HasAura(Auras.Recitation, true));
                }

                return await FightLogic.DoAndBuffer(succorSpell.Cast(Core.Me));
            }

            if (ScholarSettings.Instance.FightLogicSoilBigAoe &&
                Spells.SacredSoil.IsKnownAndReady() &&
                Core.Me.HasAetherflow())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Sacred Soil");

                Character target = Core.Me;

                if (ScholarSettings.Instance.SacredSoilCenterParty)
                {
                    var targets = Group.CastableAlliesWithin30.OrderBy(r =>
                        Group.CastableAlliesWithin30.Sum(ot => r.Distance(ot.Location))
                    ).ThenBy(t => Core.Me.Distance(t.Location));

                    target = targets.FirstOrDefault(Core.Me);
                }

                return await FightLogic.DoAndBuffer(Spells.SacredSoil.Cast(target));
            }

            if (ScholarSettings.Instance.FightLogicSuccorAoe &&
                succorSpell.IsKnownAndReady() &&
                Group.CastableParty.Count(x => x.HasAura(Auras.Galvanize)) < AoeThreshold)
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AOE Response] Cast Succor");

                return await FightLogic.DoAndBuffer(succorSpell.Cast(Core.Me));
            }
            return false;
        }

        private static async Task<bool> JustAoe()
        {
            if (!ScholarSettings.Instance.FightLogicSuccorAoe) return false;

            var succorSpell = Spells.Concitation.IsKnown() ? Spells.Concitation : Spells.Succor;
            if (Core.Me.HasAura(Auras.Seraphism) && Spells.Accession.IsKnown())
                succorSpell = Spells.Accession;

            if (!succorSpell.IsKnownAndReady())
                return false;

            var enemyTarget = (Character)Core.Me.CurrentTarget;
            if (enemyTarget.SpellCastInfo.RemainingCastTime <= succorSpell.AdjustedCastTime)
            {
                return false;
            }

            if (Core.Me.HasAura(Auras.EmergencyTactics))
                return false;

            if (await FightLogic.DoAndBuffer(succorSpell.Heal(Core.Me)))
                return await Coroutine.Wait(2500,
                    () => Casting.LastSpell == succorSpell || MovementManager.IsMoving);

            return false;
        }

        public static async Task<bool> Tankbuster()
        {
            if (!Globals.InParty)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            var target = FightLogic.EnemyIsCastingTankBuster();

            if (target == null)
                return false;

            if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                return false;

            if (!target.BeingTargetedBy(Core.Me.CurrentTarget))
            {
                while (Group.CastableTanks.Any(r => !r.HasAura(Auras.Galvanize)))
                {
                    var adloSpell = Core.Me.HasAura(Auras.Seraphism) && Spells.Manifestation.IsKnown() ? Spells.Manifestation : Spells.Adloquium;
                    await FightLogic.DoAndBuffer(
                        adloSpell.Heal(Group.CastableTanks.FirstOrDefault(r => !r.HasAura(Auras.Galvanize))));

                    await Coroutine.Yield();
                }

                return true;
            }

            if (ScholarSettings.Instance.FightLogicExcogTank &&
                Spells.Excogitation.IsKnownAndReady() &&
                Core.Me.HasAetherflow() &&
                !target.HasAura(Auras.Excogitation))
                return await FightLogic.DoAndBuffer(Spells.Excogitation.CastAura(target, Auras.Excogitation));

            var singleTargetShield = Core.Me.HasAura(Auras.Seraphism) && Spells.Manifestation.IsKnown() ? Spells.Manifestation : Spells.Adloquium;

            if (ScholarSettings.Instance.FightLogicAdloTank &&
                singleTargetShield.IsKnownAndReady() &&
                !target.HasAura(Auras.Galvanize))
                return await FightLogic.DoAndBuffer(singleTargetShield.HealAura(target, Auras.Galvanize));

            return false;
        }

        public static int AoeThreshold => PartyManager.NumMembers > 4 ? ScholarSettings.Instance.AoeNeedHealingFullParty : ScholarSettings.Instance.AoeNeedHealingLightParty;

    }
}

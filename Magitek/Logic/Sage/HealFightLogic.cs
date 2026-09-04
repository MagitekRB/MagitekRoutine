using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Sage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Sage;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Sage
{
    internal static class HealFightLogic
    {
        public static async Task<bool> Aoe()
        {
            if (!Globals.InParty)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (!FightLogic.EnemyIsCastingBigAoe() && !FightLogic.EnemyIsCastingAoe())
                return false;

            if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                return false;

            var useAoEBuffs = Heal.UseAoEHealingBuff(Group.CastableAlliesWithin20);

            if (SageSettings.Instance.FightLogic_Kerachole
                && Spells.Kerachole.IsKnownAndReady()
                && Addersgall >= 1
                && useAoEBuffs)
            {
                var targets = Group.CastableAlliesWithin20.Where(r => !r.HasAura(Auras.Kerachole) && !r.HasAura(Auras.Taurochole));
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || !SageSettings.Instance.KeracholeOnlyWithTank
                    || targets.Any(r => r.IsTank(SageSettings.Instance.KeracholeOnlyWithMainTank));

                if (targets.Count() >= Heal.AoeNeedHealing &&
                    tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        FightLogic.LogThrottled($"[AOE Response] Attempting Kerachole");
                    return await FightLogic.DoAndBuffer(Spells.Kerachole.CastAura(Core.Me, Auras.Kerachole));
                }
            }

            if (SageSettings.Instance.FightLogic_Panhaima
                && Spells.Panhaima.IsKnownAndReady()
                && useAoEBuffs)
            {
                //Radius is now 30y
                var targets = Group.CastableAlliesWithin30.Where(r => !r.HasAura(Auras.Panhaimatinon));
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || !SageSettings.Instance.PanhaimaOnlyWithTank
                    || targets.Any(r => r.IsTank(SageSettings.Instance.PanhaimaOnlyWithMainTank));

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        FightLogic.LogThrottled($"[AOE Response] Attempting Panhaima");
                    return await FightLogic.DoAndBuffer(Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon));
                }
            }

            if (SageSettings.Instance.FightLogic_Holos
                && Spells.Holos.IsKnownAndReady()
                && useAoEBuffs)
            {
                //Radius is now 30y
                var targets = Group.CastableAlliesWithin30.Where(r => !r.HasAura(Auras.Holos));
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || !SageSettings.Instance.HolosTankOnly
                    || targets.Any(r => r.IsTank(SageSettings.Instance.HolosMainTankOnly));

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        FightLogic.LogThrottled($"[AOE Response] Attempting Holos");
                    return await FightLogic.DoAndBuffer(Spells.Holos.CastAura(Core.Me, Auras.Holos));
                }
            }

            if (SageSettings.Instance.FightLogic_EukrasianPrognosis
                && Spells.Eukrasia.IsKnown()
                && Heal.IsEukrasiaReady())
            {
                var targets = Group.CastableAlliesWithin20.Where(r => !r.HasPrimaryShield());
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || targets.Any(r => r.IsTank());

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        FightLogic.LogThrottled($"[AOE Response] Attempting Eukrasian Prognosis");
                    if (await Heal.UseEukrasia(Spells.EukrasianPrognosis.Id))
                        return await FightLogic.DoAndBuffer(Spells.EukrasianPrognosis.HealAura(Core.Me, Auras.EukrasianPrognosis));
                }

            }

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
            {
                target = FightLogic.EnemyIsCastingSharedTankBuster();

                if (target == null)
                    return false;
            }

            if (!FightLogic.HodlCastTimeRemaining(hodlTillDurationInPct: BaseSettings.Instance.FightLogicResponseDelay))
                return false;

            if (SageSettings.Instance.FightLogic_Haima
                && Spells.Haima.IsKnownAndReady()
                && !target.HasAura(Auras.Haimatinon)
                && !target.HasAura(Auras.Panhaimatinon)
                && Spells.Haima.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[TankBuster Response] Attempting Haima on {target.CurrentJob}");
                return await FightLogic.DoAndBuffer(Spells.Haima.CastAura(target, Auras.Haimatinon));
            }

            if (SageSettings.Instance.FightLogic_Taurochole
                && Spells.Taurochole.IsKnownAndReady()
                && !target.HasAura(Auras.Taurochole)
                && Spells.Taurochole.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[TankBuster Response] Attempting Taurochole on {target.CurrentJob}");
                return await FightLogic.DoAndBuffer(Spells.Taurochole.HealAura(target, Auras.Taurochole));
            }

            if (SageSettings.Instance.FightLogic_EukrasianDiagnosis
                && Spells.Eukrasia.IsKnown()
                && !target.HasPrimaryShield()
                && Heal.IsEukrasiaReady())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    FightLogic.LogThrottled($"[TankBuster Response] Attempting Eukrasian Diagnosis on {target.CurrentJob}");
                if (await Heal.UseEukrasia(targetObject: target))
                    return await FightLogic.DoAndBuffer(Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis));
            }

            return false;
        }
    }
}

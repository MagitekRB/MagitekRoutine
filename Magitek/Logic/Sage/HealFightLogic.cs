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

            // One attempt window per second: the detector keeps the mechanic open, so
            // pacing costs nothing but the pulse-rate retry storm a failed cast produced.
            if (System.Environment.TickCount64 - global::Magitek.Utilities.Routines.Sage.LastAoeResponseAttemptTick < 1000)
                return false;
            global::Magitek.Utilities.Routines.Sage.LastAoeResponseAttemptTick = System.Environment.TickCount64;

            var useAoEBuffs = Heal.UseAoEHealingBuff(Group.CastableAlliesWithin20);

            if (SageSettings.Instance.FightLogic_Kerachole
                && Spells.Kerachole.IsKnownAndReady()
                && Addersgall >= 1
                && useAoEBuffs)
            {
                //Radius is 30y, same as Panhaima and Holos below - a 20y sample loses allies
                //the mitigation would have covered, and the tank check below reads the same set.
                var targets = Group.CastableAlliesWithin30.Where(r => !r.HasAura(Auras.Kerachole) && !r.HasAura(Auras.Taurochole));
                // The trailing clause waives the tank requirement when the party has no
                // castable tank at all: a tankless light party (common in field operations)
                // otherwise loses this barrier entirely - field-observed 2026-08-29, a
                // raidwide went unanswered with every barrier ready.
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || !SageSettings.Instance.KeracholeOnlyWithTank
                    || targets.Any(r => r.IsTank(SageSettings.Instance.KeracholeOnlyWithMainTank))
                    || !Group.CastableTanks.Any();

                if (targets.Count() >= Heal.AoeNeedHealing &&
                    tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        Logger.WriteInfo($"[AOE Response] Cast Kerachole");
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
                    || targets.Any(r => r.IsTank(SageSettings.Instance.PanhaimaOnlyWithMainTank))
                    || !Group.CastableTanks.Any(); // tankless party: see Kerachole above

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        Logger.WriteInfo($"[AOE Response] Cast Panhaima");
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
                    || targets.Any(r => r.IsTank(SageSettings.Instance.HolosMainTankOnly))
                    || !Group.CastableTanks.Any(); // tankless party: see Kerachole above

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    if (BaseSettings.Instance.DebugFightLogic)
                        Logger.WriteInfo($"[AOE Response] Cast Holos");
                    return await FightLogic.DoAndBuffer(Spells.Holos.CastAura(Core.Me, Auras.Holos));
                }
            }

            if (SageSettings.Instance.FightLogic_EukrasianPrognosis
                && Spells.Eukrasia.IsKnown()
                && Heal.IsEukrasiaReady())
            {
                var targets = Group.CastableAlliesWithin20.Where(r => !r.HasPrimaryShield());
                var tankCheck = !SageSettings.Instance.FightLogic_RespectOnlyTank
                    || targets.Any(r => r.IsTank())
                    || !Group.CastableTanks.Any(); // tankless party: see Kerachole above

                if (targets.Count() >= Heal.AoeNeedHealing
                    && tankCheck)
                {
                    var prognosis = Heal.EukrasianPrognosisSpell;

                    if (await Heal.UseEukrasia(prognosis.Id))
                    {
                        // After the arm succeeds, not before: printed ahead of the attempt,
                        // this line claimed a cast on every retry of the storm.
                        if (BaseSettings.Instance.DebugFightLogic)
                            Logger.WriteInfo($"[AOE Response] Cast Eukrasian Prognosis");
                        return await FightLogic.DoAndBuffer(prognosis.HealAura(Core.Me, Auras.EukrasianPrognosis));
                    }
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

            // One attempt window per second: the detector keeps the mechanic open, so
            // pacing costs nothing but the pulse-rate retry storm a failed cast produced.
            if (System.Environment.TickCount64 - global::Magitek.Utilities.Routines.Sage.LastTankbusterResponseAttemptTick < 1000)
                return false;
            global::Magitek.Utilities.Routines.Sage.LastTankbusterResponseAttemptTick = System.Environment.TickCount64;

            if (SageSettings.Instance.FightLogic_Haima
                && Spells.Haima.IsKnownAndReady()
                && !target.HasAura(Auras.Haimatinon)
                && Spells.Haima.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Haima on {target.Name}");
                return await FightLogic.DoAndBuffer(Spells.Haima.CastAura(target, Auras.Haimatinon));
            }

            if (SageSettings.Instance.FightLogic_Taurochole
                && Spells.Taurochole.IsKnownAndReady()
                && !target.HasAura(Auras.Taurochole)
                && Spells.Taurochole.CanCast(target))
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Taurochole on {target.Name}");
                return await FightLogic.DoAndBuffer(Spells.Taurochole.HealAura(target, Auras.Taurochole));
            }

            // The two branches above guard reachability; this one did not. MatchTankBuster's
            // preferred tier draws from Group.CastableTanks, which carries no distance filter, so a
            // tank sent out for a mechanic would spend a real Eukrasia GCD on a cast that cannot land.
            // Range check, not CanCast: the Eukrasian action only becomes castable after Eukrasia
            // is armed, so a pre-arm CanCast is always false and would kill this branch outright.
            if (SageSettings.Instance.FightLogic_EukrasianDiagnosis
                && Spells.Eukrasia.IsKnown()
                && !target.HasPrimaryShield()
                && target.WithinSpellRange(30)
                && Heal.IsEukrasiaReady())
            {
                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[TankBuster Response] Cast Eukrasian Diagnosis on {target.Name}");
                if (await Heal.UseEukrasia(targetObject: target))
                    return await FightLogic.DoAndBuffer(Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis));
            }

            return false;
        }
    }
}

using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Utilities.Routines
{
    internal static class Ninja
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Ninja, Spells.SpinningEdge, new List<SpellData>() { Spells.Ten, Spells.Jin, Spells.Chi, Spells.Ninjutsu });

        public static int AoeEnemies4Yards;
        public static int AoeEnemies5Yards;
        public static int AoeEnemies6Yards;

        private static bool TenChiJin = false;

        public static List<SpellData> UsedMudras = new List<SpellData>();
        public static int OpenerBurstAfterGCD = 2;

        private static readonly List<SpellData> Mudras = new List<SpellData>() { Spells.Ten, Spells.Jin, Spells.Chi };

        public static DateTime oGCD = DateTime.Now;

        // How close Trick Attack's cooldown has to be before we report the burst as
        // imminent. The base action (2258) masks to Kunai's Bane at 92 and its
        // cooldown tracks through the mask.
        private const int TrickAttackImminentMs = 5000;

        /// <summary>
        /// Reports NIN burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the NIN rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Kunai's Bane / Trick Attack (own 10% target debuff, 15s,
        /// every 60s; Dokumori/TCJ/Bunshin ride the 2-minute ones). The mudra state
        /// and Ten Chi Jin also report — any foreign action mid-mudra destroys the
        /// ninjutsu (Rabbit Medium) and any non-TCJ action forfeits Ten Chi Jin,
        /// burst or not. Since 7.1 movement no longer cancels TCJ. Window contents:
        /// Kunai's Bane, Dokumori, Ten Chi Jin, Meisui, Bunshin, Bhavacakra/Zesho
        /// Meppo spam, Raiju chain, Tenri Jindo.
        /// Sources: The Balance NIN basic guide, official job guide, consolegameswiki
        /// Ninjutsu, Lodestone 7.1 notes, live client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Mudra state: any foreign action mid-mudra destroys the ninjutsu
            // (Rabbit Medium), so this reports regardless of burst timing —
            // deliberate, same class as DNC's dancing state. UsedMudras bridges the
            // button-press-to-aura latency.
            var mudra = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.Mudra && x.CasterId == Core.Me.ObjectId);
            if (mudra != null || UsedMudras.Count > 0)
            {
                // One GCD-ish placeholder while only UsedMudras indicates; the aura
                // lands within a server tick of the press.
                RoutineState.ReportBurstWindow(mudra?.TimespanLeft ?? TimeSpan.FromMilliseconds(1500), "NIN Mudra");
                return;
            }

            // Ten Chi Jin: a non-TCJ action forfeits a 120s cooldown plus Tenri
            // Jindo. Cast-protection like the mudra state, so it shares its source.
            var tcj = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.TenChiJin && x.CasterId == Core.Me.ObjectId);
            if (tcj != null)
            {
                RoutineState.ReportBurstWindow(tcj.TimespanLeft, "NIN Mudra");
                return;
            }

            // Burst envelope: our own Kunai's Bane (Trick Attack pre-92) debuff on
            // the current target. 15s per 60s; the 1-min/2-min distinction doesn't
            // change the envelope.
            if (Core.Me.CurrentTarget is Character target && target.IsValid)
            {
                var remaining = TimeSpan.Zero;
                foreach (var aura in target.CharacterAuras)
                {
                    if (aura.CasterId != Core.Me.ObjectId)
                        continue;

                    if (aura.Id != Auras.KunaisBane && aura.Id != Auras.TrickAttack)
                        continue;

                    if (aura.TimespanLeft > remaining)
                        remaining = aura.TimespanLeft;
                }

                if (remaining > TimeSpan.Zero)
                {
                    RoutineState.ReportBurstWindow(remaining, "NIN Kunai's Bane");
                    return;
                }
            }

            // Trick Attack almost off cooldown: the debuff is about to go out, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.TrickAttack.IsKnown())
            {
                var cooldownMs = Spells.TrickAttack.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= TrickAttackImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "NIN Kunai's Bane");
            }
        }

        public static async Task<bool> PrepareNinjutsu(SpellData endMudra, int ninjustsuLength, GameObject target)
        {

            if (UsedMudras.Count < ninjustsuLength)
            {

                if (UsedMudras.Count < ninjustsuLength - 1)
                {
                    List<SpellData> availableMudras = Mudras.FindAll(x => x != endMudra && !UsedMudras.Contains(x) && x.IsKnown());

                    if (await availableMudras[new Random().Next(availableMudras.Count)].Cast(Core.Me))
                    {
                        await Casting.CheckForSuccessfulCast();
                        UsedMudras.Add(Casting.SpellCastHistory.First().Spell);
                        return true;
                    }

                }

                else if (await endMudra.Cast(Core.Me))
                {
                    UsedMudras.Add(endMudra);
                    return true;
                }
            }

            return await Spells.Ninjutsu.Cast(target);

        }

        public static async Task<bool> PrepareNinjutsu(SpellData ninjutsu, GameObject target)
        {

            Dictionary<SpellData, SpellData> NinjutsuEndMudra = new Dictionary<SpellData, SpellData>
            {
                { Spells.FumaShuriken   , Spells.Ten },
                { Spells.Raiton         , Spells.Chi },
                { Spells.Katon          , Spells.Ten },
                //Kassatsu Ninjutsu
                { Spells.GokaMekkyaku   , Spells.Ten },
                { Spells.Hyoton         , Spells.Jin },
                //Kassatsu Ninjutsu
                { Spells.HyoshoRanryu   , Spells.Jin },
                { Spells.Suiton         , Spells.Jin },
                { Spells.Doton          , Spells.Chi },
                { Spells.Huton          , Spells.Ten }
            };

            Dictionary<SpellData, int> NinjutsuComplexity = new Dictionary<SpellData, int>
            {
                { Spells.FumaShuriken   , 1 },
                { Spells.Raiton         , 2 },
                { Spells.Katon          , 2 },
                //Kassatsu Ninjutsu
                { Spells.GokaMekkyaku   , 2 },
                { Spells.Hyoton         , 2 },
                //Kassatsu Ninjutsu
                { Spells.HyoshoRanryu   , 2 },
                { Spells.Suiton         , 3 },
                { Spells.Doton          , 3 },
                { Spells.Huton          , 3 }
            };

            if (TenChiJin || Core.Me.HasAura(Auras.TenChiJin))
            {
                NinjutsuEndMudra = new Dictionary<SpellData, SpellData>
                {
                    { Spells.FumaShuriken   , Spells.Ten },
                    { Spells.Raiton         , Spells.Chi },
                    { Spells.Suiton         , Spells.Jin }
                };

                NinjutsuComplexity = new Dictionary<SpellData, int>
                {
                    { Spells.FumaShuriken   , 1 },
                    { Spells.Raiton         , 1 },
                    { Spells.Suiton         , 1 }
                };
            }

            if (!NinjutsuEndMudra.ContainsKey(ninjutsu))
                return false;

            if (UsedMudras.Count < NinjutsuComplexity[ninjutsu])
            {

                if (UsedMudras.Count < NinjutsuComplexity[ninjutsu] - 1)
                {
                    List<SpellData> availableMudras = Mudras.FindAll(x => x != NinjutsuEndMudra[ninjutsu] && !UsedMudras.Contains(x) && x.IsKnown());

                    if (await availableMudras[new Random().Next(availableMudras.Count)].Cast(Core.Me))
                    {
                        await Casting.CheckForSuccessfulCast();
                        UsedMudras.Add(Casting.SpellCastHistory.First().Spell);
                        return true;
                    }

                }

                else if (await NinjutsuEndMudra[ninjutsu].Cast(Core.Me))
                {
                    UsedMudras.Add(NinjutsuEndMudra[ninjutsu]);
                    return true;
                }
            }

            if (TenChiJin || Core.Me.HasAura(Auras.TenChiJin))
                return await NinjutsuEndMudra[ninjutsu].Cast(target);

            return await ninjutsu.Cast(target);

        }

        public static void RefreshVars()
        {

            switch (UsedMudras.Count)
            {
                case 0:
                    break;

                case 1:

                    if (Core.Me.HasMyAura(Auras.Mudra) || Core.Me.HasMyAura(Auras.TenChiJin))
                        break;

                    if (!Core.Me.HasMyAura(Auras.TenChiJin) && !Core.Me.HasMyAura(Auras.Mudra) && new List<SpellData>() { Spells.Ten, Spells.Chi, Spells.Jin }.Contains(Casting.SpellCastHistory.First().Spell))
                        break;

                    UsedMudras.Clear();
                    break;

                case 2:

                case 3:

                    if (Core.Me.HasMyAura(Auras.Mudra))
                        break;

                    UsedMudras.Clear();
                    break;

                default:
                    break;
            }

            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            if (!TenChiJin && Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell == Spells.TenChiJin)
            {
                TenChiJin = true;
            }
            if (TenChiJin && !Core.Me.HasMyAura(Auras.TenChiJin) && Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell != Spells.TenChiJin)
            {
                TenChiJin = false;
            }

            if (Core.Me.HasAura(Auras.TenChiJin))
                TenChiJin = true;

            AoeEnemies4Yards = Core.Me.EnemiesNearby(4).Count();
            AoeEnemies5Yards = Core.Me.EnemiesNearby(5).Count();
            AoeEnemies6Yards = Core.Me.CurrentTarget.EnemiesNearby(6).Count();

        }
    }
}

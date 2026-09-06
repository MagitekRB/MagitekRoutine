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

        // The ninjutsu the current chain is being built for. Every ninjutsu method decides for itself
        // each pulse, so a chain one of them started (Raiton pressing Jin first, because Kunai's Bane
        // looked unwanted on the pull's first pulse) was carried on by another one pulse later (Suiton,
        // adding Chi and then its own end mudra, Jin) - a sequence the game answers with Rabbit Medium.
        // The ninjutsu that pressed the first mudra presses the rest.
        public static SpellData ChainNinjutsu;
        private static bool ChainTargetsSelf;

        /// <summary>
        /// Finishes the chain in progress for the ninjutsu that started it, ahead of every ninjutsu
        /// method's own gates: a gate that flips mid-chain (enemy count, the estimate, a cooldown) must
        /// not leave the mudras hanging for a weaponskill to break, or for another ninjutsu to finish
        /// in its own order.
        /// </summary>
        public static async Task<bool> ContinueChain()
        {
            if (ChainNinjutsu == null || UsedMudras.Count == 0)
                return false;

            if (TenChiJin || Core.Me.HasAura(Auras.TenChiJin))
                return false;

            var target = ChainTargetsSelf ? Core.Me : Core.Me.CurrentTarget;
            if (target == null)
                return false;

            return await PrepareNinjutsu(ChainNinjutsu, target);
        }

        // When the last mudra was pressed. A press shows up in the aura list a few hundred milliseconds
        // later, and the pulse after a press used to read "no Mudra aura" and clear the list, so the
        // next press restarted the chain on top of a mudra the game had already counted.
        private static DateTime LastMudraPressUtc = DateTime.MinValue;
        private const int MudraPressGraceMs = 1500;
        public static bool MudraPressedRecently => (DateTime.UtcNow - LastMudraPressUtc).TotalMilliseconds < MudraPressGraceMs;

        // Under Ten Chi Jin every mudra press is itself a ninjutsu with an animation lock, and a press sent
        // inside the previous one's lock replaces it in the client's queue: the Ten went missing, the Chi
        // ran as the Fuma Shuriken step, and the chain could never finish. The steps are a second apart in
        // a chain that works.
        private const int TenChiJinStepMs = 1000;

        // How long after the Ten Chi Jin press its aura may still be missing from the aura list before
        // "no aura" means it is over.
        private const int TenChiJinAuraGraceMs = 2000;

        private static async Task<bool> PressMudra(SpellData mudra, GameObject target)
        {
            if (!await mudra.Cast(target))
                return false;

            await Casting.CheckForSuccessfulCast();
            UsedMudras.Add(mudra);
            LastMudraPressUtc = DateTime.UtcNow;
            return true;
        }
        public static int OpenerBurstAfterGCD = 2;

        // True while the current pull started from a countdown, i.e. the pre-pull Suiton ramp ran and the
        // opener alignment (Dokumori on GCD 2, Kunai's Bane on GCD 4) applies. Every other pull is a
        // dungeon or field pull where the guides say to use the cooldowns as they come up. A latch with
        // an expiry rather than a flag: the pull phase between the countdown ending and combat starting
        // has no reliable place to clear a flag without also clearing it too early.
        public static bool CountdownPull => DateTime.Now < CountdownPullUntil;
        private static DateTime CountdownPullUntil = DateTime.MinValue;
        private const int CountdownPullLatchMs = 30000;

        public static void NoteCountdown()
        {
            CountdownPullUntil = DateTime.Now.AddMilliseconds(CountdownPullLatchMs);
        }

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

                    var mudra = availableMudras[new Random().Next(availableMudras.Count)];
                    if (await mudra.Cast(Core.Me))
                    {
                        await Casting.CheckForSuccessfulCast();
                        UsedMudras.Add(mudra);
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

            if (TenChiJin || Core.Me.HasAura(Auras.TenChiJin))
            {
                // Every step is recorded, so the callers' counts pick the step; hold each press until
                // the previous one has had its second.
                if (UsedMudras.Count > 0 && (DateTime.UtcNow - LastMudraPressUtc).TotalMilliseconds < TenChiJinStepMs)
                    return true;

                return await PressMudra(NinjutsuEndMudra[ninjutsu], target);
            }

            // One chain, one owner. The owner is recorded once its first press has gone out, so a first
            // press that fails (no charge) leaves no ghost owner behind.
            var firstPress = UsedMudras.Count == 0;
            if (!firstPress && ChainNinjutsu != null && ChainNinjutsu != ninjutsu)
                return false;

            if (UsedMudras.Count < NinjutsuComplexity[ninjutsu] - 1)
            {
                List<SpellData> availableMudras = Mudras.FindAll(x => x != NinjutsuEndMudra[ninjutsu] && !UsedMudras.Contains(x) && x.IsKnown());

                var mudra = availableMudras[new Random().Next(availableMudras.Count)];
                if (await PressMudra(mudra, Core.Me))
                {
                    if (firstPress)
                    {
                        ChainNinjutsu = ninjutsu;
                        ChainTargetsSelf = target == Core.Me;
                    }
                    return true;
                }
            }
            else if (UsedMudras.Count < NinjutsuComplexity[ninjutsu])
            {
                if (await PressMudra(NinjutsuEndMudra[ninjutsu], Core.Me))
                {
                    if (firstPress)
                    {
                        ChainNinjutsu = ninjutsu;
                        ChainTargetsSelf = target == Core.Me;
                    }
                    return true;
                }
            }

            if (UsedMudras.Count < NinjutsuComplexity[ninjutsu])
            {
                // The mudra was not castable this pulse (its half-second recast). Pressing the ninjutsu
                // now would execute a lesser one, and falling through to a weaponskill would break the
                // chain, so wait - briefly.
                return MudraPressedRecently;
            }

            if (!await ninjutsu.Cast(target))
                return false;

            // The chain is spent whatever the game made of it; the next one starts clean.
            UsedMudras.Clear();
            ChainNinjutsu = null;
            return true;

        }

        public static void RefreshVars()
        {

            // The list mirrors the game's mudra state, which lives in the Mudra aura (or Ten Chi Jin).
            // Neither aura up and no press in the last moment means the game has nothing: the chain was
            // spent, broken, or timed out. The old rule kept a single entry for as long as the last cast
            // was a mudra, which after a Ten Chi Jin left a phantom Ten in the list for the next chain.
            if (UsedMudras.Count > 0 && !MudraPressedRecently
                && !Core.Me.HasMyAura(Auras.Mudra) && !Core.Me.HasMyAura(Auras.TenChiJin))
            {
                UsedMudras.Clear();
                ChainNinjutsu = null;
            }

            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            if (!TenChiJin && Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell == Spells.TenChiJin)
            {
                TenChiJin = true;
            }
            // Ten Chi Jin is over once its aura is gone - also when nothing was cast after it, because the
            // steps never went out and it expired. The history test alone never noticed that case: the
            // flag stayed latched and the next ordinary chain was built through the Ten Chi Jin branch.
            // Whatever the steps recorded goes with it.
            if (TenChiJin && !Core.Me.HasMyAura(Auras.TenChiJin) && Casting.SpellCastHistory.Count() > 0
                && (Casting.SpellCastHistory.First().Spell != Spells.TenChiJin
                    || (DateTime.UtcNow - Casting.SpellCastHistory.First().TimeCastUtc).TotalMilliseconds > TenChiJinAuraGraceMs))
            {
                TenChiJin = false;
                UsedMudras.Clear();
                ChainNinjutsu = null;
            }

            if (Core.Me.HasAura(Auras.TenChiJin))
                TenChiJin = true;

            AoeEnemies4Yards = Core.Me.EnemiesNearby(4).Count();
            AoeEnemies5Yards = Core.Me.EnemiesNearby(5).Count();
            AoeEnemies6Yards = Core.Me.CurrentTarget.EnemiesNearby(6).Count();

        }
    }
}

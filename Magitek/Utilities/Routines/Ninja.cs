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

        public static bool TenChiJin = false;

        public static List<SpellData> UsedMudras = new List<SpellData>();
        public static int OpenerBurstAfterGCD = 2;

        // The ninjutsu the current chain is being built for. Every ninjutsu method decides for itself
        // each pulse, so a chain one of them started (Raiton pressing Jin first, because Kunai's Bane
        // looked unwanted on the pull's first pulse) was carried on by another one pulse later (Suiton,
        // adding Chi and then its own end mudra, Jin) - a sequence the game answers with Rabbit Medium.
        // The ninjutsu that pressed the first mudra presses the rest.
        public static SpellData ChainNinjutsu;
        public static bool ChainTargetsSelf;

        // When the last mudra was pressed. A press shows up in the aura list a few hundred milliseconds
        // later, and the pulse after a press used to read "no Mudra aura" and clear the list, so the
        // next press restarted the chain on top of a mudra the game had already counted.
        private static DateTime LastMudraPressUtc = DateTime.MinValue;
        private const int MudraPressGraceMs = 1500;
        public static bool MudraPressedRecently => (DateTime.UtcNow - LastMudraPressUtc).TotalMilliseconds < MudraPressGraceMs;
        public static double MsSinceLastMudraPress => (DateTime.UtcNow - LastMudraPressUtc).TotalMilliseconds;

        // The chain executor lives in Logic/Ninja/Ninjutsu.cs (this file never casts); it records its
        // presses and its chain here.
        public static void NoteMudraPress(SpellData mudra)
        {
            UsedMudras.Add(mudra);
            LastMudraPressUtc = DateTime.UtcNow;
        }

        public static void BeginChain(SpellData ninjutsu, bool targetsSelf)
        {
            ChainNinjutsu = ninjutsu;
            ChainTargetsSelf = targetsSelf;
        }

        public static void EndChain()
        {
            UsedMudras.Clear();
            ChainNinjutsu = null;
            LastChainEndUtc = DateTime.UtcNow;
        }

        // When the last ninjutsu went out. The ninjutsu can follow its last mudra by more than the press grace
        // when it waits on the weaponskill recast (Ten at 18:18:40.4, Goka Mekkyaku at 18:18:42.2, Windurst
        // 2026-09-17), and the status fading after it would then read as a chain the routine did not start.
        private static DateTime LastChainEndUtc = DateTime.MinValue;
        public static bool ChainEndedRecently => (DateTime.UtcNow - LastChainEndUtc).TotalMilliseconds < MudraStatusLagMs;

        // How long after the Ten Chi Jin press its aura may still be missing from the aura list before
        // "no aura" means it is over.
        private const int TenChiJinAuraGraceMs = 2000;

        // The game keeps the pressed sequence in the Mudra status value, and Ten Chi Jin keeps its steps in its own
        // status the same way: base-4 digits, first press in the low digit, Ten 1, Chi 2, Jin 3 (sampled every pulse
        // on a dummy, 2026-09-17: Chi, Ten, Jin reads 54; Ten, Jin reads 13; Ten Chi Jin reads 0, then 1, then 9).
        // The status shows one or two pulses after the press and lingers a pulse after the chain resolves, so it
        // cannot drive the chain (a press every 0.47 s would stall a pulse per step); it referees the record instead.
        // A record longer than the status inside this many milliseconds of the last press is the status catching
        // up; longer than that, the client dropped the press.
        private const int MudraStatusLagMs = 700;

        // A chain the game has given up on reads 255 in the Mudra status, not a sequence: the routine pressed Chi and
        // then a weaponskill (Windurst, 2026-09-17 18:11), the status went from 2 to 255, and the next ninjutsu press
        // would have been a Rabbit Medium. Any value the encoding cannot produce (a digit past Jin, or more than three)
        // is read the same way.
        private const int MudraStatusBroken = 255;

        /// <summary>True while the Mudra status says the chain in progress is spoiled; the next ninjutsu would be a Rabbit Medium.</summary>
        public static bool MudraChainBroken { get; private set; }

        /// <summary>
        /// The mudras the game has counted, decoded from the Mudra or Ten Chi Jin status; null when neither is up or
        /// the status reads as a broken chain.
        /// </summary>
        public static List<SpellData> MudraStatusSequence()
        {
            var aura = Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.TenChiJin && x.CasterId == Core.Me.ObjectId)
                ?? Core.Me.Auras.FirstOrDefault(x => x.Id == Auras.Mudra && x.CasterId == Core.Me.ObjectId);
            MudraChainBroken = false;
            if (aura == null)
                return null;

            var value = (int)aura.Value;
            if (value == MudraStatusBroken || value >= 64)
            {
                MudraChainBroken = true;
                return null;
            }

            var sequence = new List<SpellData>();
            for (; value > 0; value /= 4)
            {
                switch (value % 4)
                {
                    case 1: sequence.Add(Spells.Ten); break;
                    case 2: sequence.Add(Spells.Chi); break;
                    case 3: sequence.Add(Spells.Jin); break;
                    default: MudraChainBroken = true; return null;
                }
            }
            return sequence;
        }

        private static string DescribeMudras(List<SpellData> mudras) => mudras.Count == 0 ? "nothing" : string.Join(", ", mudras.Select(m => m.Name));

        // The status is the truth whenever it is up: a press the client dropped (about one in seven pressed inside an
        // animation lock, 2026-09-06) left the record one mudra long and the chain ended in the wrong ninjutsu or a
        // Rabbit Medium; a chain the routine did not start, or one it lost over a reload, had no record at all and the
        // next press repeated a mudra the game already held.
        private static void ReconcileMudrasWithStatus()
        {
            var status = MudraStatusSequence();
            if (MudraChainBroken)
            {
                if (UsedMudras.Count > 0 || ChainNinjutsu != null)
                {
                    Logger.WriteInfo("[Ninja] The game has given up on the chain (" + DescribeMudras(UsedMudras) + " were pressed); the routine lets it lapse rather than press a Rabbit Medium.");
                    UsedMudras.Clear();
                    ChainNinjutsu = null;
                }
                return;
            }

            if (status == null)
                return;

            if (status.Count == UsedMudras.Count && status.Zip(UsedMudras, (a, b) => a.Id == b.Id).All(same => same))
                return;

            // Inside the lag of a press or of a chain's end the status is still moving: a press is not yet in it, or
            // the spent chain is still fading out of it. It settles within the lag; until then nothing is read from
            // it. (Windurst 2026-09-17 19:14: the next chain's Chi pressed 116 ms after a Hyosho Ranryu, the status
            // still reading the Hyosho's Ten, Jin, and the record was rewritten to it.)
            if (MsSinceLastMudraPress < MudraStatusLagMs || ChainEndedRecently)
                return;

            // The status outlives the chain by a pulse: right after the ninjutsu goes out the record is already empty
            // and the status still reads the whole sequence. That is the chain just spent, not one the routine did
            // not start (three false corrections in ten seconds on the dummy, 2026-09-17); a foreign chain has no
            // press of the routine's behind it.
            if (UsedMudras.Count == 0 && (MudraPressedRecently || ChainEndedRecently))
                return;

            Logger.WriteInfo("[Ninja] The game counts " + DescribeMudras(status) + " where the routine had " + DescribeMudras(UsedMudras) + "; the record follows the game.");
            UsedMudras.Clear();
            UsedMudras.AddRange(status);
        }


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

            ReconcileMudrasWithStatus();

            // Ten Chi Jin is over once its aura is gone - also when nothing was cast after it, because the
            // steps never went out and it expired. The history test alone never noticed that case: the
            // flag stayed latched and the next ordinary chain was built through the Ten Chi Jin branch.
            // Whatever the steps recorded goes with it. This runs out of combat as well: a Ten Chi Jin cut
            // short by the target dying (one step in, Windurst 2026-09-17 18:11) left the flag set through
            // the walk to the next pull, whose first press then went out as a Ten Chi Jin step and was
            // abandoned a pulse later, and the game marked the chain broken.
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

            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            if (!TenChiJin && Casting.SpellCastHistory.Count() > 0 && Casting.SpellCastHistory.First().Spell == Spells.TenChiJin)
            {
                TenChiJin = true;
            }

            AoeEnemies4Yards = Core.Me.EnemiesNearby(4).Count();
            AoeEnemies5Yards = Core.Me.EnemiesNearby(5).Count();
            AoeEnemies6Yards = Core.Me.CurrentTarget.EnemiesNearby(6).Count();

        }
    }
}

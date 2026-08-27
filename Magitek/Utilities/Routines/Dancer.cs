using ff14bot;
using ff14bot.Enums;
using Magitek.Extensions;
using System;

namespace Magitek.Utilities.Routines
{
    internal static class Dancer
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Dancer, Spells.Cascade);

        // How close Technical Step's cooldown has to be before we report the burst as
        // imminent. The Balance's own alignment rule (hold Standard Step if under ~6s
        // before Technical) supports ~5s.
        private const int TechnicalStepImminentMs = 5000;

        /// <summary>
        /// Reports DNC burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the DNC rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Technical Finish (20s, every 2 minutes) with Devilment
        /// inside/outlasting it; the dancing state (Technical or Standard Step) is
        /// also reported — dancing restricts the player to step actions, so any
        /// interjection wastes dance time, including non-burst 1-minute Standard
        /// Steps (deliberate). Window contents: Technical Step -> Technical Finish,
        /// Devilment, Flourish procs, Tillana, Dance of the Dawn, Last Dance,
        /// Starfall Dance, Saber Dance. Sources: The Balance DNC basic guide (7.40),
        /// official job guide, live client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Window: own-cast Technical Finish or Devilment (both land on others too —
            // Finish on the whole party, Devilment on the dance partner — so the
            // own-cast filter is mandatory), or the dancing state itself. The dance
            // auras are self-only by nature, so no caster filter there. Dancing
            // reports as a window on purpose even during non-burst 1-minute Standard
            // Steps (~3.5s): the game restricts a dancing player to step actions, so
            // a foreign interjection is rejected or wastes dance time. Window ends
            // when the last term drops, hence the max remaining. Under level sync
            // missing auras simply never appear — no level branching needed.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.Id == Auras.TechnicalStep || aura.Id == Auras.StandardStep)
                {
                    if (aura.TimespanLeft > remaining)
                        remaining = aura.TimespanLeft;
                    continue;
                }

                if (aura.Id != Auras.TechnicalFinish && aura.Id != Auras.Devilment)
                    continue;

                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "DNC Technical");
                return;
            }

            // Technical Step almost off cooldown: the 2-minute window is about to
            // open, so nothing slow should start now. Only reported while it is
            // actually cooling down — "ready but held" is unbounded and would starve
            // consumers.
            if (Core.Me.InCombat && Spells.TechnicalStep.IsKnown())
            {
                var cooldownMs = Spells.TechnicalStep.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= TechnicalStepImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "DNC Technical");
            }
        }
    }
}

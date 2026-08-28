using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using System;

namespace Magitek.Utilities.Routines
{
    internal static class DarkKnight
    {
        public static WeaveWindow GlobalCooldown
            = new WeaveWindow(ClassJobType.DarkKnight, Spells.HardSlash);

        // How close Delirium's (or Living Shadow's) cooldown has to be before we
        // report the burst as imminent. Covers the pre-window GCDs where a slow
        // foreign cast would delay the press and drift the 60s loop.
        private const int DeliriumImminentMs = 5000;

        /// <summary>
        /// Reports DRK burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the DRK rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Delirium (15s/3 stacks, every 60s; Living Shadow leads it by
        /// 1-2 GCDs in the 2-minute window, covered via its cooldown elapsed).
        /// Window contents: Delirium -> Scarlet Delirium/Comeuppance/Torcleaver,
        /// Living Shadow, Disesteem, Shadowbringer x2, Carve and Spit, Salted Earth +
        /// Salt and Darkness, Edge of Shadow spends. Sensitivity is moderate — DRK
        /// has no personal damage-percent buff; the cost is raid-buff misalignment.
        /// Sources: The Balance DRK basic guide, official job guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Window = union (max remaining) of the auras the Delirium press grants.
            // Two Delirium status records exist (1972 for Lv68-95, 3836 for the
            // Lv96+ Scarlet chain per the sheet); which applies at 100 is unverified,
            // so check both. Blood Weapon (742) rides the same press. Own-cast
            // filter throughout. Deliberately excluded: Scorn (30s — blows the
            // bound) and Salted Earth (unaligned ground DoT).
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.Delirium && aura.Id != Auras.EnhancedDelirium && aura.Id != Auras.BloodWeapon)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            // Living Shadow has no aura; derive its window head from the cooldown.
            // It is summoned at window start (Esteem's own damage is script-fixed) —
            // this term only covers the 1-2 GCDs before Delirium lands in the
            // 2-minute window: within 20s of the 120s cooldown starting.
            if (Spells.LivingShadow.IsKnown() && Spells.LivingShadow.Cooldown.TotalSeconds > 100)
            {
                var shadowRemaining = Spells.LivingShadow.Cooldown - TimeSpan.FromSeconds(100);
                if (shadowRemaining > remaining)
                    remaining = shadowRemaining;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "DRK Delirium");
                return;
            }

            // Delirium or Living Shadow almost off cooldown: the window is about to
            // open, so nothing slow should start now. Only reported while actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (!Core.Me.InCombat)
                return;

            var imminentMs = double.MaxValue;

            if (Spells.Delirium.IsKnown())
            {
                var cooldownMs = Spells.Delirium.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= DeliriumImminentMs)
                    imminentMs = cooldownMs;
            }

            if (Spells.LivingShadow.IsKnown())
            {
                var cooldownMs = Spells.LivingShadow.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= DeliriumImminentMs && cooldownMs < imminentMs)
                    imminentMs = cooldownMs;
            }

            if (imminentMs < double.MaxValue)
                RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(imminentMs), "DRK Delirium");
        }

        public static readonly SpellData[] DefensiveSpells = new SpellData[]
        {
            Spells.TheBlackestNight,
            Spells.Rampart,
            Spells.ShadowWall,
            Spells.DarkMind,
            Spells.Oblation,
        };

        public static readonly uint[] Defensives = new uint[]
        {
            Auras.Rampart,
            Auras.LivingDead,
            Auras.ShadowWall,
            Auras.Rampart,
            Auras.BlackestNight,
            Auras.Oblation
        };
    }
}
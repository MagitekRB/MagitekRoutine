using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Samurai;
using System;
using System.Collections.Generic;
using System.Linq;
using static ff14bot.Managers.ActionResourceManager.Samurai;

namespace Magitek.Utilities.Routines
{
    internal static class Samurai
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Samurai, Spells.Hakaze, new List<SpellData>() { Spells.KaeshiGoken, Spells.KaeshiNamikiri, Spells.KaeshiSetsugekka, });


        public static SpellData Fuko => Spells.Fuko.IsKnown()
                                                    ? Spells.Fuko
                                                    : Spells.Fuga;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static bool prepareFillerRotation = false;
        public static bool isReadyFillerRotation = false;

        public static void InitializeFillerVar(bool prepareFiller, bool readyFiller)
        {
            if (SamuraiFillerStrategy.None.Equals(SamuraiSettings.Instance.SamuraiFillerStrategy))
            {
                prepareFillerRotation = false;
                isReadyFillerRotation = false;
            }
            else
            {
                prepareFillerRotation = prepareFiller;
                isReadyFillerRotation = readyFiller;
            }
        }

        public static int SenCount
        {
            get
            {
                var senCount = 0;
                if (Sen.HasFlag(Iaijutsu.Getsu)) senCount++;
                if (Sen.HasFlag(Iaijutsu.Ka)) senCount++;
                if (Sen.HasFlag(Iaijutsu.Setsu)) senCount++;
                return senCount;
            }
        }

        public static Queue<SpellData> CastDuringMeikyo = new Queue<SpellData>();

        public static int EnemiesInCone;
        public static int AoeEnemies5Yards;
        public static int AoeEnemies8Yards;

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            EnemiesInCone = Core.Me.EnemiesInCone(8);
            AoeEnemies5Yards = Combat.Enemies.Count(x => x.WithinSpellRange(5) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
            AoeEnemies8Yards = Combat.Enemies.Count(x => x.WithinSpellRange(8) && x.IsTargetable && x.IsValid && !x.HasAnyAura(Auras.Invincibility) && x.NotInvulnerable());
        }

        // How close Ikishoten's cooldown (120s, Lv68) has to be before we report the
        // burst as imminent. The odd-minute Meikyo/Tendo window has no cooldown of
        // its own to predict from and deliberately arrives unannounced.
        private const int IkishotenImminentMs = 10000;

        // A Kaeshi follow-up is pressed on the very next GCD after its iaijutsu or
        // Ogi Namikiri, so one nominal GCD is all the bridge the gauge clause needs
        // (Tsubame-gaeshi Ready's real leash is 30s).
        private static readonly TimeSpan KaeshiBridge = TimeSpan.FromSeconds(2.5);

        /// <summary>
        /// Reports SAM burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the SAM rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: no single aura exists — the window is the union of the Ready
        /// states burst choreography grants: Ogi Namikiri Ready + Zanshin Ready (both
        /// from Ikishoten, even minutes) and Tendo (from Meikyo, every burst at 100),
        /// with the Kaeshi gauge bridging iaijutsu-to-follow-up. A banked plain
        /// Kaeshi: Setsugekka is filler, not burst, and is excluded. Window contents:
        /// Ikishoten, Ogi Namikiri + Kaeshi: Namikiri, Zanshin, Tendo
        /// Setsugekka/Goken + Tendo Kaeshi, Senei, Shoha, Higanbana refresh.
        /// Severity medium: all burst states have 30s leashes — an interjection
        /// drifts alignment, it does not delete anything.
        /// Sources: The Balance SAM basic guide, official job guide, Icy Veins, live
        /// client via rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: the 30s Ready states burst choreography grants — Ogi
            // Namikiri Ready and Zanshin Ready (from Ikishoten), Tendo (from Meikyo
            // at 100). They go out and get consumed staggered, so the window is
            // their union — max remaining. Meikyo Shisui alone is excluded (below
            // 100 it is a filler combo-skip tool; Tendo covers the burst press), and
            // Fugetsu/Fuka are permanent maintenance, not a window. Under level sync
            // missing buffs simply never appear — no level branching needed.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                if (aura.Id != Auras.OgiReady && aura.Id != Auras.ZanshinReady && aura.Id != Auras.Tendo)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            // Kaeshi gauge: executing a Tendo iaijutsu or Ogi Namikiri consumes its
            // aura one GCD before the Kaeshi follow-up is pressed — a burst-only
            // follow-up sitting in the gauge bridges that gap. Plain
            // Kaeshi: Setsugekka is deliberately excluded: the rotation banks it
            // mid-filler and carries it into the next burst, which would hold the
            // window open through up to 30s of filler.
            if ((Kaeshi == KaeshiAction.Namikiri || Kaeshi == KaeshiAction.TendoGoken || Kaeshi == KaeshiAction.TendoSetsugekka)
                && KaeshiBridge > remaining)
                remaining = KaeshiBridge;

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "SAM Ikishoten");
                return;
            }

            // Ikishoten almost off cooldown: the even-minute burst is about to open,
            // so nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.Ikishoten.IsKnown())
            {
                var cooldownMs = Spells.Ikishoten.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= IkishotenImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "SAM Ikishoten");
            }
        }

    }
}

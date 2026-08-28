using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using System;
using System.Linq;
using static ff14bot.Managers.ActionResourceManager.RedMage;

namespace Magitek.Utilities.Routines
{
    internal static class RedMage
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.RedMage, Spells.Jolt);

        // How close Embolden's cooldown has to be before we report the burst as
        // imminent. Manafication (110s) is held for Embolden, so one trigger covers
        // the whole buff stack.
        private const int EmboldenImminentMs = 5000;

        // Vice of Thorns has no PvE constant in Spells.cs (only ViceOfThornsPvp);
        // id 37005 verified against the live client via rb.
        private static readonly SpellData ViceOfThorns = DataManager.GetSpellData(37005);

        /// <summary>
        /// Reports RDM burst windows to the state bus. Called every combat pulse via
        /// RoutineState.Pulse() — deliberately not from the RDM rotation, which can be
        /// preempted (by Occult Crescent among others) and would starve the report.
        /// </summary>
        /// <remarks>
        /// Burst anchor: Embolden (20s, every 2 minutes; Manafication weaved beside
        /// it granting Magicked Swordplay and Prefulgence Ready), with the enchanted
        /// melee combo and Verflare/Verholy -> Scorch -> Resolution finisher chain
        /// carried by gauge and combo state. Foreign oGCDs and items do NOT break the
        /// combo chain (30s timer absorbs them) — the cost is finishers displaced out
        /// of the buff window. Window contents: Embolden, Manafication, enchanted
        /// combo + finishers, Vice of Thorns, Prefulgence.
        /// Sources: The Balance RDM basic guide, official job guide, live client via
        /// rb (researched 2026-08-26).
        /// </remarks>
        public static void ReportBurstWindows()
        {
            // Burst window: any of the four burst auras — the window ends when the
            // last drops, hence the max remaining. Embolden 1239 is the caster-only
            // self record (the received variants 1297/2282 must not match), and the
            // own-cast filter enforces that. Prefulgence Ready and Thorned Flourish
            // ride on the ability that spends them being known; under level sync
            // missing buffs simply never appear.
            var remaining = TimeSpan.Zero;
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.CasterId != Core.Me.ObjectId)
                    continue;

                var isWindowAura = aura.Id == Auras.Embolden
                    || aura.Id == Auras.MagickedSwordplay
                    || (aura.Id == Auras.PrefulgenceReady && Spells.Prefulgence.IsKnown())
                    || (aura.Id == Auras.ThornedFlourish && ViceOfThorns.IsKnown());

                if (!isWindowAura)
                    continue;

                if (aura.TimespanLeft > remaining)
                    remaining = aura.TimespanLeft;
            }

            if (remaining > TimeSpan.Zero)
            {
                RoutineState.ReportBurstWindow(remaining, "RDM Embolden");
                return;
            }

            // Finisher chain live: Verflare/Verholy -> Scorch -> Resolution rides the
            // combo timer past the buffs; interrupting it displaces finishers out of
            // the window. Off-burst melee combos are deliberately not covered — an
            // interjection there is delay-only, and flagging them would mark half the
            // fight as burst. ComboTimeLeft is the game's combo timer read unscaled,
            // in seconds (verified by decompile), despite most call sites only ever
            // comparing it to zero.
            if (ActionManager.ComboTimeLeft > 0)
            {
                var lastSpellId = ActionManager.LastSpell.Id;
                if (lastSpellId == Spells.Verflare.Id || lastSpellId == Spells.Verholy.Id || lastSpellId == Spells.Scorch.Id)
                {
                    RoutineState.ReportBurstWindow(TimeSpan.FromSeconds(ActionManager.ComboTimeLeft), "RDM Embolden");
                    return;
                }
            }

            // Mana Stacks pending: mid enchanted melee combo with the finisher chain
            // still unspent. No timer backs the gauge, so a nominal 5s stands in.
            if (ManaStacks > 0 && Spells.Verflare.IsKnown())
            {
                RoutineState.ReportBurstWindow(TimeSpan.FromSeconds(5), "RDM Embolden");
                return;
            }

            // Embolden almost off cooldown: the buff stack is about to go out, so
            // nothing slow should start now. Only reported while it is actually
            // cooling down — "ready but held" is unbounded and would starve consumers.
            if (Core.Me.InCombat && Spells.Embolden.IsKnown())
            {
                var cooldownMs = Spells.Embolden.Cooldown.TotalMilliseconds;
                if (cooldownMs > 0 && cooldownMs <= EmboldenImminentMs)
                    RoutineState.ReportImminentBurst(TimeSpan.FromMilliseconds(cooldownMs), "RDM Embolden");
            }
        }

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static bool WithinManaOf(int distance, int target) => WhiteMana >= target - distance && BlackMana >= target - distance;
    }
}

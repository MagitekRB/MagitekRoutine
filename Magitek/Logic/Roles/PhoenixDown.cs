using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    // Job-agnostic fallback revive: use a Phoenix Down (item 4570) on a fallen party member or
    // Duty Support / Trust NPC when no one has raised them. Opt-in, duties only, keeps a reserve, and
    // waits a configurable delay after death so real raise spells get first shot.
    internal static class PhoenixDown
    {
        private const uint PhoenixDownItemId = 4570;
        private const float PhoenixDownRange = 15f; // Phoenix Down reaches ~15 yalms.

        // Minimum gap between our own attempts. Deliberately not a setting: this is an anti-spam floor
        // on retries, not a tuning knob for when to revive — PhoenixDownDelaySeconds already owns that
        // decision. It also sits well under the item's own 360s recast, so it can never delay a use the
        // game would otherwise have permitted.
        private const int RetryIntervalMs = 10000;

        // Never started until the first attempt, so a fresh Stopwatch reads as "no attempt yet".
        private static readonly Stopwatch RetryTimer = new Stopwatch();

        // Each job's own raise, base classes included, since a Conjurer or Arcanist can be in a
        // low-level duty before unlocking their job stone. Blue Mage's Angel Whisper is here too: it
        // only reports known while it is actually on the active spell set, which is the condition that
        // matters anyway.
        private static readonly Dictionary<ClassJobType, SpellData> RaiseByJob = new Dictionary<ClassJobType, SpellData>
        {
            { ClassJobType.Conjurer,    Spells.Raise },
            { ClassJobType.WhiteMage,   Spells.Raise },
            { ClassJobType.Arcanist,    Spells.Resurrection },
            { ClassJobType.Scholar,     Spells.Resurrection },
            { ClassJobType.Summoner,    Spells.Resurrection },
            { ClassJobType.Astrologian, Spells.Ascend },
            { ClassJobType.Sage,        Spells.Egeiro },
            { ClassJobType.RedMage,     Spells.Verraise },
            { ClassJobType.BlueMage,    Spells.AngelWhisper }
        };

        public static async Task<bool> Execute()
        {
            if (!BaseSettings.Instance.UsePhoenixDown)
                return false;

            // O(1) gate before anything that touches the bags: DeadAllies is a plain list that
            // Group.UpdateAllies() already built earlier in this same pulse, while every FilledSlots
            // walk below reads each bag slot out of game memory (~0.3 ms per scan on a full
            // inventory). Nobody is dead in the overwhelming majority of pulses, so almost every
            // pulse ends right here. Only .Count is this cheap — running the full corpse predicate
            // first would read eight properties per body and cost more than it saves.
            if (Group.DeadAllies.Count == 0)
                return false;

            // Phoenix Down is an 8 second hardcast, so a moving character cannot land one — UseItem
            // refuses outright, and on the pulses where it doesn't, the cast dies to the first step
            // taken. Bail before spending an attempt on it rather than after. This costs nothing while
            // standing still, and deliberately does not touch the retry clock below: walking past a body
            // should not put the next real attempt 10 seconds out, so the first pulse after coming to a
            // stop is free to try.
            if (MovementManager.IsMoving)
                return false;

            // Rate-limit the attempt, not the success. Every step below this can fail without consuming
            // an item or starting the item's own recast — UseItem refuses outright while moving, and a
            // cast that does start dies to the next movement step — and with nothing recording that we
            // tried, the following pulse simply tries again. That turns one unreachable body into an
            // attempt, and a log line, every pulse for as long as it is on the floor.
            if (RetryTimer.IsRunning && RetryTimer.ElapsedMilliseconds < RetryIntervalMs)
                return false;

            // Anyone who can raise under their own power has no business spending a consumable on it.
            if (HasOwnRaise())
                return false;

            // Instanced content only (dungeons/trials/deep dungeons/field ops/etc.). The game's own
            // item.CanUse() then enforces which specific duties actually permit Phoenix Down and when
            // (e.g. blocked in combat in 8-player content), so we don't hardcode those rules.
            if (!DutyManager.InInstance)
                return false;

            // One bag scan, not two: the same walk answers "do we have any" and "how many".
            // Keep a reserve so we never burn the player's last few (0 = no reserve, use them all).
            var slots = InventoryManager.FilledSlots.Where(r => r.RawItemId == PhoenixDownItemId).ToList();
            if (slots.Count == 0)
                return false;

            if (slots.Sum(r => (long)r.Count) <= BaseSettings.Instance.PhoenixDownReserve)
                return false;

            var item = slots[0];

            var delaySeconds = BaseSettings.Instance.PhoenixDownDelaySeconds;
            var now = DateTime.Now;

            // Dead party members AND Duty Support / Trust NPCs both surface through Group.DeadAllies (it
            // is built from PartyManager.RawMembers). Skip anyone already being raised, require the death
            // to be at least `delaySeconds` old so a real raise gets priority, and require the game to
            // actually allow the use on them (item.CanUse -> duty rules) so an unusable higher-priority
            // body can't starve a reachable one. GetResurrectionWeight orders healers first, then tanks,
            // then DPS.
            // Raw Distance, not WithinSpellRange — the raise-range exception in AGENTS.md: a corpse has
            // no usable CombatReach for the game to measure against, and WithinSpellRange's Distance2D
            // ignores height entirely, so both errors run permissive and admit bodies that are genuinely
            // out of reach. Same rule, on this same list, as Healer.ResurrectionLogic. InLineOfSight for
            // the same reason Healer.cs filters it: nothing verifies that item.CanUse covers LoS.
            // u.IsValid short-circuits before every read below. Each of those touches game memory, so a
            // corpse whose object was freed mid-iteration throws ReadWriteMemoryException from inside the
            // predicate and takes down the whole Heal pulse — the same guard Healer.ResurrectionLogic
            // already carries for this list.
            var target = Group.DeadAllies
                .Where(u => u != null
                            && u.IsValid
                            && u.CurrentHealth == 0
                            && !u.HasAura(Auras.Raise)
                            && u.IsVisible
                            && u.IsTargetable
                            && u.Distance(Core.Me) <= PhoenixDownRange
                            && u.InLineOfSight()
                            && item.CanUse(u)
                            && Group.GetDeathTime(u)?.AddSeconds(delaySeconds) <= now)
                .OrderByDescending(u => u.GetResurrectionWeight())
                .FirstOrDefault();

            if (target == null)
                return false;

            // From here we are committing to an attempt, so start the retry clock before making it
            // rather than after it succeeds — a failure is precisely the case that would repeat.
            RetryTimer.Restart();

            // One targeted use per pulse. NOT the shared UseItem() extension: it loops UseItem() with no
            // target and no inter-use delay, which would fire on the wrong unit and re-fire every frame
            // until the revive lands (burning multiple Phoenix Downs).
            Logger.WriteInfo($"[Phoenix Down] Reviving {target.CurrentJob}");
            if (!item.UseItem(target))
                return false;

            // Starting the item cast is not the same as finishing it. Left untracked, the very next pulse
            // sees nothing in progress and carries on to the movement step, which walks and cancels the
            // revive. Register the cast the way SpellDataExtensions.Cast() does, so the existing
            // Casting.TrackSpellCast() hold at the top of Heal() owns it — the pulse keeps running for
            // everyone else instead of blocking here for the full 8 seconds. This registration, plus the
            // !HasAura(Raise) filter above, is the whole re-fire guard: no per-corpse timers. The residual
            // window (cast end to the server's Raise status) is one round-trip; a second down inside it is
            // possible but rare, the same trade the non-party rez guard makes.
            //
            // The wait is part of the pattern, not a hold: DoAction registers only after the cast bar is
            // confirmed, because IsCasting does not flip on the same frame as the request. Registering
            // into that gap lets one pulse see !IsCasting with the timer running, which
            // CheckForSuccessfulCast reads as a failed cast — tracking dropped, and the walk-away bug
            // this exists to fix comes straight back.
            if (!await Coroutine.Wait(3000, () => Core.Me.IsCasting))
                return false;

            // Two deliberate oddities: SpellCastTime is a literal because BackingAction.AdjustedCastTime
            // reports 0 for Phoenix Down despite the visible 8s cast bar, and the buffer maths in
            // CheckForSuccessfulCast needs the real duration (its advanced-history early return is
            // exempted separately, on CastingRevive — the 0 AdjustedCastTime would trip it regardless of
            // what we put here). CastingRevive marks the cast as a revive for TrackSpellCast: the per-job
            // interrupt checks cancel any cast on a dead target unless it is that job's own raise spell,
            // and a revive's target is dead by definition.
            Casting.CastingSpell = item.Item.BackingAction;
            Casting.SpellTarget = target;
            Casting.CastingHeal = true;
            Casting.CastingRevive = true;
            Casting.SpellCastTime = TimeSpan.FromMilliseconds(8000);
            Casting.CastingTime.Restart();

            return true;
        }

        // True when this character can raise under its own power, and so should leave the Phoenix Downs
        // in the bag.
        //
        // Gated on the raise being known rather than on job identity, because the two come apart under
        // level sync: Verraise is Lv64, so a Red Mage synced into level 50 content has no raise at all,
        // and counting them as a raiser there would leave the body on the floor for nothing.
        //
        // IsKnown, not IsKnownAndReady — a raise sitting on cooldown, or waiting on Swiftcast or MP, is
        // a reason to wait for it, not a reason to burn a consumable.
        private static bool HasOwnRaise()
        {
            if (RaiseByJob.TryGetValue(Core.Me.CurrentJob, out var raise) && raise.IsKnown())
                return true;

            // Occult Crescent's phantom raises belong to the phantom job rather than the base job, so
            // any job at all can be carrying one. Checked second so a dictionary miss is all most
            // characters pay, and behind the zone test so nobody outside the Crescent walks the
            // phantom-job aura list. OccultCrescent.Execute() already runs ahead of PhoenixDown in the
            // Heal pulse, so these two get first refusal on the body regardless of what we answer here.
            if (!OccultCrescent.IsInOccultCrescent())
                return false;

            switch (OccultCrescent.GetCurrentPhantomJob())
            {
                case OccultCrescent.PhantomJob.Chemist:
                    return OCSpells.Revive.IsKnown();

                case OccultCrescent.PhantomJob.WhiteMage:
                    return OCSpells.OccultRaise.IsKnown();

                default:
                    return false;
            }
        }
    }
}

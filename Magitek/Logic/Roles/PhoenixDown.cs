using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
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

        public static async Task<bool> Execute()
        {
            if (!BaseSettings.Instance.UsePhoenixDown)
                return false;

            // Instanced content only (dungeons/trials/deep dungeons/field ops/etc.). The game's own
            // item.CanUse() then enforces which specific duties actually permit Phoenix Down and when
            // (e.g. blocked in combat in 8-player content), so we don't hardcode those rules.
            if (!DutyManager.InInstance)
                return false;

            var item = InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == PhoenixDownItemId);
            if (item == null)
                return false;

            // Keep a reserve so we never burn the player's last few (0 = no reserve, use them all).
            var owned = InventoryManager.FilledSlots.Where(r => r.RawItemId == PhoenixDownItemId).Sum(r => (long)r.Count);
            if (owned <= BaseSettings.Instance.PhoenixDownReserve)
                return false;

            var delaySeconds = BaseSettings.Instance.PhoenixDownDelaySeconds;
            var now = DateTime.Now;

            // Dead party members AND Duty Support / Trust NPCs both surface through Group.DeadAllies (it
            // is built from PartyManager.RawMembers). Skip anyone already being raised, require the death
            // to be at least `delaySeconds` old so a real raise gets priority, and require the game to
            // actually allow the use on them (item.CanUse -> real range / line-of-sight / duty rules) so
            // an unusable higher-priority body can't starve a reachable one. GetResurrectionWeight
            // orders healers first, then tanks, then DPS.
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
                            && u.WithinSpellRange(PhoenixDownRange)
                            && item.CanUse(u)
                            && Group.GetDeathTime(u)?.AddSeconds(delaySeconds) <= now)
                .OrderByDescending(u => u.GetResurrectionWeight())
                .FirstOrDefault();

            if (target == null)
                return false;

            // One targeted use per pulse. NOT the shared UseItem() extension: it loops UseItem() with no
            // target and no inter-use delay, which would fire on the wrong unit and re-fire every frame
            // until the revive lands (burning multiple Phoenix Downs).
            Logger.WriteInfo($"[Phoenix Down] Reviving {target.Name} ({target.CurrentJob})");
            item.UseItem(target);

            // Starting the item cast is not the same as finishing it. Left untracked, the very next pulse
            // sees nothing in progress and carries on to the movement step, which walks and cancels the
            // revive. Hold here until the cast actually ends so the rest of the pulse cannot interrupt
            // it — this hold, plus the !HasAura(Raise) filter above, is the whole re-fire guard: no
            // per-corpse timers. The residual window (cast end to the server's Raise status) is one
            // round-trip; a second down inside it is possible but rare, the same trade the non-party
            // rez guard makes.
            await Coroutine.Wait(1000, () => Core.Me.IsCasting);
            await Coroutine.Wait(10000, () => !Core.Me.IsCasting);

            return true;
        }
    }
}

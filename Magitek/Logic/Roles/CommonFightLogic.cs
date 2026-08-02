using ff14bot;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Roles
{
    internal class CommonFightLogic
    {
        public static async Task<bool> FightLogic_TankDefensive(bool useDefensive, SpellData[] defensiveSpells, uint[] defensiveAuras, int castTimeRemainingMs = 0)
        {
            if (!useDefensive)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyTankbusterLogic())
                return false;

            if (FightLogic.EnemyIsCastingTankBuster() != null
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (Core.Me.HasAnyAura(defensiveAuras))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                foreach (var defensiveSpell in defensiveSpells)
                {
                    if (defensiveSpell.IsKnownAndReadyAndCastable(Core.Me))
                    {
                        if (BaseSettings.Instance.DebugFightLogic)
                            Logger.WriteInfo($"[TankDefensive Response] Cast {defensiveSpell.Name}");
                        if (await FightLogic.DoAndBuffer(defensiveSpell.Cast(Core.Me)))
                            return true; // intentionally continue to next defensive in the list. 
                    }
                }
            }
            return false;
        }

        public static async Task<bool> FightLogic_SelfShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                // Now check if spell is ready before attempting to cast
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[SelfShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_PartyShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint[] auras = null, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                // Now check if spell is ready before attempting to cast
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && auras != null && Core.Me.HasAnyAura(auras))
                    return false;

                if (selfAuraCheck && aura != 0 && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[PartyShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_Debuff(bool useDebuff, SpellData spell, bool targetAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0, float range = 0f)
        {
            if (!useDebuff)
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe()
                || FightLogic.EnemyIsCastingBigAoe()
                || FightLogic.EnemyIsCastingTankBuster() != null
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (!spell.IsKnownAndReady())
                    return false;

                if (Core.Me.CurrentTarget == null)
                    return false;

                // For range-based debuffs (e.g., Reprisal), check if current target is within range
                if (range > 0f && !Core.Me.CurrentTarget.WithinSpellRange(range))
                    return false;

                // For target-based debuffs, check target aura
                if (targetAuraCheck && Core.Me.CurrentTarget.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[Debuff Response] Cast {spell.Name} on {Core.Me.CurrentTarget.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me.CurrentTarget));
            }

            return false;
        }

        public static async Task<bool> FightLogic_Knockback(bool useAntiKnockback, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 3000)
        {
            if (!useAntiKnockback)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyKnockbackLogic())
                return false;

            if (FightLogic.EnemyIsCastingKnockback())
            {
                if (!spell.IsKnownAndReady())
                    return false;

                if (selfAuraCheck && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[AntiKnockback Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        // Action-aware mechanics (Pyretic, Acceleration Bomb): a debuff on the player that detonates if we
        // act or move while it's up. Unlike the other reactions this is player-status driven and NOT zone-
        // gated, and instead of casting something it PAUSES the routine — returning true consumes the pulse
        // so no GCD/oGCD fires until the debuff falls off. That's the "stop casting with the aura up" fix.
        public static Task<bool> FightLogic_ActionState(int expiryLeadMs = 2000)
        {
            // This one is not zone-gated, so unlike every other reaction it never passed through
            // ZoneHasFightLogic() and so ignored the master switch. Turning fight logic off has to turn
            // this off too, or "Enable Fight Logic" does not mean what it says.
            if (!BaseSettings.Instance.UseFightLogic || !BaseSettings.Instance.FightLogicActionAwareness)
                return Task.FromResult(false);

            var mechanic = FightLogic.PlayerActionPunishAura(out var msRemaining);
            if (mechanic == null)
                return Task.FromResult(false);

            // Snapshot mechanics only care what we're doing at the moment they fall off, so keep playing
            // until the window closes instead of surrendering the debuff's whole duration — Buyer's Remorse
            // sits for 8s, and freezing for all of it would cost far more than the mechanic does.
            if (mechanic.ChecksOnExpiry && msRemaining > expiryLeadMs)
                return Task.FromResult(false);

            // Halt routine-driven movement — the usual killer for movement-punish mechanics like
            // Acceleration Bomb. Best-effort: a botbase actively pathing may re-issue movement next tick.
            if (mechanic.PunishesMovement)
            {
                if (MovementManager.IsMoving)
                    Navigator.PlayerMover.MoveStop();

                // Stopping alone is not enough: the rest of the pulse would navigate straight back out
                // again. Park navigation for a moment so standing still actually sticks. Refreshed every
                // pulse the mechanic is up, so it lapses on its own the instant it is not.
                FightLogic.HoldMovement(1000);
            }

            // A mechanic that only objects to movement has no quarrel with casting, so there is nothing
            // to gain by going quiet for it. Acceleration Bomb is the common case: stop moving, then let
            // the rotation carry on rather than surrendering every action in the window for nothing.
            if (!mechanic.PunishesActions)
                return Task.FromResult(false);

            StopInFlightCast();

            // Consuming the pulse stops the routine ISSUING actions, but auto-attacks keep swinging on their
            // own — and Pyretic is "damage is taken with every action", weapon swings included. RebornBuddy
            // exposes no way to stop auto-attack directly, so turn away instead: the game only swings at what
            // we are facing. Preferred over dropping the target, which would leave the rotation with nothing
            // to resume on. Re-asserted every pulse the debuff is up, and normal combat re-faces once it ends.
            if (Core.Me.HasTarget && Core.Me.CurrentTarget.CanAttack)
                FightLogic.FaceForGaze(FightLogic.GazeDirection.Away, Core.Me.CurrentTarget);

            if (BaseSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[ActionAware] Holding — {mechanic.Name} is up; not casting until it expires.");

            return Task.FromResult(true); // consume the pulse: cast nothing
        }

        // Gaze mechanics (look away / look toward): turn away from — or toward — the caster and hold.
        // Returning true consumes the pulse so the routine doesn't fire an action and snap us back to
        // face the boss. Catalogued per encounter and NOT throttled, so facing is re-asserted the whole
        // cast; once the cast ends detection returns None and normal combat re-faces the boss ("look back").
        public static Task<bool> FightLogic_Gaze(bool useGaze, int castTimeRemainingMs = 3000, int graceMs = 800, int markerTurnDelayMs = 2500)
        {
            if (!useGaze)
                return Task.FromResult(false);

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyGazeLogic())
                return Task.FromResult(false);

            var direction = FightLogic.EnemyIsCastingGaze(out var caster);

            if (direction != FightLogic.GazeDirection.None && caster != null)
            {
                var remaining = caster.SpellCastInfo.RemainingCastTime.TotalMilliseconds;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[Gaze Detected] {caster.Name} casting {caster.CastingSpellId} — {direction}, {remaining:F0}ms left.");

                // Turn late. Holding through an entire gaze cast would bleed a lot of uptime (the long
                // variants run ~8s, and they arrive in waves), and SetFacing is instant so there's no reason
                // to commit early. Take over only for the last few seconds — wide enough to span a GCD, so a
                // weaponskill can't fire mid-hold and auto-face us back into the gaze.
                if (remaining > castTimeRemainingMs)
                    return Task.FromResult(false);

                // A gaze's snapshot lands a moment AFTER its cast bar ends — that gap is what the grace
                // window exists to cover. Kefka's statues alternate their demand and their casts overlap, so
                // the instant the first one ends the second is already detected, wanting the opposite
                // heading. Obeying it straight away turns us into the first gaze's snapshot and eats it.
                // Sit out the remainder of the latch instead; the new gaze still has its own cast left to
                // run and SetFacing is instant, so the later turn lands just as well.
                if (FightLogic.GazeHoldDirection != FightLogic.GazeDirection.None
                    && FightLogic.GazeHoldDirection != direction)
                {
                    if (FightLogic.ReassertGazeHold())
                    {
                        if (BaseSettings.Instance.DebugFightLogic)
                            Logger.WriteInfo($"[Gaze Hold] Keeping {FightLogic.GazeHoldDirection} through its grace window; {caster.Name} wants {direction}.");

                        return Task.FromResult(true);
                    }
                }

                return Hold(direction, caster, "cast", graceMs);
            }

            // Head-marker gazes carry no cast bar, so there is no countdown to turn late against. Turning
            // the moment the marker lands would surrender its entire lifetime, and we cannot attack a boss
            // we are facing away from, so that is dead time. Measure the marker's age instead and stay on
            // target until it has been up a while — the turn itself is instant, so a late one still lands.
            var markerDirection = FightLogic.GazeLockOnActive(out var markerSource, out var markerId);

            if (markerDirection != FightLogic.GazeDirection.None && markerSource != null)
            {
                if (FightLogic.GazeMarkerAgeMs(markerId) < markerTurnDelayMs)
                    return Task.FromResult(false); // keep attacking; turn shortly

                return Hold(markerDirection, markerSource, "marker", graceMs);
            }

            // The gaze is no longer detectable but its snapshot may not have landed yet — keep the facing
            // (and the auto-face suppression) alive through the grace window rather than releasing on the
            // exact frame the cast ends.
            return Task.FromResult(FightLogic.ReassertGazeHold());
        }

        /// <summary>
        /// Consuming the pulse stops the NEXT action, but a cast already under way still completes — and
        /// completing it is precisely what these mechanics punish, whether by counting as an action or by
        /// spinning us back to face the boss. Cancel it rather than watch it land.
        /// </summary>
        private static void StopInFlightCast()
        {
            if (!Core.Me.IsCasting)
                return;

            ActionManager.StopCasting();

            // Casting.CancelCast stops the stopwatch alongside StopCasting, and for good reason:
            // CheckForSuccessfulCast branches on CastingTime.IsRunning. Leaving it running means the cast we
            // just cancelled is later measured as though it had completed, and its elapsed time compared
            // against the expected duration of a cast that never landed.
            Casting.CastingTime.Stop();
        }

        private static Task<bool> Hold(FightLogic.GazeDirection direction, GameObject source, string via, int graceMs)
        {
            // Movement beats SetFacing, so a navigator still driving us towards something would undo the
            // turn every frame and leave us looking the wrong way for the whole gaze. Stop it first.
            if (MovementManager.IsMoving)
                Navigator.PlayerMover.MoveStop();

            StopInFlightCast();
            FightLogic.FaceForGaze(direction, source);
            FightLogic.LatchGazeHold(direction, source, graceMs);

            if (BaseSettings.Instance.DebugFightLogic)
                Logger.WriteInfo($"[Gaze Response] Facing {(direction == FightLogic.GazeDirection.Away ? "away from" : "toward")} {source.Name} ({via}).");

            return Task.FromResult(true); // hold: cast nothing so we stay oriented
        }
    }
}

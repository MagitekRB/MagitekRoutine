using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Models.Account;
using Magitek.Utilities.Managers;
using Magitek.Utilities.Routines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Debug = Magitek.ViewModels.Debug;

namespace Magitek.Utilities
{
    internal static class Casting
    {
        #region Variables
        public static bool CastingHeal;
        // True while the tracked cast is a revive (Phoenix Down). The healer NeedToInterruptCast
        // checks cancel any cast whose target is dead unless it is that job's own raise spell —
        // and a revive's target is dead by definition, so they need this to tell the two apart.
        // Cleared wherever CastingTime stops AND at every ordinary-cast registration (the three
        // CastingTime.Restart sites in SpellDataExtensions, beside their CastingHeal writes) — a
        // stop is not guaranteed to run if the routine halts or the zone changes mid-revive, and a
        // latched flag would hand the next cast the revive exemption.
        public static bool CastingRevive;
        public static SpellData CastingSpell;
        public static SpellData LastSpell;
        public static bool LastSpellSucceeded;
        public static DateTime LastSpellTimeFinishedUtc;
        public static readonly Stopwatch LastSpellTimeFinishAge = new Stopwatch();
        public static GameObject LastSpellTarget;
        public static GameObject SpellTarget;
        public static TimeSpan SpellCastTime;
        public static bool DoHealthChecks;
        public static bool NeedAura;
        public static uint Aura;
        public static GameObject AuraTarget;
        public static bool UseRefreshTime;
        public static int RefreshTime;
        public static readonly Stopwatch CastingTime = new Stopwatch();
        public static bool CastingGambit;
        public static List<SpellCastHistoryItem> SpellCastHistory = new List<SpellCastHistoryItem>();
        public static Func<Task> Callback;
        #endregion

        public static async Task<bool> TrackSpellCast()
        {
            // Manage SpellCastHistory entries
            if (SpellCastHistory.Count > 20)
            {
                SpellCastHistory.Remove(SpellCastHistory.Last());

                if (BaseSettings.Instance.DebugSpellCastHistory)
                {
                    // Copy the list on this thread, then hand the finished snapshot to the UI:
                    // the rotation keeps mutating SpellCastHistory, so a copy taken later on the
                    // dispatcher can tear or throw. InvokeAsync for the same reason as the twin
                    // call in CheckForSuccessfulCast below - the rotation must never block on
                    // the dispatcher.
                    var snapshot = new List<SpellCastHistoryItem>(SpellCastHistory);
                    _ = Application.Current.Dispatcher.InvokeAsync(delegate { Debug.Instance.SpellCastHistory = snapshot; });
                }
            }

            // If we're not casting we can return false to keep going down the tree
            if (!Core.Me.IsCasting)
                return false;

            // The possibility here is that we're teleporting (casting)
            // So if the timer isn't running, it means Magitek didn't cast it, and the cast shouldn't be monitored
            if (!CastingTime.IsRunning)
                return false;

            await GambitLogic.ToastGambits();

            #region Debug and Target Checks

            if (BaseSettings.Instance.DebugPlayerCasting)
            {
                Debug.Instance.CastingTime = CastingTime.ElapsedMilliseconds.ToString();
            }

            #endregion

            #region Interrupt Casting Checks
            if (CastingGambit)
                return true;

            try
            {
                if (SpellTarget == null || !SpellTarget.IsValid)
                {
                    await CancelCast("Target is no Longer Valid");
                    return true;
                }

                if (!SpellTarget.IsTargetable)
                {
                    await CancelCast("Target is no Longer Targetable");
                    return true;
                }
            }
            catch
            {
                // Object is invalid in memory (e.g., player died, entity despawned)
                await CancelCast("Target is no Longer Valid");
                return true;
            }

            if (await GambitLogic.InterruptCast())
            {
                await CancelCast();
                return true;
            }

            // A cast that finished while the gambit check was yielding is a success, not a
            // cancellation: bow out the way the entry check does, so the caller still runs
            // the success bookkeeping (LastSpell, SpellCastHistory) instead of this method
            // cancelling a cast that no longer exists.
            if (!Core.Me.IsCasting)
                return false;

            // The validity checks above go stale before the reads below run: the gambit check
            // yields across frames, so the target can despawn between those checks and these
            // reads. A freed target keeps a non-null reference — the null checks pass and the
            // first field read through the stale pointer throws, killing the whole combat
            // pulse (seen live: a nuke target died mid-cast between the checks and the job
            // interrupt switch). Skip the reads once the cast is no longer tracked, then keep
            // everything that touches the target inside the try: even the IsValid re-check
            // reads a liveness stamp through the object's pointer and can throw once the
            // target's memory is unmapped. An unreadable target is treated like an invalid one.
            if (!CastingTime.IsRunning)
                return true;

            try
            {
                if (SpellTarget == null || !SpellTarget.IsValid)
                {
                    await CancelCast("Target vanished before the interrupt checks");
                    return true;
                }

                // A revive (Phoenix Down) targets a body that is dead by definition, and the job checks
                // below cancel any cast on a dead target unless it is that job's own raise spell — so every
                // one of them would kill the revive on its first tracked pulse. Exempt the revive here once
                // rather than carving an exception into all seven jobs, but keep the ONE cancel that is
                // meaningful for it, the same one every healer applies to its own raise: someone else's
                // raise landed first, so finishing the cast would spend a Phoenix Down on a claimed corpse.
                // The target-validity checks above still run: a despawned corpse still cancels.
                if (CastingRevive)
                {
                    if (SpellTarget is Character corpse
                        && (corpse.CurrentHealth > 0 || corpse.HasAura(Auras.Raise)))
                        await CancelCast("Revive target was already raised");

                    return true;
                }

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (RotationManager.CurrentRotation)
                {
                    case ClassJobType.BlueMage:
                        {
                            if (BlueMage.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Scholar:
                        {
                            if (Scholar.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Arcanist:
                        {
                            if (Scholar.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.WhiteMage:
                        {
                            if (WhiteMage.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Conjurer:
                        {
                            if (WhiteMage.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Astrologian:
                        {
                            if (Astrologian.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Summoner:
                        {
                            if (Summoner.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.BlackMage:
                        {
                            if (BlackMage.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                    case ClassJobType.Sage:
                        {
                            if (Sage.NeedToInterruptCast())
                            {
                                await CancelCast();
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                // Object is invalid in memory (e.g., player died, entity despawned) — but any
                // exception thrown by a job's interrupt checks lands here too, so put the real
                // reason in the log instead of silently blaming the target.
                Logger.WriteInfo($"[Casting] Interrupt checks failed, cancelling cast: {ex.Message}");
                await CancelCast("Target is no Longer Readable");
            }

            #endregion

            return true;
        }

        private static async Task CancelCast(string msg = null)
        {
            try
            {
                ActionManager.StopCasting();
                await Coroutine.Wait(1000, () => !Core.Me.IsCasting);

                if (msg != null)
                    Logger.Error(msg);
            }
            catch (Exception)
            {
                //Ignore on Purpose
            }
            finally
            {
                // The IsRunning guard in TrackSpellCast depends on this timer stopping even
                // when StopCasting or the wait throws mid-transition.
                CastingTime.Stop();
                CastingRevive = false;
            }
        }

        public static async Task CheckForSuccessfulCast()
        {
            if (BaseSettings.Instance.DebugActionLockWait2)
                if (ActionManager.ActionLock != 0)
                    await Coroutine.Wait(Math.Max(Globals.AnimationLockMs, (int)(ActionManager.ActionLock * 1000)), () => ActionManager.ActionLock == 0);

            // If the timer isn't running it means it's already been stopped and the variables have already been set
            if (!CastingTime.IsRunning)
            {
                NeedAura = false;
                UseRefreshTime = false;
                DoHealthChecks = false;
                CastingHeal = false;
                CastingRevive = false;
                CastingGambit = false;
                Callback = null;
                return;
            }

            #region Verify Successful Spell Cast

            //This is to ensure that the instant Action we just tried to use
            //was indeed used and not rejected from the server.
            //Logic behind this is, that every Action will trigger some kind of cooldown
            // !CastingRevive: Phoenix Down's BackingAction reports AdjustedCastTime 0 despite the real
            // 8s cast bar, so without the exemption this shortcut returns with the timer still running
            // and the revive flag latched. The revive must fall through to the buffer maths below, which
            // its literal SpellCastTime was set up for.
            if (BaseSettings.Instance.UseAdvancedSpellHistory2)
                if (CastingSpell.AdjustedCastTime.TotalMilliseconds == 0 && CastingSpell.Cooldown.TotalMilliseconds == 0 && !CastingRevive)
                    return;

            // Compare Times
            Logger.WriteCast($@"Time Casting: {CastingTime.ElapsedMilliseconds} - Expected: {SpellCastTime.TotalMilliseconds}");
            var buffer = SpellCastTime.TotalMilliseconds - CastingTime.ElapsedMilliseconds;

            // Stop Timer
            CastingTime.Stop();
            CastingRevive = false;

            // Did we successfully cast?
            if (buffer > 800)
            {
                NeedAura = false;
                UseRefreshTime = false;
                DoHealthChecks = false;
                CastingHeal = false;
                CastingGambit = false;
                LastSpellSucceeded = false;
                Callback = null;
                return;
            }

            if (BaseSettings.Instance.DebugPlayerCasting)
            {
                Debug.Instance.CastingTime = CastingTime.ElapsedMilliseconds.ToString();
            }
            // Within 500 milliseconds we're gonna assume the spell went off
            LastSpell = CastingSpell;
            LastSpellSucceeded = true;
            Debug.Instance.LastSpell = LastSpell;
            LastSpellTimeFinishedUtc = DateTime.UtcNow;
            if (!LastSpellTimeFinishAge.IsRunning) LastSpellTimeFinishAge.Start();
            else LastSpellTimeFinishAge.Restart();
            LastSpellTarget = SpellTarget;
            Logger.WriteCast($@"Successfully Casted {LastSpell}");

            SpellCastHistory.Insert(0, new SpellCastHistoryItem
            {
                Spell = LastSpell,
                SpellTarget = SpellTarget,
                TimeCastUtc = LastSpellTimeFinishedUtc,
                TimeStartedUtc = LastSpellTimeFinishedUtc.Subtract(TimeSpan.FromMilliseconds(CastingTime.ElapsedMilliseconds)),
                DelayMs = CastingTime.ElapsedMilliseconds - SpellCastTime.TotalMilliseconds
            });

            if (BaseSettings.Instance.DebugSpellCastHistory)
            {
                // InvokeAsync, never Invoke: this runs on the pulse thread while it holds the
                // frame lock, so blocking here deadlocks the whole client. A thread dump of a
                // wedged RB caught this exact call - the pulse thread waiting on the dispatcher,
                // the UI thread sitting in Monitor.Enter reading frame-cached game state for a
                // job handler. Neither side could finish. The list is copied on this thread
                // because the rotation keeps mutating it; nothing reads the result back.
                var snapshot = new List<SpellCastHistoryItem>(SpellCastHistory);
                _ = Application.Current.Dispatcher.InvokeAsync(delegate { Debug.Instance.SpellCastHistory = snapshot; });
            }

            #endregion

            #region Aura Checks

            if (NeedAura)
            {
                var auraTarget = AuraTarget ?? SpellTarget;

                if (CastingSpell.AdjustedCastTime == TimeSpan.Zero)
                    await Coroutine.Wait(3000, () => auraTarget.HasAura(Aura, true) || !auraTarget.IsValid || auraTarget.CurrentHealth == 0);
                else
                {
                    if (UseRefreshTime)
                        await Coroutine.Wait(3000, () => auraTarget.HasAura(Aura, true, RefreshTime) || MovementManager.IsMoving || !auraTarget.IsValid || auraTarget.CurrentHealth == 0);
                    else
                        await Coroutine.Wait(3000, () => auraTarget.HasAura(Aura, true) || MovementManager.IsMoving || !auraTarget.IsValid || auraTarget.CurrentHealth == 0);
                }
            }

            if (Callback != null)
                await Callback();

            #endregion

            #region Fill Variables

            NeedAura = false;
            UseRefreshTime = false;
            DoHealthChecks = false;
            CastingHeal = false;
            CastingGambit = false;
            Callback = null;

            #endregion
        }

        /// <summary>
        /// Checks if the last spell cast was the specified spell and succeeded, without time window restriction.
        /// Automatically checks if the spell is known before comparing.
        /// </summary>
        /// <param name="spell">The spell to check for</param>
        /// <returns>True if spell is known, last spell matches, and succeeded</returns>
        public static bool LastSpellWas(SpellData spell)
        {
            return LastSpellWas(spell, -1);
        }

        /// <summary>
        /// Checks if the last spell cast was the specified spell, succeeded, and was cast within the specified time window.
        /// Automatically checks if the spell is known before comparing.
        /// </summary>
        /// <param name="spell">The spell to check for</param>
        /// <param name="withinMs">Time window in milliseconds. Defaults to 1 GCD (3000ms). Use -1 to ignore time check.</param>
        /// <returns>True if spell is known, last spell matches, succeeded, and was within the time window</returns>
        public static bool LastSpellWas(SpellData spell, int withinMs)
        {
            if (spell == null)
                return false;

            // Check if spell is known before comparing (prevents deadlocks when spells aren't unlocked)
            if (!spell.IsKnown())
                return false;

            if (LastSpell == null)
                return false;

            if (LastSpell.Id != spell.Id)
                return false;

            if (!LastSpellSucceeded)
                return false;

            // If withinMs is -1, skip time check (allow regardless of time)
            if (withinMs == -1)
                return true;

            // Check if the last spell was cast within the specified time window
            if (!LastSpellTimeFinishAge.IsRunning)
                return false;

            return LastSpellTimeFinishAge.ElapsedMilliseconds <= withinMs;
        }
    }

    public class SpellCastHistoryItem
    {
        public SpellData Spell { get; set; }
        public GameObject SpellTarget { get; set; }
        public DateTime TimeCastUtc { get; set; }
        public DateTime TimeStartedUtc { get; set; }
        public double DelayMs { get; set; }

        public int AnimationLockRemainingMs
        {
            get
            {
                double timeSinceStartMs = DateTime.UtcNow.Subtract(TimeStartedUtc).TotalMilliseconds - DelayMs;
                return timeSinceStartMs > Globals.AnimationLockMs ? 0 : Globals.AnimationLockMs - (int)timeSinceStartMs;
            }
        }
    }
}
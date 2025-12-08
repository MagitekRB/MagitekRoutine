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
                    Application.Current.Dispatcher.Invoke(delegate { Debug.Instance.SpellCastHistory = new List<SpellCastHistoryItem>(SpellCastHistory); });
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
                }
            }
            catch
            {
                // Object is invalid in memory (e.g., player died, entity despawned)
                await CancelCast("Target is no Longer Valid");
            }

            if (await GambitLogic.InterruptCast())
            {
                await CancelCast();
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

                CastingTime.Stop();
            }
            catch (Exception)
            {
                //Ignore on Purpose
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
                CastingGambit = false;
                Callback = null;
                return;
            }

            #region Verify Successful Spell Cast

            //This is to ensure that the instant Action we just tried to use
            //was indeed used and not rejected from the server.
            //Logic behind this is, that every Action will trigger some kind of cooldown
            if (BaseSettings.Instance.UseAdvancedSpellHistory2)
                if (CastingSpell.AdjustedCastTime.TotalMilliseconds == 0 && CastingSpell.Cooldown.TotalMilliseconds == 0)
                    return;

            // Compare Times
            Logger.WriteCast($@"Time Casting: {CastingTime.ElapsedMilliseconds} - Expected: {SpellCastTime.TotalMilliseconds}");
            var buffer = SpellCastTime.TotalMilliseconds - CastingTime.ElapsedMilliseconds;

            // Stop Timer
            CastingTime.Stop();

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
                Application.Current.Dispatcher.Invoke(delegate { Debug.Instance.SpellCastHistory = new List<SpellCastHistoryItem>(SpellCastHistory); });

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
        /// Checks if the last spell cast was the specified spell, succeeded, and was cast within the specified time window.
        /// </summary>
        /// <param name="spell">The spell to check for</param>
        /// <param name="withinMs">Time window in milliseconds. Defaults to 1 GCD (2500ms). Use -1 to ignore time check.</param>
        /// <returns>True if last spell matches, succeeded, and was within the time window</returns>
        public static bool LastSpellWas(SpellData spell, int withinMs = 3000)
        {
            if (spell == null)
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
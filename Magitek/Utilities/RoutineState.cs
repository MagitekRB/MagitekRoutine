using ff14bot;
using ff14bot.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Magitek.Utilities
{
    /// <summary>
    /// Cross-job combat state bus. Jobs report their burst windows here each pulse;
    /// anything else in the routine (Occult Crescent, Phoenix Down, future fight
    /// timelines) queries it instead of hardcoding per-job aura knowledge.
    ///
    /// Reports are pulse-scoped assertions: a publisher must re-assert every pulse,
    /// and an assertion expires after AssertionTtlMs. A dead publisher (job change,
    /// combat end, wipe) can therefore never leave a window stuck open.
    /// </summary>
    public static class RoutineState
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        // Publishers re-assert every pulse; anything older than this is expired.
        // Comfortably above RB's pulse interval, far below any real window length.
        private const int AssertionTtlMs = 750;

        private static long _windowAssertedAtMs = long.MinValue;
        private static TimeSpan _windowRemaining = TimeSpan.Zero;
        private static string _windowSource;
        private static bool _windowWasActive;

        private static long _imminentAssertedAtMs = long.MinValue;
        private static TimeSpan _imminentStartsIn = TimeSpan.Zero;

        /// <summary>
        /// Called once per combat pulse (from RotationComposites, before Occult
        /// Crescent runs) so window state is always current when consumers read it.
        /// Dispatches to the current job's publisher; jobs opt in one at a time.
        /// </summary>
        public static void Pulse()
        {
            // Never let a publisher bug take down the combat tree — an unhandled
            // exception here would stop every cast for every job. Log and carry on;
            // the TTL means a failing publisher just reads as "no window".
            try
            {
                ResolvePublisher(Core.Me.CurrentJob)?.Invoke();
                LogTransitions();
            }
            catch (Exception ex)
            {
                if (Clock.ElapsedMilliseconds - _lastPulseErrorMs > PulseErrorLogIntervalMs)
                {
                    _lastPulseErrorMs = Clock.ElapsedMilliseconds;
                    Logger.Error($"[BurstWindow] Publisher failed: {ex.Message}");
                }
            }
        }

        private const int PulseErrorLogIntervalMs = 10000;
        private static long _lastPulseErrorMs = -PulseErrorLogIntervalMs;

        // Publishers are discovered, not enumerated: a job opts in by declaring
        // "public static void ReportBurstWindows()" on its Utilities/Routines/<Job>
        // class, found via the same job-to-class-name map the rotation dispatcher
        // uses. Resolution happens once per job and is cached, including misses —
        // a null entry means "this job has no publisher".
        private static readonly Dictionary<ClassJobType, Action> PublisherCache = new();

        private static Action ResolvePublisher(ClassJobType job)
        {
            if (PublisherCache.TryGetValue(job, out var publisher))
                return publisher;

            if (Managers.RotationComposites.RotationClassMap.TryGetValue(job, out var className))
            {
                var method = Type.GetType($"Magitek.Utilities.Routines.{className}")
                    ?.GetMethod("ReportBurstWindows", BindingFlags.Static | BindingFlags.Public);

                if (method != null && method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                    publisher = (Action)Delegate.CreateDelegate(typeof(Action), method);
            }

            PublisherCache[job] = publisher;
            return publisher;
        }

        #region Publisher side

        /// <summary>
        /// Assert that a burst window is open right now. Re-assert every pulse while
        /// it stays open. expectedRemaining is an estimate for consumers, not a
        /// contract — the window is over when assertions stop, not when it hits zero.
        /// </summary>
        public static void ReportBurstWindow(TimeSpan expectedRemaining, string source)
        {
            _windowAssertedAtMs = Clock.ElapsedMilliseconds;
            _windowRemaining = expectedRemaining;
            _windowSource = source;
        }

        /// <summary>
        /// Assert that a burst window is expected to open in startsIn. Re-assert
        /// every pulse while the expectation holds.
        /// </summary>
        public static void ReportImminentBurst(TimeSpan startsIn, string source)
        {
            _imminentAssertedAtMs = Clock.ElapsedMilliseconds;
            _imminentStartsIn = startsIn;
        }

        #endregion

        #region Query side

        /// <summary>
        /// True while the current job reports an open burst window. Deliberately
        /// binary: consumers that would interject (phantom actions, items) should
        /// simply not act while this is true.
        /// </summary>
        public static bool InBurstWindow => _windowAssertedAtMs != long.MinValue
                                            && Clock.ElapsedMilliseconds - _windowAssertedAtMs <= AssertionTtlMs;

        /// <summary>
        /// Estimated time left in the open window. Zero when no window is open or
        /// the publisher gave no estimate. An estimate, not a contract.
        /// </summary>
        public static TimeSpan BurstWindowRemaining
        {
            get
            {
                if (!InBurstWindow)
                    return TimeSpan.Zero;

                var remaining = _windowRemaining - TimeSpan.FromMilliseconds(Clock.ElapsedMilliseconds - _windowAssertedAtMs);
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// True when the current job expects a burst window to open within the given
        /// time. Lets consumers avoid starting something that would land inside it.
        /// </summary>
        public static bool BurstImminent(TimeSpan within)
        {
            if (_imminentAssertedAtMs == long.MinValue)
                return false;

            if (Clock.ElapsedMilliseconds - _imminentAssertedAtMs > AssertionTtlMs)
                return false;

            var startsIn = _imminentStartsIn - TimeSpan.FromMilliseconds(Clock.ElapsedMilliseconds - _imminentAssertedAtMs);
            return startsIn <= within;
        }

        #endregion

        private static void LogTransitions()
        {
            var active = InBurstWindow;

            if (active == _windowWasActive)
                return;

            _windowWasActive = active;
            Logger.WriteInfo(active
                ? $"[BurstWindow] Open ({_windowSource}), ~{BurstWindowRemaining.TotalSeconds:F1}s remaining"
                : "[BurstWindow] Closed");
        }
    }
}

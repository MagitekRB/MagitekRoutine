using ff14bot;
using ff14bot.Managers;
using GreyMagic;
using Magitek.Models.Account;
using System;
using System.Diagnostics;

namespace Magitek.Utilities
{
    internal static class ZoomHack
    {
        private const int ValidationIntervalMilliseconds = 7000;

        private static readonly Stopwatch ValidationTimer = Stopwatch.StartNew();
        private static bool _refreshRequested;

        private const string MaxZoomOffsetPattern = "Search F3 0F 10 9F ? ? ? ? 4C 8D 44 24 Add 4 Read32";

        private static readonly bool offsetFound;
        private static readonly int Offset;
        static ZoomHack()
        {
            try
            {
                using var pf = new PatternFinder(Core.Memory);
                Offset = (int)pf.FindSingle(MaxZoomOffsetPattern, true);
                offsetFound = true;
            }
            catch
            {
                offsetFound = false;
                Logger.WriteInfo("ZoomHack Failed due to FFXIV Update");
            }
        }

        public static void Toggle()
        {
            _refreshRequested = false;
            ValidationTimer.Restart();

            if (!offsetFound)
                return;

            var cameraPointer = CameraManager.CameraPtr;
            if (cameraPointer == IntPtr.Zero)
                return;

            var desiredZoom = BaseSettings.Instance.ZoomHack ? 200f : 20f;
            var currentZoom = Core.Memory.Read<float>(cameraPointer + Offset);
            if (Math.Abs(currentZoom - desiredZoom) < 0.01f)
                return;

            Core.Memory.Write(cameraPointer + Offset, desiredZoom);
            Logger.WriteInfo($"ZoomHack {(BaseSettings.Instance.ZoomHack ? "Enabled" : "Disabled")}");
        }

        public static void RequestRefresh()
        {
            _refreshRequested = true;
        }

        public static void Pulse()
        {
            if (!BaseSettings.Instance.ZoomHack)
                return;

            if (!_refreshRequested && ValidationTimer.ElapsedMilliseconds < ValidationIntervalMilliseconds)
                return;

            Toggle();
        }
    }
}

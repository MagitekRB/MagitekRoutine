using ff14bot;
using ff14bot.Managers;
using GreyMagic;
using Magitek.Models.Account;
using System;

namespace Magitek.Utilities
{
    internal static class ZoomHack
    {
        private static bool _isEnabled;

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
            if (_isEnabled == BaseSettings.Instance.ZoomHack)
                return;

            if (!offsetFound)
                return;

            var status = BaseSettings.Instance.ZoomHack ? "Enabled" : "Disabled";
            Logger.WriteInfo($"ZoomHack {status}");
            Core.Memory.Write(CameraManager.CameraPtr + Offset, BaseSettings.Instance.ZoomHack ? 200f : 20f);
            _isEnabled = BaseSettings.Instance.ZoomHack;
        }
    }
}
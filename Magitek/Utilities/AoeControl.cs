using Magitek.Models.Account;

namespace Magitek.Utilities
{
    /// <summary>
    /// Global gate for AoE ability usage across every job.
    /// When disabled, all AoE damage decision points fall through to their
    /// single-target fallback. Per-job settings are left untouched, so
    /// re-enabling restores prior behavior exactly.
    ///
    /// Backed by the persisted <see cref="BaseSettings.EnableAoe"/> setting, so
    /// state survives restarts and stays in sync with the overlay checkbox.
    /// Intended to also be driven programmatically by external/third-party code:
    ///   Magitek.Utilities.AoeControl.Disable();
    ///   Magitek.Utilities.AoeControl.Enable();
    /// </summary>
    public static class AoeControl
    {
        public static bool Enabled
        {
            get => BaseSettings.Instance.EnableAoe;
            private set => BaseSettings.Instance.EnableAoe = value;
        }

        public static void Enable() => Enabled = true;

        public static void Disable() => Enabled = false;

        public static void Toggle() => Enabled = !Enabled;

        public static void Set(bool enabled) => Enabled = enabled;
    }
}

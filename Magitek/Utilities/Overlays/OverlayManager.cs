using ff14bot;
using Magitek.Models.Account;

namespace Magitek.Utilities.Overlays
{
    internal static class OverlayManager
    {
        private static MainOverlayUiComponent MainSettingsOverlay;
        private static CombatMessageUiComponent CombatMessageOverlay;
        private static PvpAggroCountUiComponent PvpAggroCountOverlay;

        public static void StartMainOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (!BaseSettings.Instance.UseOverlay)
                return;

            if (MainSettingsOverlay == null)
            {
                MainSettingsOverlay = new MainOverlayUiComponent();
            }

            Core.OverlayManager.AddUIComponent(MainSettingsOverlay);
        }

        public static void StopMainOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (MainSettingsOverlay != null)
            {
                Core.OverlayManager.RemoveUIComponent(MainSettingsOverlay);
            }

            MainSettingsOverlay = null;
        }

        public static void RestartMainOverlay()
        {
            StopMainOverlay();
            StartMainOverlay();
        }

        public static void StartCombatMessageOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (!BaseSettings.Instance.UseCombatMessageOverlay)
                return;

            if (CombatMessageOverlay == null)
            {
                CombatMessageOverlay = new CombatMessageUiComponent(BaseSettings.Instance.CombatMessageOverlayAdjustable);
            }

            Core.OverlayManager.AddUIComponent(CombatMessageOverlay);
        }

        public static void StopCombatMessageOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (CombatMessageOverlay != null)
            {
                Core.OverlayManager.RemoveUIComponent(CombatMessageOverlay);
            }

            CombatMessageOverlay = null;
        }

        public static void RestartCombatMessageOverlay()
        {
            StopCombatMessageOverlay();
            StartCombatMessageOverlay();
        }

        public static void StartPvpAggroCountOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (!BaseSettings.Instance.UsePvpAggroCountOverlay)
                return;

            if (PvpAggroCountOverlay == null)
            {
                PvpAggroCountOverlay = new PvpAggroCountUiComponent(BaseSettings.Instance.PvpAggroCountOverlayAdjustable);
            }

            Core.OverlayManager.AddUIComponent(PvpAggroCountOverlay);
        }

        public static void StopPvpAggroCountOverlay()
        {
            if (!Core.OverlayManager.IsActive)
                return;

            if (PvpAggroCountOverlay != null)
            {
                Core.OverlayManager.RemoveUIComponent(PvpAggroCountOverlay);
            }

            PvpAggroCountOverlay = null;
        }

        public static void RestartPvpAggroCountOverlay()
        {
            StopPvpAggroCountOverlay();
            StartPvpAggroCountOverlay();
        }
    }
}

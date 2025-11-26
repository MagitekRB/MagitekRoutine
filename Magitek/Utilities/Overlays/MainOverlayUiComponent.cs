using Buddy.Overlay;
using Buddy.Overlay.Controls;
using Magitek.Models.Account;
using Magitek.Views.UserControls;
using System.Windows;

namespace Magitek.Utilities.Overlays
{
    internal class MainOverlayUiComponent : OverlayUIComponent
    {
        public MainOverlayUiComponent() : base(true) { }

        private OverlayControl _control;

        public override OverlayControl Control
        {
            get
            {
                if (_control != null)
                    return _control;

                MainSettingsOverlay overlayUc;
                try
                {
                    overlayUc = new MainSettingsOverlay();
                }
                catch (System.Exception)
                {
                    // XAML resources not available (hot-reload with GUID temp file)
                    // Return empty control to prevent crash
                    return new OverlayControl() { Name = "MagitekOverlay_Unavailable" };
                }

                overlayUc.BtnOpenSettings.Click += (sender, args) =>
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        // Validate window position before showing
                        Models.Account.BaseSettings.ValidateSettingsWindowPosition(1000, 700);

                        if (!Magitek.Form.IsVisible)
                            Magitek.Form.Show();

                        Magitek.Form.Activate();
                    });
                };

                _control = new OverlayControl()
                {
                    Name = "MagitekOverlay",
                    Content = overlayUc,
                    Width = overlayUc.Width + 5,
                    X = BaseSettings.Instance.OverlayPosX,
                    Y = BaseSettings.Instance.OverlayPosY,
                    AllowMoving = true
                };

                _control.MouseLeave += (sender, args) =>
                {
                    BaseSettings.Instance.OverlayPosX = _control.X;
                    BaseSettings.Instance.OverlayPosY = _control.Y;
                    BaseSettings.Instance.Save();
                };

                _control.MouseLeftButtonDown += (sender, args) =>
                {
                    _control.DragMove();
                };

                return _control;
            }
        }
    }
}

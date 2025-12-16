using Buddy.Overlay;
using Buddy.Overlay.Controls;
using ff14bot;
using Magitek.Models.Account;
using Magitek.Views.UserControls;

namespace Magitek.Utilities.Overlays
{
    internal class PvpAggroCountUiComponent : OverlayUIComponent
    {
        public PvpAggroCountUiComponent(bool isHitTestable) : base(isHitTestable) { }

        private OverlayControl _control;

        public override OverlayControl Control
        {
            get
            {
                if (_control != null)
                    return _control;

                PvpAggroCountOverlay overlayUc;
                try
                {
                    overlayUc = new PvpAggroCountOverlay();
                }
                catch (System.Exception)
                {
                    // XAML resources not available (hot-reload with GUID temp file)
                    // Return empty control to prevent crash
                    return new OverlayControl() { Name = "MagitekPvpAggroCountOverlay_Unavailable" };
                }

                double width = BaseSettings.Instance.PvpAggroCountOverlayWidth;
                double height = BaseSettings.Instance.PvpAggroCountOverlayHeight;
                double posX = BaseSettings.Instance.PvpAggroCountOverlayPosX;
                double posY = BaseSettings.Instance.PvpAggroCountOverlayPosY;
                if (width < 0 || height < 0 || posX < 0 || posY < 0)
                {
                    /* Default (or invalid) values - take a reasonable guess where it should go */
                    width = 200;
                    height = 200;
                    posX = 60; // Top-left area
                    posY = 60; // Top-left area
                }

                _control = new OverlayControl()
                {
                    Name = "MagitekPvpAggroCountOverlay",
                    Content = overlayUc,
                    Width = width,
                    Height = height,
                    X = posX,
                    Y = posY,
                    AllowMoving = IsHitTestable,
                    AllowResizing = IsHitTestable
                };

                _control.MouseLeave += (sender, args) =>
                {
                    if (IsHitTestable)
                    {
                        BaseSettings.Instance.PvpAggroCountOverlayWidth = _control.Width;
                        BaseSettings.Instance.PvpAggroCountOverlayHeight = _control.Height;
                        BaseSettings.Instance.PvpAggroCountOverlayPosX = _control.X;
                        BaseSettings.Instance.PvpAggroCountOverlayPosY = _control.Y;
                        BaseSettings.Instance.Save();
                    }
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


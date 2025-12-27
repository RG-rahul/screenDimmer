using DimmerBeyond;
using System.Runtime.InteropServices;

namespace ScreenDimmer.Handlers
{
    internal class OverlayHandler
    {
        private int _cachedOpacityPercent;
        public OverlayHandler(int cachedOpacityPercent)
        {
            _cachedOpacityPercent = cachedOpacityPercent;
        }

        private Form? _overlayForm;

        // WinAPI imports
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public void SetupOverlayClickThrough(IntPtr handle)
        {
            int exStyle = (int)GetWindowLong(handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(handle, GWL_EXSTYLE, (IntPtr)exStyle);
        }

        public void ConfigureOverlayForm(Form form)
        {
            _overlayForm = form;
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Maximized;
            form.TopMost = true;
            form.BackColor = Color.Black;
            form.Opacity = _cachedOpacityPercent / 100.0;
            form.ShowInTaskbar = false;
        }

        public void SetOpacity(double opacity)
        {
            _overlayForm?.Invoke(() => _overlayForm.Opacity = opacity);
        }
    }
}

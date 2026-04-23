using ScreenDimmer.Handlers;

namespace ScreenDimmer
{
    public partial class ScreenDimmerForm : Form
    {
        private readonly OverlayHandler _overlayHandler;
        private readonly Screen _screen;

        public ScreenDimmerForm(Screen screen, int cachedOpacityPercent)
        {
            InitializeComponent();
            _screen = screen;
            _overlayHandler = new OverlayHandler(cachedOpacityPercent);
            InitializeApp();
        }

        private void InitializeApp()
        {
            _overlayHandler.ConfigureOverlayForm(this, _screen);
        }

        public void SetOpacity(double opacity)
        {
            _overlayHandler.SetOpacity(opacity);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _overlayHandler.SetupOverlayClickThrough(this.Handle);
        }

        // Not needed
        //protected override void SetVisibleCore(bool value)
        //{
        //    // Keep the form hidden but functional
        //    base.SetVisibleCore(true);
        //}
    }
}
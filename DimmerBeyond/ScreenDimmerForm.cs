using DimmerBeyond.Handlers;
using ScreenDimmer.Handlers;

namespace ScreenDimmer
{
    public partial class ScreenDimmerForm : Form
    {
        private readonly OverlayHandler _overlayHandler;
        private readonly TrayHandler _trayHandler;
        private readonly CacheHandler _cacheHandler = new();

        public ScreenDimmerForm()
        {
            InitializeComponent();
            var cachedOpacityPercent = _cacheHandler.GetOpacityFromCache();
            _overlayHandler = new OverlayHandler(cachedOpacityPercent);
            _trayHandler = new TrayHandler(OnOpacityChanged, UpdateOpacityPercentCache, cachedOpacityPercent);

            InitializeApp();
        }

        private void InitializeApp()
        {
            _overlayHandler.ConfigureOverlayForm(this);
        }

        private void OnOpacityChanged(double opacity)
        {
            _overlayHandler.SetOpacity(opacity);
        }

        private void UpdateOpacityPercentCache(int opacityPercent)
        {
            _cacheHandler.SaveOpacityToCache(opacityPercent);
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
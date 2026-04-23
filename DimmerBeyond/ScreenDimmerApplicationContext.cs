using DimmerBeyond;
using DimmerBeyond.Handlers;
using DimmerBeyond.Records;
using Microsoft.Win32;
using ScreenDimmer.Handlers;

namespace ScreenDimmer
{
    internal class ScreenDimmerApplicationContext : ApplicationContext
    {
        private readonly CacheHandler _cacheHandler = new();
        private readonly Dictionary<string, ScreenDimmerForm> _overlayFormsByScreen = new();
        private readonly SynchronizationContext _uiContext;
        private Dictionary<string, ScreenDimmerSettings> _screenSettingsByDeviceName = new();
        private TrayHandler? _trayHandler;
        private bool _isRefreshing;
        private bool _refreshRequested;
        private bool _isDisposed;

        public ScreenDimmerApplicationContext()
        {
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _screenSettingsByDeviceName = _cacheHandler.GetScreenSettingsFromCache();

            RebuildScreenDimmers();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            _uiContext.Post(_ => RequestRebuildScreenDimmers(), null);
        }

        private void RequestRebuildScreenDimmers()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_isRefreshing)
            {
                _refreshRequested = true;
                return;
            }

            RebuildScreenDimmers();
        }

        private void RebuildScreenDimmers()
        {
            if (_isDisposed || _isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                PersistScreenSettings();

                var screens = Screen.AllScreens
                    .OrderByDescending(screen => screen.Primary)
                    .ThenBy(screen => screen.DeviceName)
                    .ToList();

                _screenSettingsByDeviceName = BuildScreenSettingsByDeviceName(screens, _screenSettingsByDeviceName);

                SyncOverlayForms(screens);

                _trayHandler?.Dispose();
                _trayHandler = new TrayHandler(
                    screens,
                    OnOpacityChanged,
                    UpdateScreenSettingsCache,
                    _screenSettingsByDeviceName);

                PersistScreenSettings();
            }
            finally
            {
                _isRefreshing = false;
                if (_refreshRequested && !_isDisposed)
                {
                    _refreshRequested = false;
                    RequestRebuildScreenDimmers();
                }
            }
        }

        private void PersistScreenSettings()
        {
            _cacheHandler.SaveScreenSettingsToCache(_screenSettingsByDeviceName);
        }

        private void SyncOverlayForms(IReadOnlyList<Screen> screens)
        {
            var activeScreenKeys = screens
                .Select(screen => screen.DeviceName)
                .ToHashSet();

            var removedScreenKeys = _overlayFormsByScreen.Keys
                .Where(screenKey => !activeScreenKeys.Contains(screenKey))
                .ToList();

            foreach (var screenKey in removedScreenKeys)
            {
                _overlayFormsByScreen[screenKey].Dispose();
                _overlayFormsByScreen.Remove(screenKey);
            }

            foreach (var screen in screens)
            {
                var screenKey = screen.DeviceName;
                var screenSettings = _screenSettingsByDeviceName[screenKey];

                if (!_overlayFormsByScreen.TryGetValue(screenKey, out var form))
                {
                    form = new ScreenDimmerForm(screen, screenSettings.OpacityPercent);
                    form.Show();
                    _overlayFormsByScreen[screenKey] = form;
                }

                form.SetOpacity(screenSettings.Enabled ? screenSettings.OpacityPercent / 100.0 : 0);
            }
        }

        private void DisposeCurrentUi()
        {
            _trayHandler?.Dispose();
            _trayHandler = null;

            foreach (var form in _overlayFormsByScreen.Values)
            {
                form.Dispose();
            }

            _overlayFormsByScreen.Clear();
        }

        private static Dictionary<string, ScreenDimmerSettings> BuildScreenSettingsByDeviceName(
            IReadOnlyList<Screen> screens,
            Dictionary<string, ScreenDimmerSettings> cachedScreenSettingsByDeviceName)
        {
            var screenSettingsByDeviceName = cachedScreenSettingsByDeviceName
                .ToDictionary(
                    item => item.Key,
                    item => new ScreenDimmerSettings
                    {
                        OpacityPercent = Math.Clamp(item.Value.OpacityPercent, 0, 80),
                        Enabled = item.Value.Enabled
                    });

            int fallbackOpacityPercent = (int)(Constants.DefaultOpacity * 100);

            foreach (var screen in screens)
            {
                if (cachedScreenSettingsByDeviceName.TryGetValue(screen.DeviceName, out var cachedScreenSettings))
                {
                    screenSettingsByDeviceName[screen.DeviceName] = new ScreenDimmerSettings
                    {
                        OpacityPercent = Math.Clamp(cachedScreenSettings.OpacityPercent, 0, 80),
                        Enabled = cachedScreenSettings.Enabled
                    };
                }
                else
                {
                    screenSettingsByDeviceName[screen.DeviceName] = new ScreenDimmerSettings
                    {
                        OpacityPercent = Math.Clamp(fallbackOpacityPercent, 0, 80),
                        Enabled = true
                    };
                }
            }

            return screenSettingsByDeviceName;
        }

        private void OnOpacityChanged(string screenKey, double opacity)
        {
            if (_overlayFormsByScreen.TryGetValue(screenKey, out var form))
            {
                form.SetOpacity(opacity);
            }
        }

        private void UpdateScreenSettingsCache(Dictionary<string, ScreenDimmerSettings> screenSettingsByDeviceName)
        {
            _screenSettingsByDeviceName = screenSettingsByDeviceName;
            _cacheHandler.SaveScreenSettingsToCache(screenSettingsByDeviceName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                PersistScreenSettings();
                DisposeCurrentUi();
            }

            base.Dispose(disposing);
        }
    }
}

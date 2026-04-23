using DimmerBeyond;
using DimmerBeyond.Handlers;
using DimmerBeyond.Records;
using ScreenDimmer.Handlers;

namespace ScreenDimmer
{
    internal class ScreenDimmerApplicationContext : ApplicationContext
    {
        private readonly CacheHandler _cacheHandler = new();
        private readonly Dictionary<string, ScreenDimmerForm> _overlayFormsByScreen = new();
        private readonly TrayHandler _trayHandler;

        public ScreenDimmerApplicationContext()
        {
            var screens = Screen.AllScreens
                .OrderByDescending(screen => screen.Primary)
                .ThenBy(screen => screen.DeviceName)
                .ToList();

            var cachedScreenSettingsByDeviceName = _cacheHandler.GetScreenSettingsFromCache();
            var effectiveScreenSettingsByDeviceName = BuildScreenSettingsByDeviceName(screens, cachedScreenSettingsByDeviceName);

            foreach (var screen in screens)
            {
                var screenKey = screen.DeviceName;
                var screenSettings = effectiveScreenSettingsByDeviceName[screenKey];
                var form = new ScreenDimmerForm(screen, screenSettings.OpacityPercent);
                form.Show();
                if (!screenSettings.Enabled)
                {
                    form.SetOpacity(0);
                }
                _overlayFormsByScreen[screenKey] = form;
            }

            _trayHandler = new TrayHandler(
                screens,
                OnOpacityChanged,
                UpdateScreenSettingsCache,
                effectiveScreenSettingsByDeviceName);
        }

        private static Dictionary<string, ScreenDimmerSettings> BuildScreenSettingsByDeviceName(
            IReadOnlyList<Screen> screens,
            Dictionary<string, ScreenDimmerSettings> cachedScreenSettingsByDeviceName)
        {
            var screenSettingsByDeviceName = new Dictionary<string, ScreenDimmerSettings>();
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
            _cacheHandler.SaveScreenSettingsToCache(screenSettingsByDeviceName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayHandler.Dispose();
                foreach (var form in _overlayFormsByScreen.Values)
                {
                    form.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}

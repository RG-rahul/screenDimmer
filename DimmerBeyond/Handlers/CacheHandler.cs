using DimmerBeyond.Records;
using System.Text.Json;

namespace DimmerBeyond.Handlers
{
    internal class CacheHandler
    {
        private readonly JsonSerializerOptions _serializeOptions = new() { WriteIndented = true };

        public CacheHandler()
        {
            // Ensure cache directory exists
            var cacheDir = Path.GetDirectoryName(Constants.CacheDataFilePath);
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir!);
            }
        }

        public Dictionary<string, ScreenDimmerSettings> GetScreenSettingsFromCache()
        {
            try
            {
                if (File.Exists(Constants.CacheDataFilePath))
                {
                    var json = File.ReadAllText(Constants.CacheDataFilePath);
                    var cacheData = JsonSerializer.Deserialize<CacheData>(json);
                    if (cacheData?.ScreenSettingsByDeviceName != null)
                    {
                        return cacheData.ScreenSettingsByDeviceName;
                    }
                }
            }
            catch
            {
                // Handle exceptions silently and return default
            }

            return new Dictionary<string, ScreenDimmerSettings>();
        }

        public void SaveScreenSettingsToCache(Dictionary<string, ScreenDimmerSettings> screenSettingsByDeviceName)
        {
            try
            {
                var cacheData = new CacheData
                {
                    ScreenSettingsByDeviceName = screenSettingsByDeviceName
                };

                File.WriteAllText(Constants.CacheDataFilePath, JsonSerializer.Serialize(cacheData, _serializeOptions));
            }
            catch
            {
                // Handle exceptions silently
            }
        }
    }
}

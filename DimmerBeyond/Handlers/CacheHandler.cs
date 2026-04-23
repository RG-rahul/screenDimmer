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

        public int GetOpacityFromCache()
        {
            try
            {
                if (File.Exists(Constants.CacheDataFilePath))
                {
                    var json = File.ReadAllText(Constants.CacheDataFilePath);
                    var cacheData = JsonSerializer.Deserialize<CacheData>(json);
                    if (cacheData != null)
                    {
                        return cacheData.OpacityPercent;
                    }
                }
            }
            catch
            {
                // Handle exceptions silently and return default
            }

            return (int)(Constants.DefaultOpacity * 100);
        }

        public void SaveOpacityToCache(int opacityPercent)
        {
            try
            {
                var cacheData = new CacheData
                {
                    OpacityPercent = opacityPercent
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

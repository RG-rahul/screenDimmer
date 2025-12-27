namespace DimmerBeyond
{
    internal static class Constants
    {
        public static double DefaultOpacity { get; } = 0.7; // 0.1 to 0.8


        private static string ProgramDataPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static string CacheDataFilePath { get; } = Path.Combine(ProgramDataPath, "RGAppData", "DimmerBeyond", "DimmerCache.json");

    }
}

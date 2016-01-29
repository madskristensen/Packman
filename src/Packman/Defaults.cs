namespace Packman
{
    public static class Defaults
    {
        public const string ManifestFileName = "packman.json";

        public static int CacheDays { get; set; } = 3;
        public static string CachePath { get; set; } = "%userprofile%\\.packman";
    }
}

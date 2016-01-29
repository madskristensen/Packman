namespace Packman
{
    public static class Defaults
    {
        public static string ManifestFileName { get; set; } = "packman.json";
        public static string DefaultLocalPath { get; set; } = "lib";

        public static int CacheDays { get; set; } = 3;
        public static string CachePath { get; set; } = "%userprofile%\\.packman";
    }
}

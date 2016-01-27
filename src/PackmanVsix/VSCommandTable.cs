namespace PackmanVsix
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidLibrarianPackageString = "ce753d0f-f511-4b2b-93de-5cc50145dca6";
        public const string guidLibrarianCmdSetString = "9056cd3b-314d-462b-888e-95801ee4b05b";
        public const string guidImagesString = "5b696664-fde3-433c-982c-32bf4ab02398";
        public static Guid guidLibrarianPackage = new Guid(guidLibrarianPackageString);
        public static Guid guidLibrarianCmdSet = new Guid(guidLibrarianCmdSetString);
        public static Guid guidImages = new Guid(guidImagesString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int ContextMenuGroup = 0x1020;
        public const int InstallLibrary = 0x0100;
        public const int RestoreAll = 0x0200;
        public const int logo = 0x0001;
    }
}

using System;
using System.Windows.Media;

namespace Packman.Providers
{
    internal class PackageInfo : IPackageInfo
    {
        public PackageInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }

        public string Description { get; }

        public string Homepage { get; set; }

        public ImageSource Icon { get; set; }
    }
}

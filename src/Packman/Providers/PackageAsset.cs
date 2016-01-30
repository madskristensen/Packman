using System.Collections.Generic;

namespace Packman
{
    public class PackageAsset : IPackageAsset
    {
        public IEnumerable<string> Files { get; set; }
        public string Version { get; set; }
    }
}

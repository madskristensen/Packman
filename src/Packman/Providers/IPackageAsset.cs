using System.Collections.Generic;

namespace Packman
{
    public interface IPackageAsset
    {
        IEnumerable<string> Files { get; set; }
        string Version { get; set; }
    }
}
using System.Collections.Generic;

namespace Packman
{
    public interface IInstallablePackage
    {
        IEnumerable<string> Files { get; }
        int TotalFileCount { get; }
        string Name { get; }
    }
}
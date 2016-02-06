using System.Collections.Generic;
using System.Linq;

namespace Packman
{
    class UrlPackage : IInstallablePackage
    {
        public string Name { get; set; }
        public IEnumerable<string> Files { get; set; }
        public int TotalFileCount
        {
            get { return Files?.Count() ?? 0; }
        }
    }
}

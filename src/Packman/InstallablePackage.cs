using System.Collections.Generic;

namespace Packman
{
    public class InstallablePackage
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string MainFile { get; set; }
        public IEnumerable<string> Files { get; set; }
    }
}

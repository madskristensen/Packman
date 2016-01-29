using System;

namespace Packman
{
    public class PackageNotFoundException : Exception
    {
        public PackageNotFoundException()
        {

        }

        public PackageNotFoundException(string name, string version)
        {
            Name = name;
            Version = version;
        }
        public string Name { get; set; }
        public string Version { get; set; }
    }
}

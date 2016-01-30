using System;

namespace Packman
{
    public class InstallEventArgs : EventArgs
    {
        public InstallEventArgs(InstallablePackage package, string path)
        {
            Package = package;
            Path = path;
        }

        public InstallablePackage Package { get; }

        public string Path { get; }
    }
}

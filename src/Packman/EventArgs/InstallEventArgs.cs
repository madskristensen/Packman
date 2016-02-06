using System;

namespace Packman
{
    public class InstallEventArgs : EventArgs
    {
        public InstallEventArgs(IInstallablePackage package, string path)
        {
            Package = package;
            Path = path;
        }

        public IInstallablePackage Package { get; }

        public string Path { get; }
    }
}

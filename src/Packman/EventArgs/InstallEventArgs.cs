using System;

namespace Packman
{
    public class InstallEventArgs : EventArgs
    {
        public InstallEventArgs(IInstallablePackage package, string path)
            : this(null, package, path)
        { }

        public InstallEventArgs(Manifest manifest, IInstallablePackage package, string path)
        {
            Manifest = manifest;
            Package = package;
            Path = path;
        }

        public Manifest Manifest { get; }

        public IInstallablePackage Package { get; }

        public string Path { get; }
    }
}

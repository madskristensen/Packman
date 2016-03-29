using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Packman.Providers;

namespace Packman
{
    public interface IPackageProvider
    {
        string Name { get; }

        Task<InstallablePackage> GetInstallablePackageAsync(string packageName, string version);
        Task<IEnumerable<string>> GetPackageNamesAsync();
        Task<IEnumerable<string>> GetVersionsAsync(string packageName);
        Task<bool> InitializeAsync();
        bool IsInitialized { get; }
        Task<IPackageInfo> GetPackageInfoAsync(string packageName);
    }
}
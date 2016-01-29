using System.Collections.Generic;
using System.Threading.Tasks;

namespace Packman
{
    public interface IPackageProvider
    {
        string Name { get; }

        Task<InstallablePackage> GetInstallablePackage(string packageName, string version);
        Task<IEnumerable<string>> GetPackageNamesAsync();
        Task<IEnumerable<string>> GetVersionsAsync(string packageName);
        Task<IPackageMetaData> GetPackageMetaDataAsync(string packageName);
        Task InitializeAsync();
        bool IsInitialized { get; }
    }
}
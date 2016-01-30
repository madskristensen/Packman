using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Packman
{
    public class CdnjsProvider : IPackageProvider
    {
        const string _remoteApiUrl = "https://api.cdnjs.com/libraries?fields=name";

        readonly string _localPath;
        IEnumerable<CdnjsPackage> _packages;
        static AsyncLock _mutex = new AsyncLock();

        public CdnjsProvider()
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            _localPath = Path.Combine(rootCacheDir, Name, "cache.json");
        }

        public bool IsInitialized
        {
            get { return _packages != null; }
        }

        public string Name
        {
            get { return "Cdnjs"; }
        }

        public async Task<IEnumerable<string>> GetVersionsAsync(string packageName)
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
                return null;

            var metadata = await package.GetPackageMetaData(Name).ConfigureAwait(false);

            return metadata?.Assets.Select(a => a.Version);
        }

        public async Task<InstallablePackage> GetInstallablePackage(string packageName, string version)
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return await package.ToInstallablePackageAsync(version, Name);
        }

        public async Task<IEnumerable<string>> GetPackageNamesAsync()
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            return _packages.Select(p => p.Name);
        }

        public async Task<IPackageMetaData> GetPackageMetaDataAsync(string packageName)
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return await package.GetPackageMetaData(Name);
        }

        public async Task<bool> InitializeAsync()
        {
            using (await _mutex.LockAsync().ConfigureAwait(false))
            {
                try
                {
                    IEnumerable<CdnjsPackage> packages;

                    if (IsCachedVersionOld(out packages))
                    {
                        Directory.CreateDirectory(new FileInfo(_localPath).DirectoryName);

                        using (WebClient client = new WebClient())
                        {
                            await client.DownloadFileTaskAsync(_remoteApiUrl, _localPath).ConfigureAwait(false);
                        }
                    }

                    if (packages == null)
                    {
                        using (StreamReader reader = new StreamReader(_localPath))
                        {
                            string json = await reader.ReadToEndAsync();
                            var obj = (JObject)JsonConvert.DeserializeObject(json);

                            packages = JsonConvert.DeserializeObject<IEnumerable<CdnjsPackage>>(obj["results"].ToString());
                        }
                    }

                    var comparer = new PackageNameComparer();
                    var distinct = new List<CdnjsPackage>();

                    // For some reason, the Cdnjs api returns duplicate entries
                    foreach (var package in packages.OrderBy(p => p.Name, comparer))
                    {
                        if (!distinct.Any(p => p.Name == package.Name))
                        {
                            distinct.Add(package);
                        }
                    }

                    _packages = distinct;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        bool IsCachedVersionOld(out IEnumerable<CdnjsPackage> packages)
        {
            var file = new FileInfo(_localPath);
            packages = null;

            if (!file.Exists)
                return true;

            bool isTooOld = File.GetLastWriteTime(_localPath) > DateTime.Now.AddDays(Defaults.CacheDays);

            if (isTooOld)
                return true;

            try
            {
                string json = File.ReadAllText(_localPath);
                packages = JsonConvert.DeserializeObject<IEnumerable<CdnjsPackage>>(json);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}

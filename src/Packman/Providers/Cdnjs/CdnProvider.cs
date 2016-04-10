using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Packman.Providers;

namespace Packman
{
    public class CdnjsProvider : IPackageProvider
    {
        const string _remoteApiUrl = "https://api.cdnjs.com/libraries?fields=name";

        readonly string _localPath;
        IEnumerable<CdnjsPackage> _packages;
        static AsyncLock _mutex = new AsyncLock();
        private Dictionary<string, Lazy<Task<IPackageInfo>>> _searchPackages;

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

        public async Task<IPackageInfo> GetPackageInfoAsync(string packageName)
        {
            if (!IsInitialized && !await InitializeAsync().ConfigureAwait(false))
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package != null)
            {
                Lazy<Task<IPackageInfo>> packageInfo;
                if (_searchPackages.TryGetValue(packageName, out packageInfo))
                {
                    return await packageInfo.Value;
                }
            }

            return null;
        }

        public string GetAlias(string s)
        {
            switch (s)
            {
                case "twitter-bootstrap":
                    return "bootstrap";
                default:
                    return null;
            }
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

        public async Task<InstallablePackage> GetInstallablePackageAsync(string packageName, string version)
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return await package.ToInstallablePackageAsync(version, Name).ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> GetPackageNamesAsync()
        {
            if (!IsInitialized && !await InitializeAsync())
                return null;

            return _packages.Select(p => p.Name);
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
                    Dictionary<string, Lazy<Task<IPackageInfo>>> infos = new Dictionary<string, Lazy<Task<IPackageInfo>>>();

                    foreach (CdnjsPackage package in distinct)
                    {
                        CdnjsPackage local = package;
                        infos[local.Name] = new Lazy<Task<IPackageInfo>>(async () =>
                        {
                            IPackageMetaData metadata = await local.GetPackageMetaData(Name);
                            return new PackageInfo(local.Name, metadata.Description)
                            {
                                Homepage = metadata.Homepage
                            };
                        });
                    }

                    _searchPackages = infos;
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

            bool isTooOld = File.GetLastWriteTime(_localPath).AddDays(Defaults.CacheDays) < DateTime.Now;

            if (isTooOld)
                return true;

            try
            {
                string json = File.ReadAllText(_localPath);
                packages = JsonConvert.DeserializeObject<IEnumerable<CdnjsPackage>>(json);
                return packages == null;
            }
            catch
            {
                return true;
            }
        }
    }
}

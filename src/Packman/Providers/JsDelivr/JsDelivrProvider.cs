using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Packman.Providers;

namespace Packman
{
    public class JsDelivrProvider : IPackageProvider
    {
        const string _remoteApiUrl = "http://api.jsdelivr.com/v1/jsdelivr/libraries?fields=name,versions";
        const string _urlFormat = "https://cdn.jsdelivr.net/{0}/{1}/{2}";

        readonly string _localPath;
        IEnumerable<JsDelivrPackage> _packages;
        static AsyncLock _mutex = new AsyncLock();

        public JsDelivrProvider()
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
            get { return "JsDelivr"; }
        }

        public async Task<IPackageInfo> GetPackageInfoAsync(string packageName)
        {
            if (!IsInitialized && !await InitializeAsync().ConfigureAwait(false))
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package != null)
            {
                return new PackageInfo(package.Name, "(No Description)");
            }

            return null;
        }

        public async Task<IEnumerable<string>> GetVersionsAsync(string packageName)
        {
            if (!IsInitialized && !await InitializeAsync().ConfigureAwait(false))
                return null;

            var package = _packages.FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return package?.Versions;
        }

        public async Task<InstallablePackage> GetInstallablePackageAsync(string packageName, string version)
        {
            if (!IsInitialized && !await InitializeAsync().ConfigureAwait(false))
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
            if (!IsInitialized && !await InitializeAsync().ConfigureAwait(false))
                return null;

            return _packages.Select(p => p.Name);
        }

        public async Task<bool> InitializeAsync()
        {
            using (await _mutex.LockAsync())
            {
                try
                {
                    IEnumerable<JsDelivrPackage> packages;

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
                            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                            packages = JsonConvert.DeserializeObject<IEnumerable<JsDelivrPackage>>(json);
                        }
                    }

                    var comparer = new PackageNameComparer();
                    _packages = packages.OrderBy(p => p.Name, comparer);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        bool IsCachedVersionOld(out IEnumerable<JsDelivrPackage> packages)
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
                packages = JsonConvert.DeserializeObject<IEnumerable<JsDelivrPackage>>(json);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}

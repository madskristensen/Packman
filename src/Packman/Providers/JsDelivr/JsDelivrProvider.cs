using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public async Task<IEnumerable<string>> GetVersionsAsync(string packageName)
        {
            if (!IsInitialized)
                await InitializeAsync();

            var package = _packages.SingleOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return package?.Versions;
        }

        public async Task<InstallablePackage> GetInstallablePackage(string packageName, string version)
        {
            if (!IsInitialized)
                await InitializeAsync();

            var package = _packages.SingleOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return await package.ToInstallablePackageAsync(version, Name);
        }

        public async Task<IEnumerable<string>> GetPackageNamesAsync()
        {
            if (!IsInitialized)
                await InitializeAsync();

            return _packages.Select(p => p.Name);
        }

        public async Task<IPackageMetaData> GetPackageMetaDataAsync(string packageName)
        {
            if (!IsInitialized)
                await InitializeAsync();

            var package = _packages.SingleOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return package.GetPackageMetaData(Name);
        }

        public async Task InitializeAsync()
        {
            using (await _mutex.LockAsync())
            {
                if (IsCachedVersionOld())
                {
                    Directory.CreateDirectory(new FileInfo(_localPath).DirectoryName);

                    using (WebClient client = new WebClient())
                    {
                        await client.DownloadFileTaskAsync(_remoteApiUrl, _localPath);
                    }
                }

                using (StreamReader reader = new StreamReader(_localPath))
                {
                    string json = await reader.ReadToEndAsync();
                    var packages = JsonConvert.DeserializeObject<IEnumerable<JsDelivrPackage>>(json);

                    var comparer = new PackageNameComparer();
                    _packages = packages.OrderBy(p => p.Name, comparer);
                }
            }
        }

        bool IsCachedVersionOld()
        {
            var file = new FileInfo(_localPath);

            if (!file.Exists || file.Length < 1000)
                return true;

            return File.GetLastWriteTime(_localPath) > DateTime.Now.AddDays(Defaults.CacheDays);
        }
    }
}

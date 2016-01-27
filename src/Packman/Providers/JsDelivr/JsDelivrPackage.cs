using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace Packman
{
    class JsDelivrPackage
    {
        const string _metaPackageUrlFormat = "http://api.jsdelivr.com/v1/jsdelivr/libraries?name={0}";
        const string _downloadUrlFormat = "https://cdn.jsdelivr.net/{0}/{1}/{2}";

        public string Name { get; set; }
        public IEnumerable<string> Versions { get; set; }

        public async Task<InstallablePackage> ToInstallablePackageAsync(string version, string providerName)
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string dir = Path.Combine(rootCacheDir, providerName, Name, version);
            var asset = await GetAsset(version, providerName);

            if (!Directory.Exists(dir))
            {
                var list = new List<Task>();

                foreach (string fileName in asset.Files)
                {
                    string url = string.Format(_downloadUrlFormat, Name, asset.Version, fileName);
                    var localFile = new FileInfo(Path.Combine(dir, fileName));

                    localFile.Directory.Create();

                    using (WebClient client = new WebClient())
                    {
                        var task = client.DownloadFileTaskAsync(url, localFile.FullName);
                        list.Add(task);
                    }
                }

                await Task.WhenAll(list);
            }

            return new InstallablePackage
            {
                Name = Name,
                Version = asset.Version,
                Files = asset.Files,
                MainFile = asset.MainFile
            };
        }

        async Task<JsDelivrAsset> GetAsset(string version, string providerName)
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string metaPath = Path.Combine(rootCacheDir, providerName, Name, "metadata.json");
            var metaFile = new FileInfo(metaPath);
            JsDelivrAsset asset = null;

            if (metaFile.Exists && metaFile.Length > 0)
            {
                asset = await GetAssetFromDisk(version, metaFile);
            }

            if (asset == null)
            {
                metaFile.Directory.Create();

                using (WebClient client = new WebClient())
                {
                    string url = string.Format(_metaPackageUrlFormat, Name);
                    await client.DownloadFileTaskAsync(url, metaFile.FullName);
                }

                asset = await GetAssetFromDisk(version, metaFile);
            }

            return asset;
        }

        static async Task<JsDelivrAsset> GetAssetFromDisk(string version, FileInfo metaFile)
        {
            using (StreamReader reader = metaFile.OpenText())
            {
                string json = await reader.ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<List<JsDelivrMetaData>>(json).FirstOrDefault();
                return data.Assets.SingleOrDefault(a => a.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

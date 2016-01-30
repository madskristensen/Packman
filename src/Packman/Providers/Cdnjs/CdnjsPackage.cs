using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace Packman
{
    class CdnjsPackage
    {
        const string _metaPackageUrlFormat = "https://api.cdnjs.com/libraries/{0}";
        const string _downloadUrlFormat = "https://cdnjs.cloudflare.com/ajax/libs/{0}/{1}/{2}";

        public string Name { get; set; }

        public async Task<InstallablePackage> ToInstallablePackageAsync(string version, string providerName)
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string dir = Path.Combine(rootCacheDir, providerName, Name, version);
            var asset = await GetAsset(version, providerName);

            if (asset == null)
            {
                return null;
            }

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
                        try
                        {
                            var task = client.DownloadFileTaskAsync(url, localFile.FullName);
                            list.Add(task);
                        }
                        catch
                        {
                        }
                    }
                }

                try
                {
                    await Task.WhenAll(list);
                }
                catch
                {
                    return null;
                }
            }

            var package = new InstallablePackage
            {
                Name = Name,
                Version = asset.Version,
                Files = asset.Files,
                AllFiles = asset.Files
            };

            return package;
        }

        public async Task<IPackageMetaData> GetPackageMetaData(string providerName)
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string metaPath = Path.Combine(rootCacheDir, providerName, Name, "metadata.json");

            if (!File.Exists(metaPath))
            {
                using (WebClient client = new WebClient())
                {
                    string url = string.Format(_metaPackageUrlFormat, Name);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
                        await client.DownloadFileTaskAsync(url, metaPath);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            try
            {
                using (StreamReader reader = new StreamReader(metaPath))
                {
                    string json = reader.ReadToEnd();

                    if (!string.IsNullOrEmpty(json))
                    {
                        return JsonConvert.DeserializeObject<CdnjsMetaData>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }

            return null;
        }

        public async Task<IPackageAsset> GetAsset(string version, string providerName)
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string metaPath = Path.Combine(rootCacheDir, providerName, Name, "metadata.json");
            var metaFile = new FileInfo(metaPath);
            IPackageAsset asset = null;

            if (metaFile.Exists && metaFile.Length > 0)
            {
                asset = GetAssetFromDisk(version, metaFile);
            }

            if (asset == null)
            {
                metaFile.Directory.Create();

                using (WebClient client = new WebClient())
                {
                    string url = string.Format(_metaPackageUrlFormat, Name);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
                        await client.DownloadFileTaskAsync(url, metaFile.FullName);
                    }
                    catch
                    {
                        return null;
                    }
                }

                metaFile.Refresh();
                asset = GetAssetFromDisk(version, metaFile);
            }

            return asset;
        }

        static IPackageAsset GetAssetFromDisk(string version, FileInfo metaFile)
        {
            if (!metaFile.Exists)
            {
                return null;
            }

            try
            {
                using (StreamReader reader = metaFile.OpenText())
                {
                    string json = reader.ReadToEnd();
                    var data = JsonConvert.DeserializeObject<CdnjsMetaData>(json);
                    return data.Assets.FirstOrDefault(a => a.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

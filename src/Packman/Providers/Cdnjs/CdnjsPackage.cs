using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
            var metadata = await GetPackageMetaData(providerName).ConfigureAwait(false);

            if (metadata == null)
                return null;

            var asset = metadata.Assets.FirstOrDefault(a => a.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

            if (asset == null)
                return null;

            var package = new InstallablePackage
            {
                Name = Name,
                Version = asset.Version,
                Files = asset.Files,
                MainFile = metadata.MainFile,
                AllFiles = asset.Files,
                UrlFormat = _downloadUrlFormat

            };

            if (!package.Files.Contains(package.MainFile))
            {
                if (package.Files.Contains(Name))
                    package.MainFile = Name;
                else
                    package.MainFile = package.Files.FirstOrDefault();
            }

            return package;
        }

        internal async Task<IPackageMetaData> GetPackageMetaData(string providerName)
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
                        await client.DownloadFileTaskAsync(url, metaPath).ConfigureAwait(false);
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

        public override string ToString()
        {
            return Name;
        }
    }
}

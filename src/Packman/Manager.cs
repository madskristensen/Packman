using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace Packman
{
    public class Manager
    {
        public Manager(IPackageProvider provider)
        {
            Provider = provider;
        }

        public IPackageProvider Provider { get; }

        public async Task InstallAll(Manifest manifest)
        {
            foreach (var name in manifest.Packages.Keys)
            {
                var entry = manifest.Packages[name];
                var package = await Provider.GetInstallablePackage(name, entry.Version);

                if (entry.Files != null && entry.Files.Count() != package.Files.Count())
                    package.Files = entry.Files;

                var file = new FileInfo(manifest.FileName);

                var settings = new InstallSettings
                {
                    InstallDirectory = Path.Combine(file.DirectoryName, entry.Path.Replace("/", "\\")),
                    SaveManifest = false
                };

                await Install(manifest, package, settings);
            }
        }


        public async Task<Manifest> Install(string manifestFilePath, InstallablePackage entry, string installDirectory, bool saveManifest = true)
        {
            Manifest manifest = await Manifest.FromFileOrNewAsync(manifestFilePath);

            var settings = new InstallSettings
            {
                InstallDirectory = installDirectory,
                SaveManifest = saveManifest
            };

            return await Install(manifest, entry, settings);
        }

        public async Task<Manifest> Install(string manifestFilePath, InstallablePackage entry, InstallSettings settings)
        {
            Manifest manifest = await Manifest.FromFileOrNewAsync(manifestFilePath);

            return await Install(manifest, entry, settings);
        }

        public async Task<Manifest> Install(Manifest manifest, InstallablePackage entry, InstallSettings settings)
        {
            var relativePath = MakeRelative(manifest.FileName, settings.InstallDirectory);

            if (settings.SaveManifest)
            {
                var package = new ManifestPackage
                {
                    Version = entry.Version,
                    Path = relativePath
                };

                // Only write "files" to the manifest if it's different from all files.
                if (entry.Original.Files.Count() != entry.Files.Count())
                {
                    package.Files = entry.Files;
                }

                manifest.Packages[entry.Name] = package;
                await manifest.Save();
            }

            string cwd = new FileInfo(manifest.FileName).DirectoryName;

            OnInstalling(entry, settings.InstallDirectory);

            await CopyPackageContent(entry, settings);

            OnInstalled(entry, settings.InstallDirectory);

            return manifest;
        }

        private async Task CopyPackageContent(InstallablePackage entry, InstallSettings settings)
        {
            await Task.Run(() =>
            {
                string cachePath = Environment.ExpandEnvironmentVariables(Defaults.CachePath);

                foreach (string file in entry.Files)
                {
                    string cleanFile = file.Replace("/", "\\");
                    string src = Path.Combine(cachePath, Provider.Name, entry.Name, entry.Version, cleanFile);
                    string dest = Path.Combine(settings.InstallDirectory, cleanFile);

                    string dir = Path.GetDirectoryName(dest);
                    Directory.CreateDirectory(dir);

                    File.Copy(src, dest, true);
                }
            });
        }

        public async Task<Manifest> Uninstall(string manifestFilePath, string name)
        {
            var manifest = await Manifest.FromFileOrNewAsync(manifestFilePath);

            if (!manifest.Packages.ContainsKey(name))
                return manifest;

            string version = manifest.Packages[name].Version;
            string path = manifest.Packages[name].Path;
            var installable = await Provider.GetInstallablePackage(name, version);

            OnUninstalling(installable, path);

            manifest.Packages.Remove(name);
            await manifest.Save();

            //foreach (var file in installable.Asset.Files)
            //{
            //    string dir = Path.GetDirectoryName(manifest.FileName);
            //    string fullPath = Path.Combine(dir, installable.CachePath, file);
            //    File.Delete(fullPath);
            //}

            OnUninstalled(installable, path);

            return manifest;
        }

        public void ClearCache()
        {
            string rootCacheDir = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string dir = Path.Combine(rootCacheDir, Provider.Name);

            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        public static string MakeRelative(string baseFile, string file)
        {
            var baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            var fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Trim('/');
        }

        void OnInstalling(InstallablePackage package, string path)
        {
            if (Installing != null)
            {
                Installing(null, new InstallEventArgs(package, path));
            }
        }

        void OnInstalled(InstallablePackage package, string path)
        {
            if (Installed != null)
            {
                Installed(null, new InstallEventArgs(package, path));
            }
        }

        void OnUninstalling(InstallablePackage package, string path)
        {
            if (Uninstalling != null)
            {
                Uninstalling(null, new InstallEventArgs(package, path));
            }
        }

        void OnUninstalled(InstallablePackage package, string path)
        {
            if (Uninstalled != null)
            {
                Uninstalled(null, new InstallEventArgs(package, path));
            }
        }

        public event EventHandler<InstallEventArgs> Installing;
        public event EventHandler<InstallEventArgs> Installed;

        public event EventHandler<InstallEventArgs> Uninstalling;
        public event EventHandler<InstallEventArgs> Uninstalled;
    }
}

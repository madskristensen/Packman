using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

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

                var file = new FileInfo(manifest.FileName);


                var settings = new InstallSettings
                {
                    InstallDirectory = Path.Combine(file.DirectoryName, entry.Path.Replace("/", "\\")),
                    OnlyMainFile = entry.Main != null && entry.Main.Any(),
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

                if (settings.OnlyMainFile)
                {
                    package.Main = new[] { entry.MainFile };
                }

                manifest.Packages[entry.Name] = package;
                await manifest.Save();
            }

            string cwd = new FileInfo(manifest.FileName).DirectoryName;

            OnInstalling(entry, settings.InstallDirectory);

            CopyPackageContent(entry, settings);

            OnInstalled(entry, settings.InstallDirectory);

            return manifest;
        }

        private void CopyPackageContent(InstallablePackage entry, InstallSettings settings)
        {
            string cachePath = Environment.ExpandEnvironmentVariables(Defaults.CachePath);

            if (settings.OnlyMainFile)
            {
                string src = Path.Combine(cachePath, Provider.Name, entry.Name, entry.Version, entry.MainFile);
                string dest = Path.Combine(settings.InstallDirectory, entry.MainFile);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(src, dest, true);
            }
            else
            {
                string sourceFolder = Path.Combine(cachePath, Provider.Name, entry.Name, entry.Version);
                Copy(sourceFolder, settings.InstallDirectory);
            }
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

        static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
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

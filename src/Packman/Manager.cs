using System;
using System.IO;
using System.Linq;
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
                var package = await Provider.GetInstallablePackageAsync(name, entry.Version);

                if (package == null)
                    throw new PackageNotFoundException(name, entry.Version);

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
                if (entry.AllFiles.Count() != entry.Files.Count())
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
            string cachePath = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string versionDir = Path.Combine(cachePath, Provider.Name, entry.Name, entry.Version);

            await entry.DownloadFiles(versionDir);

            await Task.Run(() =>
            {
                try
                {
                    foreach (string file in entry.Files)
                    {
                        string cleanFile = file.Replace("/", "\\");
                        string src = Path.Combine(versionDir, cleanFile);
                        string dest = Path.Combine(settings.InstallDirectory, cleanFile);

                        string dir = Path.GetDirectoryName(dest);
                        Directory.CreateDirectory(dir);

                        OnCopying(src, dest);
                        File.Copy(src, dest, true);
                        OnCopied(src, dest);
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        Directory.Delete(versionDir, true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex);
                    }
                }
            });
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
                Installing(this, new InstallEventArgs(package, path));
        }

        void OnInstalled(InstallablePackage package, string path)
        {
            if (Installed != null)
                Installed(this, new InstallEventArgs(package, path));
        }

        void OnCopying(string source, string destination)
        {
            if (Copying != null)
                Copying(this, new FileCopyEventArgs(source, destination));
        }

        void OnCopied(string source, string destination)
        {
            if (Copied != null)
                Copied(this, new FileCopyEventArgs(source, destination));
        }

        public static event EventHandler<InstallEventArgs> Installing;
        public static event EventHandler<InstallEventArgs> Installed;

        public static event EventHandler<FileCopyEventArgs> Copying;
        public static event EventHandler<FileCopyEventArgs> Copied;
    }
}

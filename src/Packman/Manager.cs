using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

                if (entry.Urls != null)
                {
                    await InstallUrls(manifest, entry);
                }
                else
                {
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

            OnInstalling(manifest, entry, settings.InstallDirectory);

            var copied = await CopyPackageContent(entry, settings);

            // Check if the files where already installed. Skip if they were
            if (copied)
            {
                OnInstalled(manifest, entry, settings.InstallDirectory);
            }

            return manifest;
        }

        async Task InstallUrls(Manifest manifest, ManifestPackage package)
        {
            string dir = Path.GetDirectoryName(manifest.FileName);
            string path = Path.Combine(dir, package.Path).Replace("\\", "/");
            var files = new List<string>();

            var urlPackage = new UrlPackage
            {
                Name = package.Name,
                Files = files
            };

            OnInstalling(manifest, urlPackage, path);

            foreach (string url in package.Urls)
            {
                string fileName = Path.GetFileName(url);
                string filePath = new FileInfo(Path.Combine(dir, package.Path, fileName)).FullName;
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                files.Add(fileName);

                using (WebClient client = new WebClient())
                {
                    OnCopying(url, filePath);
                    await client.DownloadFileTaskAsync(url, filePath);
                    OnCopied(url, filePath);
                }
            }

            OnInstalled(manifest, urlPackage, path);
        }

        public async Task<ManifestPackage> UninstallAsync(Manifest manifest, string name, bool saveManifest)
        {
            var package = manifest.Packages.FirstOrDefault(p => p.Key == name).Value;

            if (package == null)
                throw new PackageNotFoundException(name, null);

            if (saveManifest)
            {
                manifest.Packages.Remove(name);
                await manifest.Save();
            }

            string cwd = Path.GetDirectoryName(manifest.FileName);
            var installDir = Path.Combine(cwd, package.Path);

            var files = package.Files;

            // If no files are specified in the entry, then find all files from package
            if (files == null)
            {
                var installable = await Provider.GetInstallablePackageAsync(name, package.Version);

                if (installable != null)
                    files = installable.AllFiles;
            }

            if (files != null)
            {
                foreach (string file in files)
                {
                    string path = Path.Combine(installDir, file);
                    File.Delete(path);
                }
            }

            return package;
        }

        async Task<bool> CopyPackageContent(InstallablePackage entry, InstallSettings settings)
        {
            string cachePath = Environment.ExpandEnvironmentVariables(Defaults.CachePath);
            string versionDir = Path.Combine(cachePath, Provider.Name, entry.Name, entry.Version);
            bool hasCopied = false;

            await entry.DownloadFilesAsync(versionDir);

            await Task.Run(() =>
            {
                try
                {
                    foreach (string file in entry.Files)
                    {
                        string cleanFile = file.Replace("/", "\\");
                        string src = Path.Combine(versionDir, cleanFile);
                        string dest = Path.Combine(settings.InstallDirectory, cleanFile);

                        if (File.Exists(dest))
                        {
                            var srcDate = File.GetLastWriteTime(src);
                            var destDate = File.GetLastWriteTime(dest);

                            if (srcDate == destDate)
                                continue;
                        }

                        string dir = Path.GetDirectoryName(dest);
                        Directory.CreateDirectory(dir);

                        OnCopying(src, dest);
                        File.Copy(src, dest, true);
                        OnCopied(src, dest);
                        hasCopied = true;
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

            return hasCopied;
        }

        public static string MakeRelative(string baseFile, string file)
        {
            var baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            var fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Trim('/');
        }

        void OnInstalling(Manifest manifest, IInstallablePackage package, string path)
        {
            if (Installing != null)
                Installing(this, new InstallEventArgs(manifest, package, path));
        }

        void OnInstalled(Manifest manifest, IInstallablePackage package, string path)
        {
            if (Installed != null)
                Installed(this, new InstallEventArgs(manifest, package, path));
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

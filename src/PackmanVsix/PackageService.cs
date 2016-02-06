using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Parser;
using Packman;

namespace PackmanVsix
{
    static class PackageService
    {
        static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            Manager.Installed += Installed;
            Manager.Installing += Installing;
            Manager.Copying += Copying;

            InstallablePackage.Downloading += Downloading;
            InstallablePackage.DownloadingRemainingFiles += DownloadingRemainingFiles;

            Manifest.Saving += Saving;
        }

        public static async Task RestorePackagesAsync(string manifestFile)
        {
            try
            {
                if (await IsValidJson(manifestFile))
                {
                    var manifest = await Manifest.FromFileOrNewAsync(manifestFile);
                    await VSPackage.Manager.InstallAll(manifest);

                    Telemetry.TrackEvent("Packages restored");
                }
                else
                {
                    VSPackage.DTE.StatusBar.Text = Properties.Resources.ManifestInvalidJson;
                }
            }
            catch (PackageNotFoundException ex)
            {
                VSPackage.DTE.StatusBar.Text = string.Format(Properties.Resources.ExceptionLoadingPackages, ex.Name, ex.Version);
                Logger.Log(ex);
            }
            catch (Exception ex)
            {
                VSPackage.DTE.StatusBar.Text = Properties.Resources.ErrorRestoringPackages;
                Logger.Log(ex);
            }
        }

        static async Task<bool> IsValidJson(string file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                string json = await reader.ReadToEndAsync();
                var doc = JSONParser.Parse(json);

                return doc.IsValid;
            }
        }

        static void DownloadingRemainingFiles(object sender, InstallEventArgs e)
        {
            int count = e.Package.TotalFileCount - e.Package.Files.Count();
            string file = count == 1 ? "file" : "files";
            Logger.Log($"Downloading remaining {e.Package.Name} ({count} {file})");
        }

        static void Downloading(object sender, InstallEventArgs e)
        {
            string file = e.Package.TotalFileCount == 1 ? "file" : "files";
            Logger.Log($"Downloading {e.Package.Name} ({e.Package.Files.Count()} {file})");
        }

        static void Copying(object sender, FileCopyEventArgs e)
        {
            Logger.Log($"Copying {Path.GetFileName(e.Destination)}");
            ProjectHelpers.CheckFileOutOfSourceControl(e.Destination);
        }

        static void Installing(object sender, InstallEventArgs e)
        {
            string msg = $"Installing {e.Package.Name}...";
            Logger.Log(msg);
            VSPackage.DTE.StatusBar.Text = msg;
        }

        static async void Installed(object sender, InstallEventArgs e)
        {
            var project = ProjectHelpers.GetActiveProject();

            foreach (var file in e.Package.Files)
            {
                string absolute = Path.Combine(e.Path, file);

                try
                {
                    var info = new FileInfo(absolute);
                    await project.AddFileToProjectAsync(info.FullName);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            string msg = $"Installed {e.Package.Name} successfully";
            Logger.Log(msg);
            VSPackage.DTE.StatusBar.Text = msg;
        }

        static void Saving(object sender, EventArgs e)
        {
            var manifest = (Manifest)sender;

            if (manifest != null && !string.IsNullOrEmpty(manifest.FileName))
                ProjectHelpers.CheckFileOutOfSourceControl(manifest.FileName);
        }
    }
}

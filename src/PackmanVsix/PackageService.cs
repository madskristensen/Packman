using System;
using System.Collections.Generic;
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
        static List<string> _files = new List<string>();
        static bool _isRestoring;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            Manager.Installed += Installed;

            Manager.Copying += Copying;
            Manager.Copied += Copied;

            InstallablePackage.Downloading += Downloading;
            InstallablePackage.DownloadingRemainingFiles += DownloadingRemainingFiles;

            Manifest.Saving += Saving;
        }

        public static async Task RestorePackagesAsync(string manifestFile)
        {
            try
            {
                _isRestoring = true;

                if (await IsValidJson(manifestFile))
                {
                    _files.Clear();

                    var manifest = await Manifest.FromFileOrNewAsync(manifestFile);
                    await VSPackage.Manager.InstallAll(manifest);

                    Telemetry.TrackEvent("Packages restored");

                    if (_files.Count > 0)
                    {
                        VSPackage.DTE.StatusBar.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationGeneral);
                        VSPackage.DTE.StatusBar.Text = $"Adding {_files.Count} files to project. This may take a while...";

                        try
                        {
                            var project = ProjectHelpers.GetActiveProject();
                            project.AddFilesToProject(_files);
                        }
                        finally
                        {
                            VSPackage.DTE.StatusBar.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationGeneral);
                        }
                    }

                    VSPackage.DTE.StatusBar.Text = $"{manifest.Packages.Count} libraries successfully installed";
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
            finally
            {
                _isRestoring = false;
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

        private static void Copied(object sender, FileCopyEventArgs e)
        {
            if (_isRestoring)
            {
                if (!_files.Contains(e.Destination))
                {
                    _files.Add(e.Destination);
                }
            }
            else
            {
                var project = ProjectHelpers.GetActiveProject();
                project.AddFileToProject(e.Destination);
            }
        }

        static void Installed(object sender, InstallEventArgs e)
        {
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

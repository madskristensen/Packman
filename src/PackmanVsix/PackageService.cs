using System;
using System.IO;
using System.Linq;
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
            InstallablePackage.Downloaded += Downloaded;
        }

        static void Downloaded(object sender, InstallEventArgs e)
        {
            Logger.Log($"  Downloaded {e.Package.Name} {e.Package.Version} successfully");
        }

        static void Downloading(object sender, InstallEventArgs e)
        {
            string file = e.Package.AllFiles.Count() == 1 ? "file" : "files";
            Logger.Log($"  Downloading package {e.Package.Name} {e.Package.Version} ({e.Package.AllFiles.Count()} {file})");
        }

        static void Copying(object sender, FileCopyEventArgs e)
        {
            Logger.Log($"  Copying package files into project");
            ProjectHelpers.CheckFileOutOfSourceControl(e.Destination);
        }

        static void Installing(object sender, InstallEventArgs e)
        {
            string msg = $"Installing package {e.Package.Name} {e.Package.Version}";
            Logger.Log(msg);
            VSPackage.DTE.StatusBar.Text = msg;
        }

        static void Installed(object sender, InstallEventArgs e)
        {
            var project = ProjectHelpers.GetActiveProject();

            foreach (var file in e.Package.Files)
            {
                string absolute = Path.Combine(e.Path, file);

                try
                {
                    var info = new FileInfo(absolute);
                    project.AddFileToProject(info.FullName);
                }
                catch (Exception)
                {
                    // Angular has issues with its huge i18n folder. No idea why
                }
            }

            string msg = $"Installed package {e.Package.Name} {e.Package.Version} successfully";
            Logger.Log(msg + Environment.NewLine);
            VSPackage.DTE.StatusBar.Text = msg;
        }
    }
}

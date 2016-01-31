using System;
using System.IO;
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
        }

        private static void Copying(object sender, FileCopyEventArgs e)
        {
            ProjectHelpers.CheckFileOutOfSourceControl(e.Destination);
        }

        private static void Installing(object sender, InstallEventArgs e)
        {
            VSPackage.DTE.StatusBar.Text = $"Installing {e.Package.Name} from Packman...";
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

            VSPackage.DTE.StatusBar.Text = $"The {e.Package.Name} package was installed successfully";
        }
    }
}

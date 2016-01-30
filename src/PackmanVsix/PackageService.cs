using System;
using System.IO;
using Packman;

namespace PackmanVsix
{
    static class PackageService
    {
        static IServiceProvider _serviceProvider;
        static Manager _manager;

        public static void Initialize(IServiceProvider serviceProvider, Manager manager)
        {
            _serviceProvider = serviceProvider;
            _manager = manager;

            _manager.Installed += Installed;
            _manager.Installing += Installing;
            _manager.Uninstalled += Uninstalled;
        }

        private static void Uninstalled(object sender, InstallEventArgs e)
        {
            foreach (var file in e.Package.Files)
            {
                string absolute = Path.Combine(e.Path, file);
                ProjectHelpers.DeleteFileFromProject(absolute);
            }
        }

        private static void Installing(object sender, InstallEventArgs e)
        {
            VSPackage.DTE.StatusBar.Text = $"Installing {e.Package.Name}...";

            foreach (var file in e.Package.Files)
            {
                string absolute = Path.Combine(e.Path, file);
                ProjectHelpers.CheckFileOutOfSourceControl(absolute);
            }
        }

        static void Installed(object sender, InstallEventArgs e)
        {
            var project = ProjectHelpers.GetActiveProject();

            foreach (var file in e.Package.Files)
            {
                string absolute = Path.Combine(e.Path, file);
                absolute = absolute.Replace("/", "\\");

                try
                {
                    //dte.ItemOperations.AddExistingItem(absolute);
                    project.AddFileToProject(absolute);
                }
                catch (Exception)
                {
                    // Angular has issues with its huge i18n folder. No idea why
                }
            }

            VSPackage.DTE.StatusBar.Text = $"{e.Package.Name} installed";
        }
    }
}

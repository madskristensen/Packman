using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Packman;
using Microsoft.VisualStudio.Shell;

namespace PackmanVsix
{
    internal sealed class RestorePackagesCommand
    {
        private readonly Package _package;

        private RestorePackagesCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            var service = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (service != null)
            {
                var cmdId = new CommandID(PackageGuids.guidLibrarianCmdSet, PackageIds.RestoreAll);
                var button = new OleMenuCommand(Restore, cmdId);
                button.BeforeQueryStatus += BeforeQueryStatus;
                service.AddCommand(button);
            }
        }

        public static RestorePackagesCommand Instance { get; private set; }

        IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new RestorePackagesCommand(package);
        }

        void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();
            button.Visible = item.IsConfigFile();
        }

        async void Restore(object sender, EventArgs e)
        {
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item != null)
            {
                var manifest = await Manifest.FromFileOrNewAsync(item.GetFullPath());
                await VSPackage.Manager.InstallAll(manifest);
            }
        }
    }
}

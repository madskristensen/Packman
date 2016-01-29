using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Packman;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PackmanVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(PackageGuids.guidLibrarianPackageString)]
    public sealed class VSPackage : Package
    {
        public const string Version = "1.0";
        public const string Name = "Packman";

        public static DTE2 DTE { get; private set; }
        public static Manager Manager { get; private set; }
        public static IPackageProvider Provider { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();
            DTE = (DTE2)GetService(typeof(DTE));

            Provider = new JsDelivrProvider();
            Manager = new Manager(Provider);

            PackageService.Initialize(this, Manager);

            Logger.Initialize(this, Name);
            Telemetry.Initialize(this, Version, "d8226d88-0507-4495-9c9c-63951a2151d3");

            InstallPackageCommand.Initialize(this);
            RestorePackagesCommand.Initialize(this);
        }
    }
}

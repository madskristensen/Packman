using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Packman;

namespace PackmanVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideOptionPage(typeof(Options), "Web", Vsix.Name, 101, 111, true, new[] { "package", "library", "framework", "install" }, ProvidesLocalizedCategoryName = false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(PackageGuids.guidLibrarianPackageString)]
    public sealed class VSPackage : Package
    {
        public const string ManifestFileName = "packman.json";

        public static DTE2 DTE { get; private set; }
        public static Manager Manager { get; private set; }
        public static Options Options { get; private set; }

        protected async override void Initialize()
        {
            DTE = (DTE2)GetService(typeof(DTE));

            Options = (Options)GetDialogPage(typeof(Options));
            Options.Saved += async (s, e) => await SetDefaults();

            await SetDefaults();

            Logger.Initialize(this, Vsix.Name);
            Telemetry.Initialize(this, Vsix.Version, "d8226d88-0507-4495-9c9c-63951a2151d3");

            PackageService.Initialize(this);
            InstallPackageCommand.Initialize(this);
            RestorePackagesCommand.Initialize(this);

            base.Initialize();
        }

        static async System.Threading.Tasks.Task SetDefaults()
        {
            Defaults.CachePath = Options.CachePath;
            Defaults.CacheDays = Options.CacheDays;

            IPackageProvider provider;

            // TODO: get rid of the enum and make this dynamic and MEF'ed out
            if (Options.Provider == Providers.JsDelivr)
                provider = new JsDelivrProvider();
            else
                provider = new CdnjsProvider();

            Manager = new Manager(provider);

            if (!provider.IsInitialized)
                await provider.InitializeAsync();
        }
    }
}

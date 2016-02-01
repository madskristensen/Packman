using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public class UninstallTest
    {
        string _cwd, _manifestPath;
        IEnumerable<Manager> _managers;
        const string _manifestFileName = "packman.json";

        [TestInitialize]
        public void Initialize()
        {
            _cwd = new DirectoryInfo("..\\..\\test").FullName;
            Directory.CreateDirectory(_cwd);

            Defaults.CacheDays = 3;

            _manifestPath = Path.Combine(_cwd, _manifestFileName);

            IPackageProvider[] managers = { new JsDelivrProvider(), new CdnjsProvider() };
            _managers = managers.Select(a => new Manager(a));
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_cwd, true);
        }

        [TestMethod, TestCategory("Unisntall")]
        public async Task InstallPackage()
        {
            string name = "jquery";
            string version = "2.2.0";

            foreach (var manager in _managers)
            {
                var entry = await manager.Provider.GetInstallablePackageAsync(name, version);
                string path = Path.Combine(_cwd, "js");
                var manifest = await manager.Install(_manifestPath, entry, path);

                string file = Path.Combine(path, "jquery.min.js");

                Assert.IsTrue(File.Exists(file), "Package was not isntalled correctly");

                await manager.UninstallAsync(manifest, name);
                Assert.IsFalse(manifest.Packages.ContainsKey(name));
                Assert.IsFalse(File.Exists(file), "Package was not removed");
            }
        }
    }
}
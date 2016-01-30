using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public static class AssemblyLoad
    {
        internal static List<IPackageProvider> Apis { get; private set; }

        static string _cwd;

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            _cwd = new DirectoryInfo("..\\..\\test").FullName;
            Directory.CreateDirectory(_cwd);

            string path = Path.Combine(_cwd, "cache.json");

            Defaults.CacheDays = 3;

            Apis = new List<IPackageProvider>() { new JsDelivrProvider(), new CdnjsProvider()};
            //Api = new CdnjsProvider();
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Directory.Delete(_cwd, true);
        }
    }
}

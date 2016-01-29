using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Packman.Test
{
    [TestClass]
    public static class AssemblyLoad
    {
        internal static IPackageProvider Api { get; private set; }

        static string _cwd;

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            _cwd = new DirectoryInfo("..\\..\\test").FullName;
            Directory.CreateDirectory(_cwd);

            string path = Path.Combine(_cwd, "cache.json");

            Defaults.CacheDays = 3;

            Api = new JsDelivrProvider();
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Directory.Delete(_cwd, true);
        }
    }
}

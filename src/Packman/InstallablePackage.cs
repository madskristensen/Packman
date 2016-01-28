using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Packman
{
    public class InstallablePackage
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string MainFile { get; set; }
        public IEnumerable<string> Files { get; set; }

        internal InstallablePackage Original { get; set; }

        public bool AreAllFilesRecommended()
        {
            var extensions = new List<string>();
            string[] ignore = { ".map" };

            foreach (string file in Files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (!ignore.Contains(ext) && !extensions.Contains(ext))
                    extensions.Add(ext);

                if (extensions.Count >= 2)
                    return true;
            }

            return false;
        }
    }
}

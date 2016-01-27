using System.Collections.Generic;
using System.IO;

namespace Packman
{
    class JsDelivrMetaData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LastVersion { get; set; }
        public string Homepage { get; set; }
        public string GitHub { get; set; }
        public string Author { get; set; }
        public string MainFile { get; set; }
        public IEnumerable<JsDelivrAsset> Assets { get; set; }
    }

    class JsDelivrAsset
    {
        public IEnumerable<string> Files { get; set; }
        public string Version { get; set; }
        public string MainFile { get; set; }
    }
}

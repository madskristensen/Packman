using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Packman
{
    public class ManifestPackage
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("main")]
        public IEnumerable<string> Main { get; set; }

        public bool ShouldSerializePath()
        {
            return !string.IsNullOrEmpty(Path) && Path != Defaults.DefaultLocalPath;
        }

        public bool ShouldSerializeMain()
        {
            return Main != null && Main.Any();
        }
    }
}
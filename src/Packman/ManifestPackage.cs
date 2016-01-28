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

        [JsonProperty("files")]
        public IEnumerable<string> Files { get; set; }

        public bool ShouldSerializePath()
        {
            return !string.IsNullOrEmpty(Path) && Path != Defaults.DefaultLocalPath;
        }

        public bool ShouldSerializeFiles()
        {
            return Files != null && Files.Any();
        }
    }
}
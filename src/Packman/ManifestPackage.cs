using System.Collections.Generic;
using Newtonsoft.Json;

namespace Packman
{
    public class ManifestPackage
    {
        [JsonProperty("version"), JsonRequired]
        public string Version { get; set; }

        [JsonProperty("path"), JsonRequired]
        public string Path { get; set; }

        [JsonProperty("files", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> Files { get; set; }
    }
}
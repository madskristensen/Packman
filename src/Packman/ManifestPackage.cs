using System.Collections.Generic;
using Newtonsoft.Json;

namespace Packman
{
    public class ManifestPackage
    {
        [JsonIgnore]
        public string Name { get; set; }

        [JsonProperty("path"), JsonRequired]
        public string Path { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("files", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> Files { get; set; }

        [JsonProperty("urls", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> Urls { get; set; }
    }
}
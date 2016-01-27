using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Packman
{
    public class Manifest
    {
        [JsonConstructor]
        Manifest()
        { }

        Manifest(string fileName)
        {
            Packages = new Dictionary<string, ManifestPackage>();
            FileName = fileName;
        }

        [JsonProperty("packages")]
        public IDictionary<string, ManifestPackage> Packages { get; set; }

        [JsonIgnore]
        public string FileName { get; private set; }

        public static async Task<Manifest> FromFileOrNewAsync(string fileName)
        {
            if (!File.Exists(fileName))
                return new Manifest(fileName);

            using (StreamReader reader = new StreamReader(fileName))
            {
                string content = await reader.ReadToEndAsync();

                Manifest manifest = JsonConvert.DeserializeObject<Manifest>(content);
                manifest.FileName = fileName;

                return manifest;
            }
        }

        public async Task Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            using (StreamWriter writer = new StreamWriter(FileName))
            {
                await writer.WriteAsync(json);
            }
        }
    }
}

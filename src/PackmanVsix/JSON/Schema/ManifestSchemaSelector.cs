using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Schema;

namespace PackmanVsix
{
    [Export(typeof(IJSONSchemaSelector))]
    class ManifestSchemaSelector : IJSONSchemaSelector
    {
        public event EventHandler AvailableSchemasChanged { add { } remove { } }

        public Task<IEnumerable<string>> GetAvailableSchemasAsync()
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public string GetSchemaFor(string fileLocation)
        {
            string fileName = Path.GetFileName(fileLocation);

            if (fileName.Equals(VSPackage.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                return JSONHelpers.SchemaFileName;

            return null;
        }
    }
}

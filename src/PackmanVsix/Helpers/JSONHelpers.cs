using System;
using System.IO;
using System.Reflection;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Schema;

namespace PackmanVsix
{
    static class JSONHelpers
    {
        public static string SchemaFileName { get; } = GetSchemaFileName();

        public static bool HasSchema(this JSONDocument document, IJSONSchemaEvaluationReportCache cache)
        {
            if (document == null || cache == null)
                return false;

            JSONObject schema = cache.DetermineSchemaToUseAsync(document).Result;

            if (schema != null && !string.IsNullOrWhiteSpace(schema.DocumentLocation))
            {
                string currentSchemaFileName = schema.DocumentLocation;

                if (string.Equals(currentSchemaFileName, SchemaFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static string GetSchemaFileName()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            return Path.Combine(folder, "json\\schema\\manifest-schema.json");
        }
    }
}

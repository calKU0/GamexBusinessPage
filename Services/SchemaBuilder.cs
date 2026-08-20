using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Services
{
    public static class SchemaBuilder
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static Dictionary<string, object?> BuildBreadcrumb(params (string Name, string Url)[] items)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["name"] = item.Name,
                    ["item"] = item.Url
                }).ToArray()
            };
        }

        public static string BuildBreadcrumbGraph(params (string Name, string Url)[] items)
        {
            const string baseUrl = "https://gamex-olkusz.pl";

            var graph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[]
                {
                    BuildBreadcrumb(items),
                    SchemaFactory.GetLocalBusinessSchema(baseUrl)
                }
            };

            return JsonSerializer.Serialize(graph, SerializerOptions);
        }
    }
}

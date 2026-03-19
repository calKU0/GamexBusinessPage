using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class IndexModel : PageModel
    {
        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Wynajem maszyn budowlanych i remonty dróg w Małopolsce | Gamex Olkusz";
            ViewData["Description"] = "Profesjonalne remonty dróg oraz wynajem maszyn budowlanych z operatorem. Obsługujemy woj. małopolskim, śląskim i świętokrzyskim. Sprawdź naszą ofertę!";
            ViewData["Keywords"] = "GAMEX, Olkusz, remonty dróg, wynajem maszyn budowlanych, minikoparki, koparki, ładowarki, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/";

            var baseUrl = "https://gamex-olkusz.pl";
            var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

            var webSite = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["@id"] = $"{baseUrl}/#website",
                ["url"] = baseUrl,
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] }
            };

            var graph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { localBusiness, webSite }
            };

            SchemaJson = JsonSerializer.Serialize(graph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
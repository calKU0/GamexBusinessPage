using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class KontaktModel : PageModel
    {
        private readonly CatalogCache _catalogCache;

        public KontaktModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public ContactFormViewModel ContactForm { get; private set; } = new(Array.Empty<MachineItem>(), null, false);

        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Kontakt - Gamex Olkusz | Zapytaj o wynajem i usługi";
            ViewData["Description"] = "Skontaktuj się z Gamex w Olkuszu. Szybka wycena wynajmu maszyn i usług drogowych. Zadzwoń lub napisz do nas – działamy w 3 województwach!";
            ViewData["Keywords"] = "Gamex, Olkusz, kontakt, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";

            var baseUrl = "https://gamex-olkusz.pl";

            var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

            var breadcrumbSchema = new Dictionary<string, object?>
            {
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 1,
                        ["name"] = "Strona główna",
                        ["item"] = baseUrl
                    },
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 2,
                        ["name"] = "Kontakt",
                        ["item"] = $"{baseUrl}/kontakt"
                    }
                }
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, localBusiness }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var catalog = _catalogCache.GetMachineCatalog();
            var machines = catalog.Machines
                .OrderBy(machine => machine.CategoryDisplayName)
                .ThenBy(machine => machine.DisplayName)
                .ToList();
            ContactForm = new ContactFormViewModel(machines, null, false);
        }
    }
}
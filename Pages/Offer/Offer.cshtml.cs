using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class OfertaModel : PageModel
    {
        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Oferta: wynajem maszyn, budowa dróg i transport | GAMEX Olkusz";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/oferta";
            ViewData["Description"] = "Wynajem maszyn budowlanych z operatorem, budowa i remonty dróg, chodników i placów oraz transport kruszyw i maszyn HDS. GAMEX Olkusz – woj. małopolskie, śląskie i świętokrzyskie.";
            ViewData["Keywords"] = "Gamex, Olkusz, oferta, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";

            var baseUrl = "https://gamex-olkusz.pl";
            var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

            var breadcrumbSchema = new Dictionary<string, object?>
            {
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new object[]
                {
                    new Dictionary<string, object?> {
                        ["@type"] = "ListItem",
                        ["position"] = 1,
                        ["name"] = "Strona główna",
                        ["item"] = $"{baseUrl}/"
                    },
                    new Dictionary<string, object?> {
                        ["@type"] = "ListItem",
                        ["position"] = 2,
                        ["name"] = "Oferta",
                        ["item"] = $"{baseUrl}/oferta"
                    }
                }
            };

            var servicesSchema = new object[]
            {
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Wynajem maszyn budowlanych z operatorem",
                    ["description"] = "Wynajem koparek, ładowarek, walców i rozkładarek asfaltu wraz z operatorem, rozliczany godzinowo.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/wypozyczenie-maszyn"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Usługi drogowe i budowlane",
                    ["description"] = "Budowa i remonty dróg, chodników, parkingów i placów wraz z robotami ziemnymi i odwodnieniem.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/uslugi"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Transport ciężarowy i HDS",
                    ["description"] = "Przewóz kruszyw i materiałów sypkich, transport niskopodwoziowy maszyn oraz rozładunek dźwigiem HDS.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/transport"
                }
            };

            var graphNodes = new List<object> { breadcrumbSchema, localBusiness };
            graphNodes.AddRange(servicesSchema);

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = graphNodes
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
using GamexBusinessPage.Services; // Zakładam, że tu masz SchemaFactory
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
            ViewData["Title"] = "Usługi i maszyny budowlane w Małopolsce | Oferta Gamex Olkusz";
            ViewData["Description"] = "Szeroki zakres usług: od wynajmu koparek po kompleksowe roboty drogowe i transport HDS. Zobacz, jak Gamex wspiera inwestycje w Olkuszu, całej Małopolce oraz okolicach";
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
                    ["name"] = "Wypożyczenie maszyn budowlanych",
                    ["description"] = "Wynajem profesjonalnego sprzętu budowlanego z obsługą operatorską.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/wypozyczenie-maszyn"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Usługi drogowe i budowlane",
                    ["description"] = "Kompleksowa budowa i modernizacja dróg oraz infrastruktury towarzyszącej.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/uslugi"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Transport ciężarowy i HDS",
                    ["description"] = "Transport materiałów sypkich i maszyn na terenie województwa Małopolskiego, Śląskiego oraz Świętokrzyskiego.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/oferta/transport"
                }
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, localBusiness, servicesSchema }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
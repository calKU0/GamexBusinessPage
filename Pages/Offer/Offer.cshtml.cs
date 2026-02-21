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
            ViewData["Title"] = "Oferta Gamex Olkusz – Maszyny budowlane i remonty dróg";
            ViewData["Description"] = "Poznaj pełną ofertę Gamex w Olkuszu – wynajem maszyn budowlanych i profesjonalne usługi remontu dróg w woj. Małopolskim.";
            ViewData["Keywords"] = "Gamex, Olkusz, oferta, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolskie";

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
                    ["url"] = $"{baseUrl}/Offer/MachineRental/MachineRental"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Usługi drogowe i budowlane",
                    ["description"] = "Kompleksowa budowa i modernizacja dróg oraz infrastruktury towarzyszącej.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/Offer/Services/Services"
                },
                new Dictionary<string, object?> {
                    ["@type"] = "Service",
                    ["name"] = "Transport ciężarowy i HDS",
                    ["description"] = "Transport materiałów sypkich i maszyn na terenie Małopolski i Śląska.",
                    ["provider"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                    ["url"] = $"{baseUrl}/Offer/Transport/Transport"
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
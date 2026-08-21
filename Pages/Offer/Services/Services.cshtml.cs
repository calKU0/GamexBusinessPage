using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages.Services
{
    [OutputCache(Duration = 3600, VaryByQueryKeys = new[] { "category" })]
    public class RoadRenovationModel : PageModel
    {
        private readonly CatalogCache _catalogCache;

        public RoadRenovationModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public IReadOnlyList<ServiceCategory> Categories { get; private set; } = Array.Empty<ServiceCategory>();

        public IReadOnlyList<ServiceCategory> FilteredCategories { get; private set; } = Array.Empty<ServiceCategory>();

        public string? SelectedCategoryKey { get; private set; }

        public IReadOnlyList<CategoryLinkItem> CategoryLinks { get; private set; } = Array.Empty<CategoryLinkItem>();

        public string? SchemaJson { get; private set; }

        public void OnGet(string? category)
        {
            var catalog = _catalogCache.GetServiceCatalog();
            Categories = catalog.Categories;

            var selected = string.IsNullOrWhiteSpace(category)
                ? null
                : Categories.FirstOrDefault(item =>
                    string.Equals(item.Key, category, StringComparison.OrdinalIgnoreCase));

            // Only a key that actually matched, so an unknown value falls back to
            // the full listing with the "all" link marked, rather than leaving
            // every link inactive.
            SelectedCategoryKey = selected?.Key;
            FilteredCategories = selected is null ? Categories : new[] { selected };

            const string listUrl = "https://gamex-olkusz.pl/oferta/uslugi";

            if (selected is not null)
            {
                ViewData["Title"] = $"{selected.DisplayName} – usługi budowlane i drogowe | GAMEX Olkusz";
                ViewData["Description"] = $"{selected.DisplayName} – GAMEX Olkusz. Realizacje na terenie Małopolski, Śląska i Świętokrzyskiego. Własne maszyny i operatorzy, wycena w jeden dzień roboczy.";
                ViewData["Keywords"] = $"{selected.DisplayName.ToLowerInvariant()}, usługi drogowe, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
                ViewData["CanonicalUrl"] = $"{listUrl}?category={Uri.EscapeDataString(selected.Key)}";
            }
            else
            {
                ViewData["Title"] = "Budowa dróg, chodników i placów – usługi drogowe | GAMEX Olkusz";
                ViewData["Description"] = "Budowa i remonty dróg, chodników, parkingów i placów. Roboty ziemne, podbudowy, nawierzchnie asfaltowe i kostka brukowa. GAMEX Olkusz – Małopolska, Śląsk, Świętokrzyskie.";
                ViewData["Keywords"] = "Gamex, Olkusz, remonty dróg, usługi drogowe, nawierzchnie asfaltowe, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";
                ViewData["CanonicalUrl"] = listUrl;
            }

            CategoryLinks = CategoryFilterLinks.Build(
                Url,
                "/Offer/Services/Services",
                "Wszystkie usługi",
                Categories.Select(item => (item.Key, item.DisplayName)),
                SelectedCategoryKey);

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
                        ["item"] = $"{baseUrl}/"
                    },
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 2,
                        ["name"] = "Oferta",
                        ["item"] = $"{baseUrl}/oferta"
                    },
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 3,
                        ["name"] = "Usługi",
                        ["item"] = $"{baseUrl}/oferta/uslugi"
                    }
                }
            };

            var services = FilteredCategories
                .SelectMany(serviceCategory => serviceCategory.Services)
                .ToList();

            var servicesListSchema = new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["name"] = "Usługi drogowe Gamex",
                ["url"] = $"{baseUrl}/oferta/uslugi",
                ["numberOfItems"] = services.Count,
                ["itemListOrder"] = "https://schema.org/ItemListOrderAscending",
                ["itemListElement"] = services
                    .Select((service, index) =>
                    {
                        var serviceUrl = $"{baseUrl}/oferta/uslugi#{service.AnchorId}";

                        var serviceItem = new Dictionary<string, object?>
                        {
                            ["@type"] = "Service",
                            ["name"] = service.Name,
                            ["description"] = service.Description,
                            ["url"] = serviceUrl,
                            ["areaServed"] = localBusiness["areaServed"],
                            ["provider"] = new Dictionary<string, object?>
                            {
                                ["@id"] = localBusiness["@id"]
                            }
                        };

                        //SchemaFactory.ApplyRating(serviceItem, "4.0", 6);

                        return new Dictionary<string, object?>
                        {
                            ["@type"] = "ListItem",
                            ["position"] = index + 1,
                            ["item"] = serviceItem
                        };
                    }).ToArray()
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, servicesListSchema, localBusiness }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}

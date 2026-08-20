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
            ViewData["Title"] = "Budowa dróg, chodników i placów – usługi drogowe | GAMEX Olkusz";
            ViewData["Description"] = "Budowa i remonty dróg, chodników, parkingów i placów. Roboty ziemne, podbudowy, nawierzchnie asfaltowe i kostka brukowa. GAMEX Olkusz – Małopolska, Śląsk, Świętokrzyskie.";
            ViewData["Keywords"] = "Gamex, Olkusz, remonty dróg, usługi drogowe, nawierzchnie asfaltowe, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/oferta/uslugi";

            var catalog = _catalogCache.GetServiceCatalog();
            Categories = catalog.Categories;
            SelectedCategoryKey = category;

            var categoryLinks = new List<CategoryLinkItem>
            {
                new(
                    "Wszystkie usługi",
                    Url.Page("/oferta/uslugi") ?? "#",
                    string.IsNullOrWhiteSpace(SelectedCategoryKey))
            };

            foreach (var serviceCategory in Categories)
            {
                categoryLinks.Add(new CategoryLinkItem(
                    serviceCategory.DisplayName,
                    Url.Page("/oferta/uslugi", new { category = serviceCategory.Key }) ?? "#",
                    string.Equals(SelectedCategoryKey, serviceCategory.Key, StringComparison.OrdinalIgnoreCase)));
            }

            CategoryLinks = categoryLinks;

            if (string.IsNullOrWhiteSpace(category))
            {
                FilteredCategories = Categories;
            }
            else
            {
                var filteredCategory = Categories.FirstOrDefault(serviceCategory =>
                    string.Equals(serviceCategory.Key, category, StringComparison.OrdinalIgnoreCase));

                FilteredCategories = filteredCategory is null
                    ? Categories
                    : new[] { filteredCategory };
            }

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

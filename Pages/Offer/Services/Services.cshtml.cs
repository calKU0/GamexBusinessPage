using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.AspNetCore.OutputCaching;

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
            ViewData["Title"] = "Remonty dróg Gamex Olkusz – Profesjonalne usługi drogowe";
            ViewData["Description"] = "Gamex z Olkusza wykonuje remonty dróg i nawierzchni w woj. Małopolskim. Profesjonalne maszyny i doświadczenie w usługach drogowych.";
            ViewData["Keywords"] = "Gamex, Olkusz, remonty dróg, usługi drogowe, nawierzchnie asfaltowe, usługi budowlane, Małopolskie";

            var catalog = _catalogCache.GetServiceCatalog();
            Categories = catalog.Categories;
            SelectedCategoryKey = category;

            var categoryLinks = new List<CategoryLinkItem>
            {
                new(
                    "Wszystkie usługi",
                    Url.Page("/Offer/Services/Services") ?? "#",
                    string.IsNullOrWhiteSpace(SelectedCategoryKey))
            };

            foreach (var serviceCategory in Categories)
            {
                categoryLinks.Add(new CategoryLinkItem(
                    serviceCategory.DisplayName,
                    Url.Page("/Offer/Services/Services", new { category = serviceCategory.Key }) ?? "#",
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
            var breadcrumbSchema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
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

            var servicesListSchema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "ItemList",
                ["itemListElement"] = FilteredCategories
                    .SelectMany(serviceCategory => serviceCategory.Services)
                    .Select((service, index) => new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = index + 1,
                        ["item"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "Service",
                            ["name"] = service.Name,
                            ["description"] = service.Description,
                            ["areaServed"] = "Małopolskie",
                            ["provider"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = "Gamex" }
                        }
                    }).ToArray()
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, servicesListSchema }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}

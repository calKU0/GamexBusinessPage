using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages.MachineRental
{
    [OutputCache(Duration = 3600, VaryByQueryKeys = new[] { "category" })]
    public class MachineRentalModel : PageModel
    {
        private readonly CatalogCache _catalogCache;

        public MachineRentalModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public IReadOnlyList<MachineCategory> Categories { get; private set; } = Array.Empty<MachineCategory>();

        public IReadOnlyList<MachineItem> Machines { get; private set; } = Array.Empty<MachineItem>();

        public string? SelectedCategoryKey { get; private set; }

        public IReadOnlyList<CategoryLinkItem> CategoryLinks { get; private set; } = Array.Empty<CategoryLinkItem>();

        public string? SchemaJson { get; private set; }

        public void OnGet(string? category)
        {
            ViewData["Title"] = "Wypożyczalnia maszyn budowlanych Gamex Olkusz";
            ViewData["Description"] = "Gamex w Olkuszu oferuje wynajem maszyn budowlanych – koparki, minikoparki, ładowarki i inne sprzęty. Sprawdź naszą ofertę w woj. Małopolskim.";
            ViewData["Keywords"] = "Gamex, Olkusz, wynajem maszyn budowlanych, wypożyczalnia koparek, minikoparki, ładowarki, sprzęt budowlany, Małopolskie";

            var catalog = _catalogCache.GetMachineCatalog();
            Categories = catalog.Categories;
            SelectedCategoryKey = category;

            Machines = string.IsNullOrWhiteSpace(category)
                ? catalog.Machines
                : catalog.Machines.Where(machine => string.Equals(machine.CategoryKey, category, StringComparison.OrdinalIgnoreCase)).ToList();

            var categoryLinks = new List<CategoryLinkItem>
            {
                new(
                    "Wszystkie maszyny",
                    Url.RouteUrl(new { page = "/Offer/MachineRental/MachineRental" }) ?? "#",
                    string.IsNullOrWhiteSpace(SelectedCategoryKey))
            };

            foreach (var machineCategory in Categories)
            {
                categoryLinks.Add(new CategoryLinkItem(
                    machineCategory.DisplayName,
                    Url.RouteUrl(new { page = "/Offer/MachineRental/MachineRental", category = machineCategory.Key }) ?? "#",
                    string.Equals(SelectedCategoryKey, machineCategory.Key, StringComparison.OrdinalIgnoreCase)));
            }

            CategoryLinks = categoryLinks;

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
                        ["name"] = "Wypożyczenie maszyn",
                        ["item"] = $"{baseUrl}/oferta/wypozyczenie-maszyn"
                    }
                }
            };

            var machinesListSchema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "ItemList",
                ["itemListElement"] = Machines.Select((machine, index) => new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["item"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Product",
                        ["name"] = machine.DisplayName,
                        ["url"] = $"{baseUrl}{Url.RouteUrl(new { page = "/Offer/MachineRental/Details", slug = machine.Slug })}",
                        ["brand"] = string.IsNullOrWhiteSpace(machine.Brand)
                            ? null
                            : new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = machine.Brand }
                    }
                }).ToArray()
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, machinesListSchema }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}

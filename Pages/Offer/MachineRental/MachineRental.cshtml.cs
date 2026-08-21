using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public IReadOnlyList<CategoryLinkItem> CategoryLinks { get; private set; } = Array.Empty<CategoryLinkItem>();

        public string? SchemaJson { get; private set; }

        public string? SelectedCategoryKey { get; private set; }

        public string? SelectedCategoryName { get; private set; }

        public IReadOnlyList<MachineCategory> FilteredCategories { get; private set; } = Array.Empty<MachineCategory>();

        public void OnGet(string? category)
        {
            var catalog = _catalogCache.GetMachineCatalog();
            Categories = catalog.Categories;

            var selectedCategory = string.IsNullOrWhiteSpace(category)
                ? null
                : Categories.FirstOrDefault(item => string.Equals(item.Key, category, StringComparison.OrdinalIgnoreCase));

            // Only a key that actually matched, so an unknown value falls back to
            // the full listing with the "all" link marked.
            SelectedCategoryKey = selectedCategory?.Key;
            SelectedCategoryName = selectedCategory?.DisplayName;

            FilteredCategories = selectedCategory is null
                ? Categories
                : new[] { selectedCategory };

            Machines = selectedCategory is null
                ? catalog.Machines
                : selectedCategory.Machines;

            const string listUrl = "https://gamex-olkusz.pl/oferta/wypozyczenie-maszyn";

            if (selectedCategory is not null)
            {
                ViewData["Title"] = $"Wynajem: {SelectedCategoryName} | Wypożyczalnia maszyn GAMEX Olkusz";
                ViewData["Description"] = $"{SelectedCategoryName} do wynajęcia z operatorem – GAMEX Olkusz. Dostępne maszyny: {Machines.Count}. Transport na budowę własnym sprzętem. Woj. małopolskie, śląskie i świętokrzyskie.";
                ViewData["Keywords"] = $"wynajem {SelectedCategoryName!.ToLowerInvariant()}, wypożyczalnia maszyn budowlanych, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
                ViewData["CanonicalUrl"] = $"{listUrl}?category={Uri.EscapeDataString(selectedCategory.Key)}";
            }
            else
            {
                ViewData["Title"] = "Wypożyczalnia maszyn budowlanych – koparki, ładowarki, walce | GAMEX Olkusz";
                ViewData["Description"] = "Wynajem maszyn budowlanych z operatorem: koparki, minikoparki, walce, rozkładarki asfaltu i frezarki. Transport na budowę. Małopolska, Śląsk, Świętokrzyskie.";
                ViewData["Keywords"] = "wypożyczalnia maszyn budowlanych, wynajem koparki, wynajem minikoparki, wynajem walca, wynajem ładowarki, sprzęt budowlany, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
                ViewData["CanonicalUrl"] = listUrl;
            }

            CategoryLinks = CategoryFilterLinks.Build(
                Url,
                "/Offer/MachineRental/MachineRental",
                "Wszystkie maszyny",
                Categories.Select(item => (item.Key, item.DisplayName)),
                SelectedCategoryKey);

            var baseUrl = "https://gamex-olkusz.pl";
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
                        ["name"] = "Wypożyczenie maszyn",
                        ["item"] = $"{baseUrl}/oferta/wypozyczenie-maszyn"
                    }
                }
            };

            var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

            var machinesListSchema = new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["name"] = "Maszyny budowlane do wynajęcia",
                ["url"] = $"{baseUrl}/oferta/wypozyczenie-maszyn",
                ["numberOfItems"] = Machines.Count,
                ["itemListElement"] = Machines.Select((machine, index) =>
                {
                    var machineUrl = $"{baseUrl}{Url.RouteUrl(new { page = "/Offer/MachineRental/Details", slug = machine.Slug })}";

                    var productSchema = new Dictionary<string, object?>
                    {
                        ["@type"] = "Product",
                        ["name"] = machine.DisplayName,
                        ["url"] = machineUrl,
                        ["description"] = $"{machine.DisplayName} do wynajęcia z operatorem. Rozliczenie godzinowe, dowóz na budowę własnym transportem. {machine.RentalOptions}".Trim(),
                        ["image"] = string.IsNullOrWhiteSpace(machine.MainImagePath) ? $"{baseUrl}/images/machines/default-machine.webp" : $"{baseUrl}{machine.MainImagePath}",
                        ["brand"] = string.IsNullOrWhiteSpace(machine.Brand)
                            ? null
                            : new Dictionary<string, object?>
                            {
                                ["@type"] = "Brand",
                                ["name"] = string.IsNullOrWhiteSpace(machine.Brand) ? "Gamex" : machine.Brand
                            },
                        ["category"] = machine.CategoryDisplayName,
                        ["offers"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "Offer",
                            ["url"] = machineUrl,
                            ["priceCurrency"] = "PLN",
                            ["price"] = "0",
                            ["priceSpecification"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "UnitPriceSpecification",
                                ["priceCurrency"] = "PLN",
                                ["description"] = "Stawka godzinowa lub dobowa ustalana indywidualnie – wycena bezpłatna",
                                ["referenceQuantity"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "QuantitativeValue",
                                    ["value"] = "1",
                                    ["unitCode"] = "HUR"
                                }
                            },
                            ["availability"] = "https://schema.org/InStock",
                            ["itemCondition"] = "https://schema.org/UsedCondition",
                            ["businessFunction"] = "http://purl.org/goodrelations/v1#LeaseOut",
                            ["seller"] = new Dictionary<string, object?>
                            {
                                ["@id"] = localBusiness["@id"]
                            }
                        }
                    };

                    //SchemaFactory.ApplyRating(productSchema, "4.0", 6);

                    return new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = index + 1,
                        ["item"] = productSchema
                    };
                }).ToArray()
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, machinesListSchema, localBusiness }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}

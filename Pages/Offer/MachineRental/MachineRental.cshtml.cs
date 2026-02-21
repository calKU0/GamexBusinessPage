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

            var priceValidUntil = "2026-12-31";
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
                        ["description"] = string.IsNullOrWhiteSpace(machine.RentalOptions) ? "Wynajem maszyny budowlanej z operatorem w Gamex Olkusz." : machine.RentalOptions,
                        ["image"] = string.IsNullOrWhiteSpace(machine.Image) ? $"{baseUrl}/images/machines/default-machine.webp" : $"{baseUrl}{machine.Image}",
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
                            ["priceValidUntil"] = priceValidUntil,
                            ["price"] = "100",
                            ["priceSpecification"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "UnitPriceSpecification",
                                ["priceCurrency"] = "PLN",
                                ["lowPrice"] = "100",
                                ["description"] = "Cena za dobę, uzależniona od długości najmu",
                                ["referenceQuantity"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "QuantitativeValue",
                                    ["value"] = "1",
                                    ["unitCode"] = "DAY"
                                }
                            },
                            ["shippingDetails"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "OfferShippingDetails",
                                ["shippingRate"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "MonetaryAmount",
                                    ["value"] = "0", // Lub stawka bazowa
                                    ["currency"] = "PLN"
                                },
                                ["shippingDestination"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "DefinedRegion",
                                    ["addressCountry"] = "PL"
                                },
                                ["deliveryTime"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "ShippingDeliveryTime",
                                    ["handlingTime"] = new Dictionary<string, object?>
                                    {
                                        ["@type"] = "QuantitativeValue",
                                        ["minValue"] = 0,
                                        ["maxValue"] = 3,
                                        ["unitCode"] = "DAY"
                                    },
                                    ["transitTime"] = new Dictionary<string, object?>
                                    {
                                        ["@type"] = "QuantitativeValue",
                                        ["minValue"] = 0,
                                        ["maxValue"] = 3,
                                        ["unitCode"] = "DAY"
                                    }
                                }
                            },
                            ["hasMerchantReturnPolicy"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "MerchantReturnPolicy",
                                ["applicableCountry"] = "PL",
                                ["returnPolicyCategory"] = "https://schema.org/MerchantReturnNotPermitted",
                                ["merchantReturnLink"] = $"{baseUrl}/regulamin"
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

                    SchemaFactory.ApplyRating(productSchema, "4.0", 6);

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

using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages.Transport;

[OutputCache(Duration = 3600, VaryByQueryKeys = new[] { "category" })]
public class TransportModel : PageModel
{
    private readonly CatalogCache _catalogCache;

    public TransportModel(CatalogCache catalogCache)
    {
        _catalogCache = catalogCache;
    }

    public IReadOnlyList<TransportCategory> Categories { get; private set; } = Array.Empty<TransportCategory>();

    public IReadOnlyList<TransportCategory> FilteredCategories { get; private set; } = Array.Empty<TransportCategory>();

    public string? SelectedCategoryKey { get; private set; }

    public IReadOnlyList<CategoryLinkItem> CategoryLinks { get; private set; } = Array.Empty<CategoryLinkItem>();

    public string? SchemaJson { get; private set; }

    public void OnGet(string? category)
    {
        ViewData["Title"] = "Transport materiałów i maszyn Gamex Olkusz";
        ViewData["Description"] = "Transport materiałów sypkich, maszyn budowlanych i ładunków HDS w Olkuszu i Małopolsce. Sprawdź naszą ofertę transportową.";
        ViewData["Keywords"] = "Gamex, Olkusz, transport materiałów, transport maszyn, HDS, laweta, niskopodwoziowy";

        var catalog = _catalogCache.GetTransportCatalog();
        Categories = catalog.Categories;
        SelectedCategoryKey = category;

        var categoryLinks = new List<CategoryLinkItem>
        {
            new(
                "Wszystkie opcje",
                Url.Page("/Offer/Transport/Transport") ?? "#",
                string.IsNullOrWhiteSpace(SelectedCategoryKey))
        };

        foreach (var transportCategory in Categories)
        {
            categoryLinks.Add(new CategoryLinkItem(
                transportCategory.DisplayName,
                Url.Page("/Offer/Transport/Transport", new { category = transportCategory.Key }) ?? "#",
                string.Equals(SelectedCategoryKey, transportCategory.Key, StringComparison.OrdinalIgnoreCase)));
        }

        CategoryLinks = categoryLinks;

        if (string.IsNullOrWhiteSpace(category))
        {
            FilteredCategories = Categories;
        }
        else
        {
            var filteredCategory = Categories.FirstOrDefault(transportCategory =>
                string.Equals(transportCategory.Key, category, StringComparison.OrdinalIgnoreCase));

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
                    ["name"] = "Transport",
                    ["item"] = $"{baseUrl}/oferta/transport"
                }
            }
        };

        var transportListSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ItemList",
            ["itemListElement"] = FilteredCategories
                .SelectMany(transportCategory => transportCategory.Transports)
                .Select((transport, index) => new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["item"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Service",
                        ["name"] = transport.Vehicle,
                        ["description"] = transport.Description,
                        ["areaServed"] = "Małopolskie",
                        ["provider"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = "Gamex" }
                    }
                }).ToArray()
        };

        var schemaGraph = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[] { breadcrumbSchema, transportListSchema }
        };

        SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

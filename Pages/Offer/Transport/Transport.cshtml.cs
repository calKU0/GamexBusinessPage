using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        var catalog = _catalogCache.GetTransportCatalog();
        Categories = catalog.Categories;

        var selected = string.IsNullOrWhiteSpace(category)
            ? null
            : Categories.FirstOrDefault(item =>
                string.Equals(item.Key, category, StringComparison.OrdinalIgnoreCase));

        // See the services listing: an unknown key must not leave the sidebar
        // with nothing marked.
        SelectedCategoryKey = selected?.Key;
        FilteredCategories = selected is null ? Categories : new[] { selected };

        const string listUrl = "https://gamex-olkusz.pl/oferta/transport";

        if (selected is not null)
        {
            ViewData["Title"] = $"{selected.DisplayName} – transport | GAMEX Olkusz";
            ViewData["Description"] = $"{selected.DisplayName} – GAMEX Olkusz. Własny tabor, realizacja na terenie województwa małopolskiego, śląskiego i świętokrzyskiego.";
            ViewData["Keywords"] = $"{selected.DisplayName.ToLowerInvariant()}, transport, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
            ViewData["CanonicalUrl"] = $"{listUrl}?category={Uri.EscapeDataString(selected.Key)}";
        }
        else
        {
            ViewData["Title"] = "Transport kruszyw i maszyn budowlanych – HDS, laweta | GAMEX Olkusz";
            ViewData["Description"] = "Oferujemy transport kruszyw, piasku oraz przewóz maszyn budowlanych lawetą i HDS. Szybka realizacja na terenie województwa małopolskiego, śląskiego i świętokrzyskiego.";
            ViewData["Keywords"] = "Gamex, Olkusz, transport materiałów, transport maszyn, HDS, laweta, niskopodwoziowy, wywrotka, Małopolska";
            ViewData["CanonicalUrl"] = listUrl;
        }

        CategoryLinks = CategoryFilterLinks.Build(
            Url,
            "/Offer/Transport/Transport",
            "Wszystkie opcje",
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
                    ["name"] = "Transport",
                    ["item"] = $"{baseUrl}/oferta/transport"
                }
            }
        };

        var transportListSchema = new Dictionary<string, object?>
        {
            ["@type"] = "ItemList",
            ["name"] = "Transport materiałów i maszyn Gamex Olkusz",
            ["itemListElement"] = FilteredCategories
                .SelectMany(transportCategory => transportCategory.Transports)
                .Select((transport, index) =>
                {
                    var transportServiceItem = new Dictionary<string, object?>
                    {
                        ["@type"] = "Service",
                        ["name"] = transport.Vehicle,
                        ["description"] = transport.Description,
                        ["provider"] = new Dictionary<string, object?>
                        {
                            ["@id"] = localBusiness["@id"]
                        },
                        ["areaServed"] = localBusiness["areaServed"],
                        ["serviceType"] = "Transport materiałów i maszyn"
                    };

                    // SchemaFactory.ApplyRating(transportServiceItem, "4.0", 6);

                    return new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = index + 1,
                        ["item"] = transportServiceItem
                    };
                }).ToArray()
        };

        var schemaGraph = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[] { breadcrumbSchema, transportListSchema, localBusiness }
        };

        SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

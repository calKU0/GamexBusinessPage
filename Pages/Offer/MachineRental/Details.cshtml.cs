using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages.MachineRental;

[OutputCache(Duration = 3600, VaryByRouteValueNames = new[] { "slug" })]
public class DetailsModel : PageModel
{
    private readonly CatalogCache _catalogCache;

    public DetailsModel(CatalogCache catalogCache)
    {
        _catalogCache = catalogCache;
    }

    public IReadOnlyList<MachineCategory> Categories { get; private set; } = Array.Empty<MachineCategory>();

    public MachineItem? Machine { get; private set; }

    public string? SelectedCategoryKey { get; private set; }

    public IReadOnlyList<CategoryLinkItem> CategoryLinks { get; private set; } = Array.Empty<CategoryLinkItem>();

    public ContactFormViewModel ContactForm { get; private set; } = new(Array.Empty<MachineItem>(), null, true);

    [TempData]
    public string? ContactFormStatusMessage { get; set; }

    [TempData]
    public bool? ContactFormStatusSuccess { get; set; }

    public string? SchemaJson { get; private set; }

    public IActionResult OnGet(string slug)
    {
        var catalog = _catalogCache.GetMachineCatalog();
        Categories = catalog.Categories;
        Machine = catalog.FindBySlug(slug);

        if (Machine is null)
        {
            return NotFound();
        }

        SelectedCategoryKey = Machine.CategoryKey;

        var categoryLinks = new List<CategoryLinkItem>
        {
            new(
                "Wszystkie maszyny",
                Url.Page("/oferta/wypozyczenie-maszyn") ?? "#",
                string.IsNullOrWhiteSpace(SelectedCategoryKey))
        };

        foreach (var category in Categories)
        {
            categoryLinks.Add(new CategoryLinkItem(
                category.DisplayName,
                Url.Page("/oferta/wypozyczenie-maszyn", new { category = category.Key }) ?? "#",
                string.Equals(SelectedCategoryKey, category.Key, StringComparison.OrdinalIgnoreCase)));
        }

        CategoryLinks = categoryLinks;

        var machines = catalog.Machines
            .OrderBy(machine => machine.CategoryDisplayName)
            .ThenBy(machine => machine.DisplayName)
            .ToList();

        ContactForm = new ContactFormViewModel(machines, Machine.Slug, true, null, ContactFormStatusMessage, ContactFormStatusSuccess);

        ViewData["Title"] = $"Wynajem {Machine.DisplayName} – {Machine.CategoryDisplayName} w Małopolsce | Gamex Olkusz";
        ViewData["Description"] = $"Szukasz {Machine.DisplayName}? Oferujemy wynajem krótko i długoterminowy z profesjonalną obsługą operatorską w Małopolce, Śląsku i Świętokrzyskim. Zadzwoń po wycenę!";
        ViewData["Keywords"] = $"{Machine.Brand}, {Machine.Model}, wynajem {Machine.CategoryDisplayName}, maszyny budowlane Olkusz, Małopolska, Śląsk, Świętokrzyskie";

        var priceValidUntil = "2026-12-31";
        var baseUrl = "https://gamex-olkusz.pl";
        if (!string.IsNullOrWhiteSpace(Machine.MainImagePath))
        {
            ViewData["OgImage"] = string.IsNullOrWhiteSpace(Machine.MainImagePath) ? $"{baseUrl}/images/machines/default-machine.webp" : $"{baseUrl}{Machine.MainImagePath}";
        }

        var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

        var detailsUrl = $"{baseUrl}{Url.Page("/oferta/wypozyczenie-maszyn", new { slug = Machine.Slug })}";

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
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = 4,
                    ["name"] = Machine.DisplayName,
                    ["item"] = detailsUrl
                }
            }
        };

        var machineSchema = new Dictionary<string, object?>
        {
            ["@type"] = "Product",
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["url"] = detailsUrl,
                ["priceCurrency"] = "PLN",
                ["price"] = "100",
                ["priceValidUntil"] = priceValidUntil,
                ["itemCondition"] = "https://schema.org/UsedCondition",
                ["availability"] = "https://schema.org/InStock",
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
                ["seller"] = new Dictionary<string, object?>
                {
                    ["@id"] = localBusiness["@id"]
                },
                ["businessFunction"] = "http://purl.org/goodrelations/v1#LeaseOut"
            },
            ["name"] = Machine.DisplayName,
            ["description"] = $"Wynajem: {Machine.DisplayName}. Oferujemy profesjonalny sprzęt budowlany z transportem do klienta w województwie małopolskim, śląskim i świętokrzyskim. {Machine.RentalOptions}",
            ["image"] = string.IsNullOrWhiteSpace(Machine.MainImagePath) ? $"{baseUrl}/images/machines/default-machine.webp" : $"{baseUrl}{Machine.MainImagePath}",
            ["brand"] = string.IsNullOrWhiteSpace(Machine.Brand)
                ? null
            : new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = Machine.Brand },
            ["url"] = detailsUrl,
            ["category"] = Machine.CategoryDisplayName
        };

        SchemaFactory.ApplyRating(machineSchema, "4.0", 6);

        var schemaGraph = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[] { breadcrumbSchema, machineSchema, localBusiness }
        };

        SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        return Page();
    }
}

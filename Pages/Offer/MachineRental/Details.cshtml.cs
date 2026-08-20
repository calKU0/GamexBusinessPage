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

        ViewData["Title"] = $"Wynajem {Machine.DisplayName} | GAMEX Olkusz";
        // When a machine carries its own description, its first sentence becomes the
        // meta description, so every card gets unique copy rather than a variant of
        // the same template.
        var firstSentence = string.IsNullOrWhiteSpace(Machine.Description)
            ? null
            : Machine.Description.Split(". ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim().TrimEnd('.');

        var prefix = $"{Machine.DisplayName} na wynajem z operatorem. ";
        const string suffix = " GAMEX Olkusz.";
        const int limit = 160;

        if (!string.IsNullOrWhiteSpace(firstSentence))
        {
            // Google truncates the description around 160 characters. Cut on a word
            // boundary so the result does not end mid-word.
            var budget = limit - prefix.Length - suffix.Length;
            if (firstSentence.Length > budget && budget > 20)
            {
                var trimmed = firstSentence[..budget];
                var lastSpace = trimmed.LastIndexOf(' ');
                firstSentence = (lastSpace > 0 ? trimmed[..lastSpace] : trimmed).TrimEnd(',', ' ') + "…";
            }
        }

        ViewData["Description"] = string.IsNullOrWhiteSpace(firstSentence)
            ? $"{Machine.DisplayName} do wynajęcia z operatorem – {Machine.CategoryDisplayName}. Rozliczenie godzinowe, dowóz na budowę. GAMEX Olkusz."
            : prefix + firstSentence + "." + suffix;
        ViewData["Keywords"] = $"{Machine.Brand}, {Machine.Model}, wynajem {Machine.CategoryDisplayName}, maszyny budowlane Olkusz, Małopolska, Śląsk, Świętokrzyskie";

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
                // Stawki ustalamy indywidualnie. Kwota "0" z opisem to zalecany przez Google
                // zapis "cena na zapytanie" - podanie zmyslonej ceny grozi kara za dane strukturalne.
                ["price"] = "0",
                ["itemCondition"] = "https://schema.org/UsedCondition",
                ["availability"] = "https://schema.org/InStock",
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
                ["seller"] = new Dictionary<string, object?>
                {
                    ["@id"] = localBusiness["@id"]
                },
                ["businessFunction"] = "http://purl.org/goodrelations/v1#LeaseOut"
            },
            ["name"] = Machine.DisplayName,
            // Opis maszyny, jesli istnieje, jest znacznie lepszym sygnalem niz szablon.
            ["description"] = string.IsNullOrWhiteSpace(Machine.Description)
                ? $"{Machine.DisplayName} do wynajęcia z operatorem. Rozliczenie godzinowe, dowóz na budowę własnym transportem. {Machine.RentalOptions}".Trim()
                : Machine.Description,
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

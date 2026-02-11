using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OutputCaching;

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
                Url.Page("/Offer/MachineRental/MachineRental") ?? "#",
                string.IsNullOrWhiteSpace(SelectedCategoryKey))
        };

        foreach (var category in Categories)
        {
            categoryLinks.Add(new CategoryLinkItem(
                category.DisplayName,
                Url.Page("/Offer/MachineRental/MachineRental", new { category = category.Key }) ?? "#",
                string.Equals(SelectedCategoryKey, category.Key, StringComparison.OrdinalIgnoreCase)));
        }

        CategoryLinks = categoryLinks;

        var machines = catalog.Machines
            .OrderBy(machine => machine.CategoryDisplayName)
            .ThenBy(machine => machine.DisplayName)
            .ToList();
        ContactForm = new ContactFormViewModel(machines, Machine.Slug, true);

        ViewData["Title"] = $"{Machine.DisplayName} – wynajem maszyn Gamex";
        ViewData["Description"] = $"Sprawdź szczegóły wynajmu maszyny {Machine.DisplayName} w Gamex Olkusz. Profesjonalny sprzęt i obsługa operatorska.";
        ViewData["Keywords"] = $"{Machine.Brand}, {Machine.Model}, wynajem maszyn budowlanych, Gamex, Olkusz";

        var baseUrl = "https://gamex-olkusz.pl";
        if (!string.IsNullOrWhiteSpace(Machine.Image))
        {
            ViewData["OgImage"] = $"{baseUrl}{Machine.Image}";
        }

        var detailsUrl = $"{baseUrl}{Url.Page("/Offer/MachineRental/Details", new { slug = Machine.Slug })}";
        var machineSchema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = Machine.DisplayName,
            ["description"] = string.IsNullOrWhiteSpace(Machine.RentalOptions)
                    ? "Wynajem maszyny budowlanej z operatorem w Gamex Olkusz."
                : Machine.RentalOptions,
            ["image"] = string.IsNullOrWhiteSpace(Machine.Image) ? null : $"{baseUrl}{Machine.Image}",
            ["brand"] = string.IsNullOrWhiteSpace(Machine.Brand)
                ? null
            : new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = Machine.Brand },
            ["url"] = detailsUrl,
            ["category"] = Machine.CategoryDisplayName
        };

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

        var schemaGraph = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[] { breadcrumbSchema, machineSchema }
        };

        SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        return Page();
    }
}

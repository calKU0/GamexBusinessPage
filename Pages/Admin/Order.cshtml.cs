using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace GamexBusinessPage.Pages.Admin;

/// <summary>
/// Sets the order in which categories, and the items inside each category,
/// appear on the public pages.
/// </summary>
public class OrderModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AdminCatalogService _catalogService;

    public OrderModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>"machines", "services" or "transport".</summary>
    [BindProperty(SupportsGet = true)]
    public string Catalog { get; set; } = "machines";

    /// <summary>The new order, serialised by the page script on submit.</summary>
    [BindProperty]
    public string? OrderJson { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string CatalogTitle { get; private set; } = string.Empty;

    public string ListPage { get; private set; } = "/Admin/Machines";

    public IReadOnlyList<OrderCategory> Categories { get; private set; } = Array.Empty<OrderCategory>();

    public static IReadOnlyList<(string Key, string Title)> Catalogs { get; } = new[]
    {
        ("machines", "Maszyny"),
        ("services", "Usługi"),
        ("transport", "Transport")
    };

    public IActionResult OnGet()
    {
        if (!IsKnownCatalog(Catalog))
        {
            return RedirectToPage(new { catalog = "machines" });
        }

        ViewData["Title"] = "Kolejność na stronie - panel administracyjny";
        Load();
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!IsKnownCatalog(Catalog))
        {
            return RedirectToPage(new { catalog = "machines" });
        }

        List<CategoryOrder>? order;
        try
        {
            order = string.IsNullOrWhiteSpace(OrderJson)
                ? null
                : JsonSerializer.Deserialize<List<CategoryOrder>>(OrderJson, JsonOptions);
        }
        catch (JsonException)
        {
            order = null;
        }

        if (order is null)
        {
            StatusMessage = "Nie udało się odczytać nowej kolejności. Nic nie zostało zapisane.";
            return RedirectToPage(new { catalog = Catalog });
        }

        string? error;
        var saved = Catalog switch
        {
            "services" => _catalogService.ReorderServices(order, out error),
            "transport" => _catalogService.ReorderTransport(order, out error),
            _ => _catalogService.ReorderMachines(order, out error)
        };

        StatusMessage = saved ? "Kolejność została zapisana." : error;
        return RedirectToPage(new { catalog = Catalog });
    }

    private void Load()
    {
        switch (Catalog)
        {
            case "services":
                CatalogTitle = "Usługi";
                ListPage = "/Admin/Services";
                Categories = Project(_catalogService.LoadServiceData(), item => item.Name);
                break;

            case "transport":
                CatalogTitle = "Transport";
                ListPage = "/Admin/Transport";
                Categories = Project(_catalogService.LoadTransportData(), item => item.Vehicle);
                break;

            default:
                CatalogTitle = "Maszyny";
                ListPage = "/Admin/Machines";
                Categories = Project(_catalogService.LoadMachineData(), item => item.DisplayName);
                break;
        }
    }

    private static IReadOnlyList<OrderCategory> Project<TItem>(
        Dictionary<string, List<TItem>> data,
        Func<TItem, string?> label)
    {
        return data
            .Select(category => new OrderCategory(
                category.Key,
                category.Value
                    .Select((item, index) => new OrderItem(index, string.IsNullOrWhiteSpace(label(item)) ? "(bez nazwy)" : label(item)!))
                    .ToList()))
            .ToList();
    }

    private static bool IsKnownCatalog(string? catalog)
    {
        return Catalogs.Any(entry => string.Equals(entry.Key, catalog, StringComparison.Ordinal));
    }

    public sealed record OrderCategory(string Key, IReadOnlyList<OrderItem> Items);

    public sealed record OrderItem(int Index, string Label);
}

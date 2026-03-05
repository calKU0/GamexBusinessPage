using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class TransportModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public TransportModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty(SupportsGet = true)]
    public string? TransportSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TransportCategoryFilter { get; set; }

    [BindProperty]
    public string? OriginalTransportCategory { get; set; }

    [BindProperty]
    public string? OriginalTransportVehicle { get; set; }

    public List<TransportListItem> Transports { get; private set; } = [];

    public List<string> TransportCategoryKeys { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Transport - panel administracyjny";
        LoadData();
    }

    public IActionResult OnPostDeleteTransport()
    {
        var data = _catalogService.LoadTransportData();
        if (string.IsNullOrWhiteSpace(OriginalTransportCategory) || string.IsNullOrWhiteSpace(OriginalTransportVehicle))
        {
            return RedirectToPage("/Admin/Transport");
        }

        if (data.TryGetValue(OriginalTransportCategory, out var items))
        {
            var existing = items.FirstOrDefault(item => string.Equals(item.Vehicle, OriginalTransportVehicle, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                items.Remove(existing);
                _catalogService.SaveTransportData(data);
                StatusMessage = "Transport został usunięty.";
            }
        }

        return RedirectToPage("/Admin/Transport");
    }

    private void LoadData()
    {
        var transportData = _catalogService.LoadTransportData();
        TransportCategoryKeys = transportData.Keys.OrderBy(key => key).ToList();

        Transports = transportData.SelectMany(category => category.Value.Select(item => new TransportListItem
            {
                CategoryKey = category.Key,
                Vehicle = item.Vehicle ?? string.Empty,
                WeightLimit = item.WeightLimit ?? string.Empty
            }))
            .Where(item => MatchesFilter(item.CategoryKey, item.Vehicle, TransportCategoryFilter, TransportSearch)
                || MatchesFilter(item.CategoryKey, item.WeightLimit, TransportCategoryFilter, TransportSearch))
            .OrderBy(item => item.CategoryKey)
            .ThenBy(item => item.Vehicle)
            .ToList();
    }

    private static bool MatchesFilter(string categoryKey, string text, string? categoryFilter, string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(categoryFilter) && !string.Equals(categoryKey, categoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return true;
        }

        return text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class TransportListItem
    {
        public string CategoryKey { get; set; } = string.Empty;

        public string Vehicle { get; set; } = string.Empty;

        public string WeightLimit { get; set; } = string.Empty;
    }
}

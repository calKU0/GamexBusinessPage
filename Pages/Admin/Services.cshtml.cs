using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class ServicesModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public ServicesModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty(SupportsGet = true)]
    public string? ServiceSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ServiceCategoryFilter { get; set; }

    [BindProperty]
    public string? OriginalServiceCategory { get; set; }

    [BindProperty]
    public string? OriginalServiceName { get; set; }

    public List<ServiceListItem> Services { get; private set; } = [];

    public List<string> ServiceCategoryKeys { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Usługi - panel administracyjny";
        LoadData();
    }

    public IActionResult OnPostDeleteService()
    {
        var data = _catalogService.LoadServiceData();
        if (string.IsNullOrWhiteSpace(OriginalServiceCategory) || string.IsNullOrWhiteSpace(OriginalServiceName))
        {
            return RedirectToPage("/Admin/Services");
        }

        if (data.TryGetValue(OriginalServiceCategory, out var items))
        {
            var existing = items.FirstOrDefault(item => string.Equals(item.Name, OriginalServiceName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                items.Remove(existing);
                _catalogService.SaveServiceData(data);
                StatusMessage = "Usługa została usunięta.";
            }
        }

        return RedirectToPage("/Admin/Services");
    }

    private void LoadData()
    {
        var serviceData = _catalogService.LoadServiceData();
        ServiceCategoryKeys = serviceData.Keys.OrderBy(key => key).ToList();

        Services = serviceData.SelectMany(category => category.Value.Select(item => new ServiceListItem
            {
                CategoryKey = category.Key,
                Name = item.Name ?? string.Empty,
                Description = item.Description ?? string.Empty
            }))
            .Where(item => MatchesFilter(item.CategoryKey, item.Name, ServiceCategoryFilter, ServiceSearch)
                || MatchesFilter(item.CategoryKey, item.Description, ServiceCategoryFilter, ServiceSearch))
            .OrderBy(item => item.CategoryKey)
            .ThenBy(item => item.Name)
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

    public sealed class ServiceListItem
    {
        public string CategoryKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}

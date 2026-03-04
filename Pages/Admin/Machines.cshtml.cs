using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class MachinesModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public MachinesModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty(SupportsGet = true)]
    public string? MachineSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? MachineCategoryFilter { get; set; }

    [BindProperty]
    public string? OriginalMachineCategory { get; set; }

    [BindProperty]
    public string? OriginalMachineSlug { get; set; }

    public List<MachineListItem> Machines { get; private set; } = [];

    public List<string> MachineCategoryKeys { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Maszyny - panel administracyjny";
        LoadData();
    }

    public IActionResult OnPostDeleteMachine()
    {
        var data = _catalogService.LoadMachineData();
        if (string.IsNullOrWhiteSpace(OriginalMachineCategory) || string.IsNullOrWhiteSpace(OriginalMachineSlug))
        {
            return RedirectToPage("/Admin/Machines");
        }

        if (data.TryGetValue(OriginalMachineCategory, out var items))
        {
            var existing = items.FirstOrDefault(item => string.Equals(item.Slug, OriginalMachineSlug, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                items.Remove(existing);
                _catalogService.SaveMachineData(data);
                StatusMessage = "Maszyna została usunięta.";
            }
        }

        return RedirectToPage("/Admin/Machines");
    }

    private void LoadData()
    {
        var machineData = _catalogService.LoadMachineData();
        MachineCategoryKeys = machineData.Keys.OrderBy(key => key).ToList();

        Machines = machineData.SelectMany(category => category.Value.Select(item => new MachineListItem
            {
                CategoryKey = category.Key,
                DisplayName = item.DisplayName,
                ProductionYear = item.ProductionYear,
                Weight = item.Weight,
                Slug = item.Slug
            }))
            .Where(item => MatchesFilter(item.CategoryKey, item.DisplayName, MachineCategoryFilter, MachineSearch))
            .OrderBy(item => item.CategoryKey)
            .ThenBy(item => item.DisplayName)
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

    public sealed class MachineListItem
    {
        public string CategoryKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public int? ProductionYear { get; set; }

        public string? Weight { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class TransportEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public TransportEditModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty]
    public TransportEditor TransportInput { get; set; } = new();

    [BindProperty]
    public string? OriginalTransportCategory { get; set; }

    [BindProperty]
    public string? OriginalTransportVehicle { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Vehicle { get; set; }

    public List<string> TransportCategoryKeys { get; private set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalTransportVehicle) && !string.IsNullOrWhiteSpace(OriginalTransportCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Transport - edycja";
        LoadCategories();
        PopulateEditor();
    }

    public IActionResult OnPost()
    {
        LoadCategories();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = _catalogService.LoadTransportData();
        var categoryKey = NormalizeKey(TransportInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(TransportInput.CategoryKey), "Podaj kategorię.");
            return Page();
        }

        var vehicleName = NormalizeKey(TransportInput.Vehicle);
        if (string.IsNullOrWhiteSpace(vehicleName))
        {
            ModelState.AddModelError(nameof(TransportInput.Vehicle), "Podaj nazwę pojazdu.");
            return Page();
        }

        if (IsEdit)
        {
            if (!data.TryGetValue(OriginalTransportCategory!, out var items))
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii transportu.");
                return Page();
            }

            var existing = items.FirstOrDefault(item => string.Equals(item.Vehicle, OriginalTransportVehicle, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono transportu do edycji.");
                return Page();
            }

            items.Remove(existing);
        }

        if (!data.TryGetValue(categoryKey, out var targetItems))
        {
            targetItems = [];
            data[categoryKey] = targetItems;
        }

        if (targetItems.Any(item => string.Equals(item.Vehicle, vehicleName, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, "Pozycja o tej nazwie już istnieje w tej kategorii.");
            return Page();
        }

        targetItems.Add(CreateTransportItem());
        _catalogService.SaveTransportData(data);

        return RedirectToPage("/Admin/Transport");
    }

    private void LoadCategories()
    {
        var transportData = _catalogService.LoadTransportData();
        TransportCategoryKeys = transportData.Keys.OrderBy(key => key).ToList();
    }

    private void PopulateEditor()
    {
        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Vehicle))
        {
            return;
        }

        var data = _catalogService.LoadTransportData();
        if (!data.TryGetValue(Category, out var items))
        {
            return;
        }

        var transport = items.FirstOrDefault(item => string.Equals(item.Vehicle, Vehicle, StringComparison.OrdinalIgnoreCase));
        if (transport is null)
        {
            return;
        }

        TransportInput = new TransportEditor
        {
            CategoryKey = Category,
            Vehicle = transport.Vehicle ?? string.Empty,
            Description = transport.Description,
            WeightLimit = transport.WeightLimit,
            Highlight = transport.Highlight,
            Image = transport.MainImagePath
        };

        OriginalTransportCategory = Category;
        OriginalTransportVehicle = Vehicle;
    }

    private TransportItem CreateTransportItem()
    {
        var imagePath = NormalizeKey(TransportInput.Image);
        return new TransportItem
        {
            Vehicle = NormalizeKey(TransportInput.Vehicle),
            Description = NormalizeKey(TransportInput.Description),
            Images = BuildImages(imagePath),
            WeightLimit = NormalizeKey(TransportInput.WeightLimit),
            Highlight = NormalizeKey(TransportInput.Highlight)
        };
    }

    private static List<CatalogImage> BuildImages(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return [];
        }

        return [new CatalogImage { IsMain = true, Path = imagePath }];
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public sealed class TransportEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nazwę pojazdu.")]
        public string Vehicle { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Image { get; set; }

        public string? WeightLimit { get; set; }

        public string? Highlight { get; set; }
    }
}

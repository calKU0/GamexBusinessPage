using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class ServiceEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public ServiceEditModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty]
    public ServiceEditor ServiceInput { get; set; } = new();

    [BindProperty]
    public string? OriginalServiceCategory { get; set; }

    [BindProperty]
    public string? OriginalServiceName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Name { get; set; }

    public List<string> ServiceCategoryKeys { get; private set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalServiceName) && !string.IsNullOrWhiteSpace(OriginalServiceCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Usługi - edycja";
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

        var data = _catalogService.LoadServiceData();
        var categoryKey = NormalizeKey(ServiceInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(ServiceInput.CategoryKey), "Podaj kategorię.");
            return Page();
        }

        var name = NormalizeKey(ServiceInput.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(ServiceInput.Name), "Podaj nazwę usługi.");
            return Page();
        }

        if (IsEdit)
        {
            if (!data.TryGetValue(OriginalServiceCategory!, out var items))
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii usługi.");
                return Page();
            }

            var existing = items.FirstOrDefault(item => string.Equals(item.Name, OriginalServiceName, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono usługi do edycji.");
                return Page();
            }

            items.Remove(existing);
        }

        if (!data.TryGetValue(categoryKey, out var targetItems))
        {
            targetItems = [];
            data[categoryKey] = targetItems;
        }

        if (targetItems.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, "Usługa o tej nazwie już istnieje w tej kategorii.");
            return Page();
        }

        targetItems.Add(CreateServiceItem());
        _catalogService.SaveServiceData(data);

        return RedirectToPage("/Admin/Services");
    }

    private void LoadCategories()
    {
        var serviceData = _catalogService.LoadServiceData();
        ServiceCategoryKeys = serviceData.Keys.OrderBy(key => key).ToList();
    }

    private void PopulateEditor()
    {
        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Name))
        {
            return;
        }

        var data = _catalogService.LoadServiceData();
        if (!data.TryGetValue(Category, out var items))
        {
            return;
        }

        var service = items.FirstOrDefault(item => string.Equals(item.Name, Name, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            return;
        }

        ServiceInput = new ServiceEditor
        {
            CategoryKey = Category,
            Name = service.Name ?? string.Empty,
            Description = service.Description,
            Highlight = service.Highlight,
            Image = service.MainImagePath
        };

        OriginalServiceCategory = Category;
        OriginalServiceName = Name;
    }

    private ServiceItem CreateServiceItem()
    {
        var imagePath = NormalizeKey(ServiceInput.Image);
        return new ServiceItem
        {
            Name = NormalizeKey(ServiceInput.Name),
            Description = NormalizeKey(ServiceInput.Description),
            Images = BuildImages(imagePath),
            Highlight = NormalizeKey(ServiceInput.Highlight)
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

    public sealed class ServiceEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nazwę usługi.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Image { get; set; }

        public string? Highlight { get; set; }
    }
}

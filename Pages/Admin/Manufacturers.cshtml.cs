using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

/// <summary>
/// The manufacturers shown on the home page: their order, and removing them.
/// Adding and editing happen on <see cref="ManufacturerEditModel"/>.
/// </summary>
public class ManufacturersModel : PageModel
{
    private readonly AdminCatalogService _catalogService;
    private readonly ImageService _imageService;

    public ManufacturersModel(AdminCatalogService catalogService, ImageService imageService)
    {
        _catalogService = catalogService;
        _imageService = imageService;
    }

    public IReadOnlyList<Manufacturer> Manufacturers { get; private set; } = Array.Empty<Manufacturer>();

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
        ViewData["Title"] = "Producenci - panel administracyjny";
        Manufacturers = _catalogService.LoadManufacturers();
    }

    public IActionResult OnPostMove(string name, string direction)
    {
        var manufacturers = _catalogService.LoadManufacturers();
        var index = FindIndex(manufacturers, name);
        var target = direction == "up" ? index - 1 : index + 1;

        if (index >= 0 && target >= 0 && target < manufacturers.Count)
        {
            (manufacturers[index], manufacturers[target]) = (manufacturers[target], manufacturers[index]);
            _catalogService.SaveManufacturers(manufacturers);
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(string name)
    {
        var manufacturers = _catalogService.LoadManufacturers();
        var index = FindIndex(manufacturers, name);
        if (index < 0)
        {
            StatusMessage = "Nie znaleziono producenta.";
            return RedirectToPage();
        }

        var removed = manufacturers[index];
        manufacturers.RemoveAt(index);
        _catalogService.SaveManufacturers(manufacturers);
        DeleteUnusedLogo(_imageService, manufacturers, removed.Logo);

        StatusMessage = $"Usunięto producenta {removed.Name}.";
        return RedirectToPage();
    }

    internal static int FindIndex(List<Manufacturer> manufacturers, string? name)
    {
        return manufacturers.FindIndex(manufacturer =>
            string.Equals(manufacturer.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes a logo file nothing points to any more. Logos that ship with the
    /// site stay on disk.
    /// </summary>
    internal static void DeleteUnusedLogo(ImageService imageService, List<Manufacturer> manufacturers, string? logo)
    {
        if (string.IsNullOrWhiteSpace(logo)
            || ManufacturerList.IsBundledLogo(logo)
            || manufacturers.Any(manufacturer => string.Equals(manufacturer.Logo, logo, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        imageService.DeleteImage(logo);
    }
}

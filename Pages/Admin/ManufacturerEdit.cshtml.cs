using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class ManufacturerEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;
    private readonly ImageService _imageService;

    public ManufacturerEditModel(AdminCatalogService catalogService, ImageService imageService)
    {
        _catalogService = catalogService;
        _imageService = imageService;
    }

    /// <summary>The manufacturer being edited, by name; empty when adding one.</summary>
    [BindProperty(SupportsGet = true, Name = "name")]
    public string? OriginalName { get; set; }

    [BindProperty]
    public ManufacturerEditor Input { get; set; } = new();

    /// <summary>The saved manufacturer, for the logo preview.</summary>
    public Manufacturer? Current { get; private set; }

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalName);

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet()
    {
        ViewData["Title"] = "Producenci - edycja";

        if (IsEdit)
        {
            Current = Find(_catalogService.LoadManufacturers());
            if (Current is null)
            {
                StatusMessage = "Nie znaleziono producenta.";
                return RedirectToPage("/Admin/Manufacturers");
            }

            Input.Name = Current.Name;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Producenci - edycja";

        var manufacturers = _catalogService.LoadManufacturers();
        Current = IsEdit ? Find(manufacturers) : null;
        if (IsEdit && Current is null)
        {
            StatusMessage = "Nie znaleziono producenta.";
            return RedirectToPage("/Admin/Manufacturers");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var name = Input.Name.Trim();
        var taken = manufacturers.Any(manufacturer => manufacturer != Current
            && string.Equals(manufacturer.Name, name, StringComparison.OrdinalIgnoreCase));
        if (taken)
        {
            ModelState.AddModelError("Input.Name", "Producent o tej nazwie już jest na liście.");
            return Page();
        }

        SavedLogo? uploaded = null;
        if (Input.LogoFile is { Length: > 0 })
        {
            uploaded = await _imageService.ProcessLogoAsync(Input.LogoFile, name);
            if (uploaded is null)
            {
                ModelState.AddModelError("Input.LogoFile", "Nie udało się odczytać logo. Wgraj plik PNG, WebP lub JPG.");
                return Page();
            }
        }

        var manufacturer = Current ?? new Manufacturer();
        var previousLogo = manufacturer.Logo;

        manufacturer.Name = name;
        if (uploaded is not null)
        {
            manufacturer.Logo = uploaded.Path;
            manufacturer.LogoWidth = uploaded.Width;
            manufacturer.LogoHeight = uploaded.Height;
        }
        else if (Input.RemoveLogo)
        {
            manufacturer.Logo = null;
            manufacturer.LogoWidth = null;
            manufacturer.LogoHeight = null;
        }

        if (Current is null)
        {
            manufacturers.Add(manufacturer);
        }

        _catalogService.SaveManufacturers(manufacturers);

        if (!string.Equals(previousLogo, manufacturer.Logo, StringComparison.OrdinalIgnoreCase))
        {
            ManufacturersModel.DeleteUnusedLogo(_imageService, manufacturers, previousLogo);
        }

        StatusMessage = Current is null ? $"Dodano producenta {name}." : $"Zapisano producenta {name}.";
        return RedirectToPage("/Admin/Manufacturers");
    }

    private Manufacturer? Find(List<Manufacturer> manufacturers)
    {
        var index = ManufacturersModel.FindIndex(manufacturers, OriginalName);
        return index < 0 ? null : manufacturers[index];
    }

    public sealed class ManufacturerEditor
    {
        [Required(ErrorMessage = "Podaj nazwę producenta.")]
        [StringLength(40, ErrorMessage = "Nazwa może mieć maksymalnie 40 znaków.")]
        public string Name { get; set; } = string.Empty;

        public IFormFile? LogoFile { get; set; }

        public bool RemoveLogo { get; set; }
    }
}

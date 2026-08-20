using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class MachineEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;
    private readonly ImageService _imageService;

    public MachineEditModel(AdminCatalogService catalogService, ImageService imageService)
    {
        _catalogService = catalogService;
        _imageService = imageService;
    }

    [BindProperty]
    public MachineEditor MachineInput { get; set; } = new();

    [BindProperty]
    public List<string> EquipmentItems { get; set; } = [];

    [BindProperty]
    public List<SpecificationEntry> SpecificationItems { get; set; } = [];

    [BindProperty]
    public List<IFormFile> ImageFiles { get; set; } = [];

    [BindProperty]
    public string? MainImagePath { get; set; }

    [BindProperty]
    public string? MainImageType { get; set; }

    [BindProperty]
    public string? OriginalMachineCategory { get; set; }

    [BindProperty]
    public string? OriginalMachineSlug { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Slug { get; set; }

    public List<string> MachineCategoryKeys { get; private set; } = [];

    public List<ImageViewModel> ExistingImages { get; set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalMachineSlug) && !string.IsNullOrWhiteSpace(OriginalMachineCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Maszyny - edycja";
        LoadCategories();
        PopulateEditor();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadCategories();

        if (!ModelState.IsValid)
        {
            EnsureLists();
            LoadExistingImages();
            return Page();
        }

        var data = _catalogService.LoadMachineData();
        var categoryKey = NormalizeKey(MachineInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(MachineInput.CategoryKey), "Podaj kategorię.");
            EnsureLists();
            LoadExistingImages();
            return Page();
        }

        try
        {
            // Always reload existing images before creating new item
            LoadExistingImages();

            // Process new image uploads
            List<string> newImagePaths = [];
            if (ImageFiles?.Any(f => f.Length > 0) == true)
            {
                newImagePaths = await _imageService.ProcessAndSaveImagesAsync(
                    ImageFiles.Where(f => f.Length > 0), 
                    "machines");
            }

            var newItem = await CreateMachineItemAsync(newImagePaths);
            var newSlug = newItem.Slug;
            if (string.IsNullOrWhiteSpace(newSlug))
            {
                ModelState.AddModelError(string.Empty, "Podaj markę, model lub typ maszyny.");
                EnsureLists();
                LoadExistingImages();
                return Page();
            }

            if (IsEdit)
            {
                if (!data.TryGetValue(OriginalMachineCategory!, out var items))
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii maszyny.");
                    EnsureLists();
                    LoadExistingImages();
                    return Page();
                }

                var existing = items.FirstOrDefault(item => string.Equals(item.Slug, OriginalMachineSlug, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono maszyny do edycji.");
                    EnsureLists();
                    LoadExistingImages();
                    return Page();
                }

                items.Remove(existing);
            }

            if (!data.TryGetValue(categoryKey, out var targetItems))
            {
                targetItems = [];
                data[categoryKey] = targetItems;
            }

            if (targetItems.Any(item => string.Equals(item.Slug, newSlug, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, "Maszyna o tej nazwie już istnieje w tej kategorii.");
                EnsureLists();
                LoadExistingImages();
                return Page();
            }

            targetItems.Add(newItem);
            _catalogService.SaveMachineData(data);

            return RedirectToPage("/Admin/Machines");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Błąd podczas zapisywania: {ex.Message}");
            EnsureLists();
            LoadExistingImages();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(string imagePath, string category, string slug)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(slug))
        {
            return new JsonResult(new { success = false, message = $"Nieprawidłowe parametry - imagePath: '{imagePath}', category: '{category}', slug: '{slug}'" });
        }

        try
        {
            var data = _catalogService.LoadMachineData();

            // Debug logging
            var availableCategories = string.Join(", ", data.Keys);
            Console.WriteLine($"Looking for category: '{category}' in available categories: [{availableCategories}]");

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = $"Nie znaleziono kategorii '{category}'. Dostępne kategorie: {availableCategories}" });
            }

            var machine = items.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (machine == null)
            {
                var availableSlugs = string.Join(", ", items.Select(m => m.Slug));
                return new JsonResult(new { success = false, message = $"Nie znaleziono maszyny '{slug}'. Dostępne maszyny: {availableSlugs}" });
            }

            var imageToRemove = machine.Images.FirstOrDefault(img => img.Path == imagePath);
            if (imageToRemove == null)
            {
                var availableImages = string.Join(", ", machine.Images.Select(i => i.Path));
                return new JsonResult(new { success = false, message = $"Nie znaleziono obrazu '{imagePath}'. Dostępne obrazy: {availableImages}" });
            }

            // Remove the image regardless of whether it's main or the last one
            machine.Images.Remove(imageToRemove);

            // If we removed the main image and there are still images left, set the first one as main
            if (imageToRemove.IsMain && machine.Images.Count > 0)
            {
                machine.Images[0].IsMain = true;
            }

            // Delete physical file
            _imageService.DeleteImage(imagePath);

            // Save updated data
            _catalogService.SaveMachineData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnPostSetMainImageAsync(string imagePath, string category, string slug)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(slug))
        {
            return new JsonResult(new { success = false, message = "Nieprawidłowe parametry" });
        }

        try
        {
            var data = _catalogService.LoadMachineData();

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono kategorii" });
            }

            var machine = items.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (machine == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono maszyny" });
            }

            // Set all images to not main
            foreach (var img in machine.Images)
            {
                img.IsMain = false;
            }

            // Set selected image as main
            var mainImage = machine.Images.FirstOrDefault(img => img.Path == imagePath);
            if (mainImage != null)
            {
                mainImage.IsMain = true;
            }

            // Save updated data
            _catalogService.SaveMachineData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
    }

    private void LoadCategories()
    {
        var machineData = _catalogService.LoadMachineData();
        MachineCategoryKeys = machineData.Keys.OrderBy(key => key).ToList();
    }

    private void PopulateEditor()
    {
        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Slug))
        {
            EnsureLists();
            return;
        }

        var data = _catalogService.LoadMachineData();
        if (!data.TryGetValue(Category, out var items))
        {
            EnsureLists();
            return;
        }

        var machine = items.FirstOrDefault(item => string.Equals(item.Slug, Slug, StringComparison.OrdinalIgnoreCase));
        if (machine is null)
        {
            EnsureLists();
            return;
        }

        MachineInput = new MachineEditor
        {
            CategoryKey = Category,
            Brand = machine.Brand,
            Model = machine.Model,
            Type = machine.Type,
            ProductionYear = machine.ProductionYear,
            Weight = machine.Weight,
            Description = machine.Description,
            RentalOptions = machine.RentalOptions,
            Notes = machine.Notes
        };

        EquipmentItems = machine.Equipment?.ToList() ?? [];
        SpecificationItems = machine.Specifications
            .Select(spec => new SpecificationEntry { Key = spec.Key, Value = spec.Value })
            .ToList();

        OriginalMachineCategory = Category;
        OriginalMachineSlug = Slug;

        LoadExistingImages();
        EnsureLists();
    }

    private void LoadExistingImages()
    {
        ExistingImages = [];

        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Slug))
            return;

        var data = _catalogService.LoadMachineData();
        if (!data.TryGetValue(Category, out var items))
            return;

        var machine = items.FirstOrDefault(item => string.Equals(item.Slug, Slug, StringComparison.OrdinalIgnoreCase));
        if (machine?.Images != null)
        {
            ExistingImages = machine.Images.Select((img, index) => new ImageViewModel
            {
                Path = img.Path,
                IsMain = img.IsMain,
                Order = index
            }).ToList();
        }
    }

    private void EnsureLists()
    {
        if (EquipmentItems.Count == 0)
        {
            EquipmentItems.Add(string.Empty);
        }

        if (SpecificationItems.Count == 0)
        {
            SpecificationItems.Add(new SpecificationEntry());
        }
    }

    private async Task<MachineItem> CreateMachineItemAsync(List<string> newImagePaths)
    {
        var allImages = new List<CatalogImage>();

        // Add existing images if we're editing
        if (IsEdit && ExistingImages.Count > 0)
        {
            allImages.AddRange(ExistingImages.Select(img => new CatalogImage 
            { 
                Path = img.Path!, 
                IsMain = false // Reset all to false, will set based on user selection
            }));
        }

        // Add new images
        foreach (var imagePath in newImagePaths)
        {
            allImages.Add(new CatalogImage 
            { 
                Path = imagePath, 
                IsMain = false 
            });
        }

        // Set the main image based on user selection
        if (!string.IsNullOrWhiteSpace(MainImagePath))
        {
            if (MainImageType == "existing")
            {
                // Set existing image as main
                var mainImage = allImages.FirstOrDefault(img => img.Path == MainImagePath);
                if (mainImage != null)
                {
                    mainImage.IsMain = true;
                }
            }
            else if (MainImageType == "preview")
            {
                // For preview images, find by the generated path pattern
                var previewIndex = GetPreviewImageIndex(MainImagePath);
                if (previewIndex >= 0 && previewIndex < newImagePaths.Count)
                {
                    var previewImage = allImages.FirstOrDefault(img => img.Path == newImagePaths[previewIndex]);
                    if (previewImage != null)
                    {
                        previewImage.IsMain = true;
                    }
                }
            }
        }

        // Ensure we have at least one main image if none was selected
        if (allImages.Count > 0 && !allImages.Any(img => img.IsMain))
        {
            allImages[0].IsMain = true;
        }

        return new MachineItem
        {
            Brand = NormalizeKey(MachineInput.Brand),
            Model = NormalizeKey(MachineInput.Model),
            Type = NormalizeKey(MachineInput.Type),
            Images = allImages,
            ProductionYear = MachineInput.ProductionYear,
            Weight = NormalizeKey(MachineInput.Weight),
            Description = NormalizeKey(MachineInput.Description),
            RentalOptions = NormalizeKey(MachineInput.RentalOptions),
            Notes = NormalizeKey(MachineInput.Notes),
            Equipment = EquipmentItems
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList(),
            Specifications = SpecificationItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                .GroupBy(item => item.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value!.Trim(), StringComparer.OrdinalIgnoreCase)
        };
    }

    private int GetPreviewImageIndex(string previewImagePath)
    {
        // Extract index from preview path like "new-image-0", "new-image-1", etc.
        var parts = previewImagePath.Split('-');
        if (parts.Length >= 3 && int.TryParse(parts[2], out int index))
        {
            return index;
        }
        return -1;
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public sealed class MachineEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public string? Type { get; set; }

        public int? ProductionYear { get; set; }

        public string? Weight { get; set; }

        public string? Description { get; set; }

        public string? RentalOptions { get; set; }

        public string? Notes { get; set; }
    }

    public sealed class SpecificationEntry
    {
        public string? Key { get; set; }

        public string? Value { get; set; }
    }

    public sealed class ImageViewModel
    {
        public string? Path { get; set; }
        public bool IsMain { get; set; }
        public int Order { get; set; }
    }
}

using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GamexBusinessPage.Pages.Admin;

public class TransportEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;
    private readonly ImageService _imageService;

    public TransportEditModel(AdminCatalogService catalogService, ImageService imageService)
    {
        _catalogService = catalogService;
        _imageService = imageService;
    }

    [BindProperty]
    public TransportEditor TransportInput { get; set; } = new();

    [BindProperty]
    public List<IFormFile> ImageFiles { get; set; } = [];

    [BindProperty]
    public string? MainImagePath { get; set; }

    [BindProperty]
    public string? MainImageType { get; set; }

    [BindProperty]
    public string? OriginalTransportCategory { get; set; }

    [BindProperty]
    public string? OriginalTransportVehicle { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Vehicle { get; set; }

    public List<string> TransportCategoryKeys { get; private set; } = [];

    public List<ImageViewModel> ExistingImages { get; set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalTransportVehicle) && !string.IsNullOrWhiteSpace(OriginalTransportCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Transport - edycja";
        LoadCategories();
        PopulateEditor();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadCategories();

        if (!ModelState.IsValid)
        {
            LoadExistingImages();
            return Page();
        }

        var data = _catalogService.LoadTransportData();
        var categoryKey = NormalizeKey(TransportInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(TransportInput.CategoryKey), "Podaj kategorię.");
            LoadExistingImages();
            return Page();
        }

        var vehicleName = NormalizeKey(TransportInput.Vehicle);
        if (string.IsNullOrWhiteSpace(vehicleName))
        {
            ModelState.AddModelError(nameof(TransportInput.Vehicle), "Podaj nazwę pojazdu.");
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
                    "transport");
            }

            if (IsEdit)
            {
                if (!data.TryGetValue(OriginalTransportCategory!, out var items))
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii transportu.");
                    LoadExistingImages();
                    return Page();
                }

                var existing = items.FirstOrDefault(item => string.Equals(item.Vehicle, OriginalTransportVehicle, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono transportu do edycji.");
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

            if (targetItems.Any(item => string.Equals(item.Vehicle, vehicleName, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, "Pozycja o tej nazwie już istnieje w tej kategorii.");
                LoadExistingImages();
                return Page();
            }

            var newItem = await CreateTransportItemAsync(newImagePaths);
            targetItems.Add(newItem);
            _catalogService.SaveTransportData(data);

            return RedirectToPage("/Admin/Transport");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Błąd podczas zapisywania: {ex.Message}");
            LoadExistingImages();
            return Page();
        }
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
            Highlight = transport.Highlight
        };

        OriginalTransportCategory = Category;
        OriginalTransportVehicle = Vehicle;

        LoadExistingImages();
    }

    private void LoadExistingImages()
    {
        ExistingImages = [];

        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Vehicle))
            return;

        var data = _catalogService.LoadTransportData();
        if (!data.TryGetValue(Category, out var items))
            return;

        var transport = items.FirstOrDefault(item => string.Equals(item.Vehicle, Vehicle, StringComparison.OrdinalIgnoreCase));
        if (transport?.Images != null)
        {
            ExistingImages = transport.Images.Select((img, index) => new ImageViewModel
            {
                Path = img.Path,
                IsMain = img.IsMain,
                Order = index
            }).ToList();
        }
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(string imagePath, string category, string vehicle)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(vehicle))
        {
            return new JsonResult(new { success = false, message = "Nieprawidłowe parametry" });
        }

        try
        {
            var data = _catalogService.LoadTransportData();

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono kategorii" });
            }

            var transport = items.FirstOrDefault(item => string.Equals(item.Vehicle, vehicle, StringComparison.OrdinalIgnoreCase));
            if (transport == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono transportu" });
            }

            var imageToRemove = transport.Images.FirstOrDefault(img => img.Path == imagePath);
            if (imageToRemove == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono obrazu" });
            }

            // Remove the image regardless of whether it's main or the last one
            transport.Images.Remove(imageToRemove);

            // If we removed the main image and there are still images left, set the first one as main
            if (imageToRemove.IsMain && transport.Images.Count > 0)
            {
                transport.Images[0].IsMain = true;
            }

            _imageService.DeleteImage(imagePath);
            _catalogService.SaveTransportData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnPostSetMainImageAsync(string imagePath, string category, string vehicle)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(vehicle))
        {
            return new JsonResult(new { success = false, message = "Nieprawidłowe parametry" });
        }

        try
        {
            var data = _catalogService.LoadTransportData();

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono kategorii" });
            }

            var transport = items.FirstOrDefault(item => string.Equals(item.Vehicle, vehicle, StringComparison.OrdinalIgnoreCase));
            if (transport == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono transportu" });
            }

            // Set all images to not main
            foreach (var img in transport.Images)
            {
                img.IsMain = false;
            }

            // Set selected image as main
            var mainImage = transport.Images.FirstOrDefault(img => img.Path == imagePath);
            if (mainImage != null)
            {
                mainImage.IsMain = true;
            }

            // Save updated data
            _catalogService.SaveTransportData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
    }

    private async Task<TransportItem> CreateTransportItemAsync(List<string> newImagePaths)
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

        return new TransportItem
        {
            Vehicle = NormalizeKey(TransportInput.Vehicle),
            Description = NormalizeKey(TransportInput.Description),
            Images = allImages,
            WeightLimit = NormalizeKey(TransportInput.WeightLimit),
            Highlight = NormalizeKey(TransportInput.Highlight)
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

    public sealed class TransportEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nazwę pojazdu.")]
        public string Vehicle { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? WeightLimit { get; set; }

        public string? Highlight { get; set; }
    }

    public sealed class ImageViewModel
    {
        public string? Path { get; set; }
        public bool IsMain { get; set; }
        public int Order { get; set; }
    }
}


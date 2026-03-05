using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class ServiceEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;
    private readonly ImageService _imageService;

    public ServiceEditModel(AdminCatalogService catalogService, ImageService imageService)
    {
        _catalogService = catalogService;
        _imageService = imageService;
    }

    [BindProperty]
    public ServiceEditor ServiceInput { get; set; } = new();

    [BindProperty]
    public List<IFormFile> ImageFiles { get; set; } = [];

    [BindProperty]
    public string? MainImagePath { get; set; }

    [BindProperty]
    public string? MainImageType { get; set; }

    [BindProperty]
    public string? OriginalServiceCategory { get; set; }

    [BindProperty]
    public string? OriginalServiceName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Name { get; set; }

    public List<string> ServiceCategoryKeys { get; private set; } = [];

    public List<ImageViewModel> ExistingImages { get; set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalServiceName) && !string.IsNullOrWhiteSpace(OriginalServiceCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Usługi - edycja";
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

        var data = _catalogService.LoadServiceData();
        var categoryKey = NormalizeKey(ServiceInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(ServiceInput.CategoryKey), "Podaj kategorię.");
            LoadExistingImages();
            return Page();
        }

        var name = NormalizeKey(ServiceInput.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(ServiceInput.Name), "Podaj nazwę usługi.");
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
                    "services");
            }

            if (IsEdit)
            {
                if (!data.TryGetValue(OriginalServiceCategory!, out var items))
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii usługi.");
                    LoadExistingImages();
                    return Page();
                }

                var existing = items.FirstOrDefault(item => string.Equals(item.Name, OriginalServiceName, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    ModelState.AddModelError(string.Empty, "Nie znaleziono usługi do edycji.");
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

            if (targetItems.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(string.Empty, "Usługa o tej nazwie już istnieje w tej kategorii.");
                LoadExistingImages();
                return Page();
            }

            var newItem = await CreateServiceItemAsync(newImagePaths);
            targetItems.Add(newItem);
            _catalogService.SaveServiceData(data);

            return RedirectToPage("/Admin/Services");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Błąd podczas zapisywania: {ex.Message}");
            LoadExistingImages();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(string imagePath, string category, string name)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name))
        {
            return new JsonResult(new { success = false, message = "Nieprawidłowe parametry" });
        }

        try
        {
            var data = _catalogService.LoadServiceData();

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono kategorii" });
            }

            var service = items.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (service == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono usługi" });
            }

            var imageToRemove = service.Images.FirstOrDefault(img => img.Path == imagePath);
            if (imageToRemove == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono obrazu" });
            }

            // Remove the image regardless of whether it's main or the last one
            service.Images.Remove(imageToRemove);

            // If we removed the main image and there are still images left, set the first one as main
            if (imageToRemove.IsMain && service.Images.Count > 0)
            {
                service.Images[0].IsMain = true;
            }

            _imageService.DeleteImage(imagePath);
            _catalogService.SaveServiceData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnPostSetMainImageAsync(string imagePath, string category, string name)
    {
        if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name))
        {
            return new JsonResult(new { success = false, message = "Nieprawidłowe parametry" });
        }

        try
        {
            var data = _catalogService.LoadServiceData();

            if (!data.TryGetValue(category, out var items))
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono kategorii" });
            }

            var service = items.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (service == null)
            {
                return new JsonResult(new { success = false, message = "Nie znaleziono usługi" });
            }

            foreach (var img in service.Images)
            {
                img.IsMain = false;
            }

            var mainImage = service.Images.FirstOrDefault(img => img.Path == imagePath);
            if (mainImage != null)
            {
                mainImage.IsMain = true;
            }

            _catalogService.SaveServiceData(data);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = $"Błąd: {ex.Message}" });
        }
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
            Highlight = service.Highlight
        };

        OriginalServiceCategory = Category;
        OriginalServiceName = Name;

        LoadExistingImages();
    }

    private void LoadExistingImages()
    {
        ExistingImages = [];

        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Name))
            return;

        var data = _catalogService.LoadServiceData();
        if (!data.TryGetValue(Category, out var items))
            return;

        var service = items.FirstOrDefault(item => string.Equals(item.Name, Name, StringComparison.OrdinalIgnoreCase));
        if (service?.Images != null)
        {
            ExistingImages = service.Images.Select((img, index) => new ImageViewModel
            {
                Path = img.Path,
                IsMain = img.IsMain,
                Order = index
            }).ToList();
        }
    }

    private async Task<ServiceItem> CreateServiceItemAsync(List<string> newImagePaths)
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

        return new ServiceItem
        {
            Name = NormalizeKey(ServiceInput.Name),
            Description = NormalizeKey(ServiceInput.Description),
            Images = allImages,
            Highlight = NormalizeKey(ServiceInput.Highlight)
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

    public sealed class ServiceEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nazwę usługi.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Highlight { get; set; }
    }

    public sealed class ImageViewModel
    {
        public string? Path { get; set; }
        public bool IsMain { get; set; }
        public int Order { get; set; }
    }
}

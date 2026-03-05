using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace GamexBusinessPage.Services;

public class ImageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<string>> ProcessAndSaveImagesAsync(IEnumerable<IFormFile> files, string category, string? existingMainImagePath = null)
    {
        var savedPaths = new List<string>();
        var targetDirectory = GetTargetDirectory(category);

        // Ensure directory exists
        Directory.CreateDirectory(targetDirectory);

        foreach (var file in files)
        {
            if (file.Length == 0 || !IsValidImageFile(file))
                continue;

            try
            {
                var fileName = await ProcessImageAsync(file, targetDirectory);
                if (!string.IsNullOrEmpty(fileName))
                {
                    savedPaths.Add($"/images/{category}/{fileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {FileName}", file.FileName);
            }
        }

        return savedPaths;
    }

    public bool DeleteImage(string imagePath)
    {
        try
        {
            if (string.IsNullOrEmpty(imagePath))
                return false;

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted image: {ImagePath}", imagePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImagePath}", imagePath);
        }

        return false;
    }

    public async Task<string?> ProcessSingleImageAsync(IFormFile file, string category)
    {
        if (file.Length == 0 || !IsValidImageFile(file))
            return null;

        var targetDirectory = GetTargetDirectory(category);
        Directory.CreateDirectory(targetDirectory);

        try
        {
            var fileName = await ProcessImageAsync(file, targetDirectory);
            return !string.IsNullOrEmpty(fileName) ? $"/images/{category}/{fileName}" : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single image {FileName}", file.FileName);
            return null;
        }
    }

    private async Task<string> ProcessImageAsync(IFormFile file, string targetDirectory)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
        var sanitizedFileName = SanitizeFileName(fileNameWithoutExtension);
        var uniqueFileName = $"{sanitizedFileName}_{Guid.NewGuid():N}.webp";
        var filePath = Path.Combine(targetDirectory, uniqueFileName);

        using var image = await Image.LoadAsync(file.OpenReadStream());
        
        // Resize if too large (max 1920px width, maintaining aspect ratio)
        if (image.Width > 1920)
        {
            var newHeight = (int)((1920.0 / image.Width) * image.Height);
            image.Mutate(x => x.Resize(1920, newHeight));
        }

        // Configure WebP encoder for high quality with good compression
        var encoder = new WebpEncoder
        {
            Quality = 85, // High quality but still compressed
            Method = WebpEncodingMethod.BestQuality,
            FileFormat = WebpFileFormatType.Lossy
        };

        await image.SaveAsync(filePath, encoder);
        
        _logger.LogInformation("Processed and saved image: {FileName} -> {FilePath}", file.FileName, uniqueFileName);
        
        return uniqueFileName;
    }

    private string GetTargetDirectory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "machines" => Path.Combine(_environment.WebRootPath, "images", "machines"),
            "transport" => Path.Combine(_environment.WebRootPath, "images", "transport"),
            "services" => Path.Combine(_environment.WebRootPath, "images", "services"),
            _ => throw new ArgumentException($"Unknown category: {category}")
        };
    }

    private static bool IsValidImageFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c) && c != ' ').ToArray());
        return string.IsNullOrEmpty(sanitized) ? "image" : sanitized.ToLowerInvariant();
    }
}
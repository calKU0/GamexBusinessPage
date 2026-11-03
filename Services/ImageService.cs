using GamexBusinessPage.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GamexBusinessPage.Services;

public class ImageService
{
    private const string LogoFolder = "producenci";
    private const int LogoMaxWidth = 600;
    private const int LogoMaxHeight = 200;

    // Pixels at or below this alpha count as background when trimming a logo.
    private const byte VisibleAlpha = 10;

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

    /// <summary>
    /// Saves an uploaded manufacturer logo for the home page strip. Returns null
    /// when the file is not a readable image or has nothing visible in it.
    /// </summary>
    public async Task<SavedLogo?> ProcessLogoAsync(IFormFile file, string manufacturerName)
    {
        if (file.Length == 0 || !IsValidImageFile(file))
            return null;

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(file.OpenReadStream());
            if (!PrepareLogo(image))
                return null;

            var targetDirectory = Path.Combine(_environment.WebRootPath, "images", LogoFolder);
            Directory.CreateDirectory(targetDirectory);

            // The path ends up inside a CSS url(), so the name is reduced to a slug.
            var stem = MachineCatalog.BuildSlug(manufacturerName);
            var fileName = $"{(stem.Length > 0 ? stem : "logo")}-{Guid.NewGuid():N}.webp";

            // Lossless: logos are flat shapes with hard edges, and small either way.
            await image.SaveAsync(Path.Combine(targetDirectory, fileName), new WebpEncoder { FileFormat = WebpFileFormatType.Lossless });

            _logger.LogInformation("Processed and saved logo: {FileName} -> {SavedName}", file.FileName, fileName);
            return new SavedLogo($"/images/{LogoFolder}/{fileName}", image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logo {FileName}", file.FileName);
            return null;
        }
    }

    /// <summary>
    /// Turns a logo into what the strip needs. The strip draws only the logo's
    /// shape, in the text colour (a CSS mask), so the result is that shape alone:
    /// a logo uploaded on a white background has the white made transparent, the
    /// colour is dropped, the empty margin trimmed, and the size capped at
    /// 600 x 200 px. Returns false when nothing visible is left.
    /// </summary>
    public static bool PrepareLogo(Image<Rgba32> image)
    {
        if (IsFullyOpaque(image))
        {
            MakeWhiteTransparent(image);
        }

        KeepShapeOnly(image);

        var bounds = FindVisibleBounds(image);
        if (bounds is null)
        {
            return false;
        }

        image.Mutate(x => x.Crop(bounds.Value));

        if (image.Width > LogoMaxWidth || image.Height > LogoMaxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(LogoMaxWidth, LogoMaxHeight)
            }));
        }

        return true;
    }

    private static bool IsFullyOpaque(Image<Rgba32> image)
    {
        var opaque = true;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && opaque; y++)
            {
                foreach (var pixel in accessor.GetRowSpan(y))
                {
                    if (pixel.A < 250)
                    {
                        opaque = false;
                        break;
                    }
                }
            }
        });

        return opaque;
    }

    // Opacity follows the distance from white: near-white, JPEG noise included,
    // becomes fully transparent, anything clearly darker stays solid, and the
    // band in between keeps anti-aliased edges smooth.
    private static void MakeWhiteTransparent(Image<Rgba32> image)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                foreach (ref var pixel in accessor.GetRowSpan(y))
                {
                    var distance = 255 - Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                    pixel.A = (byte)Math.Clamp((distance - 16) * 3, 0, 255);
                }
            }
        });
    }

    // Only the alpha channel is ever drawn, so colour is dropped; it would
    // otherwise be most of the file, JPEG noise and all.
    private static void KeepShapeOnly(Image<Rgba32> image)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                foreach (ref var pixel in accessor.GetRowSpan(y))
                {
                    pixel = new Rgba32(0, 0, 0, pixel.A);
                }
            }
        });
    }

    private static Rectangle? FindVisibleBounds(Image<Rgba32> image)
    {
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A <= VisibleAlpha)
                    {
                        continue;
                    }

                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = y;
                }
            }
        });

        return right < 0 ? null : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
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

public sealed record SavedLogo(string Path, int Width, int Height);
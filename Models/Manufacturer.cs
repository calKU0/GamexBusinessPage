using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Models;

/// <summary>
/// A make shown in the "Pracujemy na sprzęcie renomowanych producentów" strip
/// on the home page. Without a logo the name is shown as text.
/// </summary>
public sealed class Manufacturer
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Site-relative path of the logo, e.g. /images/producenci/volvo.webp.</summary>
    public string? Logo { get; set; }

    /// <summary>Pixel size of the logo, so the strip reserves its space before it loads.</summary>
    public int? LogoWidth { get; set; }

    public int? LogoHeight { get; set; }

    [JsonIgnore]
    public bool HasLogo => !string.IsNullOrWhiteSpace(Logo) && LogoWidth > 0 && LogoHeight > 0;
}

public static class ManufacturerList
{
    public const string FileName = "manufacturers.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// The list in display order. Until the list is first saved in the admin
    /// panel there is no file - Data/ is not deployed - so the makes the client
    /// asked for are used, followed by the ones the strip showed before, with
    /// the logos that ship with the site.
    /// </summary>
    public static List<Manufacturer> Load(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "Data", FileName);
        if (!File.Exists(path))
        {
            return CreateDefaults();
        }

        return JsonSerializer.Deserialize<List<Manufacturer>>(File.ReadAllText(path), Options) ?? [];
    }

    /// <summary>
    /// Logos that ship with the site rather than being uploaded. They are never
    /// deleted from disk when a manufacturer drops them.
    /// </summary>
    public static bool IsBundledLogo(string? path)
    {
        return CreateDefaults().Any(manufacturer =>
            string.Equals(manufacturer.Logo, path, StringComparison.OrdinalIgnoreCase));
    }

    private static List<Manufacturer> CreateDefaults() =>
    [
        new() { Name = "Vögele", Logo = "/images/producenci/Vogele.webp", LogoWidth = 600, LogoHeight = 125 },
        new() { Name = "Wirtgen", Logo = "/images/producenci/Wirtgen.webp", LogoWidth = 600, LogoHeight = 108 },
        new() { Name = "Atlas" },
        new() { Name = "Volvo", Logo = "/images/producenci/Volvo.webp", LogoWidth = 600, LogoHeight = 83 },
        new() { Name = "Liebherr", Logo = "/images/producenci/Liebherr.webp", LogoWidth = 600, LogoHeight = 81 },
        new() { Name = "Bobcat", Logo = "/images/producenci/Bobcat.webp", LogoWidth = 238, LogoHeight = 64 },
        new() { Name = "Caterpillar", Logo = "/images/producenci/Caterpillar.webp", LogoWidth = 240, LogoHeight = 145 },
        new() { Name = "JCB", Logo = "/images/producenci/JCB.webp", LogoWidth = 240, LogoHeight = 75 },
        new() { Name = "Komatsu", Logo = "/images/producenci/Komatsu.webp", LogoWidth = 240, LogoHeight = 47 },
        new() { Name = "Manitou", Logo = "/images/producenci/Manitou.webp", LogoWidth = 240, LogoHeight = 37 }
    ];
}

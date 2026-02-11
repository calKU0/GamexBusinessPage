using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Models;

public sealed class ServiceCatalog
{
    private static readonly Dictionary<string, string> CategoryDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["earthworks"] = "Roboty ziemne",
        ["roadworks"] = "Roboty drogowe"
    };

    public ServiceCatalog(IReadOnlyList<ServiceCategory> categories)
    {
        Categories = categories;
    }

    public IReadOnlyList<ServiceCategory> Categories { get; }

    public static ServiceCatalog Load(string contentRootPath)
    {
        var jsonPath = Path.Combine(contentRootPath, "Data", "services.json");
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<Dictionary<string, List<ServiceItem>>>(json, options)
            ?? new Dictionary<string, List<ServiceItem>>();

        var categories = new List<ServiceCategory>();

        foreach (var (categoryKey, items) in data)
        {
            var displayName = CategoryDisplayNames.TryGetValue(categoryKey, out var name)
                ? name
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryKey.Replace('_', ' '));
            var services = items ?? new List<ServiceItem>();

            foreach (var service in services)
            {
                service.CategoryKey = categoryKey;
                service.CategoryDisplayName = displayName;
            }

            categories.Add(new ServiceCategory(categoryKey, displayName, services));
        }

        return new ServiceCatalog(categories);
    }
}

public sealed class ServiceCategory
{
    public ServiceCategory(string key, string displayName, IReadOnlyList<ServiceItem> services)
    {
        Key = key;
        DisplayName = displayName;
        Services = services;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public IReadOnlyList<ServiceItem> Services { get; }
}

public sealed class ServiceItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("highlight")]
    public string? Highlight { get; set; }

    [JsonIgnore]
    public string? CategoryKey { get; set; }

    [JsonIgnore]
    public string? CategoryDisplayName { get; set; }
}

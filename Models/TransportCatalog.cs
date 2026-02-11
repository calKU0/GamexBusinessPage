using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Models;

public sealed class TransportCatalog
{
    private static readonly Dictionary<string, string> CategoryDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bulk_materials"] = "Materiały sypkie",
        ["lowbed_transport_and_trailer"] = "Transport niskopodwoziowy i laweta",
        ["truck_crane_transport"] = "Transport HDS"
    };

    public TransportCatalog(IReadOnlyList<TransportCategory> categories)
    {
        Categories = categories;
    }

    public IReadOnlyList<TransportCategory> Categories { get; }

    public static TransportCatalog Load(string contentRootPath)
    {
        var jsonPath = Path.Combine(contentRootPath, "Data", "transport.json");
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<Dictionary<string, List<TransportItem>>>(json, options)
            ?? new Dictionary<string, List<TransportItem>>();

        var categories = new List<TransportCategory>();

        foreach (var (categoryKey, items) in data)
        {
            var displayName = CategoryDisplayNames.TryGetValue(categoryKey, out var name)
                ? name
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryKey.Replace('_', ' '));
            var transports = items ?? new List<TransportItem>();

            foreach (var transport in transports)
            {
                transport.CategoryKey = categoryKey;
                transport.CategoryDisplayName = displayName;
            }

            categories.Add(new TransportCategory(categoryKey, displayName, transports));
        }

        return new TransportCatalog(categories);
    }
}

public sealed class TransportCategory
{
    public TransportCategory(string key, string displayName, IReadOnlyList<TransportItem> transports)
    {
        Key = key;
        DisplayName = displayName;
        Transports = transports;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public IReadOnlyList<TransportItem> Transports { get; }
}

public sealed class TransportItem
{
    [JsonPropertyName("vehicle")]
    public string? Vehicle { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("weight_limit")]
    public string? WeightLimit { get; set; }

    [JsonPropertyName("highlight")]
    public string? Highlight { get; set; }

    [JsonIgnore]
    public string? CategoryKey { get; set; }

    [JsonIgnore]
    public string? CategoryDisplayName { get; set; }
}

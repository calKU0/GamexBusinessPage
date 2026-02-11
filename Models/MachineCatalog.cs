using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Models;

public sealed class MachineCatalog
{
    private static readonly Dictionary<string, string> CategoryDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["backhoe_loaders"] = "Koparko ładowarki",
        ["mini_excavators"] = "Minikoparki",
        ["midi_excavators"] = "Midikoparki",
        ["excavators"] = "Koparki",
        ["mini_loaders"] = "Mini ładowarki",
        ["telescopic_loaders"] = "Ładowarki teleskopowe",
        ["screeners"] = "Przesiewacze",
        ["rollers"] = "Walce",
        ["asphalt_pavers"] = "Rozkładarki asfaltu",
        ["milling_machines"] = "Frezarki",
        ["dumpers"] = "Wozidła",
        ["pneumatic_compressors"] = "Zasilacze pneumatyczne",
        ["compactors"] = "Zagęszczarki",
        ["tractors"] = "Ciągniki"
    };

    private static readonly Dictionary<string, string> SpecificationDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["max_lift_capacity"] = "Maks. udźwig",
        ["max_lifting_height"] = "Maks. wysokość podnoszenia",
        ["max_reach"] = "Maks. zasięg",
        ["hopper_capacity"] = "Pojemność zasobnika",
        ["throughput"] = "Wydajność",
        ["screen_size"] = "Wymiary sita",
        ["paving_width"] = "Szerokość robocza",
        ["leveling"] = "System poziomowania",
        ["screed_heating"] = "Podgrzewanie stołu",
        ["working_width"] = "Szerokość robocza",
        ["max_working_depth"] = "Maks. głębokość pracy",
        ["payload"] = "Ładowność",
        ["working_pressure"] = "Ciśnienie robocze",
        ["drive"] = "Napęd",
        ["speed"] = "Prędkość",
        ["gearbox"] = "Skrzynia biegów",
        ["trailer_payload"] = "Ładowność przyczepy",
        ["type"] = "Typ"
    };

    public MachineCatalog(IReadOnlyList<MachineCategory> categories, IReadOnlyList<MachineItem> machines)
    {
        Categories = categories;
        Machines = machines;
    }

    public IReadOnlyList<MachineCategory> Categories { get; }

    public IReadOnlyList<MachineItem> Machines { get; }

    public MachineItem? FindBySlug(string slug)
    {
        return Machines.FirstOrDefault(machine => string.Equals(machine.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public static MachineCatalog Load(string contentRootPath)
    {
        var jsonPath = Path.Combine(contentRootPath, "Data", "machines.json");
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<Dictionary<string, List<MachineItem>>>(json, options)
            ?? new Dictionary<string, List<MachineItem>>();
        var categories = new List<MachineCategory>();
        var machines = new List<MachineItem>();

        foreach (var (categoryKey, categoryMachines) in data)
        {
            var displayName = CategoryDisplayNames.TryGetValue(categoryKey, out var name)
                ? name
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryKey.Replace('_', ' '));
            var items = categoryMachines ?? new List<MachineItem>();

            foreach (var machine in items)
            {
                machine.CategoryKey = categoryKey;
                machine.CategoryDisplayName = displayName;
            }

            categories.Add(new MachineCategory(categoryKey, displayName, items));
            machines.AddRange(items);
        }

        return new MachineCatalog(categories, machines);
    }

    public static string BuildSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    public static string GetSpecificationDisplayName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return SpecificationDisplayNames.TryGetValue(key, out var name)
            ? name
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key.Replace('_', ' '));
    }
}

public sealed class MachineCategory
{
    public MachineCategory(string key, string displayName, IReadOnlyList<MachineItem> machines)
    {
        Key = key;
        DisplayName = displayName;
        Machines = machines;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public IReadOnlyList<MachineItem> Machines { get; }
}

public sealed class MachineItem
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }


    [JsonPropertyName("production_year")]
    public int? ProductionYear { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("equipment")]
    public List<string> Equipment { get; set; } = [];

    [JsonPropertyName("rental_options")]
    public string? RentalOptions { get; set; }

    [JsonPropertyName("specifications")]
    public Dictionary<string, string> Specifications { get; set; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonIgnore]
    public string? CategoryKey { get; set; }

    [JsonIgnore]
    public string? CategoryDisplayName { get; set; }

    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            var nameParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Brand))
            {
                nameParts.Add(Brand);
            }

            if (!string.IsNullOrWhiteSpace(Model))
            {
                nameParts.Add(Model);
            }

            if (string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(Type))
            {
                nameParts.Add(Type);
            }

            var displayName = string.Join(" ", nameParts);

            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(Type))
            {
                return Type;
            }

            return displayName;
        }
    }

    [JsonIgnore]
    public string Slug
    {
        get
        {
            var slugParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Brand))
            {
                slugParts.Add(Brand);
            }

            if (!string.IsNullOrWhiteSpace(Model))
            {
                slugParts.Add(Model);
            }

            if (string.IsNullOrWhiteSpace(Model) && !string.IsNullOrWhiteSpace(Type))
            {
                slugParts.Add(Type);
            }

            var slugSource = string.Join(" ", slugParts);

            if (string.IsNullOrWhiteSpace(slugSource) && !string.IsNullOrWhiteSpace(Type))
            {
                slugSource = Type;
            }

            return MachineCatalog.BuildSlug(slugSource);
        }
    }
}

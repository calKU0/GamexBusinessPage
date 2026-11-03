using GamexBusinessPage.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Services;

public sealed class AdminCatalogService
{
    private readonly IWebHostEnvironment _environment;
    private readonly CatalogCache _catalogCache;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AdminCatalogService(IWebHostEnvironment environment, CatalogCache catalogCache)
    {
        _environment = environment;
        _catalogCache = catalogCache;
    }

    public Dictionary<string, List<MachineItem>> LoadMachineData()
    {
        return LoadData<MachineItem>(GetDataPath("machines.json"));
    }

    public Dictionary<string, List<ServiceItem>> LoadServiceData()
    {
        return LoadData<ServiceItem>(GetDataPath("services.json"));
    }

    public Dictionary<string, List<TransportItem>> LoadTransportData()
    {
        return LoadData<TransportItem>(GetDataPath("transport.json"));
    }

    /// <summary>The home page manufacturer strip, in display order.</summary>
    public List<Manufacturer> LoadManufacturers()
    {
        return ManufacturerList.Load(_environment.ContentRootPath);
    }

    public void SaveMachineData(Dictionary<string, List<MachineItem>> data)
    {
        SaveData(GetDataPath("machines.json"), data);
        _catalogCache.ClearMachineCatalog();
    }

    public void SaveServiceData(Dictionary<string, List<ServiceItem>> data)
    {
        SaveData(GetDataPath("services.json"), data);
        _catalogCache.ClearServiceCatalog();
    }

    public void SaveTransportData(Dictionary<string, List<TransportItem>> data)
    {
        SaveData(GetDataPath("transport.json"), data);
        _catalogCache.ClearTransportCatalog();
    }

    public void SaveManufacturers(List<Manufacturer> manufacturers)
    {
        SaveData(GetDataPath(ManufacturerList.FileName), manufacturers);
        _catalogCache.ClearManufacturers();
    }

    public bool ReorderMachines(IReadOnlyList<CategoryOrder> order, out string? error)
    {
        var reordered = ApplyOrder(LoadMachineData(), order, out error);
        if (reordered is null)
        {
            return false;
        }

        SaveMachineData(reordered);
        return true;
    }

    public bool ReorderServices(IReadOnlyList<CategoryOrder> order, out string? error)
    {
        var reordered = ApplyOrder(LoadServiceData(), order, out error);
        if (reordered is null)
        {
            return false;
        }

        SaveServiceData(reordered);
        return true;
    }

    public bool ReorderTransport(IReadOnlyList<CategoryOrder> order, out string? error)
    {
        var reordered = ApplyOrder(LoadTransportData(), order, out error);
        if (reordered is null)
        {
            return false;
        }

        SaveTransportData(reordered);
        return true;
    }

    /// <summary>
    /// Rebuilds a catalog in the requested order. Returns null, with a reason,
    /// when the request does not describe exactly the data on disk.
    /// </summary>
    /// <remarks>
    /// The public pages list categories and items in file order, so this is the
    /// whole of "ordering": the dictionary is rebuilt with its keys inserted in
    /// the new sequence and each list rearranged. Items are addressed by their
    /// position when the page was loaded rather than by name, since names are not
    /// guaranteed unique within a category.
    ///
    /// The request must be a complete permutation of what is stored. If anything
    /// was added, removed or renamed since the page was opened, saving the order
    /// would silently drop or duplicate entries - so it is refused instead.
    /// </remarks>
    public static Dictionary<string, List<TItem>>? ApplyOrder<TItem>(
        Dictionary<string, List<TItem>> current,
        IReadOnlyList<CategoryOrder> order,
        out string? error)
    {
        const string stale = "Dane zmieniły się od otwarcia tej strony. Odśwież ją i ustaw kolejność ponownie.";

        if (order.Count != current.Count)
        {
            error = stale;
            return null;
        }

        var result = new Dictionary<string, List<TItem>>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in order)
        {
            if (string.IsNullOrWhiteSpace(category.Key)
                || result.ContainsKey(category.Key)
                || !current.TryGetValue(category.Key, out var items))
            {
                error = stale;
                return null;
            }

            var positions = category.Items ?? Array.Empty<int>();
            if (positions.Length != items.Count
                || positions.Any(position => position < 0 || position >= items.Count)
                || positions.Distinct().Count() != positions.Length)
            {
                error = stale;
                return null;
            }

            // Keep the key exactly as stored, whatever casing the request used.
            var storedKey = current.Keys.First(key => string.Equals(key, category.Key, StringComparison.OrdinalIgnoreCase));
            result.Add(storedKey, positions.Select(position => items[position]).ToList());
        }

        error = null;
        return result;
    }

    private string GetDataPath(string fileName)
    {
        return Path.Combine(_environment.ContentRootPath, "Data", fileName);
    }

    private Dictionary<string, List<TItem>> LoadData<TItem>(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, List<TItem>>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, List<TItem>>>(json, _serializerOptions)
            ?? new Dictionary<string, List<TItem>>();

        return new Dictionary<string, List<TItem>>(data, StringComparer.OrdinalIgnoreCase);
    }

    private void SaveData<TData>(string path, TData data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, _serializerOptions);
        File.WriteAllText(path, json);
    }
}

/// <summary>
/// One category in a requested ordering: its key, and the positions its items
/// held when the page was loaded, listed in their new sequence.
/// </summary>
public sealed record CategoryOrder(string Key, int[] Items);

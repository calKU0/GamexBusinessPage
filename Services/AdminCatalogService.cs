using System.Text.Json;
using System.Text.Json.Serialization;
using GamexBusinessPage.Models;

namespace GamexBusinessPage.Services;

public sealed class AdminCatalogService
{
    private readonly IWebHostEnvironment _environment;
    private readonly CatalogCache _catalogCache;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

    private void SaveData<TItem>(string path, Dictionary<string, List<TItem>> data)
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

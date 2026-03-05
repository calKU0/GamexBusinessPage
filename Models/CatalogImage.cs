using System.Text.Json.Serialization;

namespace GamexBusinessPage.Models;

public sealed class CatalogImage
{
    [JsonPropertyName("main")]
    public bool IsMain { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

using System.Text.Json.Serialization;

namespace DisplayConfigManager.Models.Dtos;

public sealed class RationalDto
{
    [JsonPropertyName("numerator")]
    public uint Numerator { get; set; }

    [JsonPropertyName("denominator")]
    public uint Denominator { get; set; }
}

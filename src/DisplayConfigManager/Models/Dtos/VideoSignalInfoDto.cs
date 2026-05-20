using System.Text.Json.Serialization;

namespace DisplayConfigManager.Models.Dtos;

public sealed class VideoSignalInfoDto
{
    // Stored as integer (UINT64). Never cast to double — precision loss corrupts the timing.
    [JsonPropertyName("pixelRate")]
    public ulong PixelRate { get; set; }

    [JsonPropertyName("hSyncFreq")]
    public RationalDto HSyncFreq { get; set; } = new();

    // Critical: vSyncFreq is the fractional refresh rate (e.g., {60000,1001} = 59.94 Hz).
    // Numerator and Denominator must be preserved exactly.
    [JsonPropertyName("vSyncFreq")]
    public RationalDto VSyncFreq { get; set; } = new();

    [JsonPropertyName("activeWidth")]
    public uint ActiveWidth { get; set; }

    [JsonPropertyName("activeHeight")]
    public uint ActiveHeight { get; set; }

    [JsonPropertyName("totalWidth")]
    public uint TotalWidth { get; set; }

    [JsonPropertyName("totalHeight")]
    public uint TotalHeight { get; set; }

    // Packed bits: [0-15] = videoStandard (PC/DMT vs TV/CEA), [16-21] = vSyncFreqDivider.
    // Stored verbatim — splitting/recombining the sub-fields is not allowed.
    [JsonPropertyName("additionalSignalInfo")]
    public uint AdditionalSignalInfo { get; set; }

    [JsonPropertyName("scanLineOrdering")]
    public uint ScanLineOrdering { get; set; }
}

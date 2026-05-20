using System.Text.Json.Serialization;

namespace DisplayConfigManager.Models.Dtos;

public sealed class PathInfoDto
{
    // Source info
    [JsonPropertyName("srcAdapterLow")]
    public uint SrcAdapterIdLowPart { get; set; }

    [JsonPropertyName("srcAdapterHigh")]
    public int SrcAdapterIdHighPart { get; set; }

    [JsonPropertyName("srcId")]
    public uint SrcId { get; set; }

    [JsonPropertyName("srcModeIdx")]
    public uint SrcModeInfoIdx { get; set; }

    [JsonPropertyName("srcStatus")]
    public uint SrcStatusFlags { get; set; }

    // Target info
    [JsonPropertyName("tgtAdapterLow")]
    public uint TgtAdapterIdLowPart { get; set; }

    [JsonPropertyName("tgtAdapterHigh")]
    public int TgtAdapterIdHighPart { get; set; }

    [JsonPropertyName("tgtId")]
    public uint TgtId { get; set; }

    [JsonPropertyName("tgtModeIdx")]
    public uint TgtModeInfoIdx { get; set; }

    [JsonPropertyName("outputTech")]
    public int OutputTechnology { get; set; }

    [JsonPropertyName("rotation")]
    public uint Rotation { get; set; }

    [JsonPropertyName("scaling")]
    public uint Scaling { get; set; }

    [JsonPropertyName("refreshRateNum")]
    public uint RefreshRateNumerator { get; set; }

    [JsonPropertyName("refreshRateDen")]
    public uint RefreshRateDenominator { get; set; }

    [JsonPropertyName("scanLineOrder")]
    public uint ScanLineOrdering { get; set; }

    [JsonPropertyName("tgtAvailable")]
    public int TargetAvailable { get; set; }

    [JsonPropertyName("tgtStatus")]
    public uint TgtStatusFlags { get; set; }

    // Path-level flags
    [JsonPropertyName("flags")]
    public uint Flags { get; set; }
}

using System.Text.Json.Serialization;

namespace DisplayConfigManager.Models.Dtos;

public sealed class ModeInfoDto
{
    [JsonPropertyName("infoType")]
    public uint InfoType { get; set; }

    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("adapterIdLow")]
    public uint AdapterIdLowPart { get; set; }

    [JsonPropertyName("adapterIdHigh")]
    public int AdapterIdHighPart { get; set; }

    // Present when InfoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET (2)
    [JsonPropertyName("targetSignal")]
    public VideoSignalInfoDto? TargetVideoSignalInfo { get; set; }

    // Present when InfoType == DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE (1)
    [JsonPropertyName("srcWidth")]
    public uint? SourceWidth { get; set; }

    [JsonPropertyName("srcHeight")]
    public uint? SourceHeight { get; set; }

    [JsonPropertyName("pixelFormat")]
    public uint? PixelFormat { get; set; }

    [JsonPropertyName("posX")]
    public int? PositionX { get; set; }

    [JsonPropertyName("posY")]
    public int? PositionY { get; set; }
}

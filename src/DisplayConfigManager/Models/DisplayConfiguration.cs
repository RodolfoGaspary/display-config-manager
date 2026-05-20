using DisplayConfigManager.Native;

namespace DisplayConfigManager.Models;

internal sealed class DisplayConfiguration
{
    public required DISPLAYCONFIG_PATH_INFO[] Paths { get; init; }
    public required DISPLAYCONFIG_MODE_INFO[] Modes { get; init; }
}

using DisplayConfigManager.Models.Dtos;

namespace DisplayConfigManager.Models;

public sealed class Preset
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required PathInfoDto[] Paths { get; init; }
    public required ModeInfoDto[] Modes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

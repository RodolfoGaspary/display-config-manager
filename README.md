# Display Config Manager

A lightweight Windows system-tray application that captures, saves, and applies multi-monitor display configurations as named presets — preserving **exact fractional refresh rates** (e.g. 59.94 Hz stored as 60000/1001, never rounded to 60 Hz).

Windows' built-in "Best Mode Logic" silently downgrades PC timings to TV timings whenever monitors reconnect, wake from sleep, or reboot. This tool bypasses that behavior entirely by calling [`SetDisplayConfig`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdisplayconfig) with the strict `SDC_USE_SUPPLIED_DISPLAY_CONFIG` flag — no `SDC_ALLOW_CHANGES`, ever.

![Windows 10/11](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- **Save & restore** multi-monitor layouts as named presets
- **Bit-perfect refresh rates** — `vSyncFreq` numerator/denominator stored as integers, pixel rate as `ulong`
- **Auto-restore on wake/unlock** — hooks `PowerModeChanged` and `SessionSwitch` to reapply your default preset after sleep, screen-off, or session unlock
- **Start with Windows** — optional registry entry to restore your display config on every boot
- **Set a default preset** — right-click any preset → "Set as default" so it's always reapply target
- **Rename & delete** presets from the Manage Presets window
- **Zero dependencies** — no NuGet packages, just .NET 8 + Windows CCD API via P/Invoke
- **Single-file executable** — publish as a self-contained `.exe`, no runtime install required

## Quick Start

### Download

Grab the latest `DisplayConfigManager.exe` from the [Releases](https://github.com/RodolfoGaspary/display-config-manager/releases) page — it's a self-contained single file, no installer needed.

### Build from Source

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows 10/11.

```bash
# Clone
git clone https://github.com/RodolfoGaspary/display-config-manager.git
cd display-config-manager

# Build
dotnet build

# Run
dotnet run --project src/DisplayConfigManager

# Publish self-contained single-file exe
dotnet publish src/DisplayConfigManager -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The published exe will be in `src/DisplayConfigManager/bin/Release/net8.0-windows10.0.17763.0/win-x64/publish/`.

## Usage

1. **Launch** — the app starts minimized to the system tray (blue "D" icon)
2. **Right-click** the tray icon to open the menu
3. **Save Current as Preset…** — captures your current multi-monitor layout
4. **Click a preset name** — applies that display configuration immediately
5. **Set as default** — right-click a preset → "Set as default" to auto-restore it on wake/reboot
6. **Start with Windows** — toggle from the tray menu to launch on boot
7. **Manage Presets…** — rename or delete saved presets

## How It Works

The app uses the Windows [CCD (Connecting and Configuring Displays) API](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/ccd-apis):

1. **`GetDisplayConfigBufferSizes`** + **`QueryDisplayConfig`** — reads the current display topology including all path and mode info
2. Every field is serialized to JSON as-is: refresh rates stay as integer ratios, pixel rates as 64-bit integers, signal info as packed bitfields
3. **`SetDisplayConfig`** with flags `SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG` (0x00A0) — applies the saved configuration without Windows "helping" by substituting its preferred modes

### Why Not `SDC_ALLOW_CHANGES`?

The `SDC_ALLOW_CHANGES` flag (0x04) tells Windows it's OK to substitute the closest available mode if the exact one isn't available. In practice, this means Windows will:
- Replace 59.94 Hz (60000/1001) with 60.00 Hz (60/1)
- Switch from PC to TV timing standards
- Change color formats silently

This app **never** uses that flag. If a monitor isn't connected or a mode isn't available, you get an explicit error instead of a silent downgrade.

## Data Storage

Presets and settings are stored as JSON in:

```
%LOCALAPPDATA%\DisplayConfigManager\
├── presets.json    # Saved display configurations
└── settings.json   # Default preset ID
```

## Project Structure

```
display/
├── DisplayConfigManager.sln
└── src/DisplayConfigManager/
    ├── App.xaml / App.xaml.cs          # WPF entry point (no main window)
    ├── Native/
    │   ├── NativeConstants.cs          # Win32 CCD constants
    │   ├── NativeMethods.cs            # P/Invoke declarations
    │   └── Structs.cs                  # CCD struct definitions
    ├── Models/
    │   ├── DisplayConfiguration.cs     # Runtime config wrapper
    │   ├── Preset.cs                   # Named preset model
    │   ├── Settings.cs                 # App settings model
    │   └── Dtos/                       # JSON-serializable DTOs
    │       ├── DtoMapper.cs
    │       ├── ModeInfoDto.cs
    │       ├── PathInfoDto.cs
    │       ├── RationalDto.cs
    │       └── VideoSignalInfoDto.cs
    ├── Services/
    │   ├── DisplayManagerService.cs    # CCD API wrapper
    │   ├── PresetStorageService.cs     # Preset JSON persistence
    │   ├── SettingsStorageService.cs   # Settings persistence
    │   └── StartupRegistrationService.cs # Windows startup registry
    ├── Exceptions/
    │   └── DisplayConfigException.cs
    └── UI/
        ├── TrayApplicationContext.cs   # Tray icon & context menu
        ├── SavePresetDialog.xaml/.cs   # "Save preset" dialog
        └── ManagePresetsWindow.xaml/.cs # Preset management window
```

## License

[MIT](LICENSE)

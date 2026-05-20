using System.Windows;
using DisplayConfigManager.Exceptions;
using DisplayConfigManager.Models;
using DisplayConfigManager.Models.Dtos;
using DisplayConfigManager.Services;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace DisplayConfigManager.UI;

internal sealed class TrayApplicationContext : IDisposable
{
    private readonly WinForms.NotifyIcon _trayIcon;
    private readonly DisplayManagerService _displayService = new();
    private readonly PresetStorageService _presetStorage = new();
    private readonly SettingsStorageService _settingsStorage = new();

    public TrayApplicationContext()
    {
        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "Display Config Manager",
            Icon = CreateIcon(),
            Visible = true,
        };
        _trayIcon.ContextMenuStrip = new WinForms.ContextMenuStrip();
        _trayIcon.ContextMenuStrip.Opening += (_, _) => RebuildMenu();
        RebuildMenu();

        // ── Auto-restore wiring ──────────────────────────────────────────────
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch    += OnSessionSwitch;

        // Apply the default preset on startup so that after a reboot the saved
        // configuration is restored even if Windows defaulted to another EDID.
        ApplyDefaultPreset();
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private void RebuildMenu()
    {
        var menu = _trayIcon.ContextMenuStrip!;
        menu.Items.Clear();

        var settings = _settingsStorage.Load();
        var presets  = _presetStorage.LoadPresets();

        foreach (var preset in presets)
        {
            var captured = preset;
            var item = new WinForms.ToolStripMenuItem(preset.Name)
            {
                Checked     = preset.Id == settings.DefaultPresetId,
                CheckOnClick = false,
            };
            item.Click += (_, _) => ApplyPreset(captured);

            var setDefault = new WinForms.ToolStripMenuItem("Set as default")
            {
                Checked = preset.Id == settings.DefaultPresetId,
            };
            setDefault.Click += (_, _) => SetAsDefault(captured.Id);
            item.DropDownItems.Add(setDefault);

            menu.Items.Add(item);
        }

        if (presets.Count > 0)
            menu.Items.Add(new WinForms.ToolStripSeparator());

        menu.Items.Add("Save Current as Preset…", null, (_, _) => SaveCurrentPreset());
        menu.Items.Add("Manage Presets…",         null, (_, _) => ManagePresets());

        var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
        {
            Checked = StartupRegistrationService.IsRegistered(),
        };
        startupItem.Click += (_, _) =>
        {
            StartupRegistrationService.SetRegistered(!startupItem.Checked);
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void SetAsDefault(Guid presetId)
    {
        var settings = _settingsStorage.Load();
        settings.DefaultPresetId = presetId;
        _settingsStorage.Save(settings);
    }

    private void ApplyPreset(Preset preset, bool silent = false)
    {
        try
        {
            _displayService.ApplyConfiguration(preset);
        }
        catch (DisplayConfigException ex)
        {
            if (!silent)
                MessageBox.Show(ex.Message, "Display Config Manager",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyDefaultPreset()
    {
        var settings = _settingsStorage.Load();
        if (settings.DefaultPresetId is not { } id) return;

        var preset = _presetStorage.LoadPresets().FirstOrDefault(p => p.Id == id);
        if (preset is null) return;

        // Silent: don't pop modal errors after wake — only show a tray tooltip.
        ApplyPreset(preset, silent: true);
    }

    private void SaveCurrentPreset()
    {
        try
        {
            var config = _displayService.GetCurrentConfiguration();
            var dialog = new SavePresetDialog();

            if (dialog.ShowDialog() == true
                && !string.IsNullOrWhiteSpace(dialog.PresetName))
            {
                var preset = new Preset
                {
                    Name  = dialog.PresetName.Trim(),
                    Paths = config.Paths.Select(DtoMapper.PathInfoToDto).ToArray(),
                    Modes = config.Modes.Select(DtoMapper.ModeInfoToDto).ToArray(),
                };
                _presetStorage.AddPreset(preset);
            }
        }
        catch (DisplayConfigException ex)
        {
            MessageBox.Show(ex.Message, "Display Config Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManagePresets()
    {
        var window = new ManagePresetsWindow(_presetStorage);
        window.Show();
        window.Activate();
    }

    // ── System event handlers ─────────────────────────────────────────────────

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            ScheduleReapply();
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock
         || e.Reason == SessionSwitchReason.ConsoleConnect)
            ScheduleReapply();
    }

    /// <summary>
    /// Re-apply the default preset on the WPF dispatcher after a small delay so
    /// Windows finishes its own display reconfiguration before we override it.
    /// </summary>
    private void ScheduleReapply()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            dispatcher.Invoke(ApplyDefaultPreset);
        });
    }

    // ── Icon ──────────────────────────────────────────────────────────────────

    private static System.Drawing.Icon CreateIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215));
        g.FillRectangle(bgBrush, 2, 2, 28, 28);
        using var font = new System.Drawing.Font("Arial", 13, System.Drawing.FontStyle.Bold);
        g.DrawString("D", font, System.Drawing.Brushes.White, 7f, 7f);
        var hIcon = bitmap.GetHicon();
        return System.Drawing.Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch    -= OnSessionSwitch;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}

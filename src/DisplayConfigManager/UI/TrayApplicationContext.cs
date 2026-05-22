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
    private readonly DisplayChangeListener _displayListener = new();

    // Debounce: when WE apply a preset, Windows fires WM_DISPLAYCHANGE which
    // would re-trigger ApplyDefaultPreset. We ignore display-change events for
    // a short window after every apply.
    private DateTime _suppressUntil = DateTime.MinValue;

    // Lightweight monotonic counter so a brand-new wake event invalidates
    // pending retries from a previous event.
    private int _reapplyGeneration;

    public TrayApplicationContext()
    {
        DiagnosticLog.Write("──────── App starting ────────");

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
        _displayListener.DisplayChanged += OnDisplayChanged;

        // Apply the default preset on startup so that after a reboot the saved
        // configuration is restored even if Windows defaulted to another EDID.
        DiagnosticLog.Write("Startup: applying default preset (if any).");
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
            DiagnosticLog.Write($"Start-with-Windows toggled to {!startupItem.Checked}.");
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Reapply default now",  null, (_, _) =>
        {
            DiagnosticLog.Write("Manual reapply requested from tray menu.");
            ApplyDefaultPreset(silent: false);
        });
        menu.Items.Add("Open log folder",      null, (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    $"/select,\"{DiagnosticLog.LogPath}\"");
            }
            catch { /* ignore */ }
        });

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void SetAsDefault(Guid presetId)
    {
        var settings = _settingsStorage.Load();
        settings.DefaultPresetId = presetId;
        _settingsStorage.Save(settings);
        DiagnosticLog.Write($"Default preset set to {presetId}.");
    }

    private void ApplyPreset(Preset preset, bool silent = false)
    {
        try
        {
            // Suppress the WM_DISPLAYCHANGE feedback loop caused by our own apply.
            _suppressUntil = DateTime.UtcNow.AddSeconds(5);
            DiagnosticLog.Write($"Applying preset '{preset.Name}' ({preset.Id}).");
            _displayService.ApplyConfiguration(preset);
            DiagnosticLog.Write($"Applied preset '{preset.Name}' successfully.");
        }
        catch (DisplayConfigException ex)
        {
            DiagnosticLog.Write($"Apply FAILED for '{preset.Name}': {ex.Message}");
            if (!silent)
                MessageBox.Show(ex.Message, "Display Config Manager",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyDefaultPreset(bool silent = true)
    {
        var settings = _settingsStorage.Load();
        if (settings.DefaultPresetId is not { } id)
        {
            DiagnosticLog.Write("ApplyDefaultPreset: no default set, skipping.");
            return;
        }

        var preset = _presetStorage.LoadPresets().FirstOrDefault(p => p.Id == id);
        if (preset is null)
        {
            DiagnosticLog.Write(
                $"ApplyDefaultPreset: default id {id} not found in presets, skipping.");
            return;
        }

        ApplyPreset(preset, silent: silent);
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
                DiagnosticLog.Write($"Saved new preset '{preset.Name}' ({preset.Id}).");
            }
        }
        catch (DisplayConfigException ex)
        {
            DiagnosticLog.Write($"SaveCurrentPreset FAILED: {ex.Message}");
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
        DiagnosticLog.Write($"PowerModeChanged: {e.Mode}");
        if (e.Mode == PowerModes.Resume)
            ScheduleReapply("PowerResume");
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        DiagnosticLog.Write($"SessionSwitch: {e.Reason}");
        if (e.Reason == SessionSwitchReason.SessionUnlock
         || e.Reason == SessionSwitchReason.ConsoleConnect
         || e.Reason == SessionSwitchReason.SessionLogon)
            ScheduleReapply($"SessionSwitch:{e.Reason}");
    }

    private void OnDisplayChanged()
    {
        if (DateTime.UtcNow < _suppressUntil)
        {
            // This change was caused by us applying the preset — ignore.
            return;
        }

        DiagnosticLog.Write("WM_DISPLAYCHANGE received (external).");
        ScheduleReapply("WM_DISPLAYCHANGE");
    }

    /// <summary>
    /// Re-apply the default preset multiple times after wake/unlock/display
    /// change. Windows can take several seconds to finish its own
    /// reconfiguration after these events, and may even override us once we
    /// apply — so we retry at 2s, 5s, and 10s.
    /// </summary>
    private void ScheduleReapply(string trigger)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        var generation = Interlocked.Increment(ref _reapplyGeneration);
        DiagnosticLog.Write($"ScheduleReapply triggered by {trigger} (gen {generation}).");

        _ = Task.Run(async () =>
        {
            int[] delaysMs = [2000, 3000, 5000]; // cumulative: 2s, 5s, 10s

            foreach (var delay in delaysMs)
            {
                await Task.Delay(delay);

                // If another trigger fired meanwhile, abandon this chain so the
                // newer one's retries take over.
                if (Volatile.Read(ref _reapplyGeneration) != generation)
                {
                    DiagnosticLog.Write(
                        $"Reapply chain gen {generation} superseded — exiting.");
                    return;
                }

                dispatcher.Invoke(() =>
                {
                    DiagnosticLog.Write(
                        $"Reapply attempt (gen {generation}, after +{delay}ms).");
                    ApplyDefaultPreset();
                });
            }
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
        _displayListener.DisplayChanged -= OnDisplayChanged;
        _displayListener.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        DiagnosticLog.Write("App shutting down.");
    }
}

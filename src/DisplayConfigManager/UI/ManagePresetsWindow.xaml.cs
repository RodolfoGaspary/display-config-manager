using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DisplayConfigManager.Models;
using DisplayConfigManager.Services;

namespace DisplayConfigManager.UI;

public partial class ManagePresetsWindow : Window, INotifyPropertyChanged
{
    private readonly PresetStorageService _storageService;

    public ObservableCollection<Preset> Presets { get; } = [];

    public ManagePresetsWindow(PresetStorageService storageService)
    {
        _storageService = storageService;
        InitializeComponent();
        DataContext = this;
        RefreshList();
    }

    private void RefreshList()
    {
        Presets.Clear();
        foreach (var p in _storageService.LoadPresets())
            Presets.Add(p);
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Preset preset }) return;

        var dialog = new SavePresetDialog { Title = "Rename Preset" };
        dialog.PresetName = preset.Name;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PresetName))
        {
            _storageService.RenamePreset(preset.Id, dialog.PresetName.Trim());
            RefreshList();
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Preset preset }) return;

        var result = MessageBox.Show(
            $"Delete preset \"{preset.Name}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _storageService.DeletePreset(preset.Id);
            RefreshList();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

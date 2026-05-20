using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DisplayConfigManager.UI;

public partial class SavePresetDialog : Window, INotifyPropertyChanged
{
    private string _presetName = string.Empty;

    public string PresetName
    {
        get => _presetName;
        set
        {
            _presetName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNameValid));
        }
    }

    public bool IsNameValid => !string.IsNullOrWhiteSpace(_presetName);

    public SavePresetDialog()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) => NameBox.Focus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsNameValid) return;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void NameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && IsNameValid)
        {
            DialogResult = true;
            Close();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

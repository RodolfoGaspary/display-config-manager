using WinForms = System.Windows.Forms;

namespace DisplayConfigManager.UI;

/// <summary>
/// Hidden message-only Win32 window that receives <c>WM_DISPLAYCHANGE</c>.
/// We need this because a tray-only WPF app has no top-level window of its own,
/// and broadcast messages like WM_DISPLAYCHANGE are only delivered to top-level
/// windows.
/// </summary>
internal sealed class DisplayChangeListener : WinForms.NativeWindow, IDisposable
{
    private const int WM_DISPLAYCHANGE  = 0x007E;
    private const int HWND_MESSAGE      = -3;

    public event Action? DisplayChanged;

    public DisplayChangeListener()
    {
        var cp = new WinForms.CreateParams
        {
            Caption = "DisplayConfigManagerMessageWindow",
            Parent  = (IntPtr)HWND_MESSAGE,
        };
        CreateHandle(cp);
    }

    protected override void WndProc(ref WinForms.Message m)
    {
        if (m.Msg == WM_DISPLAYCHANGE)
        {
            DisplayChanged?.Invoke();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}

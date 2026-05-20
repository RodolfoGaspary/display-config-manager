using System.Runtime.InteropServices;

namespace DisplayConfigManager.Native;

internal static class NativeMethods
{
    [DllImport("user32.dll")]
    internal static extern int GetDisplayConfigBufferSizes(
        uint     flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    internal static extern int QueryDisplayConfig(
        uint                          flags,
        ref uint                      numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint                      numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr                        currentTopologyId);

    [DllImport("user32.dll")]
    internal static extern int SetDisplayConfig(
        uint                         numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
        uint                         numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        uint                         flags);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}

namespace DisplayConfigManager.Native;

internal static class NativeConstants
{
    // QueryDisplayConfig flags
    public const uint QDC_ALL_PATHS = 0x00000001;
    public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;

    // SetDisplayConfig flags
    public const uint SDC_APPLY = 0x00000080;
    public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    // Persist the supplied configuration to Windows' display-config database so it
    // becomes the canonical config for the currently connected display topology.
    // Without this, our apply only affects the current session — Windows still
    // has the OLD (bad) entry in its DB and will revert to it on the next
    // topology change (game fullscreen toggle, monitor wake, EDID re-read, etc.).
    public const uint SDC_SAVE_TO_DATABASE = 0x00000200;
    // *** PROHIBITED: SDC_ALLOW_CHANGES = 0x00000004 ***
    // Adding this flag enables Windows "Best Mode Logic," which silently downgrades
    // high-refresh-rate PC timings (e.g., 144Hz) to TV mode fallback timings.
    // Its absence is the core invariant that preserves exact vSyncFreq and videoStandard values.

    // Win32 error codes
    public const int ERROR_SUCCESS = 0;
    public const int ERROR_GEN_FAILURE = 31;
    public const int ERROR_NOT_SUPPORTED = 50;
    public const int ERROR_INVALID_PARAMETER = 87;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const int ERROR_BAD_CONFIGURATION = 1610;

    // DISPLAYCONFIG_PATH_FLAGS
    public const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;
    public const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xffffffff;

    // DISPLAYCONFIG_MODE_INFO_TYPE values
    public const uint DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1;
    public const uint DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2;
    public const uint DISPLAYCONFIG_MODE_INFO_TYPE_DESKTOP_IMAGE = 3;
}

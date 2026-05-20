using DisplayConfigManager.Exceptions;
using DisplayConfigManager.Models;
using DisplayConfigManager.Models.Dtos;
using DisplayConfigManager.Native;

namespace DisplayConfigManager.Services;

internal sealed class DisplayManagerService
{
    // ── Read ──────────────────────────────────────────────────────────────────

    public DisplayConfiguration GetCurrentConfiguration()
    {
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Step 1: ask Windows how many paths and modes to allocate.
            int sizeResult = NativeMethods.GetDisplayConfigBufferSizes(
                NativeConstants.QDC_ALL_PATHS,
                out uint numPaths,
                out uint numModes);

            if (sizeResult != NativeConstants.ERROR_SUCCESS)
            {
                throw new DisplayConfigException(
                    $"GetDisplayConfigBufferSizes failed with Win32 error {sizeResult}.", sizeResult);
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
            var modes = new DISPLAYCONFIG_MODE_INFO[numModes];

            // Step 2: fill the allocated buffers.
            int result = NativeMethods.QueryDisplayConfig(
                NativeConstants.QDC_ALL_PATHS,
                ref numPaths, paths,
                ref numModes, modes,
                IntPtr.Zero);

            if (result == NativeConstants.ERROR_SUCCESS)
            {
                // Trim arrays to the actual count returned (may be smaller than allocated).
                if (numPaths < paths.Length || numModes < modes.Length)
                {
                    paths = paths[..(int)numPaths];
                    modes = modes[..(int)numModes];
                }
                return new DisplayConfiguration { Paths = paths, Modes = modes };
            }

            // If the config changed between the two calls, retry.
            if (result == NativeConstants.ERROR_INSUFFICIENT_BUFFER && attempt < maxRetries - 1)
                continue;

            throw new DisplayConfigException(
                $"QueryDisplayConfig (fill) failed with Win32 error {result}.", result);
        }

        throw new DisplayConfigException("QueryDisplayConfig failed after maximum retries.");
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    public void ApplyConfiguration(Preset preset)
    {
        var paths = preset.Paths.Select(DtoMapper.DtoToPathInfo).ToArray();
        var modes = preset.Modes.Select(DtoMapper.DtoToModeInfo).ToArray();

        // IMPORTANT: flags must be EXACTLY SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG.
        // DO NOT add SDC_ALLOW_CHANGES — it enables Windows "Best Mode Logic" which
        // silently substitutes TV timings for PC timings and downgrades refresh rates.
        uint flags = NativeConstants.SDC_APPLY | NativeConstants.SDC_USE_SUPPLIED_DISPLAY_CONFIG;

        int result = NativeMethods.SetDisplayConfig(
            (uint)paths.Length, paths,
            (uint)modes.Length, modes,
            flags);

        switch (result)
        {
            case NativeConstants.ERROR_SUCCESS:
                return;

            case NativeConstants.ERROR_BAD_CONFIGURATION:
                throw new DisplayConfigException(
                    $"Could not apply preset \"{preset.Name}\".\n\n" +
                    "The monitor's hardware signature (EDID) no longer matches the saved configuration. " +
                    "This can happen when a monitor is moved to a different port or replaced.\n\n" +
                    "Save a new preset to capture the current configuration.",
                    result);

            case NativeConstants.ERROR_GEN_FAILURE:
                throw new DisplayConfigException(
                    $"Could not apply preset \"{preset.Name}\".\n\n" +
                    "One or more monitors in this preset are not currently connected.",
                    result);

            case NativeConstants.ERROR_NOT_SUPPORTED:
                throw new DisplayConfigException(
                    $"Could not apply preset \"{preset.Name}\".\n\n" +
                    "This configuration is not supported by the current hardware or display driver.",
                    result);

            default:
                throw new DisplayConfigException(
                    $"Could not apply preset \"{preset.Name}\". Win32 error: {result}.",
                    result);
        }
    }
}

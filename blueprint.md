\# Blueprint: Windows Display Configuration Manager



\## 1. Project Overview

A lightweight Windows desktop application designed to capture, save, and seamlessly switch between complex multi-monitor configurations (e.g., extended, mirrored, specific active monitors). The app will live in the System Tray for quick access to presets.



\## 2. Architecture \& Tech Stack

\*   \*\*Language \& Runtime:\*\* C# / .NET 8 (or later).

\*   \*\*Framework:\*\* WPF (Windows Presentation Foundation) for any configuration dialogs, utilizing `NotifyIcon` for the System Tray interface.

\*   \*\*Core Windows APIs:\*\* 

&#x20;   \*   `QueryDisplayConfig` (to read current monitor states, resolutions, and orientations).

&#x20;   \*   `SetDisplayConfig` (to apply saved monitor states).

\*   \*\*Storage:\*\* Local `.json` file (e.g., `presets.json` in `AppData\\Local\\DisplayConfigManager`) to store serialized configuration arrays.



\## 3. Core Features

1\.  \*\*Capture Active Layout:\*\* Read the exact current state of all connected displays (Paths and Modes) and save it under a custom name.

2\.  \*\*Seamless Switching:\*\* Apply a saved configuration instantly without requiring a system reboot or manual Display Settings tweaks.

3\.  \*\*Strict Refresh Rate \& Timing Preservation:\*\* Accurately save fractional refresh rates (e.g., 59.94Hz vs 60Hz) and retain specific "PC mode" over "TV mode" display configurations (preventing Windows from silently down-clocking or changing color subsampling).

4\.  \*\*System Tray Integration:\*\* A right-click context menu on a tray icon to quickly select a preset, save a new preset, or exit the app.



\## 4. Module Breakdown \& Implementation Steps



\### Step 1: Win32 API Interop (P/Invoke)

\*   \*\*Goal:\*\* Define the necessary C# structs and external methods to communicate with the Windows CCD API.

\*   \*\*Tasks:\*\*

&#x20;   \*   Define `DISPLAYCONFIG\_PATH\_INFO`, `DISPLAYCONFIG\_MODE\_INFO`, and `DISPLAYCONFIG\_TOPOLOGY\_ID` structs.

&#x20;   \*   \*Critical:\* Carefully define `DISPLAYCONFIG\_VIDEO\_SIGNAL\_INFO` and its nested `DISPLAYCONFIG\_RATIONAL` struct for `vSyncFreq` (Numerator / Denominator).

&#x20;   \*   \*Agent Warning:\* Pay strict attention to struct layout and memory marshaling (`LayoutKind.Sequential`), as the CCD API is highly sensitive to memory alignment.



\### Step 2: Display Manager Service

\*   \*\*Goal:\*\* Create a wrapper class to handle the complex CCD API calls.

\*   \*\*Tasks:\*\*

&#x20;   \*   Implement `GetCurrentConfiguration()`: Calls `QueryDisplayConfig` using `QDC\_ALL\_PATHS`. Returns an object containing the full Path and Mode arrays.

&#x20;   \*   Implement `ApplyConfiguration(ConfigObject)`: Calls `SetDisplayConfig` using the paths and modes retrieved from a saved preset. 

&#x20;   \*   \*Strict Flag Constraint:\* Use \*\*`SDC\_APPLY | SDC\_USE\_SUPPLIED\_DISPLAY\_CONFIG`\*\*. 

&#x20;   \*   \*Crucial Warning for Agent:\* \*\*DO NOT\*\* use the `SDC\_ALLOW\_CHANGES` flag. Including this flag allows Windows to use "Best Mode Logic," which causes the OS to silently fallback from a high-refresh-rate "PC" timing (e.g., 144Hz) to a standard "TV" timing (e.g., 120Hz/60Hz). Omitting it forces the OS to strictly respect the exact timings provided in the array.



\### Step 3: Preset Management \& Storage

\*   \*\*Goal:\*\* Handle saving and loading of the configuration objects.

\*   \*\*Tasks:\*\*

&#x20;   \*   Create a `Preset` class with properties: `Id` (GUID), `Name` (string), `Paths` (Array), and `Modes` (Array).

&#x20;   \*   \*Fractional Refresh Rate Handling:\* When serializing `vSyncFreq`, save the exact `Numerator` and `Denominator` integer values. \*\*Do not convert to a float or round them.\*\* Windows relies on these exact math ratios to differentiate between TV timings (e.g., 59.94Hz) and standard timings (60.00Hz).

&#x20;   \*   Use `System.Text.Json` to serialize/deserialize the list of `Preset` objects to the local AppData directory. (Consider using DTOs to map Win32 Structs safely to JSON).



\### Step 4: System Tray UI (WPF)

\*   \*\*Goal:\*\* Build the user-facing interface.

\*   \*\*Tasks:\*\*

&#x20;   \*   Set up a hidden main window (WPF apps can run without a main window by configuring `App.xaml`).

&#x20;   \*   Implement a `NotifyIcon`.

&#x20;   \*   \*\*Context Menu Items:\*\*

&#x20;       \*   \*Dynamic List of Presets\* (clicking one calls `ApplyConfiguration`).

&#x20;       \*   Separator.

&#x20;       \*   "Save Current as Preset..." (opens a small WPF input box to name the preset).

&#x20;       \*   "Manage Presets" (opens a small window to rename, delete, or re-order presets).

&#x20;       \*   "Exit".



\## 5. Development Phases for Claude Code

1\.  \*\*Phase 1 - Scaffold \& Interop:\*\* Setup the .NET 8 WPF project. Write the P/Invoke definitions and ensure `QueryDisplayConfig` runs without throwing memory errors.

2\.  \*\*Phase 2 - Core Logic:\*\* Implement serialization. Ensure the exact `vSyncFreq`, `pixelRate`, and `videoStandard` fields in the `DISPLAYCONFIG\_MODE\_INFO` structs are saved and restored perfectly to maintain PC/TV mode separation.

3\.  \*\*Phase 3 - Validation:\*\* Test `SetDisplayConfig` omitting the `SDC\_ALLOW\_CHANGES` flag to guarantee Windows doesn't downgrade high refresh rates.

4\.  \*\*Phase 4 - UI Implementation:\*\* Build the System Tray icon, wire up the context menu to the saved JSON presets, and build the "Save Preset" input dialog.



\## 6. Known Edge Cases to Handle

\*   \*\*PC vs TV Modes:\*\* By omitting `SDC\_ALLOW\_CHANGES`, the OS might reject the configuration if the monitor handshake changed. If `SetDisplayConfig` returns an error (like `ERROR\_BAD\_CONFIGURATION`), catch it and prompt the user: \*"Failed to apply exact timings. The monitor may have changed ports or EDID."\*

\*   \*\*Monitor Disconnection:\*\* The CCD API will fail if a preset attempts to apply a configuration to a hardware ID that is physically disconnected.


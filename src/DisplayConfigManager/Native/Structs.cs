using System.Runtime.InteropServices;

namespace DisplayConfigManager.Native;

// ─── Primitive building blocks ───────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    public uint LowPart;
    public int  HighPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINTL
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECTL
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

// ─── CCD rational (exact fraction — NEVER convert to float) ──────────────────

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_RATIONAL
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_2DREGION
{
    public uint cx;
    public uint cy;
}

// ─── Enums (stored as their 4-byte underlying integer in structs) ─────────────

internal enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
{
    Unspecified              = 0,
    Progressive              = 1,
    InterlacedUpperFieldFirst = 2,
    InterlacedLowerFieldFirst = 3,
}

internal enum DISPLAYCONFIG_PIXELFORMAT : uint
{
    Bpp8         = 1,
    Bpp16        = 2,
    Bpp24        = 3,
    Bpp32        = 4,
    NonGdi       = 5,
}

internal enum DISPLAYCONFIG_ROTATION : uint
{
    Identity  = 1,
    Rotate90  = 2,
    Rotate180 = 3,
    Rotate270 = 4,
}

internal enum DISPLAYCONFIG_SCALING : uint
{
    Identity                = 1,
    Centered                = 2,
    Stretched               = 3,
    AspectRatioCenteredMax  = 4,
    Custom                  = 5,
    Preferred               = 128,
}

internal enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : int
{
    Other                  = -1,
    Hd15                   = 0,
    SVideo                 = 1,
    CompositeVideo         = 2,
    ComponentVideo         = 3,
    Dvi                    = 4,
    Hdmi                   = 5,
    Lvds                   = 6,
    DJpn                   = 8,
    Sdi                    = 9,
    DisplayPortExternal    = 10,
    DisplayPortEmbedded    = 11,
    UdiExternal            = 12,
    UdiEmbedded            = 13,
    SdtvDongle             = 14,
    Miracast               = 15,
    IndirectWired          = 16,
    IndirectVirtual        = 17,
    DisplayPortUsbTunnel   = 18,
    Internal               = unchecked((int)0x80000000),
}

internal enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
{
    Source        = 1,
    Target        = 2,
    DesktopImage  = 3,
}

// ─── Video signal info (the heart of PC-vs-TV timing preservation) ────────────

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
{
    // Pixel clock in Hz — must be stored as ulong (UINT64), never cast to double.
    public ulong pixelRate;

    public DISPLAYCONFIG_RATIONAL hSyncFreq;

    // Exact fractional refresh rate: 59.94 Hz = {60000, 1001}, 60 Hz = {60, 1}.
    // Numerator and Denominator must be round-tripped as integers.
    public DISPLAYCONFIG_RATIONAL vSyncFreq;

    public DISPLAYCONFIG_2DREGION activeSize;
    public DISPLAYCONFIG_2DREGION totalSize;

    // Packed union: bits 0-15 = videoStandard (distinguishes PC/DMT vs TV/CEA modes),
    // bits 16-21 = vSyncFreqDivider, bits 22-31 = reserved.
    // Store verbatim — never extract and re-pack the sub-fields.
    public uint AdditionalSignalInfo;

    public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
}

// ─── Mode info union members ──────────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_TARGET_MODE
{
    public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_SOURCE_MODE
{
    public uint width;
    public uint height;
    public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
    public POINTL position;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
{
    public POINTL PathSourceSize;
    public RECTL  DesktopImageRegion;
    public RECTL  DesktopImageClip;
}

// Union of the three mode types. LayoutKind.Explicit overlays all three at offset 0.
// The runtime sizes the struct to the largest member (DISPLAYCONFIG_TARGET_MODE = 48 bytes).
[StructLayout(LayoutKind.Explicit)]
internal struct DISPLAYCONFIG_MODE_INFO_UNION
{
    [FieldOffset(0)] public DISPLAYCONFIG_TARGET_MODE     targetMode;
    [FieldOffset(0)] public DISPLAYCONFIG_SOURCE_MODE     sourceMode;
    [FieldOffset(0)] public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
}

// Total size: 4 (infoType) + 4 (id) + 8 (adapterId) + 48 (union) = 64 bytes.
[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
    public uint id;
    public LUID adapterId;
    public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
}

// ─── Path info structs ────────────────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    public LUID   adapterId;
    public uint   id;
    public uint   modeInfoIdx;   // DISPLAYCONFIG_PATH_MODE_IDX_INVALID if not active
    public uint   statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    public LUID                                adapterId;
    public uint                                id;
    public uint                                modeInfoIdx;
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
    public DISPLAYCONFIG_ROTATION              rotation;
    public DISPLAYCONFIG_SCALING               scaling;
    public DISPLAYCONFIG_RATIONAL              refreshRate;
    public DISPLAYCONFIG_SCANLINE_ORDERING     scanLineOrdering;
    public int                                 targetAvailable;  // Win32 BOOL = 4-byte int
    public uint                                statusFlags;
}

// Total size: 20 (sourceInfo) + 48 (targetInfo) + 4 (flags) = 72 bytes.
[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    public uint flags;
}

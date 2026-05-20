using DisplayConfigManager.Native;

namespace DisplayConfigManager.Models.Dtos;

internal static class DtoMapper
{
    // ── Path info ──────────────────────────────────────────────────────────────

    public static PathInfoDto PathInfoToDto(DISPLAYCONFIG_PATH_INFO p) => new()
    {
        SrcAdapterIdLowPart  = p.sourceInfo.adapterId.LowPart,
        SrcAdapterIdHighPart = p.sourceInfo.adapterId.HighPart,
        SrcId                = p.sourceInfo.id,
        SrcModeInfoIdx       = p.sourceInfo.modeInfoIdx,
        SrcStatusFlags       = p.sourceInfo.statusFlags,

        TgtAdapterIdLowPart  = p.targetInfo.adapterId.LowPart,
        TgtAdapterIdHighPart = p.targetInfo.adapterId.HighPart,
        TgtId                = p.targetInfo.id,
        TgtModeInfoIdx       = p.targetInfo.modeInfoIdx,
        OutputTechnology     = (int)p.targetInfo.outputTechnology,
        Rotation             = (uint)p.targetInfo.rotation,
        Scaling              = (uint)p.targetInfo.scaling,
        RefreshRateNumerator   = p.targetInfo.refreshRate.Numerator,
        RefreshRateDenominator = p.targetInfo.refreshRate.Denominator,
        ScanLineOrdering     = (uint)p.targetInfo.scanLineOrdering,
        TargetAvailable      = p.targetInfo.targetAvailable,
        TgtStatusFlags       = p.targetInfo.statusFlags,
        Flags                = p.flags,
    };

    public static DISPLAYCONFIG_PATH_INFO DtoToPathInfo(PathInfoDto d) => new()
    {
        sourceInfo = new DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            adapterId    = new LUID { LowPart = d.SrcAdapterIdLowPart, HighPart = d.SrcAdapterIdHighPart },
            id           = d.SrcId,
            modeInfoIdx  = d.SrcModeInfoIdx,
            statusFlags  = d.SrcStatusFlags,
        },
        targetInfo = new DISPLAYCONFIG_PATH_TARGET_INFO
        {
            adapterId        = new LUID { LowPart = d.TgtAdapterIdLowPart, HighPart = d.TgtAdapterIdHighPart },
            id               = d.TgtId,
            modeInfoIdx      = d.TgtModeInfoIdx,
            outputTechnology = (DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY)d.OutputTechnology,
            rotation         = (DISPLAYCONFIG_ROTATION)d.Rotation,
            scaling          = (DISPLAYCONFIG_SCALING)d.Scaling,
            refreshRate      = new DISPLAYCONFIG_RATIONAL
            {
                Numerator   = d.RefreshRateNumerator,
                Denominator = d.RefreshRateDenominator,
            },
            scanLineOrdering = (DISPLAYCONFIG_SCANLINE_ORDERING)d.ScanLineOrdering,
            targetAvailable  = d.TargetAvailable,
            statusFlags      = d.TgtStatusFlags,
        },
        flags = d.Flags,
    };

    // ── Mode info ──────────────────────────────────────────────────────────────

    public static ModeInfoDto ModeInfoToDto(DISPLAYCONFIG_MODE_INFO m)
    {
        var dto = new ModeInfoDto
        {
            InfoType           = (uint)m.infoType,
            Id                 = m.id,
            AdapterIdLowPart   = m.adapterId.LowPart,
            AdapterIdHighPart  = m.adapterId.HighPart,
        };

        if (m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Target)
        {
            var sig = m.modeInfo.targetMode.targetVideoSignalInfo;
            dto.TargetVideoSignalInfo = new VideoSignalInfoDto
            {
                PixelRate            = sig.pixelRate,
                HSyncFreq            = new RationalDto { Numerator = sig.hSyncFreq.Numerator, Denominator = sig.hSyncFreq.Denominator },
                VSyncFreq            = new RationalDto { Numerator = sig.vSyncFreq.Numerator, Denominator = sig.vSyncFreq.Denominator },
                ActiveWidth          = sig.activeSize.cx,
                ActiveHeight         = sig.activeSize.cy,
                TotalWidth           = sig.totalSize.cx,
                TotalHeight          = sig.totalSize.cy,
                AdditionalSignalInfo = sig.AdditionalSignalInfo,
                ScanLineOrdering     = (uint)sig.scanLineOrdering,
            };
        }
        else if (m.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.Source)
        {
            var src = m.modeInfo.sourceMode;
            dto.SourceWidth  = src.width;
            dto.SourceHeight = src.height;
            dto.PixelFormat  = (uint)src.pixelFormat;
            dto.PositionX    = src.position.x;
            dto.PositionY    = src.position.y;
        }
        // DesktopImage type: no additional fields to map

        return dto;
    }

    public static DISPLAYCONFIG_MODE_INFO DtoToModeInfo(ModeInfoDto d)
    {
        var m = new DISPLAYCONFIG_MODE_INFO
        {
            infoType  = (DISPLAYCONFIG_MODE_INFO_TYPE)d.InfoType,
            id        = d.Id,
            adapterId = new LUID { LowPart = d.AdapterIdLowPart, HighPart = d.AdapterIdHighPart },
        };

        if (d.InfoType == NativeConstants.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET
            && d.TargetVideoSignalInfo is { } v)
        {
            m.modeInfo.targetMode.targetVideoSignalInfo = new DISPLAYCONFIG_VIDEO_SIGNAL_INFO
            {
                pixelRate            = v.PixelRate,
                hSyncFreq            = new DISPLAYCONFIG_RATIONAL { Numerator = v.HSyncFreq.Numerator, Denominator = v.HSyncFreq.Denominator },
                vSyncFreq            = new DISPLAYCONFIG_RATIONAL { Numerator = v.VSyncFreq.Numerator, Denominator = v.VSyncFreq.Denominator },
                activeSize           = new DISPLAYCONFIG_2DREGION { cx = v.ActiveWidth,  cy = v.ActiveHeight },
                totalSize            = new DISPLAYCONFIG_2DREGION { cx = v.TotalWidth,   cy = v.TotalHeight },
                AdditionalSignalInfo = v.AdditionalSignalInfo,
                scanLineOrdering     = (DISPLAYCONFIG_SCANLINE_ORDERING)v.ScanLineOrdering,
            };
        }
        else if (d.InfoType == NativeConstants.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE)
        {
            m.modeInfo.sourceMode = new DISPLAYCONFIG_SOURCE_MODE
            {
                width       = d.SourceWidth  ?? 0,
                height      = d.SourceHeight ?? 0,
                pixelFormat = (DISPLAYCONFIG_PIXELFORMAT)(d.PixelFormat ?? 0),
                position    = new POINTL { x = d.PositionX ?? 0, y = d.PositionY ?? 0 },
            };
        }

        return m;
    }
}

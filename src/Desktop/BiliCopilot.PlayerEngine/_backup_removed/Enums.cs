// Copyright (c) Bili Copilot. All rights reserved.
// Enum contract mirror of Richasy.MpvKernel (libVLC-backed engine).

namespace Richasy.MpvKernel.Core.Enums;

/// <summary>
/// 播放器状态.
/// </summary>
public enum MpvPlayerState
{
    Idle,
    Playing,
    Paused,
    End,
    Buffering,
    Seeking,
}

/// <summary>
/// 视频输出类型.
/// </summary>
public enum VideoOutputType
{
    Direct3D,
    Gpu,
    GpuNext,
    Null,
    SDL,
}

/// <summary>
/// GPU API 类型.
/// </summary>
public enum GpuApiType
{
    Auto,
    D3D11,
    OpenGL,
    Vulkan,
}

/// <summary>
/// GPU 上下文类型.
/// </summary>
public enum GpuContextType
{
    Angle,
    Auto,
    D3D11,
    DxInterop,
    Windows,
    WindowsVulkan,
}

/// <summary>
/// 硬件解码类型.
/// </summary>
public enum HardwareDecodeType
{
    Auto,
    AutoUnsafe,
    D3D11va,
    D3D11vaCopy,
    D3D12va,
    D3D12vaCopy,
    Dxva2,
    Dxva2Copy,
    None,
    Nvdec,
    NvdecCopy,
    Vulkan,
    VulkanCopy,
}

/// <summary>
/// 音频通道布局类型.
/// </summary>
public enum AudioChannelLayoutType
{
    Auto,
    Custom,
    Mono,
    Stereo,
}

/// <summary>
/// 自动创建播放列表类型.
/// </summary>
public enum AutoCreatePlaylistKind
{
    Filter,
    No,
    Same,
}

/// <summary>
/// 精确拖动定位类型.
/// </summary>
public enum HrSeekType
{
    Default,
    Yes,
    No,
    Absolute,
    Off,
}

/// <summary>
/// 字幕混合类型.
/// </summary>
public enum SubtitleBlendType
{
    Default,
    Video,
    Ovl,
}

/// <summary>
/// 截图格式.
/// </summary>
public enum ScreenshotFormat
{
    Png,
    Jpg,
}

/// <summary>
/// 轨道类型.
/// </summary>
public enum MpvTrackType
{
    Unknown,
    Audio,
    Video,
    Subtitle,
}

/// <summary>
/// mpv 错误码 (libVLC 引擎尽量映射).
/// </summary>
public enum MpvError
{
    Success,
    Generic,
    EventQueueFull,
    Nomem,
    InvalidParameter,
    OptionError,
    OptionFormat,
    OptionNotFound,
    PropertyError,
    PropertyFormat,
    PropertyNotFound,
    PropertyUnavailable,
    Command,
    NotImplemented,
    Uninitialized,
    LoadingFailed,
    VoInitFailed,
    AoInitFailed,
    NothingToPlay,
    UnknownFormat,
    Unsupported,
    PassthroughFormatUnsupported,
    BluRayInitFailed,
    DvdInitFailed,
    TlsError,
    Http403,
    Http404,
    Http500,
}

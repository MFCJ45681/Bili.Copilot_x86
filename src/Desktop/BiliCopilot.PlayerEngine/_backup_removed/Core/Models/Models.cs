// Copyright (c) Bili Copilot. All rights reserved.
// Model contract mirror of Richasy.MpvKernel.Core.Models (libVLC-backed engine).

using System;
using System.Collections.Generic;
using Richasy.MpvKernel.Core.Enums;

namespace Richasy.MpvKernel.Core.Models;

/// <summary>
/// 播放器初始化选项.
/// </summary>
public sealed class MpvInitializeOptions
{
    public bool UseConfig { get; set; }

    public string? ConfigDirectory { get; set; }

    public string? InputConfigPath { get; set; }

    public bool LoadScripts { get; set; }

    public string? ScriptPath { get; set; }

    public PlayerOperationMode PlayerOperationMode { get; set; } = PlayerOperationMode.Default;
}

/// <summary>
/// 播放操作模式.
/// </summary>
public enum PlayerOperationMode
{
    Default,
    Single,
}

/// <summary>
/// 播放选项.
/// </summary>
public sealed class MpvPlayOptions
{
    public IntPtr WindowHandle { get; set; }

    public double StartPosition { get; set; }

    public double InitialVolume { get; set; } = 100;

    public double InitialSpeed { get; set; } = 1;

    public bool InitExtraLoader { get; set; }

    public Dictionary<string, string> HttpHeaders { get; set; } = new();

    public string? UserAgent { get; set; }

    public bool EnableCookies { get; set; }

    public bool EnableYtdl { get; set; }

    public string? MediaName { get; set; }

    public List<MpvSubtitle>? Subtitles { get; set; }

    public List<string>? AudioTracks { get; set; }
}

/// <summary>
/// 字幕.
/// </summary>
public sealed class MpvSubtitle
{
    public MpvSubtitle(double position, string content)
    {
        Position = position;
        Content = content;
    }

    public double Position { get; set; }

    public string Content { get; set; }
}

/// <summary>
/// 章节信息.
/// </summary>
public sealed class MpvChapterInfo
{
    public string? Title { get; set; }

    public double Time { get; set; }
}

/// <summary>
/// 轨道信息.
/// </summary>
public sealed class MpvTrackInfo
{
    public MpvTrackType Type { get; set; }

    public string? Title { get; set; }

    public int? Id { get; set; }

    public string? Language { get; set; }

    public string? AdditionalText { get; set; }

    public bool Current { get; set; }

    public string? Codec { get; set; }
}

/// <summary>
/// 可定位范围.
/// </summary>
public sealed class MpvSeekableRange
{
    public double Start { get; set; }

    public double End { get; set; }
}

/// <summary>
/// 缓存状态变更事件参数.
/// </summary>
public sealed class MpvCacheStateEventArgs : EventArgs
{
    public MpvCacheStateEventArgs(List<MpvSeekableRange> seekableRanges)
    {
        SeekableRanges = seekableRanges;
    }

    public List<MpvSeekableRange> SeekableRanges { get; }

    public bool BofCached { get; set; }

    public bool EofCached { get; set; }

    public long FwBytes { get; set; }

    public long FileCacheBytes { get; set; }
}

/// <summary>
/// 章节查询结果.
/// </summary>
public sealed class MpvChapterResult
{
    public bool IsSuccess { get; set; }

    public List<MpvChapterInfo> Value { get; set; } = new();
}

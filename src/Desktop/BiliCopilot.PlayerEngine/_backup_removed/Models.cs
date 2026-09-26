// Copyright (c) Bili Copilot. All rights reserved.
// Model contract mirror of Richasy.MpvKernel.Player.Models (libVLC-backed engine).

using System;
using System.Collections.Generic;
using Richasy.MpvKernel.Core.Models;

namespace Richasy.MpvKernel.Player.Models;

/// <summary>
/// 媒体源.
/// </summary>
public sealed class MpvMediaSource
{
    public MpvMediaSource(string url, string id, string title, MpvPlayOptions options)
    {
        Url = url;
        Id = id;
        Title = title;
        Options = options;
    }

    public string Url { get; set; }

    public string Id { get; set; }

    public string Title { get; set; }

    public MpvPlayOptions Options { get; set; }
}

/// <summary>
/// 轨道更新事件参数.
/// </summary>
public sealed class MpvTrackUpdatedEventArgs : EventArgs
{
    public MpvTrackUpdatedEventArgs(List<MpvTrackInfo> tracks)
    {
        Tracks = tracks;
    }

    public List<MpvTrackInfo> Tracks { get; }
}

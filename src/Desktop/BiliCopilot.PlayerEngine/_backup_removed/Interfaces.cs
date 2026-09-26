// Copyright (c) Bili Copilot. All rights reserved.
// Resolver contract mirror of Richasy.MpvKernel.Player.

using System.Threading.Tasks;
using Richasy.MpvKernel.Core.Enums;
using Richasy.MpvKernel.Player.Models;

namespace Richasy.MpvKernel.Player;

/// <summary>
/// 媒体源解析器.
/// </summary>
public interface IMpvMediaSourceResolver
{
    Task<MpvMediaSource> GetSourceAsync();

    IMpvMediaSourceResolver Clone();
}

/// <summary>
/// 媒体历史记录解析器.
/// </summary>
public interface IMpvMediaHistoryResolver
{
    Task<double> GetStartPositionAsync();

    Task SaveHistoryAsync(double position, double duration, MpvPlayerState state, bool isExiting = false);
}

/// <summary>
/// 字幕解析器.
/// </summary>
public interface IMpvMediaSubtitleResolver
{
    Task ShowSubtitle(int position);
}

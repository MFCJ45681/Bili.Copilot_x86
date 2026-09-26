// Copyright (c) Bili Copilot. All rights reserved.
// MpvClient mirror backed by libVLC. mpv-specific configuration is a safe no-op; the
// playback-control surface is wired to the native engine.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Richasy.MpvKernel.Core.Enums;
using Richasy.MpvKernel.Core.Models;
using BiliCopilot.PlayerEngine.LibVlc;

namespace Richasy.MpvKernel.Core;

/// <summary>
/// 播放客户端 (libVLC 引擎). 负责原生实例与媒体播放器的生命周期与控制.
/// </summary>
public sealed class MpvClient : IDisposable, IAsyncDisposable
{
    private readonly ILogger? _logger;
    private readonly LibVlcEngine _engine;
    private bool _disposed;
    private bool _idle = true;
    private bool _keepOpen;

    public MpvClient(ILogger? logger = null)
    {
        _logger = logger;
        _engine = new LibVlcEngine(logger);
    }

    public IntPtr Handle => _engine.PlayerHandle;

    public bool IsDisposed => _disposed;

    public bool IsInitialized { get; private set; }

    public event EventHandler<MpvError>? ErrorOccurred;

    public event EventHandler<MpvCacheStateEventArgs>? CacheStateChanged;

    public event EventHandler? DataNotify;

    public event EventHandler? ReachFileEnd;

    public event EventHandler? ReachFileLoaded;

    public event EventHandler? ReachFileLoading;

    public event EventHandler? Shutdown;

    public static async Task<MpvClient> CreateAsync(string libPath, MpvInitializeOptions options, ILogger? logger = null)
    {
        var client = new MpvClient(logger);
        await client.InitializeAsync(options, libPath);
        return client;
    }

    public Task InitializeAsync(MpvInitializeOptions options, string? libPath = null)
    {
        var path = libPath ?? string.Empty;
        _engine.Initialize(path);
        IsInitialized = true;
        return Task.CompletedTask;
    }

    // ---- playback control (functional) ----

    public Task PlayAsync(string url, MpvPlayOptions options)
    {
        ReachFileLoading?.Invoke(this, EventArgs.Empty);
        var req = new PlayRequest
        {
            Url = url,
            AudioTracks = options.AudioTracks ?? new List<string>(),
            HttpHeaders = options.HttpHeaders,
            UserAgent = options.UserAgent,
            MediaName = options.MediaName,
            StartPosition = options.StartPosition,
            InitialVolume = options.InitialVolume,
            InitialSpeed = options.InitialSpeed,
            WindowHandle = options.WindowHandle,
        };
        try
        {
            _engine.Play(req);
            _idle = false;
            ReachFileLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "播放失败: {Url}", url);
            ErrorOccurred?.Invoke(this, MpvError.LoadingFailed);
        }

        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _engine.Pause();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        _engine.Resume();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _engine.Stop();
        _idle = true;
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume)
    {
        _engine.SetVolume((int)Math.Clamp(volume, 0, 200));
        return Task.CompletedTask;
    }

    public Task SetSpeedAsync(double speed)
    {
        _engine.SetRate((float)speed);
        return Task.CompletedTask;
    }

    public Task SetCurrentPositionAsync(double position)
    {
        _engine.SetTime((long)(position * 1000));
        return Task.CompletedTask;
    }

    public Task SetMuteAsync(bool isMuted)
    {
        _engine.SetMute(isMuted);
        return Task.CompletedTask;
    }

    public Task SetFullScreenStateAsync(bool isFullScreen)
    {
        _engine.SetFullscreen(isFullScreen);
        return Task.CompletedTask;
    }

    public Task SetCompactOverlayStateAsync(bool isCompactOverlay) => Task.CompletedTask;

    public Task ToggleStatsOverlayAsync() => Task.CompletedTask;

    public async Task<MpvChapterResult> GetChaptersAsync()
    {
        // libVLC chapter enumeration is best-effort; report empty when unavailable.
        return await Task.FromResult(new MpvChapterResult { IsSuccess = true, Value = new List<MpvChapterInfo>() });
    }

    // ---- internal accessors used by MpvPlayer ----

    internal int GetState() => _engine.GetState();

    internal long GetTime() => _engine.GetTime();

    internal long GetLength() => _engine.GetLength();

    internal float GetRate() => _engine.GetRate();

    internal int GetVolume() => _engine.GetVolume();

    internal void RaiseCacheState(MpvCacheStateEventArgs e) => CacheStateChanged?.Invoke(this, e);

    internal void RaiseError(MpvError error) => ErrorOccurred?.Invoke(this, error);

    internal void RaiseFileEnd() => ReachFileEnd?.Invoke(this, EventArgs.Empty);

    // ---- mpv-specific configuration (safe no-ops) ----

    public Task SetVideoOutputAsync(VideoOutputType type) => Task.CompletedTask;

    public Task SetGpuApiAsync(GpuApiType type) => Task.CompletedTask;

    public Task SetGpuContextAsync(GpuContextType type) => Task.CompletedTask;

    public Task SetHardwareDecodeAsync(HardwareDecodeType type) => Task.CompletedTask;

    public Task SetConfigFileAsync(string path) => Task.CompletedTask;

    public Task SetAudioChannelLayoutAsync(AudioChannelLayoutType layout) => Task.CompletedTask;

    public Task SetAudioChannelLayoutAsync(AudioChannelLayoutType layout, string[] channels) => Task.CompletedTask;

    public Task SetDemuxerMaxBytesAsync(string size) => Task.CompletedTask;

    public Task SetDemuxerReadheadSecondsAsync(int seconds) => Task.CompletedTask;

    public Task SetDemuxerMaxBackBytesAsync(string size) => Task.CompletedTask;

    public Task SetCacheOnDiskAsync(bool useCache) => Task.CompletedTask;

    public Task SetCacheDirAsync(string dir) => Task.CompletedTask;

    public Task SetLoadAutoProfilesAsync(bool? load) => Task.CompletedTask;

    public Task SetBuiltInProfileAsync(string profile) => Task.CompletedTask;

    public Task SetHrSeekAsync(HrSeekType type) => Task.CompletedTask;

    public Task SetMaxVolumeAsync(int volume) => Task.CompletedTask;

    public Task SetAudioExclusiveAsync(bool exclusive) => Task.CompletedTask;

    public Task SetLogLevelAsync(MpvLogLevel level) => Task.CompletedTask;

    public Task UseIdleAsync(bool? idle) { _idle = idle ?? true; return Task.CompletedTask; }

    public Task UseKeepOpenAsync(bool keepOpen) { _keepOpen = keepOpen; return Task.CompletedTask; }

    public Task SetTlsVerifyAsync(bool verify) => Task.CompletedTask;

    public Task SetAutoCreatePlaylistAsync(AutoCreatePlaylistKind kind) => Task.CompletedTask;

    public Task LoadScriptAsync(string path) => Task.CompletedTask;

    public Task SendKeyPressAsync(string key) => Task.CompletedTask;

    public Task RemoveSubtitleAsync(string path) => Task.CompletedTask;

    public Task SetExternalAudioTrackAsync(string path) => Task.CompletedTask;

    public Task SetExternalSubtitleTrackAsync(string path, string? encoding = null)
    {
        _engine.SetExternalSubtitle(path);
        return Task.CompletedTask;
    }

    public Task SetBlendSubtitleAsync(SubtitleBlendType type) => Task.CompletedTask;

    public Task SetAudioLavcDownmixAsync(bool downmix) => Task.CompletedTask;

    public Task SetAudioSpdifAsync(string[] codecs) => Task.CompletedTask;

    public Task SetAudioTrackAsync(int? track) => Task.CompletedTask;

    public Task SetNvidiaVsrAsync(bool enabled) => Task.CompletedTask;

    public Task SetNvidiaVsrAsync(bool enabled, double strength) => Task.CompletedTask;

    public Task SetPanscanAsync(double panscan) => Task.CompletedTask;

    public Task SetScreenshotDirectoryAsync(string dir) => Task.CompletedTask;

    public Task SetScreenshotFormatAsync(ScreenshotFormat format) => Task.CompletedTask;

    public Task SetScreenshotTemplateAsync(string template) => Task.CompletedTask;

    public Task SetSecondarySubtitlePositionAsync(int position) => Task.CompletedTask;

    public Task SetSecondarySubtitleTrackAsync(int? track) => Task.CompletedTask;

    public Task SetShadersAsync(string[] shaders) => Task.CompletedTask;

    public Task SetStretchImageSubtitleToScreenAsync(bool stretch) => Task.CompletedTask;

    public Task SetSubtitleDelaySecondsAsync(double seconds) => Task.CompletedTask;

    public Task SetSubtitleFontFamilyAsync(string family) => Task.CompletedTask;

    public Task SetSubtitleFontSizeAsync(int size) => Task.CompletedTask;

    public Task SetSubtitlePositionAsync(int position) => Task.CompletedTask;

    public Task SetSubtitleTrackAsync(int? track) => Task.CompletedTask;

    public Task SetTargetColorspaceHintAsync(bool? hint) => Task.CompletedTask;

    public Task SetVulkanDeviceAsync(string device) => Task.CompletedTask;

    public Task SetHttpHeadersAsync(Dictionary<string, string> headers) => Task.CompletedTask;

    public Task SetHttpProxyAsync(string proxy) => Task.CompletedTask;

    public Task TakeScreenshotAsync(string path) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.Dispose();
    }
}

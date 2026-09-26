// Copyright (c) Bili Copilot. All rights reserved.
// libVLC backend powered by LibVLCSharp (the official, maintained libVLC .NET binding).
// Replaces the hand-rolled P/Invoke layer: media state, events and DASH mux are now
// handled by the existing project instead of custom marshalling.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using LibVLCSharp.Shared;

namespace BiliCopilot.PlayerEngine.LibVlc;

/// <summary>
/// A single playback request translated from <c>MpvPlayOptions</c> by <c>MpvClient</c>.
/// </summary>
internal sealed class PlayRequest
{
    public string Url { get; init; } = string.Empty;
    public List<string> AudioTracks { get; init; } = new();
    public Dictionary<string, string> HttpHeaders { get; init; } = new();
    public string? UserAgent { get; init; }
    public string? MediaName { get; init; }
    public double StartPosition { get; init; }
    public double InitialVolume { get; init; }
    public double InitialSpeed { get; init; }
    public IntPtr WindowHandle { get; init; }
}

/// <summary>
/// Wraps a LibVLCSharp <see cref="LibVLC"/> instance and one <see cref="MediaPlayer"/>.
/// </summary>
internal sealed class LibVlcEngine : IDisposable
{
    private static readonly object CoreLock = new();
    private static bool _coreInitialized;
    private static string? _corePath;

    private readonly ILogger? _logger;
    private LibVLC? _libvlc;
    private MediaPlayer? _player;
    private Media? _currentMedia;
    private string? _pendingSubtitlePath;
    private bool _disposed;

    public LibVlcEngine(ILogger? logger) => _logger = logger;

    /// <summary>
    /// Load the 32-bit libVLC runtime from <paramref name="mpvPath"/> (the host passes the
    /// VLC dll path, e.g. <c>Assets/libvlc/x86/libvlc.dll</c>) and create the instance.
    /// </summary>
    public void Initialize(string mpvPath)
    {
        var vlcDir = Path.GetDirectoryName(mpvPath);
        if (string.IsNullOrEmpty(vlcDir) || !File.Exists(mpvPath))
        {
            vlcDir = Path.Combine(AppContext.BaseDirectory, "libvlc");
            mpvPath = Path.Combine(vlcDir, "libvlc.dll");
        }

        if (!File.Exists(mpvPath))
        {
            throw new FileNotFoundException("找不到 libVLC 运行时 (libvlc.dll)。请将 32 位 VLC 运行时放置于 Assets/libvlc/x86/ 目录。", mpvPath);
        }

        // Core.Initialize is process-global and may only run once; all players share the
        // same bundled VLC directory, so subsequent engines reuse the first initialization.
        lock (CoreLock)
        {
            if (!_coreInitialized || !string.Equals(_corePath, vlcDir, StringComparison.OrdinalIgnoreCase))
            {
                Core.Initialize(vlcDir);
                _coreInitialized = true;
                _corePath = vlcDir;
            }
        }

        _logger?.LogInformation("已加载 libVLC (LibVLCSharp): {Path}", vlcDir);

        var pluginPath = Path.Combine(vlcDir, "plugins");
        var args = new List<string>
        {
            "--no-video-title-show",
            "--no-snapshot-preview",
            "--no-osd",
            "--quiet",
            "--no-overlay",
            // Pre-buffer ~4s of network so Bilibili DASH starts faster and does not
            // stall/rebuffer repeatedly (directly addresses slow video loading).
            "--network-caching=4000",
        };
        if (Directory.Exists(pluginPath))
        {
            args.Add($"--plugin-path={pluginPath}");
        }

        _libvlc = new LibVLC(args.ToArray());
        _player = new MediaPlayer(_libvlc);
        _player.EncounteredError += OnEncounteredError;
    }

    private void OnEncounteredError(object? sender, EventArgs e)
        => _logger?.LogError("libVLC 播放错误 (EncounteredError)");

    public void SetWindow(IntPtr hwnd)
    {
        if (_player is not null && hwnd != IntPtr.Zero)
        {
            NativeMethods.libvlc_media_player_set_hwnd(_player.NativeReference, hwnd);
        }
    }

    public void Play(PlayRequest req)
    {
        if (_player is null || _libvlc is null)
        {
            return;
        }

        _currentMedia?.Dispose();
        _currentMedia = new Media(_libvlc, req.Url, FromType.FromLocation);

        // HTTP headers.
        if (!string.IsNullOrEmpty(req.UserAgent))
        {
            _currentMedia.AddOption($":http-user-agent={req.UserAgent}");
        }

        if (req.HttpHeaders.Count > 0)
        {
            _currentMedia.AddOption(":http-cookies=1");
            foreach (var kv in req.HttpHeaders)
            {
                var key = kv.Key.Trim();
                var value = kv.Value;
                if (string.Equals(key, "Referer", StringComparison.OrdinalIgnoreCase))
                {
                    _currentMedia.AddOption($":http-referrer={value}");
                }
                else if (string.Equals(key, "Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    _currentMedia.AddOption($":http-cookie={value}");
                }
                else
                {
                    _currentMedia.AddOption($":http-custom={key}: {value}");
                }
            }
        }

        // Separate audio streams muxed in via input-slave.
        foreach (var audio in req.AudioTracks)
        {
            if (!string.IsNullOrEmpty(audio))
            {
                _currentMedia.AddOption($":input-slave={audio}");
            }
        }

        // External subtitle (.srt) loaded as a media slave.
        if (!string.IsNullOrEmpty(_pendingSubtitlePath))
        {
            _currentMedia.AddOption($":sub-file={_pendingSubtitlePath}");
            _pendingSubtitlePath = null;
        }

        _player.Media = _currentMedia;

        if (req.WindowHandle != IntPtr.Zero)
        {
            NativeMethods.libvlc_media_player_set_hwnd(_player.NativeReference, req.WindowHandle);
        }

        var started = _player.Play();
        if (!started)
        {
            _logger?.LogError("播放启动失败");
        }

        // Initial volume / rate.
        SetVolume((int)Math.Clamp(req.InitialVolume, 0, 200));
        if (req.InitialSpeed > 0)
        {
            _player.SetRate((float)req.InitialSpeed);
        }

        // Start position (after playback begins).
        if (req.StartPosition > 0)
        {
            _player.Time = (long)(req.StartPosition * 1000);
        }
    }

    public void Pause() => _player?.Pause();

    public void Resume() => _player?.Play();

    public void Stop()
    {
        _player?.Stop();
        _currentMedia?.Dispose();
        _currentMedia = null;
    }

    public void SetRate(float rate) { if (_player is not null) _player.SetRate(rate); }

    public float GetRate() => _player is null ? 1f : _player.Rate;

    public void SetVolume(int volume) { if (_player is not null) _player.Volume = volume; }

    public int GetVolume() => _player is null ? 0 : _player.Volume;

    public void SetMute(bool mute) { if (_player is not null) _player.Mute = mute; }

    public void SetTime(long ms) { if (_player is not null) _player.Time = ms; }

    public long GetTime() => _player is null ? 0 : _player.Time;

    public long GetLength() => _player is null ? 0 : _player.Length;

    public int GetState() => _player is null ? NativeMethods.LIBVLC_ENDED : (int)_player.State;

    public void SetFullscreen(bool full) { if (_player is not null) _player.Fullscreen = full; }

    public void SetAudioTrack(int track) => _player?.SetAudioTrack(track);

    public void SetExternalSubtitle(string path) => _pendingSubtitlePath = path;

    public IntPtr PlayerHandle => _player?.NativeReference ?? IntPtr.Zero;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_player is not null)
        {
            _player.EncounteredError -= OnEncounteredError;
            _player.Dispose();
            _player = null;
        }

        _currentMedia?.Dispose();
        _currentMedia = null;

        if (_libvlc is not null)
        {
            _libvlc.Dispose();
            _libvlc = null;
        }
    }
}

// Copyright (c) Bili Copilot. All rights reserved.
// MpvPlayer mirror backed by libVLC. Resolves media via the host resolver and exposes
// playback state through INotifyPropertyChanged (driven by a polling timer).

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Richasy.MpvKernel.Core;
using Richasy.MpvKernel.Core.Enums;
using Richasy.MpvKernel.Core.Models;
using Richasy.MpvKernel.Player.Models;
using BiliCopilot.PlayerEngine.LibVlc;

namespace Richasy.MpvKernel.Player;

/// <summary>
/// 播放器 (libVLC 引擎).
/// </summary>
public sealed class MpvPlayer : INotifyPropertyChanged, IAsyncDisposable, IDisposable
{
    private readonly MpvClient _client;
    private readonly ILogger? _logger;
    private IMpvMediaSourceResolver? _sourceResolver;
    private IMpvMediaHistoryResolver? _historyResolver;
    private IMpvMediaSubtitleResolver? _subtitleResolver;
    private MpvMediaSource? _currentSource;
    private readonly System.Timers.Timer _poller;
    private readonly object _lock = new();
    private bool _initialized;
    private bool _errorRaised;
    private double _position;
    private double _duration;
    private double _volume = 100;
    private double _playbackRate = 1;
    private bool _isPlaybackInitialized;
    private bool _isFullScreen;
    private bool _isCompactOverlay;

    public MpvPlayer(
        MpvClient client,
        IMpvMediaSourceResolver sourceResolver,
        IMpvMediaHistoryResolver historyResolver,
        IMpvMediaSubtitleResolver? subtitleResolver = null,
        ILogger? logger = null)
    {
        _client = client;
        _sourceResolver = sourceResolver;
        _historyResolver = historyResolver;
        _subtitleResolver = subtitleResolver;
        _logger = logger;
        _poller = new System.Timers.Timer(250);
        _poller.Elapsed += OnPoll;
        _poller.AutoReset = true;
    }

    public MpvClient Client => _client;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public bool PreferExtraLoader { get; set; }

    public bool IsInternalLoading { get; set; }

    public MpvPlayerState PlaybackState { get; private set; } = MpvPlayerState.Idle;

    public bool IsPlaying { get; private set; }

    public bool IsLoading { get; private set; }

    public bool IsBuffering { get; private set; }

    public bool IsStopped { get; private set; } = true;

    public double Volume
    {
        get => _volume;
        set
        {
            if (Math.Abs(_volume - value) < 0.01)
            {
                return;
            }

            _volume = value;
            _ = _client.SetVolumeAsync(value);
            RaisePropertyChanged();
        }
    }

    public double PlaybackRate
    {
        get => _playbackRate;
        set
        {
            if (Math.Abs(_playbackRate - value) < 0.001)
            {
                return;
            }

            _playbackRate = value;
            _ = _client.SetSpeedAsync(value);
            RaisePropertyChanged();
        }
    }

    public double Position
    {
        get => _position;
        set
        {
            if (Math.Abs(_position - value) < 0.01)
            {
                return;
            }

            _position = value;
            _ = _client.SetCurrentPositionAsync(value);
            RaisePropertyChanged();
        }
    }

    public double Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            RaisePropertyChanged();
        }
    }

    public bool IsFullScreen
    {
        get => _isFullScreen;
        set
        {
            if (_isFullScreen == value)
            {
                return;
            }

            _isFullScreen = value;
            _ = _client.SetFullScreenStateAsync(value);
            RaisePropertyChanged();
        }
    }

    public bool IsCompactOverlay
    {
        get => _isCompactOverlay;
        set
        {
            if (_isCompactOverlay == value)
            {
                return;
            }

            _isCompactOverlay = value;
            _ = _client.SetCompactOverlayStateAsync(value);
            RaisePropertyChanged();
        }
    }

    public string? Title { get; private set; }

    public bool IsPlaybackInitialized
    {
        get => _isPlaybackInitialized;
        private set
        {
            _isPlaybackInitialized = value;
            RaisePropertyChanged();
        }
    }

    public double CacheSpeed { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<MpvTrackUpdatedEventArgs>? TrackUpdated;

    public Task InitializeAsync(bool? useDefault = null)
    {
        if (!_initialized)
        {
            _initialized = true;
            _poller.Start();
        }

        IsPlaybackInitialized = true;
        return PlayInternalAsync(null);
    }

    public void UpdateResolvers(IMpvMediaSourceResolver sourceResolver, IMpvMediaHistoryResolver historyResolver)
    {
        _sourceResolver = sourceResolver;
        _historyResolver = historyResolver;
    }

    public void UpdateResolvers(IMpvMediaSourceResolver sourceResolver, IMpvMediaHistoryResolver historyResolver, IMpvMediaSubtitleResolver subtitleResolver)
    {
        _sourceResolver = sourceResolver;
        _historyResolver = historyResolver;
        _subtitleResolver = subtitleResolver;
    }

    public Task ReplayAsync(double? position = null) => PlayInternalAsync(position);

    private async Task PlayInternalAsync(double? position)
    {
        if (_sourceResolver is null)
        {
            return;
        }

        try
        {
            _errorRaised = false;
            var source = await _sourceResolver.GetSourceAsync();
            _currentSource = source;
            Title = source.Title;
            RaisePropertyChanged(nameof(Title));

            await _client.PlayAsync(source.Url, source.Options);
            if (position is > 0)
            {
                await _client.SetCurrentPositionAsync(position.Value);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "解析或播放媒体失败");
            throw;
        }
    }

    public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnPoll(object? sender, ElapsedEventArgs e)
    {
        if (_client.IsDisposed)
        {
            return;
        }

        try
        {
            var state = _client.GetState();
            var time = _client.GetTime();
            var length = _client.GetLength();
            var rate = _client.GetRate();
            var volume = _client.GetVolume();

            lock (_lock)
            {
                var mapped = MapState(state);
                UpdateState(mapped);

                if (length > 0)
                {
                    var dur = length / 1000d;
                    if (Math.Abs(dur - _duration) > 0.01)
                    {
                        _duration = dur;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
                    }
                }

                if (time >= 0)
                {
                    var pos = time / 1000d;
                    if (Math.Abs(pos - _position) > 0.05)
                    {
                        _position = pos;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
                    }
                }

                if (Math.Abs(rate - _playbackRate) > 0.001)
                {
                    _playbackRate = rate;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaybackRate)));
                }

                if (Math.Abs(volume - _volume) > 0.01)
                {
                    _volume = volume;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Volume)));
                }

                if (state == NativeMethods.LIBVLC_ERROR && !_errorRaised)
                {
                    _errorRaised = true;
                    _client.RaiseError(MpvError.LoadingFailed);
                }
            }
        }
        catch
        {
            // Poller must never throw into the timer.
        }
    }

    private void UpdateState(MpvPlayerState state)
    {
        if (PlaybackState != state)
        {
            PlaybackState = state;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaybackState)));
        }

        var playing = state == MpvPlayerState.Playing;
        var loading = state is MpvPlayerState.Buffering or MpvPlayerState.Seeking;
        var buffering = state == MpvPlayerState.Buffering;
        var stopped = state is MpvPlayerState.End or MpvPlayerState.Idle;

        if (IsPlaying != playing)
        {
            IsPlaying = playing;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
        }

        if (IsLoading != loading)
        {
            IsLoading = loading;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
        }

        if (IsBuffering != buffering)
        {
            IsBuffering = buffering;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBuffering)));
        }

        if (IsStopped != stopped)
        {
            IsStopped = stopped;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStopped)));
        }
    }

    private static MpvPlayerState MapState(int state) => state switch
    {
        NativeMethods.LIBVLC_PLAYING => MpvPlayerState.Playing,
        NativeMethods.LIBVLC_PAUSED => MpvPlayerState.Paused,
        NativeMethods.LIBVLC_BUFFERING or NativeMethods.LIBVLC_OPENING => MpvPlayerState.Buffering,
        NativeMethods.LIBVLC_STOPPED or NativeMethods.LIBVLC_ENDED => MpvPlayerState.End,
        NativeMethods.LIBVLC_ERROR => MpvPlayerState.End,
        _ => MpvPlayerState.Idle,
    };

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _poller.Stop();
        _poller.Dispose();
    }
}

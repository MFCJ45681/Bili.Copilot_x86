// Copyright (c) Bili Copilot. All rights reserved.
// Low-level P/Invoke bindings for libVLC 3.0 (32-bit). AOT compatible.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BiliCopilot.PlayerEngine.LibVlc;

/// <summary>
/// Native libVLC 3.0 entry points. All string parameters are UTF-8.
/// The module is loaded explicitly by full path (see <see cref="LibVlcEngine"/>)
/// so the standard <c>[LibraryImport("libvlc")]</c> resolution finds the already
/// loaded module regardless of the process DLL search path.
/// </summary>
internal static unsafe partial class NativeMethods
{
    private const string Lib = "libvlc";

    // ---- instance / media player lifecycle ----
    [LibraryImport(Lib)]
    internal static partial IntPtr libvlc_new(int argc, IntPtr[] argv);

    [LibraryImport(Lib)]
    internal static partial void libvlc_release(IntPtr p_instance);

    [LibraryImport(Lib)]
    internal static partial IntPtr libvlc_media_player_new(IntPtr p_instance);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_release(IntPtr p_mi);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr libvlc_media_new_location(IntPtr p_instance, string psz_mrl);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr libvlc_media_new_path(IntPtr p_instance, string path);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_release(IntPtr p_md);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void libvlc_media_add_option(IntPtr p_md, string psz_options);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_media(IntPtr p_mi, IntPtr p_md);

    [LibraryImport(Lib)]
    internal static partial int libvlc_media_player_play(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_pause(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_stop(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_pause(IntPtr p_mi, int do_pause);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_hwnd(IntPtr p_mi, IntPtr drawable);

    // ---- playback control ----
    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_rate(IntPtr p_mi, float rate);

    [LibraryImport(Lib)]
    internal static partial float libvlc_media_player_get_rate(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial int libvlc_media_player_set_volume(IntPtr p_mi, int volume);

    [LibraryImport(Lib)]
    internal static partial int libvlc_media_player_get_volume(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_mute(IntPtr p_mi, int status);

    [LibraryImport(Lib)]
    internal static partial long libvlc_media_player_get_time(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_time(IntPtr p_mi, long time);

    [LibraryImport(Lib)]
    internal static partial long libvlc_media_player_get_length(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial int libvlc_media_player_get_state(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial float libvlc_media_player_get_position(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_position(IntPtr p_mi, float pos);

    [LibraryImport(Lib)]
    internal static partial IntPtr libvlc_media_player_get_media(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial void libvlc_media_player_set_fullscreen(IntPtr p_mi, int b_fullscreen);

    // ---- audio ----
    [LibraryImport(Lib)]
    internal static partial int libvlc_audio_set_track(IntPtr p_mi, int track);

    [LibraryImport(Lib)]
    internal static partial int libvlc_audio_get_track(IntPtr p_mi);

    [LibraryImport(Lib)]
    internal static partial int libvlc_audio_get_track_count(IntPtr p_mi);

    // ---- error ----
    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial string? libvlc_errmsg();

    // libvlc_state enum (subset we care about).
    internal const int LIBVLC_ENDED = 6;
    internal const int LIBVLC_OPENING = 1;
    internal const int LIBVLC_BUFFERING = 2;
    internal const int LIBVLC_PLAYING = 3;
    internal const int LIBVLC_PAUSED = 4;
    internal const int LIBVLC_STOPPED = 5;
    internal const int LIBVLC_ERROR = 7;
}

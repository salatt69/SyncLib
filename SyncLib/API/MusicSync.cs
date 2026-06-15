using System;
using SyncLib.Core;

namespace SyncLib.API
{
    /// <summary>
    /// High-level, thread-safe accessors for synchronizing gameplay or application logic to musical timing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MusicSync"/> exposes the current playback state and provides per-frame “edge” queries (for example,
    /// <see cref="OnBeat"/> or <see cref="OnBar"/>) that return <c>true</c> only on the frame a timing event occurs.
    /// </para>
    /// <para>
    /// Indices such as <see cref="BeatIndex"/> and <see cref="BarIndex"/> are monotonic counters for the current
    /// playback session and reset when a new session begins (see <see cref="ActivePlayingId"/>).
    /// </para>
    /// <para>
    /// All members are safe to call from any thread; however, the “ThisFrame” queries are intended to be polled from a
    /// regular update loop for deterministic behavior.
    /// </para>
    /// </remarks>
    public static class MusicSync
    {
        /// <summary>
        /// Gets whether music playback is currently active.
        /// </summary>
        /// <value>
        /// <c>true</c> if music is playing (or considered active by the runtime); otherwise <c>false</c>.
        /// </value>
        public static bool IsMusicActive => MusicSyncRuntime.IsMusicActive;

        /// <summary>
        /// Gets the identifier of the current playback session.
        /// </summary>
        /// <remarks>
        /// When it changes, timing indices such as <see cref="BeatIndex"/> and <see cref="BarIndex"/> may reset.
        /// </remarks>
        /// <value>A session identifier that is stable for the duration of the current playback session.</value>
        public static uint ActivePlayingId => MusicSyncRuntime.ActivePlayingId;

        /// <summary>
        /// Gets the current entry marker index.
        /// </summary>
        /// <remarks>
        /// An entry marker represents a logical “start” point in the synchronization timeline (for example, the moment
        /// music enters an active segment). The meaning of “entry” is defined by the underlying runtime integration.
        /// </remarks>
        /// <value>A monotonically increasing index for entry markers within the current session.</value>
        public static long EntryIndex => MusicSyncRuntime.EntryIndex;

        /// <summary>
        /// Gets the current exit marker index.
        /// </summary>
        /// <remarks>
        /// An exit marker represents a logical “end” point in the synchronization timeline (for example, the moment
        /// music exits an active segment). The meaning of “exit” is defined by the underlying runtime integration.
        /// </remarks>
        /// <value>A monotonically increasing index for exit markers within the current session.</value>
        public static long ExitIndex => MusicSyncRuntime.ExitIndex;

        /// <summary>
        /// Gets the current beat counter for the active playback session.
        /// </summary>
        /// <remarks>
        /// This is a monotonic counter that increments on each beat detected/reported by the runtime for the current
        /// session.
        /// </remarks>
        /// <value>A monotonically increasing, session-local beat index.</value>
        public static long BeatIndex => MusicSyncRuntime.BeatIndex;

        /// <summary>
        /// Gets the current bar (measure) counter for the active playback session.
        /// </summary>
        /// <remarks>
        /// This is a monotonic counter that increments on each bar detected/reported by the runtime for the current
        /// session.
        /// </remarks>
        /// <value>A monotonically increasing, session-local bar index.</value>
        public static long BarIndex => MusicSyncRuntime.BarIndex;

        /// <summary>
        /// Gets the current counter for custom bar events.
        /// </summary>
        /// <remarks>
        /// Custom bars are user-defined bar groupings exposed by the runtime (for example, “every N beats” or other
        /// project-specific segmentation). Use <see cref="OnCustomBar"/> to detect the per-frame edge.
        /// </remarks>
        /// <value>A monotonically increasing index for custom bar events within the current session.</value>
        public static long CustomBarIndex => MusicSyncRuntime.CustomBarIndex;

        /// <summary>
        /// Gets the duration of a single beat in seconds.
        /// </summary>
        /// <remarks>
        /// This value is authoritative and exact for the currently playing track.
        /// </remarks>
        public static double BeatInterval => MusicSyncRuntime.BeatInterval;

        /// <summary>
        /// Gets the tempo of the current track in beats per minute (BPM).
        /// </summary>
        /// <remarks>
        /// This is calculated from <see cref="BeatInterval"/> and may take a few beats
        /// to stabilize after music starts or changes.
        /// <para>
        /// Returns 0 if the interval is not yet available.
        /// </para>
        /// </remarks>
        public static double BPM => MusicSyncRuntime.BPM;

        /// <summary>
        /// Initializes music synchronization settings for the current process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <paramref name="logBeats"/> <c>true</c>, enables beat logging for diagnostics.
        /// </para>
        /// <para>
        /// Optional: Call this once during startup before polling timing events.
        /// </para>
        /// </remarks>
        public static void Initialize(bool logBeats = false) => Plugin.LogBeats = logBeats;

        /// <summary>
        /// Checks whether an entry marker occurred during the current frame.
        /// </summary>
        /// <returns>
        /// <c>true</c> on the frame an entry event occurs; otherwise <c>false</c>.
        /// </returns>
        public static bool OnEntry() => MusicSyncRuntime.EntryThisFrame();

        /// <summary>
        /// Checks whether an exit marker occurred during the current frame.
        /// </summary>
        /// <returns>
        /// <c>true</c> on the frame an exit event occurs; otherwise <c>false</c>.
        /// </returns>
        public static bool OnExit() => MusicSyncRuntime.ExitThisFrame();

        /// <summary>
        /// Checks whether a beat occurred during the current frame.
        /// </summary>
        /// <returns>
        /// <c>true</c> on the frame a beat occurs; otherwise <c>false</c>.
        /// </returns>
        public static bool OnBeat() => MusicSyncRuntime.BeatThisFrame();

        /// <summary>
        /// Checks whether a bar (measure) occurred during the current frame.
        /// </summary>
        /// <remarks>
        /// <para>
        /// RoR2's Wwise integration sometimes emits bar events on every beat, so use it with caution.
        /// </para>
        /// <para>
        /// For reliable bar detection, consider tracking bars using <see cref="OnCustomBar"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>true</c> on the frame a bar occurs; otherwise <c>false</c>.
        /// </returns>
        public static bool OnBar() => MusicSyncRuntime.BarThisFrame();

        /// <summary>
        /// Checks whether a custom bar event occurred during the current frame.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Allows for user-defined bar tracking (every 'N' beats), which may be more reliable than standard bar events depending on your goals.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>true</c> on the frame a custom bar event occurs; otherwise <c>false</c>.
        /// </returns>
        public static bool OnCustomBar() => MusicSyncRuntime.CustomBarThisFrame();

        /// <summary>
        /// Checks whether the specified beat subdivision occurred during the current frame.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <paramref name="NthBeat"/>: The subdivision to test (for example, <c>2</c> for every 2nd beat, <c>3</c> for every 3rd beat).
        /// </para>
        /// <para>
        /// Prefer this method when you need regular musical subdivisions without tracking counters manually.
        /// </para>
        /// </remarks>
        /// <returns>
        /// <c>true</c> on frames where the current beat is an <paramref name="NthBeat"/> multiple; otherwise <c>false</c>.
        /// </returns>
        public static bool OnNthBeat(int NthBeat) => MusicSyncRuntime.NthBeatThisFrame(NthBeat);

        /// <summary>
        /// Registers a custom Wwise event name prefix to be recognized as music by SyncLib.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this during your mod's <c>Awake</c> before any music events are posted.
        /// </para>
        /// <para>
        /// Matching is case-insensitive and uses <see cref="string.Contains(string)"/>.
        /// </para>
        /// </remarks>
        public static void RegisterMusicEventPrefix(string prefix) => WwisePostEventRedirect.RegisterMusicEventPrefix(prefix);
    }
}

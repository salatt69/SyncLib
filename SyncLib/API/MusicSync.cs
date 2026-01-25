using System;
using SyncLib.Core;

namespace SyncLib.API
{
    public static class MusicSync
    {
        public static bool IsMusicActive => MusicSyncRuntime.IsMusicActive;
        public static uint ActivePlayingId => MusicSyncRuntime.ActivePlayingId;
        public static long EntryIndex => MusicSyncRuntime.EntryIndex;
        public static long ExitIndex => MusicSyncRuntime.ExitIndex;
        public static long BeatIndex => MusicSyncRuntime.BeatIndex;
        public static long BarIndex => MusicSyncRuntime.BarIndex;
        public static long CustomBarIndex => MusicSyncRuntime.CustomBarIndex;

        public static void Initialize(bool logBeats = false) => Plugin.LogBeats = logBeats;

        public static bool OnEntry() => MusicSyncRuntime.EntryThisFrame();
        public static bool OnExit() => MusicSyncRuntime.ExitThisFrame();
        public static bool OnBeat() => MusicSyncRuntime.BeatThisFrame();
        public static bool OnBar() => MusicSyncRuntime.BarThisFrame();
        public static bool OnCustomBar() => MusicSyncRuntime.CustomBarThisFrame();
        public static bool OnNthBeat(int nBeat) => MusicSyncRuntime.NthBeatThisFrame(nBeat);
    }
}

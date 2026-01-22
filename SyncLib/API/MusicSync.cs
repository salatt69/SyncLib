using System;
using SyncLib.Core;

namespace SyncLib.API
{
    public static class MusicSync
    {
        public static bool IsMusicActive => MusicSyncRuntime.IsMusicActive;

        public static uint ActivePlayingId => MusicSyncRuntime.ActivePlayingId;

        public static long BeatIndex => MusicSyncRuntime.BeatIndex;

        public static long BarIndex => MusicSyncRuntime.BarIndex;

        public static event Action<BeatEvent> Beat
        {
            add => MusicSyncRuntime.Beat += value;
            remove => MusicSyncRuntime.Beat -= value;
        }

        public static event Action<BarEvent> Bar
        {
            add => MusicSyncRuntime.Bar += value;
            remove => MusicSyncRuntime.Bar -= value;
        }

        public static void Initialize(bool printEveryBeat = false) => Plugin.BeatDebug = printEveryBeat;

        public static bool OnBeat() => MusicSyncRuntime.ConsumeBeatPulse();

        public static bool OnBar() => MusicSyncRuntime.ConsumeBarPulse();

        public static bool OnNBeat(int nBeat) => MusicSyncRuntime.ConsumeNBeat(nBeat);
    }

    public readonly struct BeatEvent
    {
        public readonly uint PlayingId;
        public readonly long BeatIndex;
        public readonly double AudioTimeSeconds;

        public BeatEvent(uint playingId, long beatIndex, double audioTimeSeconds)
        {
            PlayingId = playingId;
            BeatIndex = beatIndex;
            AudioTimeSeconds = audioTimeSeconds;
        }
    }

    public readonly struct BarEvent
    {
        public readonly uint PlayingId;
        public readonly long BarIndex;
        public readonly double AudioTimeSeconds;

        public BarEvent(uint playingId, long barIndex, double audioTimeSeconds)
        {
            PlayingId = playingId;
            BarIndex = barIndex;
            AudioTimeSeconds = audioTimeSeconds;
        }
    }
}

using System;

namespace SYNC.Core
{
    internal readonly struct BeatEvent(uint playingId, long beatIndex, double audioTimeSeconds)
    {
        public readonly uint PlayingId = playingId;
        public readonly long BeatIndex = beatIndex;
        public readonly double AudioTimeSeconds = audioTimeSeconds;

        public static event Action<BeatEvent> Beat
        {
            add => MusicSyncRuntime.Beat += value;
            remove => MusicSyncRuntime.Beat -= value;
        }
    }

    internal readonly struct BarEvent(uint playingId, long barIndex, double audioTimeSeconds)
    {
        public readonly uint PlayingId = playingId;
        public readonly long BarIndex = barIndex;
        public readonly double AudioTimeSeconds = audioTimeSeconds;

        public static event Action<BarEvent> Bar
        {
            add => MusicSyncRuntime.Bar += value;
            remove => MusicSyncRuntime.Bar -= value;
        }
    }

    internal readonly struct EntryEvent(uint playingId, long entryIndex, double audioTimeSeconds)
    {
        public readonly uint PlayingId = playingId;
        public readonly long EntryIndex = entryIndex;
        public readonly double AudioTimeSeconds = audioTimeSeconds;

        public static event Action<EntryEvent> Entry
        {
            add => MusicSyncRuntime.Entry += value;
            remove => MusicSyncRuntime.Entry -= value;
        }
    }

    internal readonly struct ExitEvent(uint playingId, long exitIndex, double audioTimeSeconds)
    {
        public readonly uint PlayingId = playingId;
        public readonly long ExitIndex = exitIndex;
        public readonly double AudioTimeSeconds = audioTimeSeconds;

        public static event Action<ExitEvent> Exit
        {
            add => MusicSyncRuntime.Exit += value;
            remove => MusicSyncRuntime.Exit -= value;
        }
    }

    internal readonly struct CustomBarEvent(uint playingId, long customBarIndex, double audioTimeSeconds)
    {
        public readonly uint PlayingId = playingId;
        public readonly long CustomBarIndex = customBarIndex;
        public readonly double AudioTimeSeconds = audioTimeSeconds;

        public static event Action<CustomBarEvent> CustomBar
        {
            add => MusicSyncRuntime.CustomBar += value;
            remove => MusicSyncRuntime.CustomBar -= value;
        }
    }
}

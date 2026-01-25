using SyncLib.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncLib.API
{
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

        public static event Action<BeatEvent> Beat
        {
            add => MusicSyncRuntime.Beat += value;
            remove => MusicSyncRuntime.Beat -= value;
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

        public static event Action<BarEvent> Bar
        {
            add => MusicSyncRuntime.Bar += value;
            remove => MusicSyncRuntime.Bar -= value;
        }
    }

    public readonly struct EntryEvent
    {
        public readonly uint PlayingId;
        public readonly long EntryIndex;
        public readonly double AudioTimeSeconds;

        public EntryEvent(uint playingId, long entryIndex, double audioTimeSeconds)
        {
            PlayingId = playingId;
            EntryIndex = entryIndex;
            AudioTimeSeconds = audioTimeSeconds;
        }

        public static event Action<EntryEvent> Entry
        {
            add => MusicSyncRuntime.Entry += value;
            remove => MusicSyncRuntime.Entry -= value;
        }
    }

    public readonly struct ExitEvent
    {
        public readonly uint PlayingId;
        public readonly long ExitIndex;
        public readonly double AudioTimeSeconds;

        public ExitEvent(uint playingId, long exitIndex, double audioTimeSeconds)
        {
            PlayingId = playingId;
            ExitIndex = exitIndex;
            AudioTimeSeconds = audioTimeSeconds;
        }

        public static event Action<ExitEvent> Exit
        {
            add => MusicSyncRuntime.Exit += value;
            remove => MusicSyncRuntime.Exit -= value;
        }
    }

    public readonly struct CustomBarEvent
    {
        public readonly uint PlayingId;
        public readonly long CustomBarIndex;
        public readonly double AudioTimeSeconds;

        public CustomBarEvent(uint playingId, long customBarIndex, double audioTimeSeconds)
        {
            PlayingId = playingId;
            CustomBarIndex = customBarIndex;
            AudioTimeSeconds = audioTimeSeconds;
        }

        public static event Action<CustomBarEvent> CustomBar
        {
            add => MusicSyncRuntime.CustomBar += value;
            remove => MusicSyncRuntime.CustomBar -= value;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SYNC.Core
{
    internal readonly struct SyncMsg(SyncType type, uint playingId, int gridDiv, double beatDuration)
    {
        public readonly SyncType Type = type;
        public readonly uint PlayingId = playingId;
        public readonly int GridDiv = gridDiv;
        public readonly double BeatDuration = beatDuration;
    }

    internal static class MusicSyncRuntime
    {
        private static readonly ConcurrentQueue<SyncMsg> Queue = new();

        private static int _initialized;

        private static uint _activePlayingId;
        public static uint ActivePlayingId => _activePlayingId;

        // time signature
        private static readonly int _beatsPerBar = 4;
        private static readonly int _note = 4;

        private static long _entryIndex;
        private static long _exitIndex;
        private static long _beatIndex;
        private static long _barIndex;

        private static long _customBarIndex;

        public static long EntryIndex => Interlocked.Read(ref _entryIndex);
        public static long ExitIndex => Interlocked.Read(ref _exitIndex);
        public static long BeatIndex => Interlocked.Read(ref _beatIndex);
        public static long BarIndex => Interlocked.Read(ref _barIndex);
        public static long CustomBarIndex => Interlocked.Read(ref _customBarIndex);

        public static bool IsMusicActive => ActivePlayingId != 0;

        public static double BeatInterval => Volatile.Read(ref _currentBeatDuration);
        public static double BPM => Volatile.Read(ref _currentBeatDuration) > 0 ? 60.0 / Volatile.Read(ref _currentBeatDuration) : 0;

        // per-frame tracking
        private static int _entryThisFrame;
        private static int _exitThisFrame;
        private static int _beatThisFrame;
        private static int _barThisFrame;

        // custom bar tracking
        private static int _customBarThisFrame;
        
        // That allows custom bar to start at the first beat of the new track
        // and don't wait for "_beatsPerBar amount of beats" to pass, to invoke bar event   
        private static int _beatsUntilBar = _beatsPerBar - 1;

        // Nth-beat tracking
        private static long _firstBeatIndexThisFrame;
        private static int _beatCountThisFrame;

        private static double _currentBeatDuration;

        public static event Action<EntryEvent> Entry;
        public static event Action<ExitEvent> Exit;
        public static event Action<BeatEvent> Beat;
        public static event Action<BarEvent> Bar;
        public static event Action<CustomBarEvent> CustomBar;

        public static void EnsureInitialized()
        {
            Interlocked.CompareExchange(ref _initialized, 1, 0);
        }

        public static void EnqueueFromWwise(SyncMsg msg)
        {
            Queue.Enqueue(msg);
        }

        public static void Update()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 1) == 0)
            {
                EnsureInitialized();
            }

            // per-frame counters reset
            Interlocked.Exchange(ref _beatThisFrame, 0);
            Interlocked.Exchange(ref _barThisFrame, 0);
            Interlocked.Exchange(ref _entryThisFrame, 0);
            Interlocked.Exchange(ref _exitThisFrame, 0);
            Interlocked.Exchange(ref _customBarThisFrame, 0);

            Interlocked.Exchange(ref _firstBeatIndexThisFrame, 0);
            Interlocked.Exchange(ref _beatCountThisFrame, 0);

            while (Queue.TryDequeue(out var msg))
            {
                if (_activePlayingId == 0 && (msg.Type == SyncType.Beat || msg.Type == SyncType.Bar))
                {
                    _activePlayingId = msg.PlayingId;
                }

                if (_activePlayingId != 0 && msg.PlayingId != _activePlayingId && msg.Type == SyncType.Beat)
                {
                    ResetForNewStream(msg.PlayingId);
                }

                if (_activePlayingId == 0 || msg.PlayingId != _activePlayingId)
                    continue;

                switch (msg.Type)
                {
                    case SyncType.Beat:
                        {
                            Volatile.Write(ref _currentBeatDuration, msg.BeatDuration);

                            int beatInBar = Interlocked.Increment(ref _beatsUntilBar);
                            if (beatInBar >= _beatsPerBar)
                            {
                                Interlocked.Exchange(ref _beatsUntilBar, 0);

                                var newCustomBar = Interlocked.Increment(ref _customBarIndex);
                                Interlocked.Increment(ref _customBarThisFrame);
                                CustomBar?.Invoke(new CustomBarEvent(_activePlayingId, newCustomBar, msg.BeatDuration));

                                if (Plugin.LogBeats) Log.Info($"CustomBar! PlayingID={_activePlayingId} CustomBarIndex={newCustomBar}");
                            }

                            var newBeat = Interlocked.Increment(ref _beatIndex);
                            if (Volatile.Read(ref _beatCountThisFrame) == 0)
                                Volatile.Write(ref _firstBeatIndexThisFrame, newBeat);

                            Interlocked.Increment(ref _beatThisFrame);
                            Interlocked.Increment(ref _beatCountThisFrame);

                            Beat?.Invoke(new BeatEvent(_activePlayingId, newBeat, msg.BeatDuration));

                            if (Plugin.LogBeats) Log.Info($"Beat! PlayingID={_activePlayingId} BeatIndex={newBeat}, Interval: {msg.BeatDuration}");
                            break;
                        }

                    case SyncType.Bar:
                        {
                            var newBar = Interlocked.Increment(ref _barIndex);
                            Interlocked.Increment(ref _barThisFrame);
                            Bar?.Invoke(new BarEvent(_activePlayingId, newBar, msg.BeatDuration));

                            if (Plugin.LogBeats) Log.Info($"Bar! PlayingID={_activePlayingId} BarIndex={newBar}");
                            break;
                        }

                    case SyncType.Entry:
                        {
                            var newEntry = Interlocked.Increment(ref _entryIndex);
                            Interlocked.Increment(ref _entryThisFrame);
                            Entry?.Invoke(new EntryEvent(_activePlayingId, newEntry, msg.BeatDuration));

                            if (Plugin.LogBeats) Log.Info($"Entry! PlayingID={_activePlayingId} EntryIndex={newEntry}");

                            // to keep in in sync with everything else, reset when new entry occurs
                            Interlocked.Exchange(ref _beatsUntilBar, _beatsPerBar - 1);
                            break;
                        }

                    case SyncType.Exit:
                        {
                            var newExit = Interlocked.Increment(ref _exitIndex);
                            Interlocked.Increment(ref _exitThisFrame);
                            Exit?.Invoke(new ExitEvent(_activePlayingId, newExit, msg.BeatDuration));

                            if (Plugin.LogBeats) Log.Info($"Exit! PlayingID={_activePlayingId} ExitIndex={newExit}");
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }
        }

        private static void ResetForNewStream(uint newPlayingId)
        {
            _activePlayingId = newPlayingId;
            Interlocked.Exchange(ref _beatIndex, 0);
            Interlocked.Exchange(ref _barIndex, 0);
            Interlocked.Exchange(ref _entryIndex, 0);
            Interlocked.Exchange(ref _exitIndex, 0);
            Interlocked.Exchange(ref _customBarIndex, 0);

            Interlocked.Exchange(ref _beatThisFrame, 0);
            Interlocked.Exchange(ref _barThisFrame, 0);
            Interlocked.Exchange(ref _entryThisFrame, 0);
            Interlocked.Exchange(ref _exitThisFrame, 0);
            Interlocked.Exchange(ref _customBarThisFrame, 0);

            Interlocked.Exchange(ref _beatsUntilBar, _beatsPerBar - 1);

            Interlocked.Exchange(ref _firstBeatIndexThisFrame, 0);
            Interlocked.Exchange(ref _beatCountThisFrame, 0);

            Volatile.Write(ref _currentBeatDuration, 0);
        }

        public static bool EntryThisFrame() => Volatile.Read(ref _entryThisFrame) > 0;
        public static bool ExitThisFrame() => Volatile.Read(ref _exitThisFrame) > 0;
        public static bool BeatThisFrame() => Volatile.Read(ref _beatThisFrame) > 0;
        public static bool BarThisFrame() => Volatile.Read(ref _barThisFrame) > 0;
        public static bool CustomBarThisFrame() => Volatile.Read(ref _customBarThisFrame) > 0;
        public static bool NthBeatThisFrame(int NthBeat)
        {
            if (NthBeat <= 0) return false;

            var count = Volatile.Read(ref _beatCountThisFrame);
            if (count <= 0) return false;

            var first = Volatile.Read(ref _firstBeatIndexThisFrame);
            if (first <= 0) return false;

            for (long i = 0; i < count; i++)
            {
                var beat = first + i;
                if ((beat % NthBeat) == 0)
                    return true;
            }

            return false;
        }
    }
}

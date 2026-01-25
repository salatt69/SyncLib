using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SyncLib.Core
{
    internal readonly struct SyncMsg
    {
        public readonly SyncType Type;
        public readonly uint PlayingId;
        public readonly int GridDiv;
        public readonly double AudioTimeSeconds;

        public SyncMsg(SyncType type, uint playingId, int gridDiv, double audioTimeSeconds)
        {
            this.Type = type;
            PlayingId = playingId;
            GridDiv = gridDiv;
            AudioTimeSeconds = audioTimeSeconds;
        }
    }

    internal static class MusicSyncRuntime
    {
        private static readonly ConcurrentQueue<SyncMsg> Queue = new ConcurrentQueue<SyncMsg>();

        private static int _initialized;

        private static uint _activePlayingId;
        public static uint ActivePlayingId => _activePlayingId;

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

        // per-frame tracking
        private static int _entryThisFrame;
        private static int _exitThisFrame;
        private static int _beatThisFrame;
        private static int _barThisFrame;

        // custom bar tracking
        private static int _customBarThisFrame;
        private static int _beatsToBarCount = -1;

        // Nth beat tracking
        private static long _firstBeatIndexThisFrame;
        private static int _beatCountThisFrame;

        public static event Action<API.EntryEvent>? Entry;
        public static event Action<API.ExitEvent>? Exit;
        public static event Action<API.BeatEvent>? Beat;
        public static event Action<API.BarEvent>? Bar;
        public static event Action<API.CustomBarEvent>? CustomBar;

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
            Volatile.Write(ref _beatThisFrame, 0);
            Volatile.Write(ref _barThisFrame, 0);
            Volatile.Write(ref _entryThisFrame, 0);
            Volatile.Write(ref _exitThisFrame, 0);
            Volatile.Write(ref _customBarThisFrame, 0);

            Volatile.Write(ref _firstBeatIndexThisFrame, 0);
            Volatile.Write(ref _beatCountThisFrame, 0);

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
                            int beatInBar = Interlocked.Increment(ref _beatsToBarCount);
                            if (beatInBar >= 4)
                            {
                                Interlocked.Exchange(ref _beatsToBarCount, 0);

                                var newCustomBar = Interlocked.Increment(ref _customBarIndex);
                                Volatile.Write(ref _customBarThisFrame, Volatile.Read(ref _customBarThisFrame) + 1);
                                CustomBar?.Invoke(new API.CustomBarEvent(_activePlayingId, newCustomBar, msg.AudioTimeSeconds));

                                if (Plugin.LogBeats) Log.Info($"CustomBar! PlayingID={_activePlayingId} CustomBarIndex={newCustomBar}");
                            }

                            var newBeat = Interlocked.Increment(ref _beatIndex);
                            if (Volatile.Read(ref _beatCountThisFrame) == 0)
                                Volatile.Write(ref _firstBeatIndexThisFrame, newBeat);

                            Volatile.Write(ref _beatThisFrame, Volatile.Read(ref _beatThisFrame) + 1);
                            Volatile.Write(ref _beatCountThisFrame, Volatile.Read(ref _beatCountThisFrame) + 1);

                            Beat?.Invoke(new API.BeatEvent(_activePlayingId, newBeat, msg.AudioTimeSeconds));

                            if (Plugin.LogBeats) Log.Info($"Beat! PlayingID={_activePlayingId} BeatIndex={newBeat}");
                            break;
                        }

                    case SyncType.Bar:
                        {
                            var newBar = Interlocked.Increment(ref _barIndex);
                            Volatile.Write(ref _barThisFrame, Volatile.Read(ref _barThisFrame) + 1);
                            Bar?.Invoke(new API.BarEvent(_activePlayingId, newBar, msg.AudioTimeSeconds));

                            if (Plugin.LogBeats) Log.Info($"Bar! PlayingID={_activePlayingId} BarIndex={newBar}");
                            break;
                        }

                    case SyncType.Entry:
                        {
                            var newEntry = Interlocked.Increment(ref _entryIndex);
                            Volatile.Write(ref _entryThisFrame, Volatile.Read(ref _entryThisFrame) + 1);
                            Entry?.Invoke(new API.EntryEvent(_activePlayingId, newEntry, msg.AudioTimeSeconds));

                            if (Plugin.LogBeats) Log.Info($"Entry! PlayingID={_activePlayingId} EntryIndex={newEntry}");

                            // to keep in in sync with everything else, reset when new entry occurs
                            Interlocked.Exchange(ref _beatsToBarCount, -1);
                            break;
                        }

                    case SyncType.Exit:
                        {
                            var newExit = Interlocked.Increment(ref _exitIndex);
                            Volatile.Write(ref _exitThisFrame, Volatile.Read(ref _exitThisFrame) + 1);
                            Exit?.Invoke(new API.ExitEvent(_activePlayingId, newExit, msg.AudioTimeSeconds));

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

            Volatile.Write(ref _beatThisFrame, 0);
            Volatile.Write(ref _barThisFrame, 0);
            Volatile.Write(ref _entryThisFrame, 0);
            Volatile.Write(ref _exitThisFrame, 0);
            Volatile.Write(ref _customBarThisFrame, 0);

            Volatile.Write(ref _beatsToBarCount, -1);

            Volatile.Write(ref _firstBeatIndexThisFrame, 0);
            Volatile.Write(ref _beatCountThisFrame, 0);
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

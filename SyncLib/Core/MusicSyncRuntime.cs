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

        private static long _beatIndex;
        private static long _barIndex;

        public static long BeatIndex => Interlocked.Read(ref _beatIndex);
        public static long BarIndex => Interlocked.Read(ref _barIndex);

        public static bool IsMusicActive => ActivePlayingId != 0;

        private static int _beatsThisFrame;
        private static int _barsThisFrame;
        private static long _firstBeatIndexThisFrame;
        private static int _beatCountThisFrame;

        public static event Action<API.BeatEvent>? Beat;
        public static event Action<API.BarEvent>? Bar;

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
            Volatile.Write(ref _beatsThisFrame, 0);
            Volatile.Write(ref _barsThisFrame, 0);
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
                            var newBeat = Interlocked.Increment(ref _beatIndex);
                            if (Volatile.Read(ref _beatCountThisFrame) == 0)
                                Volatile.Write(ref _firstBeatIndexThisFrame, newBeat);

                            Volatile.Write(ref _beatsThisFrame, Volatile.Read(ref _beatsThisFrame) + 1);
                            Volatile.Write(ref _beatCountThisFrame, Volatile.Read(ref _beatCountThisFrame) + 1);

                            Beat?.Invoke(new API.BeatEvent(_activePlayingId, newBeat, msg.AudioTimeSeconds));

                            if (Plugin.BeatDebug) Log.Info($"Beat! PlayingID={_activePlayingId} BeatIndex={newBeat}");
                            break;
                        }

                    case SyncType.Bar:
                        {
                            var newBar = Interlocked.Increment(ref _barIndex);
                            Volatile.Write(ref _barsThisFrame, Volatile.Read(ref _barsThisFrame) + 1);
                            Bar?.Invoke(new API.BarEvent(_activePlayingId, newBar, msg.AudioTimeSeconds));

                            if (Plugin.BeatDebug) Log.Info($"Bar! PlayingID={_activePlayingId} BarIndex={newBar}");
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

            Volatile.Write(ref _beatsThisFrame, 0);
            Volatile.Write(ref _barsThisFrame, 0);
            Volatile.Write(ref _firstBeatIndexThisFrame, 0);
            Volatile.Write(ref _beatCountThisFrame, 0);
        }

        public static bool ConsumeBeatPulse() => Volatile.Read(ref _beatsThisFrame) > 0;

        public static bool ConsumeBarPulse() => Volatile.Read(ref _barsThisFrame) > 0;

        public static bool ConsumeNBeat(int nBeat)
        {
            if (nBeat <= 0) return false;

            var count = Volatile.Read(ref _beatCountThisFrame);
            if (count <= 0) return false;

            var first = Volatile.Read(ref _firstBeatIndexThisFrame);
            if (first <= 0) return false;

            for (long i = 0; i < count; i++)
            {
                var beat = first + i;
                if ((beat % nBeat) == 0)
                    return true;
            }

            return false;
        }
    }
}

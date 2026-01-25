using System;
using System.Threading;

namespace SyncLib.Core
{
    internal static class WwiseCallbackInjector
    {
        internal static readonly uint Mask = (uint)(
            AkCallbackType.AK_MusicSyncBeat |
            AkCallbackType.AK_MusicSyncBar |
            AkCallbackType.AK_MusicSyncGrid |
            AkCallbackType.AK_MusicSyncEntry |
            AkCallbackType.AK_MusicSyncExit
        );

        internal static readonly AkCallbackManager.EventCallback Callback = OnAkCallback;

        private static int _loggedFirst;

        internal static void EnsureReady()
        {
            MusicSyncRuntime.EnsureInitialized();
        }

        internal static uint OrMask(uint existing) => existing | Mask;

        internal static AkCallbackManager.EventCallback Chain(AkCallbackManager.EventCallback existing)
        {
            if (existing == null) return Callback;

            // if our callback already exists in the invocation list, don't add.
            foreach (var d in existing.GetInvocationList())
            {
                if (d == (Delegate)Callback)
                    return existing;
            }

            return (AkCallbackManager.EventCallback)Delegate.Combine(existing, Callback);
        }

        private static void OnAkCallback(object cookie, AkCallbackType type, AkCallbackInfo info)
        {
            try
            {

#if DEBUG
                if (Interlocked.Exchange(ref _loggedFirst, 1) == 0)
                {
                    Log.Info($"AkCallback received: type={type} infoType={info?.GetType().FullName ?? "null"}");
                }
#endif

                if (type != AkCallbackType.AK_MusicSyncBeat &&
                    type != AkCallbackType.AK_MusicSyncBar &&
                    type != AkCallbackType.AK_MusicSyncGrid &&
                    type != AkCallbackType.AK_MusicSyncEntry &&
                    type != AkCallbackType.AK_MusicSyncExit)
                {
                    return;
                }

                if (info is not AkMusicSyncCallbackInfo m)
                {
                    Log.Warning($"MusicSync info type mismatch: {info?.GetType().FullName ?? "null"}");
                    return;
                }

                double audioTimeSeconds = 0; // idk

                switch (type)
                {
                    case AkCallbackType.AK_MusicSyncBeat:
                        MusicSyncRuntime.EnqueueFromWwise(new SyncMsg(SyncType.Beat, m.playingID, 0, audioTimeSeconds));
                        break;
                    case AkCallbackType.AK_MusicSyncBar:
                        MusicSyncRuntime.EnqueueFromWwise(new SyncMsg(SyncType.Bar, m.playingID, 0, audioTimeSeconds));
                        break;
                    case AkCallbackType.AK_MusicSyncGrid:
                        MusicSyncRuntime.EnqueueFromWwise(new SyncMsg(SyncType.Grid, m.playingID, 0, audioTimeSeconds));
                        break;
                    case AkCallbackType.AK_MusicSyncEntry:
                        MusicSyncRuntime.EnqueueFromWwise(new SyncMsg(SyncType.Entry, m.playingID, 0, audioTimeSeconds));
                        break;
                    case AkCallbackType.AK_MusicSyncExit:
                        MusicSyncRuntime.EnqueueFromWwise(new SyncMsg(SyncType.Exit, m.playingID, 0, audioTimeSeconds));
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Wwise callback error: {e}");
            }
        }
    }
}

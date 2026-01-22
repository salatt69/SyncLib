using UnityEngine;

namespace SyncLib.Core
{
    internal sealed class MusicSyncRunner : MonoBehaviour
    {
        private static int _logged;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            MusicSyncRuntime.EnsureInitialized();
        }

        private void Update()
        {
#if DEBUG
            if (System.Threading.Interlocked.Exchange(ref _logged, 1) == 0)
            {
                Log.Info("MusicSyncRunner is updating.");
            }
#endif
            MusicSyncRuntime.Update();
        }
    }
}

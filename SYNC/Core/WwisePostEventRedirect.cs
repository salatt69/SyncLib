using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SYNC.Core
{
    [HarmonyPatch(typeof(AkSoundEngine))]
    internal static class WwisePostEventRedirect
    {
        private const bool LogRedirects = true;
        private const int RedirectLogEvery = 50;

        private static int _redirectCount;
        private static readonly HashSet<string> _musicPrefixes = [ "Play_Music" ];

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AkSoundEngine.PostEvent), [typeof(string), typeof(GameObject)])]
        private static bool Prefix(string in_pszEventName, GameObject in_gameObjectID, ref uint __result)
        {
            if (WwiseMusicSyncTap.Redirecting) return true;
            if (!IsProbablyMusic(in_pszEventName)) return true;

            try
            {
                WwiseMusicSyncTap.Redirecting = true;

                WwiseCallbackInjector.EnsureReady();
                __result = AkSoundEngine.PostEvent(
                    in_pszEventName,
                    in_gameObjectID,
                    WwiseCallbackInjector.Mask,
                    WwiseCallbackInjector.Callback,
                    null
                );
#if DEBUG
                int n = Interlocked.Increment(ref _redirectCount);
                if ((n % RedirectLogEvery) == 0)
                {
                    Log.Message($"Redirected music PostEvent x{n} (latest={in_pszEventName})");
                }
#endif

                return false;
            }
            catch (Exception e)
            {
                Log.Error($"Redirect failed for PostEvent(\"{in_pszEventName}\"): {e}");
                return true;
            }
            finally
            {
                WwiseMusicSyncTap.Redirecting = false;
            }
        }

        private static bool IsProbablyMusic(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (string prefix in _musicPrefixes)
            {
                if (name.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static void RegisterMusicEventPrefix(string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                _musicPrefixes.Add(prefix);
        }
    }
}

using HarmonyLib;
using System;
using UnityEngine;

namespace SYNC.Core
{
    [HarmonyPatch(typeof(AkSoundEngine))]
    internal static class WwiseMusicSyncTap
    {
        [ThreadStatic] internal static bool Redirecting;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AkSoundEngine.PostEvent), new Type[]
        {
            typeof(string),
            typeof(GameObject),
            typeof(uint),
            typeof(AkCallbackManager.EventCallback),
            typeof(object)
        })]
        private static void Prefix(ref uint in_uFlags, ref AkCallbackManager.EventCallback in_pfnCallback)
        {
            if (Redirecting) return;

            try
            {
                WwiseCallbackInjector.EnsureReady();
                in_uFlags = WwiseCallbackInjector.OrMask(in_uFlags);
                in_pfnCallback = WwiseCallbackInjector.Chain(in_pfnCallback);
            }
            catch (Exception e)
            {
                Log.Error($"WwiseMusicSyncTap failed: {e}");
            }
        }
    }
}

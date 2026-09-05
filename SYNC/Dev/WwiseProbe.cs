using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SYNC.Dev
{
    internal static class WwiseProbe
    {
        private static bool _ran;

        public static void DumpPostEventOverloads()
        {
            if (_ran) return;
            _ran = true;

            try
            {
                var methods = AccessTools.GetDeclaredMethods(typeof(AkSoundEngine))
                    .Where(m => m.Name == "PostEvent")
                    .ToArray();

                Log.Info($"AkSoundEngine.PostEvent overloads found: {methods.Length}");

                int cbCapable = 0;
                foreach (var m in methods)
                {
                    var ps = m.GetParameters();
                    bool hasCallback = ps.Any(p => p.ParameterType == typeof(AkCallbackManager.EventCallback));
                    bool hasMask = ps.Any(p => p.ParameterType == typeof(uint));
                    if (hasCallback) cbCapable++;

                    var sig = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Log.Info($"PostEvent({sig})  callbackCapable={hasCallback}  hasUInt={hasMask}");
                }

                Log.Info($"Callback-capable PostEvent overloads: {cbCapable}");
            }
            catch (Exception e)
            {
                Log.Warning($"WwiseProbe failed: {e}");
            }
        }
    }
}

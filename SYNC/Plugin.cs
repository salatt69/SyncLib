using BepInEx;
using HarmonyLib;
using R2API;
using RoR2;
using SYNC.Core;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SYNC
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com." + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "salatt";
        public const string PluginName = "SyncLib";
        public const string PluginVersion = "0.0.0";

        internal static Plugin Instance;
        internal static Harmony Harmony;

        internal static bool LogBeats;

        public void Awake()
        {
            Instance = this;

            Log.Init(Logger);

            Log.Info("SyncLib initializing...");

            LogBeats = false;

            Harmony = new Harmony(PluginGUID);
            Harmony.PatchAll();

            try
            {
                var go = new GameObject("SyncLib_MusicSyncRunner")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                go.AddComponent<MusicSyncRunner>();
                DontDestroyOnLoad(go);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to create MusicSyncRunner: {e}");
            }
            
            Log.Info("SyncLib initialized!");
        }
    }
}

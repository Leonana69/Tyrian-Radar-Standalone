using BepInEx;
using BepInEx.Logging;
using Radar.Patches;
using UnityEngine;

namespace Radar
{
    [BepInPlugin(PluginMetadata.Guid, PluginMetadata.Name, PluginMetadata.Version)]
    public class RadarPlugin : BaseUnityPlugin
    {
        internal static RadarPlugin Instance { get; private set; } = null!;
        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RadarConfig.Bind(Config);
            AssetFileManager.Load();

            new GameStartPatch().Enable();

            Log.LogInfo("Radar plugin enabled.");
        }
    }
}

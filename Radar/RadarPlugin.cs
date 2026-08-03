using BepInEx;
using BepInEx.Logging;
using Radar.Patches;
using UnityEngine;

namespace Radar
{
    [BepInPlugin("com.leonana69.radar", "Leonana69-Radar", "1.3.0")]
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

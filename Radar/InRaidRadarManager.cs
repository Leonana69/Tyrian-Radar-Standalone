using BepInEx.Configuration;
using System;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Spawned into the raid by <see cref="Patches.GameStartPatch"/>. Owns the radar HUD instance and
    /// the keyboard shortcuts that toggle it.
    /// </summary>
    public class InRaidRadarManager : MonoBehaviour
    {
        private const string FpsCameraName = "FPS Camera";

        private static GameObject? _radarObject;

        /// <summary>The radar in the current raid, or null when there is nothing to notify.</summary>
        public static HaloRadar? LiveRadar
        {
            get
            {
                if (_radarObject == null)
                    return null;

                HaloRadar radar = _radarObject.GetComponent<HaloRadar>();
                return radar != null && radar.InGame ? radar : null;
            }
        }

        private bool _radarKeyDown;
        private bool _corpseKeyDown;
        private bool _lootKeyDown;

        private void Awake()
        {
            new Patches.LootItemAddPatch().Enable();
            new Patches.LootItemRemovePatch().Enable();
            new Patches.PlayerOnMakingShotPatch().Enable();

            GameObject playerCamera = GameObject.Find(FpsCameraName);
            if (playerCamera == null)
            {
                RadarPlugin.Log.LogError("FPS Camera not found");
                Destroy(gameObject);
                return;
            }

            _radarObject = Instantiate(AssetFileManager.RadarHudPrefab);
            _radarObject.transform.SetParent(playerCamera.transform);
            _radarObject.AddComponent<HaloRadar>();

            RadarPlugin.Log.LogInfo("Radar instantiated");
        }

        private void OnEnable()
        {
            RadarConfig.Enabled.SettingChanged += OnRadarEnableChanged;
            UpdateRadarStatus();
        }

        private void OnDisable()
        {
            RadarConfig.Enabled.SettingChanged -= OnRadarEnableChanged;
        }

        private void Update()
        {
            ToggleOnKeyPress(RadarConfig.ToggleRadarKey, RadarConfig.Enabled, ref _radarKeyDown);
            ToggleOnKeyPress(RadarConfig.ToggleCorpseKey, RadarConfig.CorpseEnabled, ref _corpseKeyDown);
            ToggleOnKeyPress(RadarConfig.ToggleLootKey, RadarConfig.LootEnabled, ref _lootKeyDown);
        }

        private void OnRadarEnableChanged(object sender, EventArgs e) => UpdateRadarStatus();

        private void UpdateRadarStatus()
        {
            if (_radarObject == null)
            {
                RadarPlugin.Log.LogWarning("Radar did not load properly or has been destroyed");
                Destroy(gameObject);
                return;
            }

            _radarObject.SetActive(RadarConfig.Enabled.Value);
        }

        /// <summary>Flips a setting once per key press rather than once per frame it is held.</summary>
        private static void ToggleOnKeyPress(ConfigEntry<KeyboardShortcut> shortcut, ConfigEntry<bool> setting, ref bool wasDown)
        {
            bool isDown = shortcut.Value.IsDown();

            if (isDown && !wasDown)
                setting.Value = !setting.Value;

            wasDown = isDown;
        }
    }
}

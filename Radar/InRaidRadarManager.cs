using EFT;
using System;
using UnityEngine;

namespace Radar
{
    public class InRaidRadarManager : MonoBehaviour
    {
        static public GameObject? _radarGo;

        private bool _enableSCDown = false;
        private bool _reloadSCDown = false;
        private bool _corpseSCDown = false;
        private bool _lootSCDown = false;

        private void Awake()
        {
            new Patches.GClass723PatchAdd().Enable();
            new Patches.GClass723PatchRemove().Enable();
            new Patches.PlayerOnMakingShotPatch().Enable();
            var playerCamera = GameObject.Find("FPS Camera");
            if (playerCamera == null)
            {
                Radar.Log.LogError("FPS Camera not found");
                Destroy(gameObject);
                return;
            }

            _radarGo = Instantiate(AssetBundleManager.RadarhudPrefab);
            _radarGo.transform.SetParent(playerCamera.transform);
            _radarGo.AddComponent<HaloRadar>();
            Radar.Log.LogInfo("Radar instantiated");
        }

        private void OnEnable()
        {
            Radar.radarEnableConfig.SettingChanged += OnRadarEnableChanged;
            UpdateRadarStatus();
        }

        private void OnDisable()
        {
            Radar.radarEnableConfig.SettingChanged -= OnRadarEnableChanged;
        }

        private void OnRadarEnableChanged(object sender, EventArgs e)
        {
            UpdateRadarStatus();
        }

        private void UpdateRadarStatus()
        {
            if (_radarGo != null)
            {
                _radarGo.SetActive(Radar.radarEnableConfig.Value);
            }
            else
            {
                // Radar game object is null or is destroyed
                Radar.Log.LogWarning("Radar did not load properly or has been destroyed");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // enable radar shortcut process
            if (!_reloadSCDown && Radar.radarReloadListShortCutConfig.Value.IsDown())
            {
                Radar.radarListReloadRequested = true;
                _reloadSCDown = true;
            }
            if (!Radar.radarReloadListShortCutConfig.Value.IsDown())
            {
                _reloadSCDown = false;
            }

            // enable radar shortcut process
            if (!_enableSCDown && Radar.radarEnableShortCutConfig.Value.IsDown())
            {
                Radar.radarEnableConfig.Value = !Radar.radarEnableConfig.Value;
                _enableSCDown = true;
            }
            if (!Radar.radarEnableShortCutConfig.Value.IsDown())
            {
                _enableSCDown = false;
            }

            // enable corpse shortcut process
            if (!_corpseSCDown && Radar.radarEnableCorpseShortCutConfig.Value.IsDown())
            {
                Radar.radarEnableCorpseConfig.Value = !Radar.radarEnableCorpseConfig.Value;
                _corpseSCDown = true;
            }
            if (!Radar.radarEnableCorpseShortCutConfig.Value.IsDown())
            {
                _corpseSCDown = false;
            }

            // enable loot shortcut process
            if (!_lootSCDown && Radar.radarEnableLootShortCutConfig.Value.IsDown())
            {
                Radar.radarEnableLootConfig.Value = !Radar.radarEnableLootConfig.Value;
                _lootSCDown = true;
            }
            if (!Radar.radarEnableLootShortCutConfig.Value.IsDown())
            {
                _lootSCDown = false;
            }
        }
    }
}
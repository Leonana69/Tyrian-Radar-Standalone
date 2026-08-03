using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Drives the radar: owns the HUD layout, the trackers and the per-frame update loop.
    /// Attached to the HUD prefab instance by <see cref="InRaidRadarManager"/>.
    /// </summary>
    public class HaloRadar : MonoBehaviour
    {
        private const string FpsCameraName = "FPS Camera";

        /// <summary>Fire Mode sweeps players far more often, so muzzle flashes register promptly.</summary>
        private const float FireModeScanInterval = 0.1f;

        /// <summary>Slowest the pulse sweep may run, in seconds per revolution.</summary>
        private const float MinPulseInterval = 1f;

        /// <summary>False until a raid is live, so the Harmony patches know not to call in.</summary>
        public bool InGame { get; private set; }

        private readonly Dictionary<string, BlipPlayer> _players = new Dictionary<string, BlipPlayer>();

        private GameWorld _gameWorld = null!;
        private Player _player = null!;
        private RadarHudLayout _layout = null!;
        private LootTracker _loot = null!;
        private MinefieldTracker _minefield = null!;
        private ExfilTracker _exfil = null!;

        private bool _ready;
        private Coroutine? _pulseCoroutine;
        private float _pulseInterval = MinPulseInterval;
        private float _lastPlayerScanTime;

        // --- Called by the Harmony patches -------------------------------------------------------

        public void AddLoot(string id, Item item, Transform transform, bool lazyUpdate = false) =>
            _loot.Add(id, item, transform, lazyUpdate);

        public void RemoveLootByKey(int key) => _loot.RemoveByKey(key);

        public void UpdateFireTime(string profileId)
        {
            if (_players.TryGetValue(profileId, out BlipPlayer blip))
                blip.UpdateLastFireTime(Time.time);
        }

        // --- Unity lifecycle ---------------------------------------------------------------------

        private void Awake()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                RadarPlugin.Log.LogWarning("GameWorld singleton not found.");
                Destroy(gameObject);
                return;
            }

            _gameWorld = Singleton<GameWorld>.Instance;
            if (_gameWorld.MainPlayer == null)
            {
                RadarPlugin.Log.LogWarning("MainPlayer is null.");
                Destroy(gameObject);
                return;
            }

            _player = _gameWorld.MainPlayer;

            _layout = new RadarHudLayout(transform, GameObject.Find(FpsCameraName));
            _loot = new LootTracker(_gameWorld, _player);
            _minefield = new MinefieldTracker(_gameWorld);
            _exfil = new ExfilTracker(_player);

            RadarRegion.FillColor = RadarConfig.MinefieldColor.Value;

            ItemPricing.Init(this);

            // Destroy() only takes effect at the end of the frame, so the bail-outs above still let
            // OnEnable and Update run once. This gate keeps them from touching half-built state.
            _ready = true;
            RadarPlugin.Log.LogInfo("Radar loaded");
        }

        private void OnEnable()
        {
            if (!_ready) return;

            InitRadar();
            RadarPlugin.Instance.Config.SettingChanged += OnSettingChanged;
            ApplyAllSettings();
            InGame = true;
        }

        private void OnDisable()
        {
            if (!_ready) return;

            RadarPlugin.Instance.Config.SettingChanged -= OnSettingChanged;
            StopPulse();
            InGame = false;
        }

        private void Update()
        {
            if (!_ready || _player == null) return;

            if (!RadarConfig.CompassEnabled.Value)
                _layout.UpdateBorderRotation();

            _loot.Tick();
            Render(positionUpdate: ScanPlayers());

            if (RadarConfig.CompassEnabled.Value)
                _layout.TickCompass();
        }

        // --- Radar state -------------------------------------------------------------------------

        private void InitRadar()
        {
            _layout.Apply();

            if (RadarConfig.LootEnabled.Value)
                _loot.Rebuild();

            _minefield.Rebuild();
            _exfil.Rebuild();
        }

        /// <summary>
        /// Registers any players that have appeared since the last sweep.
        /// </summary>
        /// <returns>False when the sweep was throttled, meaning blips should hold their position.</returns>
        private bool ScanPlayers()
        {
            float interval = RadarConfig.FireModeEnabled.Value
                ? FireModeScanInterval
                : RadarConfig.ScanInterval.Value;

            if (Time.time - _lastPlayerScanTime < interval)
                return false;

            _lastPlayerScanTime = Time.time;

            IEnumerable<Player> allPlayers = _gameWorld.AllPlayersEverExisted;

            // Everyone is already tracked apart from us, so there is nothing to register.
            if (allPlayers.Count() == _players.Count + 1)
                return true;

            foreach (Player player in allPlayers)
            {
                if (player == null || player == _player)
                    continue;

                if (!_players.ContainsKey(player.ProfileId))
                    _players[player.ProfileId] = new BlipPlayer(player);
            }

            return true;
        }

        private void Render(bool positionUpdate)
        {
            Target.SetPlayerTransform(_player.Transform);
            Target.SetRadarRange(RadarConfig.InnerRange.Value, RadarConfig.OuterRange.Value);
            RadarRegion.SetPlayerPosition(_player.Transform.position);

            foreach (BlipPlayer blip in _players.Values)
                blip.Update(positionUpdate);

            _loot.Render();
            _minefield.Render();
            _exfil.Render();
        }

        // --- Settings ----------------------------------------------------------------------------

        /// <summary>Pushes every setting into the HUD, for start-up and after a mode switch.</summary>
        private void ApplyAllSettings()
        {
            _pulseInterval = Mathf.Max(MinPulseInterval, RadarConfig.ScanInterval.Value);
            TogglePulse(RadarConfig.PulseEnabled.Value);
            _layout.ApplyPosition();
            _layout.ApplyScale();
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            // The HUD is torn down while the radar is toggled off; nothing to push settings into.
            if (!gameObject.activeInHierarchy) return;

            _pulseInterval = Mathf.Max(MinPulseInterval, RadarConfig.ScanInterval.Value);

            ConfigEntryBase changed = e.ChangedSetting;

            if (changed == RadarConfig.PulseEnabled)
                TogglePulse(RadarConfig.PulseEnabled.Value);
            else if (changed == RadarConfig.BackgroundColor)
                _layout.SetBackgroundColor(RadarConfig.BackgroundColor.Value);
            else if (changed == RadarConfig.CompassEnabled)
                InitRadar();
            else if (changed == RadarConfig.MinefieldEnabled)
                _minefield.Rebuild();
            else if (changed == RadarConfig.ExfilEnabled)
                _exfil.Rebuild();
            else if (changed == RadarConfig.MinefieldColor)
                RadarRegion.FillColor = RadarConfig.MinefieldColor.Value;
            else if (changed == RadarConfig.LootEnabled || changed == RadarConfig.WishlistLootEnabled ||
                     changed == RadarConfig.LootValuePerSlot || changed == RadarConfig.LootThreshold)
                RefreshLoot();
            else if (changed == RadarConfig.OffsetX || changed == RadarConfig.OffsetY)
                _layout.ApplyPosition();
            else if (changed == RadarConfig.HudSize)
                _layout.ApplyScale();
        }

        private void RefreshLoot()
        {
            if (RadarConfig.LootEnabled.Value)
                _loot.Rebuild();
            else
                _loot.Clear();
        }

        // --- Pulse animation ---------------------------------------------------------------------

        private void TogglePulse(bool enable)
        {
            StopPulse();

            if (enable)
                _pulseCoroutine = StartCoroutine(PulseCoroutine());

            _layout.SetPulseVisible(enable);
        }

        private void StopPulse()
        {
            if (_pulseCoroutine == null) return;

            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        private IEnumerator PulseCoroutine()
        {
            while (true)
            {
                // One revolution per pulse interval, sweeping backwards from 360 to 0 degrees.
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime / _pulseInterval;
                    _layout.SetPulseAngle((1f - t) * 360f);
                    yield return null;
                }
            }
        }
    }
}

using EFT;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using UnityEngine;
using UnityEngine.UI;
using EFT.Interactive;
using System.Linq;
using BepInEx.Configuration;
using EFT.InventoryLogic;
using System.IO;
using Newtonsoft.Json;
using System;
using SPT.Reflection.Utils;

namespace Radar
{
    [System.Serializable]
    public class CustomLootList
    {
        public HashSet<string> items { get; set; } = new HashSet<string>();
    }
    public class HaloRadar : MonoBehaviour
    {
        private const float DEFAULT_SCALE = 1f;
        private const string PLAYER_INVENTORY_PREFIX = "55d7217a4bdc2d86028b456d";
        private const string DRAWER_PREFIX = "578f87b7245977356274f2cd";
        private const string FPS_CAMERA_NAME = "FPS Camera";
        private const string COMPASS_GLASS_NAME = "compas_glass_LOD0";

        private readonly bool debugInfo = false;

        private GameWorld _gameWorld;
        private Player _player;
        public bool inGame = false;

        public static RectTransform RadarBorderTransform { get; private set; }
        public static RectTransform RadarBaseTransform { get; private set; }
        public static GameObject RadarBase { get; private set; }

        private RectTransform _radarPulseTransform;

        private Coroutine _pulseCoroutine;
        private float _radarPulseInterval = 1f;

        private Vector3 _radarScaleStart;

        public static float RadarLastUpdateTime = 0;

        private readonly Dictionary<string, BlipPlayer> _enemyList = new Dictionary<string, BlipPlayer>();

        private readonly List<BlipLoot> _lootCustomObject = new List<BlipLoot>();
        private readonly HashSet<string> _lootInList = new HashSet<string>();
        private readonly Dictionary<string, Transform> _containerTransforms = new Dictionary<string, Transform>();
        private Quadtree _lootTree;
        private List<BlipLoot> _activeLootOnRadar;
        private readonly List<BlipLoot> _lootToHide = new List<BlipLoot>();

        private readonly HashSet<string> _containerSet = new HashSet<string>();

        private CustomLootList _customLoots;

        private bool _compassOn = false;
        private GameObject _compassGlass;
        private Canvas _radarCanvas;
        private Image _radarBorderImage;
        private Image _radarPulseImage;
        private Image _radarBackgroundImage;
        private Transform _radarBackgroundTransform;
        private GameObject _fpsCameraObject;

        // FPS Camera (this.transform.parent) -> RadarHUD (this.transform) -> RadarBaseTransform (transform.Find("Radar").transform) -> RadarBorderTransform
        private void Awake()
        {
            if (debugInfo)
                Debug.LogError("# Awake");

            if (!Singleton<GameWorld>.Instantiated)
            {
                Radar.Log.LogWarning("GameWorld singleton not found.");
                Destroy(gameObject);
                return;
            }

            _gameWorld = Singleton<GameWorld>.Instance;
            if (_gameWorld.MainPlayer == null)
            {
                Radar.Log.LogWarning("MainPlayer is null.");
                Destroy(gameObject);
                return;
            }

            _player = _gameWorld.MainPlayer;
            _fpsCameraObject = GameObject.Find(FPS_CAMERA_NAME);

            RadarBaseTransform = (transform.Find("Radar") as RectTransform)!;
            RadarBase = RadarBaseTransform.gameObject;
            _radarScaleStart = RadarBaseTransform.localScale;

            RadarBorderTransform = transform.Find("Radar/RadarBorder") as RectTransform;
            RadarBorderTransform.SetAsLastSibling();
            _radarBorderImage = RadarBorderTransform.GetComponent<Image>();
            _radarBorderImage.color = Radar.backgroundColor.Value;

            _radarPulseTransform = transform.Find("Radar/RadarPulse") as RectTransform;
            _radarPulseImage = _radarPulseTransform.GetComponent<Image>();
            _radarPulseImage.color = Radar.backgroundColor.Value;

            _radarBackgroundTransform = transform.Find("Radar/RadarBackground");
            _radarBackgroundImage = _radarBackgroundTransform.GetComponent<Image>();
            _radarBackgroundImage.color = Radar.backgroundColor.Value;

            _radarCanvas = GetComponentInChildren<Canvas>();

            //Debug.LogError($"& HUD: {this.transform.position} {this.transform.localPosition} {this.transform.rotation} {this.transform.localRotation} {this.transform.localScale}");
            //Debug.LogError($"& RBT: {RadarBaseTransform.position} {RadarBaseTransform.localPosition} {RadarBaseTransform.rotation} {RadarBaseTransform.localRotation} {RadarBaseTransform.localScale}");
            //Debug.LogError($"& BOD: {RadarBorderTransform.position} {RadarBorderTransform.localPosition} {RadarBorderTransform.rotation} {RadarBorderTransform.localRotation} {RadarBorderTransform.localScale}");
            
            // Load custom loot list
            LoadCustomLootList();
            ItemExtensions.Init(this);

            Radar.Log.LogInfo("Radar loaded");
        }

        private void LoadCustomLootList()
        {
            string filePath = Path.Combine(Application.dataPath, "..\\BepInEx\\plugins\\radar-list.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    _customLoots = JsonConvert.DeserializeObject<CustomLootList>(jsonContent)!;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read or parse settings file: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("Custom loot file not found.");
            }
            if (debugInfo)
                Debug.LogError("# Loaded list");
        }

        private void InitRadar()
        {
            if (Radar.radarEnableCompassConfig.Value)
                InitCompassRadar();
            else
                InitNormalRadar();

            if (Radar.radarEnableLootConfig.Value)
                UpdateLootList();
        }

        private void InitNormalRadar()
        {
            if (debugInfo)
                Debug.LogError("# InitNormalRadar");

            _compassGlass = null;
            if (_radarCanvas != null)
            {
                _radarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _radarCanvas.worldCamera = null;
            }

            transform.SetParent(_fpsCameraObject.transform);

            transform.rotation = Quaternion.identity;
            RadarBaseTransform.position = new Vector2(Radar.radarOffsetXConfig.Value, Radar.radarOffsetYConfig.Value);
            RadarBaseTransform.rotation = Quaternion.identity;
            RadarBaseTransform.localScale = _radarScaleStart * Radar.radarSizeConfig.Value;
            RadarBorderTransform.rotation = Quaternion.identity;
            RadarBase.SetActive(true);
        }

        public void SetCompassParent(bool enable)
        {
            transform.SetParent(enable && _compassGlass != null ? _compassGlass.transform : _fpsCameraObject.transform);
        }

        private void InitCompassRadar()
        {
            if (debugInfo)
                Debug.LogError("# InitCompassRadar");

            // Ensure the Canvas is set to World Space
            if (_radarCanvas != null)
            {
                _radarCanvas.renderMode = RenderMode.WorldSpace;
                _radarCanvas.worldCamera = Camera.main;
            }

            // Set the parent of RadarHUD
            SetCompassParent(true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            RadarBaseTransform.localPosition = new Vector3(0, 0, 0.001f);
            RadarBaseTransform.localRotation = Quaternion.Euler(0, -180, 0);
            RadarBaseTransform.localScale = Vector3.one * 0.000123f;
            RadarBorderTransform.localRotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            if (debugInfo)
                Debug.LogError("# OnEnable");

            InitRadar();
            Radar.Instance.Config.SettingChanged += UpdateRadarSettings;
            UpdateRadarSettings();
            inGame = true;
        }

        private void OnDisable()
        {
            if (debugInfo)
                Debug.LogError("# OnDisable");

            Radar.Instance.Config.SettingChanged -= UpdateRadarSettings;

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            inGame = false;
        }

        private void ClearLoot()
        {
            if (_lootTree != null)
            {
                _lootTree.Clear();
                _lootTree = null;
            }

            if (_lootCustomObject.Count > 0)
            {
                foreach (var loot in _lootCustomObject)
                    loot.DestoryBlip();

                _lootCustomObject.Clear();
            }

            if (_lootInList != null)
            {
                _lootInList.Clear();
            }
        }

        private void UpdateRadarSettings(object sender = null, SettingChangedEventArgs e = null)
        {
            if (!gameObject.activeInHierarchy) return; // Don't update if the radar object is disabled

            _radarPulseInterval = Mathf.Max(1f, Radar.radarScanInterval.Value);

            if (e == null || e.ChangedSetting == Radar.radarEnablePulseConfig)
                TogglePulseAnimation(Radar.radarEnablePulseConfig.Value);

            if (e != null && e.ChangedSetting == Radar.backgroundColor)
            {
                Color newColor = Radar.backgroundColor.Value;
                _radarBorderImage.color = newColor;
                _radarPulseImage.color = newColor;
                _radarBackgroundImage.color = newColor;
            }

            if (e != null && e.ChangedSetting == Radar.radarEnableCompassConfig)
                InitRadar();

            if (!Radar.radarEnableCompassConfig.Value)
            {
                if (e == null || e.ChangedSetting == Radar.radarOffsetXConfig || e.ChangedSetting == Radar.radarOffsetYConfig)
                {
                    RadarBaseTransform.position = new Vector2(Radar.radarOffsetXConfig.Value, Radar.radarOffsetYConfig.Value);
                }

                if (e == null || e.ChangedSetting == Radar.radarSizeConfig)
                    RadarBaseTransform.localScale = _radarScaleStart * Radar.radarSizeConfig.Value;
            }

            if (e != null && (e.ChangedSetting == Radar.radarEnableLootConfig || e.ChangedSetting == Radar.radarLootThreshold))
            {
                if (Radar.radarEnableLootConfig.Value)
                    UpdateLootList();
                else
                {
                    ClearLoot();
                }
            }
        }

        private void UpdateLootList()
        {
            ClearLoot();
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            var allItemOwner = _gameWorld.ItemOwners;

            // Process containers and filter out duplicates
            HashSet<Vector3> duplicatePositions = new HashSet<Vector3>();
            foreach (var item in allItemOwner.Reverse())
            {
                if (item.Key.RootItem.Name.StartsWith(PLAYER_INVENTORY_PREFIX) || item.Value.Transform == null)
                    continue;

                // Handle duplicates
                if (!item.Key.ContainerName.StartsWith(DRAWER_PREFIX) && !duplicatePositions.Add(item.Value.Transform.position))
                    continue;

                AddLoot(item.Key.ID, item.Key.Items.First(), item.Value.Transform);

                // Set up event handlers for containers
                if (item.Key.Items.First().IsContainer && _containerSet.Add(item.Key.ID))
                {
                    item.Key.RemoveItemEvent += (args) => OnContainerRemoveItemEvent(item.Key, args);
                    item.Key.AddItemEvent += (args) => OnContainerAddItemEvent(item.Key, args);
                    _containerTransforms[item.Key.ID] = item.Value.Transform;
                }

                // Track bounds for quadtree
                Vector3 pos = item.Value.Transform.position;
                xMin = Mathf.Min(xMin, pos.x);
                xMax = Mathf.Max(xMax, pos.x);
                yMin = Mathf.Min(yMin, pos.z);
                yMax = Mathf.Max(yMax, pos.z);
            }

            // Create quadtree with padding
            _lootTree = new Quadtree(Rect.MinMaxRect(xMin - 5, yMin - 2, xMax + 5, yMax + 2));
            foreach (BlipLoot loot in _lootCustomObject)
                _lootTree.Insert(loot);
        }

        private void OnContainerAddItemEvent(IItemOwner itemOwner, GEventArgs2 args)
        {
            // New item has high price && has not been added
            if (CheckPrice(args.Item) && !_lootInList.Contains(itemOwner.ID))
                AddLoot(itemOwner.ID, itemOwner.Items.First(), _containerTransforms[itemOwner.ID]);
            // New item is wanted && has not been added
            if (CheckCustom(args.Item) && !_lootInList.Contains(itemOwner.ID))
                AddLoot(itemOwner.ID, itemOwner.Items.First(), _containerTransforms[itemOwner.ID]);
        }

        private void OnContainerRemoveItemEvent(IItemOwner itemOwner, GEventArgs3 args)
        {
            if (CheckPrice(args.Item) && !CheckPrice(itemOwner.Items.First()))
                RemoveLoot(itemOwner.ID);
            if (CheckCustom(args.Item) && !CheckCustom(itemOwner.Items.First()))
                RemoveLoot(itemOwner.ID);
        }

        private bool CheckCustom(Item item)
        {
            // Is the item itself wanted, container or not?
            if (_customLoots?.items.Contains(item.TemplateId) ?? false)
                return true;

            if (item.IsContainer)
            {
                var allItems = item.GetAllItems();

                foreach (var subItem in allItems)
                {
                    if (_customLoots?.items.Contains(subItem.TemplateId) ?? false)
                        return true;
                }
                return false;
            }
            else
            {
                return _customLoots?.items.Contains(item.TemplateId) ?? false;
            }
        }

        private bool CheckPrice(Item item)
        {
            int highestPrice = 0;

            if (item.IsContainer)
            {
                var allItems = item.GetAllItems();

                // Then calculate highest price
                foreach (var subItem in allItems)
                {
                    int price = ItemExtensions.GetBestPrice(subItem);

                    if (Radar.radarLootPerSlotConfig.Value)
                    {
                        var cellSize = subItem.CalculateCellSize();
                        int slotCount = cellSize.X * cellSize.Y;
                        if (slotCount > 0) // Avoid division by zero
                            price /= slotCount;
                    }

                    highestPrice = Mathf.Max(highestPrice, price);
                }
            }
            else
            {
                highestPrice = ItemExtensions.GetBestPrice(item);
                if (Radar.radarLootPerSlotConfig.Value)
                {
                    var cellSize = item.CalculateCellSize();
                    int slotCount = cellSize.X * cellSize.Y;
                    if (slotCount > 0) // Avoid division by zero
                        highestPrice /= slotCount;
                }
            }

            return highestPrice > Radar.radarLootThreshold.Value;
        }

        public void AddLoot(string id, Item item, Transform transform, bool lazyUpdate = false)
        {
            //Debug.LogError($"AddLoot {item.IsContainer} {item.Name} {item.LocalizedName()} {transform.position}");
            bool isCustomItem = CheckCustom(item);
            bool isValuableItem = !item.Name.StartsWith(PLAYER_INVENTORY_PREFIX) && CheckPrice(item);

            if (debugInfo && isValuableItem)
            {
                Debug.LogError(item);
                Debug.LogError(item.TemplateId);
            }

            if (isCustomItem || isValuableItem)
            {
                
                var blip = new BlipLoot(id, transform, lazyUpdate);
                _lootCustomObject.Add(blip);
                _lootTree?.Insert(blip);
                _lootInList.Add(id);
            }
        }

        public void UpdateFireTime(string id)
        {
            if (_enemyList.ContainsKey(id))
                _enemyList[id].UpdateLastFireTime(Time.time);
        }

        public void RemoveLootByKey(int key)
        {
            LootItem item = _gameWorld.LootItems.GetByKey(key);
            RemoveLoot(item.ItemId);
        }

        public void RemoveLoot(string id)
        {
            Vector2 point = Vector2.zero;
            foreach (var loot in _lootCustomObject)
            {
                if (loot._id == id)
                {
                    point.x = loot.targetPosition.x;
                    point.y = loot.targetPosition.z;
                    loot.DestoryBlip();
                    _lootCustomObject.Remove(loot);
                    break;
                }
            }
            //Debug.LogError($"Remove Loot: {id}");
            _lootTree?.Remove(point, id);
            _lootInList.Remove(id);
        }

        private void TogglePulseAnimation(bool enable)
        {
            if (enable)
            {
                // always create a new coroutine
                if (_pulseCoroutine != null)
                    StopCoroutine(_pulseCoroutine);

                _pulseCoroutine = StartCoroutine(PulseCoroutine());
            }
            else if (_pulseCoroutine != null && !enable)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            _radarPulseTransform.gameObject.SetActive(enable);
        }

        private void Update()
        {
            if (_player == null) return;

            if (!Radar.radarEnableCompassConfig.Value)
                RadarBorderTransform.eulerAngles = new Vector3(0, 0, transform.parent.transform.eulerAngles.y);

            UpdateLoot();
            UpdateRadar(UpdateActivePlayer() != -1);

            if (Radar.radarEnableCompassConfig.Value)
            {
                Player.ItemHandsController? handsController = _player.HandsController as Player.ItemHandsController;
                var compassInHand = handsController != null && handsController.CurrentCompassState;
                if (compassInHand && !_compassOn)
                {
                    _compassOn = true;
                    RadarBase.SetActive(true);
                    SetCompassParent(true);
                }
                if (!compassInHand && _compassOn)
                {
                    _compassOn = false;
                    RadarBase.SetActive(false);
                    SetCompassParent(false);
                }

                if (_compassOn && _compassGlass == null)
                {
                    _compassGlass = GameObject.Find(COMPASS_GLASS_NAME);
                    if (_compassGlass != null && transform.parent != _compassGlass.transform)
                    {
                        transform.parent = _compassGlass.transform;
                        transform.localPosition = Vector3.zero;
                        transform.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        private IEnumerator PulseCoroutine()
        {
            while (true)
            {
                // Rotate from 360 to 0 over the animation duration
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime / _radarPulseInterval;
                    float angle = Mathf.Lerp(0f, 1f, 1 - t) * 360;

                    // Apply the scale to all axes
                    _radarPulseTransform.localEulerAngles = new Vector3(0, 0, angle);
                    yield return null;
                }
                // Pause for the specified duration
                // yield return new WaitForSeconds(interval);
            }
        }

        private long UpdateActivePlayer()
        {
            float interval = Radar.radarScanInterval.Value;
            if (Radar.radarEnableFireModeConfig.Value)
                interval = 0.1f;
                
            if (Time.time - RadarLastUpdateTime < interval)
                return -1;
            else
                RadarLastUpdateTime = Time.time;
            IEnumerable<Player> allPlayers = _gameWorld.AllPlayersEverExisted;

            if (allPlayers.Count() == _enemyList.Count + 1)
                return -2;

            foreach (Player enemyPlayer in allPlayers)
            {
                if (enemyPlayer == null || enemyPlayer == _player)
                    continue;

                if (!_enemyList.ContainsKey(enemyPlayer.ProfileId))
                    _enemyList.Add(enemyPlayer.ProfileId, new BlipPlayer(enemyPlayer));
            }
            return 0;
        }

        private void UpdateLoot()
        {
            if (Time.time - RadarLastUpdateTime < Radar.radarScanInterval.Value)
                return;

            if (Radar.radarListReloadRequested)
            {
                LoadCustomLootList();
                Radar.radarListReloadRequested = false;
                UpdateLootList();
            }

            Vector2 center = new Vector2(_player.Transform.position.x, _player.Transform.position.z);
            var latestActiveLootOnRadar = _lootTree?.QueryRange(center, Radar.radarOuterRangeConfig.Value);
            _lootToHide.Clear();
            if (_activeLootOnRadar != null)
            {
                foreach (var old in _activeLootOnRadar)
                {
                    if (latestActiveLootOnRadar == null || !latestActiveLootOnRadar.Contains(old))
                        _lootToHide.Add(old);
                }
            }

            _activeLootOnRadar?.Clear();
            _activeLootOnRadar = latestActiveLootOnRadar;
        }

        private void UpdateRadar(bool positionUpdate = true)
        {
            Target.setPlayerPosition(_player.Transform.position);
            Target.setRadarRange(Radar.radarInnerRangeConfig.Value, Radar.radarOuterRangeConfig.Value);
            foreach (var obj in _enemyList)
                obj.Value.Update(positionUpdate);

            foreach (var obj in _lootToHide)
                obj.Update(false);

            if (_activeLootOnRadar != null)
            {
                foreach (var obj in _activeLootOnRadar)
                    obj.Update(true);
            }
        }
    }
}
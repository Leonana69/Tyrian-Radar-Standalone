using EFT;
using System.Collections.Generic;
using Comfort.Common;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Radar.Patches;
using UnityEditor;
using UnityEngine;

namespace Radar
{
    [BepInPlugin("Tyrian.Radar", "Radar", "1.1.10")]
    public class Radar : BaseUnityPlugin
    {
        internal static Radar Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();

        const string baseSettings = "Base Settings";
        const string advancedSettings = "Advanced Settings";
        const string colorSettings = "Color Settings";
        const string radarSettings = "Radar Settings";

        public static bool radarListReloadRequested = false;

        public static ConfigEntry<string> radarLanguage;
        public static ConfigEntry<bool> radarEnableConfig;
        public static ConfigEntry<bool> radarEnablePulseConfig;
        public static ConfigEntry<bool> radarEnableCorpseConfig;
        public static ConfigEntry<bool> radarEnableLootConfig;
        public static ConfigEntry<bool> radarEnableFireModeConfig;
        public static ConfigEntry<bool> radarEnableCompassConfig;
        public static ConfigEntry<KeyboardShortcut> radarReloadListShortCutConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableShortCutConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableCorpseShortCutConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableLootShortCutConfig;

        public static ConfigEntry<float> radarSizeConfig;
        public static ConfigEntry<float> radarBlipSizeConfig;
        public static ConfigEntry<float> radarDistanceScaleConfig;
        public static ConfigEntry<float> radarYHeightThreshold;
        public static ConfigEntry<int> radarOffsetYConfig;
        public static ConfigEntry<int> radarOffsetXConfig;
        public static ConfigEntry<int> radarOuterRangeConfig;
        public static ConfigEntry<int> radarInnerRangeConfig;
        public static ConfigEntry<float> radarScanInterval;
        public static ConfigEntry<int> radarLootThreshold;
        public static ConfigEntry<bool> radarLootPerSlotConfig;

        public static ConfigEntry<Color> bossBlipColor;
        public static ConfigEntry<Color> usecBlipColor;
        public static ConfigEntry<Color> bearBlipColor;
        public static ConfigEntry<Color> scavBlipColor;
        public static ConfigEntry<Color> corpseBlipColor;
        public static ConfigEntry<Color> lootBlipColor;
        public static ConfigEntry<Color> backgroundColor;

        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Radar Plugin Enabled.");
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Add a custom configuration option for the Apply button
            radarLanguage = Config.Bind<string>(baseSettings, "Language", "EN",
                new ConfigDescription("Preferred language, if not available will tried English",
                new AcceptableValueList<string>("EN", "ZH", "RU", "KO")));

            radarEnableConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable"), true);
            radarReloadListShortCutConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_reload_list_shortcut"), new KeyboardShortcut(KeyCode.F8));
            radarEnableShortCutConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable_shortcut"), new KeyboardShortcut(KeyCode.F10));
            radarEnablePulseConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_pulse_enable"), true, Locales.GetTranslatedString("radar_pulse_enable_info"));
            radarEnableFireModeConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_fire_mode_enable"), false, Locales.GetTranslatedString("radar_fire_mode_enable_info"));
            radarEnableCompassConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_compass_enable"), false, Locales.GetTranslatedString("radar_compass_enable_info"));

            radarEnableCorpseConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_enable"), false);
            radarEnableCorpseShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_shortcut"), new KeyboardShortcut(KeyCode.F11));
            radarEnableLootConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_enable"), false);
            radarEnableLootShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_shortcut"), new KeyboardShortcut(KeyCode.F9));
            radarLootPerSlotConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_per_slot"), false);
            radarSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_hud_size"), 0.8f,
                new ConfigDescription(Locales.GetTranslatedString("radar_hud_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarBlipSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_blip_size"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_blip_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarDistanceScaleConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_distance_scale"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_distance_scale_info"), new AcceptableValueRange<float>(0.1f, 2f)));
            radarYHeightThreshold = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_y_height_threshold"), 1f,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_height_threshold_info"), new AcceptableValueRange<float>(1f, 4f)));
            radarOffsetXConfig = Config.Bind<int>(radarSettings, Locales.GetTranslatedString("radar_x_position"), 300,
                new ConfigDescription(Locales.GetTranslatedString("radar_x_position_info"), new AcceptableValueRange<int>(0, 4000)));
            radarOffsetYConfig = Config.Bind<int>(radarSettings, Locales.GetTranslatedString("radar_y_position"), 150,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_position_info"), new AcceptableValueRange<int>(0, 3000)));
            radarOuterRangeConfig = Config.Bind<int>(radarSettings, Locales.GetTranslatedString("radar_outer_range"), 128,
                new ConfigDescription(Locales.GetTranslatedString("radar_outer_range_info"), new AcceptableValueRange<int>(32, 1024)));
            radarInnerRangeConfig = Config.Bind<int>(radarSettings, Locales.GetTranslatedString("radar_inner_range"), 0,
                new ConfigDescription(Locales.GetTranslatedString("radar_inner_range_info"), new AcceptableValueRange<int>(0, 64)));
            radarScanInterval = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_scan_interval"), 1f,
                new ConfigDescription(Locales.GetTranslatedString("radar_scan_interval_info"), new AcceptableValueRange<float>(0.1f, 30f)));
            radarLootThreshold = Config.Bind<int>(radarSettings, Locales.GetTranslatedString("radar_loot_threshold"), 30000,
                new ConfigDescription(Locales.GetTranslatedString("radar_loot_threshold_info"), new AcceptableValueRange<int>(5000, 200000)));

            bossBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_boss_blip_color"), new Color(1f, 0f, 0f));
            scavBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_scav_blip_color"), new Color(0f, 1f, 0f));
            usecBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_usec_blip_color"), new Color(1f, 1f, 0f));
            bearBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_bear_blip_color"), new Color(1f, 0.5f, 0f));
            corpseBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_corpse_blip_color"), new Color(0.5f, 0.5f, 0.5f));
            lootBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_loot_blip_color"), new Color(0.9f, 0.5f, 0.5f));
            backgroundColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_background_blip_color"), new Color(0f, 0.7f, 0.85f));

            AssetBundleManager.LoadAssetBundle();

            new GameStartPatch().Enable();
        }
    }
}
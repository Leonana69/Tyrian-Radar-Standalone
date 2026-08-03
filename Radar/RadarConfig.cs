using BepInEx.Configuration;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Every BepInEx setting the radar exposes, grouped by the section it appears under in the F12 menu.
    /// </summary>
    /// <remarks>
    /// Section names and key strings are the on-disk config format. Changing one silently resets that
    /// setting for every existing user, so they are kept verbatim - only the C# identifiers are ours.
    /// Keys are looked up through <see cref="Locales"/>, which means <see cref="Language"/> has to be
    /// bound before anything else.
    /// </remarks>
    internal static class RadarConfig
    {
        private const string BaseSection = "Base Settings";
        private const string AdvancedSection = "Advanced Settings";
        private const string ColorSection = "Color Settings";
        private const string UiSection = "UI Settings";

        // Base
        public static ConfigEntry<string> Language { get; private set; } = null!;
        public static ConfigEntry<bool> Enabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleRadarKey { get; private set; } = null!;
        public static ConfigEntry<bool> PulseEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> FireModeEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> CompassEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> MinefieldEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> ExfilEnabled { get; private set; } = null!;

        // Advanced
        public static ConfigEntry<bool> CorpseEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> CorpseTypeEnabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleCorpseKey { get; private set; } = null!;
        public static ConfigEntry<bool> LootEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> WishlistLootEnabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleLootKey { get; private set; } = null!;
        public static ConfigEntry<bool> LootValuePerSlot { get; private set; } = null!;

        // UI
        public static ConfigEntry<float> HudSize { get; private set; } = null!;
        public static ConfigEntry<float> BlipSize { get; private set; } = null!;
        public static ConfigEntry<float> DistanceScale { get; private set; } = null!;
        public static ConfigEntry<float> HeightThreshold { get; private set; } = null!;
        public static ConfigEntry<int> OffsetX { get; private set; } = null!;
        public static ConfigEntry<int> OffsetY { get; private set; } = null!;
        public static ConfigEntry<int> OuterRange { get; private set; } = null!;
        public static ConfigEntry<int> InnerRange { get; private set; } = null!;
        public static ConfigEntry<float> ScanInterval { get; private set; } = null!;
        public static ConfigEntry<int> LootThreshold { get; private set; } = null!;

        // Colors
        public static ConfigEntry<Color> BossColor { get; private set; } = null!;
        public static ConfigEntry<Color> ScavColor { get; private set; } = null!;
        public static ConfigEntry<Color> UsecColor { get; private set; } = null!;
        public static ConfigEntry<Color> BearColor { get; private set; } = null!;
        public static ConfigEntry<Color> LootColor { get; private set; } = null!;
        public static ConfigEntry<Color> WishlistLootColor { get; private set; } = null!;
        public static ConfigEntry<Color> CorpseColor { get; private set; } = null!;
        public static ConfigEntry<Color> BackgroundColor { get; private set; } = null!;
        public static ConfigEntry<Color> MinefieldColor { get; private set; } = null!;

        public static void Bind(ConfigFile config)
        {
            // Must come first: every key below is resolved through the selected language.
            Language = config.Bind(BaseSection, "Language", "EN",
                new ConfigDescription("Preferred language, if not available will tried English",
                    new AcceptableValueList<string>("EN", "ZH", "RU", "KO")));

            Enabled = config.Bind(BaseSection, Key("radar_enable"), true);
            ToggleRadarKey = config.Bind(BaseSection, Key("radar_enable_shortcut"), new KeyboardShortcut(KeyCode.F10));
            PulseEnabled = config.Bind(BaseSection, Key("radar_pulse_enable"), true, Key("radar_pulse_enable_info"));
            FireModeEnabled = config.Bind(BaseSection, Key("radar_fire_mode_enable"), false, Key("radar_fire_mode_enable_info"));
            CompassEnabled = config.Bind(BaseSection, Key("radar_compass_enable"), false, Key("radar_compass_enable_info"));
            MinefieldEnabled = config.Bind(BaseSection, Key("radar_minefield_enable"), false, Key("radar_minefield_enable_info"));
            ExfilEnabled = config.Bind(BaseSection, Key("radar_exfil_enable"), false, Key("radar_exfil_enable_info"));

            CorpseEnabled = config.Bind(AdvancedSection, Key("radar_corpse_enable"), false);
            CorpseTypeEnabled = config.Bind(AdvancedSection, Key("radar_corpse_type_enable"), false);
            ToggleCorpseKey = config.Bind(AdvancedSection, Key("radar_corpse_shortcut"), new KeyboardShortcut(KeyCode.F11));
            LootEnabled = config.Bind(AdvancedSection, Key("radar_loot_enable"), false);
            WishlistLootEnabled = config.Bind(AdvancedSection, Key("radar_wishlist_enable"), false);
            ToggleLootKey = config.Bind(AdvancedSection, Key("radar_loot_shortcut"), new KeyboardShortcut(KeyCode.F9));
            LootValuePerSlot = config.Bind(AdvancedSection, Key("radar_loot_per_slot"), false);

            HudSize = config.Bind(UiSection, Key("radar_hud_size"), 0.8f,
                new ConfigDescription(Key("radar_hud_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            BlipSize = config.Bind(UiSection, Key("radar_blip_size"), 0.7f,
                new ConfigDescription(Key("radar_blip_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            DistanceScale = config.Bind(UiSection, Key("radar_distance_scale"), 0.7f,
                new ConfigDescription(Key("radar_distance_scale_info"), new AcceptableValueRange<float>(0.1f, 2f)));
            HeightThreshold = config.Bind(UiSection, Key("radar_y_height_threshold"), 1f,
                new ConfigDescription(Key("radar_y_height_threshold_info"), new AcceptableValueRange<float>(1f, 4f)));
            OffsetX = config.Bind(UiSection, Key("radar_x_position"), 300,
                new ConfigDescription(Key("radar_x_position_info"), new AcceptableValueRange<int>(0, 4000)));
            OffsetY = config.Bind(UiSection, Key("radar_y_position"), 150,
                new ConfigDescription(Key("radar_y_position_info"), new AcceptableValueRange<int>(0, 3000)));
            OuterRange = config.Bind(UiSection, Key("radar_outer_range"), 128,
                new ConfigDescription(Key("radar_outer_range_info"), new AcceptableValueRange<int>(32, 1024)));
            InnerRange = config.Bind(UiSection, Key("radar_inner_range"), 0,
                new ConfigDescription(Key("radar_inner_range_info"), new AcceptableValueRange<int>(0, 64)));
            ScanInterval = config.Bind(UiSection, Key("radar_scan_interval"), 1f,
                new ConfigDescription(Key("radar_scan_interval_info"), new AcceptableValueRange<float>(0.1f, 30f)));
            LootThreshold = config.Bind(UiSection, Key("radar_loot_threshold"), 30000,
                new ConfigDescription(Key("radar_loot_threshold_info"), new AcceptableValueRange<int>(5000, 200000)));

            BossColor = config.Bind(ColorSection, Key("radar_boss_blip_color"), new Color(1f, 0f, 0f));
            ScavColor = config.Bind(ColorSection, Key("radar_scav_blip_color"), new Color(0f, 1f, 0f));
            UsecColor = config.Bind(ColorSection, Key("radar_usec_blip_color"), new Color(1f, 1f, 0f));
            BearColor = config.Bind(ColorSection, Key("radar_bear_blip_color"), new Color(1f, 0.5f, 0f));
            LootColor = config.Bind(ColorSection, Key("radar_loot_blip_color"), new Color(0.9f, 0.5f, 0.5f));
            WishlistLootColor = config.Bind(ColorSection, Key("radar_wishlist_loot_blip_color"), new Color(0.9f, 0.8f, 0.5f));
            CorpseColor = config.Bind(ColorSection, Key("radar_corpse_blip_color"), new Color(0.5f, 0.5f, 0.5f));
            BackgroundColor = config.Bind(ColorSection, Key("radar_background_color"), new Color(0f, 0.7f, 0.85f));
            MinefieldColor = config.Bind(ColorSection, Key("radar_minefield_color"), new Color(0.7f, 0.7f, 0.7f, 0.3f));
        }

        private static string Key(string id) => Locales.GetTranslatedString(id);
    }
}

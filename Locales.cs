using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Radar
{
    public enum Language
    {
        [Description("English")] English,
        [Description("简体中文")] SimplifiedChinese
    }

    public abstract class LanguageList
    {
        private static readonly LanguageInfo English = new(Language.English, "en");

        private static readonly LanguageInfo SimplifiedChinese = new(Language.SimplifiedChinese, "zh-CN");

        private static readonly LanguageInfo[] Languages =
        {
            English, SimplifiedChinese
        };

        public static readonly LanguageInfo Default = English;

        public class LanguageInfo
        {
            public string CultureName { get; }

            public Language Language { get; }

            internal LanguageInfo(Language language, string cultureName)
            {
                Language = language;
                CultureName = cultureName;
            }
        }

        public static Language ByCultureName(string cultureName)
        {
            try
            {
                return Languages.Single(language => string.Equals(language.CultureName, cultureName,
                    StringComparison.CurrentCultureIgnoreCase)).Language;
            }
            catch (Exception)
            {
                return Default.Language;
            }
        }
    }

    internal abstract class Locales
    {
        private static readonly Dictionary<Language, Dictionary<string, string>> Translations = new()
        {
            {
                Language.English, new Dictionary<string, string>
                {
                    { "radar_enable", "Radar Enabled" },
                    { "radar_enable_shortcut", "Short cut for enable/disable radar" },
                    { "radar_pulse_enable", "Radar Pulse Enabled" },
                    { "radar_pulse_enable_info", "Adds the radar scan effect." },
                    { "radar_corpse_enable", "Corpse Detection Enabled" },
                    { "radar_corpse_shortcut", "Short cut for enable/disable corpse dection" },
                    { "radar_loot_enable", "Loot Detection Enabled" },
                    { "radar_loot_shortcut", "Short cut for enable/disable loot dection" },
                    { "radar_hud_size", "HUD Size" },
                    { "radar_hud_size_info", "The size of the radar Hud." },
                    { "radar_blip_size", "HUD Blip Size" },
                    { "radar_blip_size_info", "The size of the blip." },
                    { "radar_distance_scale", "HUD Blip Disntance Scale Offset" },
                    {
                        "radar_distance_scale_info",
                        "This scales the blips distances from the player, effectively zooming it in and out."
                    },
                    { "radar_y_height_threshold", "HUD Blip Height Threshold" },
                    {
                        "radar_y_height_threshold_info",
                        "This distance threshold decides blips turning into up or down arrows depending on enemies height levels."
                    },
                    { "radar_x_position", "HUD X Position Offset" },
                    { "radar_x_position_info", "X Position for the Radar Hud." },
                    { "radar_y_position", "HUD Y Position Offset" },
                    { "radar_y_position_info", "Y Position for the Radar Hud." },
                    { "radar_range", "Radar Range" },
                    { "radar_range_info", "The range within which enemies and loots are displayed on the radar." },
                    { "radar_scan_interval", "Radar Scan Interval" },
                    { "radar_scan_interval_info", "The interval between two scans." },
                    { "radar_loot_threshold", "Loot Threshold" },
                    { "radar_loot_threshold_info", "Item above this price will show on radar." },
                    { "radar_boss_blip_color", "Boss Color" },
                    { "radar_scav_blip_color", "SCAV Color" },
                    { "radar_usec_blip_color", "USEC Color" },
                    { "radar_bear_blip_color", "BEAR Color" },
                    { "radar_corpse_blip_color", "Corpse Color" },
                    { "radar_loot_blip_color", "Loot Color" },
                    { "radar_background_blip_color", "Background Color" },
                }
            },
            {
                Language.SimplifiedChinese, new Dictionary<string, string>
                {
                    { "radar_enable", "开启雷达" },
                    { "radar_enable_shortcut", "雷达开启热键" },
                    { "radar_pulse_enable", "开启雷达扫描动画" },
                    { "radar_pulse_enable_info", "增加转圈扫描效果" },
                    { "radar_corpse_enable", "尸体位置显示" },
                    { "radar_corpse_shortcut", "尸体显示热键" },
                    { "radar_loot_enable", "高价值物品显示" },
                    { "radar_loot_shortcut", "物品显示热键" },
                    { "radar_hud_size", "雷达界面大小" },
                    { "radar_hud_size_info", "雷达界面的大小" },
                    { "radar_blip_size", "目标点大小" },
                    { "radar_blip_size_info", "目标点大小" },
                    { "radar_distance_scale", "目标点距离缩放" },
                    { "radar_distance_scale_info", "调整近处与远处目标在雷达上的分布位置" },
                    { "radar_y_height_threshold", "目标点高度变化阈值" },
                    { "radar_y_height_threshold_info", "目标点与玩家的高度差超过该值会显示为箭头" },
                    { "radar_x_position", "雷达X" },
                    { "radar_x_position_info", "雷达界面X位置" },
                    { "radar_y_position", "雷达Y" },
                    { "radar_y_position_info", "雷达界面Y位置" },
                    { "radar_range", "雷达范围" },
                    { "radar_range_info", "在该范围内的目标会显示在雷达上" },
                    { "radar_scan_interval", "雷达扫描间隔" },
                    { "radar_scan_interval_info", "两次扫描的时间间隔" },
                    { "radar_loot_threshold", "物品阈值" },
                    { "radar_loot_threshold_info", "物品高于此售卖价格会显示在雷达上" },
                    { "radar_boss_blip_color", "Boss颜色" },
                    { "radar_scav_blip_color", "SCAV颜色" },
                    { "radar_usec_blip_color", "USEC颜色" },
                    { "radar_bear_blip_color", "BEAR颜色" },
                    { "radar_corpse_blip_color", "尸体颜色" },
                    { "radar_loot_blip_color", "物品颜色" },
                    { "radar_background_blip_color", "背景颜色" },
                }
            }
        };

        public static string GetTranslatedString(string key)
        {
            // Default to English if the selected language is not found
            var language = Radar.radarLanguage.Value;
            if (!Translations.ContainsKey(language))
            {
                language = LanguageList.Default.Language;
            }

            // Default to the original English text if the translation is not found
            return Translations[language].ContainsKey(key)
                ? Translations[language][key]
                : Translations[LanguageList.Default.Language][key];
        }
    }
}
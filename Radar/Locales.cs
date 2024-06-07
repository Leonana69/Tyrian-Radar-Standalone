using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radar
{
    internal class Locales
    {
        private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
{
            {"EN", new Dictionary<string, string>{
                {"radar_enable","Radar Enabled"},
                {"radar_enable_shortcut","Short cut for enable/disable radar"},
                {"radar_pulse_enable","Radar Pulse Enabled"},
                {"radar_pulse_enable_info","Adds the radar scan effect"},
                {"radar_corpse_enable","Corpse Detection Enabled"},
                {"radar_scav_corpse_enable", "Scav Corpse Detection Enabled"},
                {"radar_corpse_shortcut","Short cut for enable/disable corpse dection"},
                {"radar_loot_enable","Loot Detection Enabled"},
                {"radar_loot_shortcut","Short cut for enable/disable loot dection"},
                {"radar_fire_mode_enable","Fire Mode"},
                {"radar_fire_mode_enable_info","Only show fired enemy on radar"},
                {"radar_compass_enable","Compass Mode"},
                {"radar_compass_enable_info","Show radar on the compass"},
                {"radar_hud_size","HUD Size"},
                {"radar_hud_size_info","The size of the radar Hud"},
                {"radar_blip_size","HUD Blip Size"},
                {"radar_blip_size_info","The size of the blip"},
                {"radar_distance_scale","HUD Blip Disntance Scale Offset"},
                {"radar_distance_scale_info","This scales the blips distances from the player, effectively zooming it in and out"},
                {"radar_y_height_threshold","HUD Blip Height Threshold"},
                {"radar_y_height_threshold_info","This distance threshold decides blips turning into up or down arrows depending on enemies height levels"},
                {"radar_x_position","HUD X Position Offset"},
                {"radar_x_position_info","X Position for the Radar Hud"},
                {"radar_y_position","HUD Y Position Offset"},
                {"radar_y_position_info","Y Position for the Radar Hud"},
                {"radar_outer_range","Radar Outer Range"},
                {"radar_outer_range_info","The range within which enemies and loots are displayed on the radar"},
                {"radar_inner_range","Radar Inner Range"},
                {"radar_inner_range_info","The range outside which enemies are displayed on the radar"},
                {"radar_scan_interval","Radar Scan Interval"},
                {"radar_scan_interval_info","The interval between two scans"},
                {"radar_loot_threshold","Loot Threshold"},
                {"radar_loot_threshold_info","Item above this price will show on radar"},
                {"radar_loot_per_slot", "Per slot value"},
                {"radar_boss_blip_color","Boss Color"},
                {"radar_scav_blip_color","SCAV Color"},
                {"radar_usec_blip_color","USEC Color"},
                {"radar_bear_blip_color","BEAR Color"},
                {"radar_corpse_blip_color","Corpse Color"},
                {"radar_loot_blip_color","Loot Color"},
                {"radar_background_blip_color","Background Color"},
            }},
            {"ZH", new Dictionary<string, string>{
                {"radar_enable", "开启雷达"},
                {"radar_enable_shortcut", "雷达开启热键"},
                {"radar_pulse_enable", "开启雷达扫描动画"},
                {"radar_pulse_enable_info", "增加转圈扫描效果"},
                {"radar_corpse_enable", "尸体位置显示"},
                {"radar_scav_corpse_enable", "SCAV尸体位置显示"},
                {"radar_corpse_shortcut", "尸体显示热键"},
                {"radar_loot_enable", "高价值物品显示"},
                {"radar_loot_shortcut", "物品显示热键"},
                {"radar_fire_mode_enable","开火模式"},
                {"radar_fire_mode_enable_info","只显示开火的敌人"},
                {"radar_compass_enable","罗盘模式"},
                {"radar_compass_enable_info","雷达显示在罗盘上"},
                {"radar_hud_size", "雷达界面大小"},
                {"radar_hud_size_info", "雷达界面的大小"},
                {"radar_blip_size", "目标点大小"},
                {"radar_blip_size_info", "目标点大小"},
                {"radar_distance_scale", "目标点距离缩放"},
                {"radar_distance_scale_info", "调整近处与远处目标在雷达上的分布位置"},
                {"radar_y_height_threshold", "目标点高度变化阈值"},
                {"radar_y_height_threshold_info", "目标点与玩家的高度差超过该值会显示为箭头"},
                {"radar_x_position", "雷达X"},
                {"radar_x_position_info", "雷达界面X位置"},
                {"radar_y_position", "雷达Y"},
                {"radar_y_position_info", "雷达界面Y位置"},
                {"radar_outer_range", "雷达范围 (外)"},
                {"radar_outer_range_info", "在该范围内的目标会显示在雷达上"},
                {"radar_inner_range", "雷达范围 (内)"},
                {"radar_inner_range_info", "在该范围内的目标会隐藏"},
                {"radar_scan_interval", "雷达扫描间隔"},
                {"radar_scan_interval_info", "两次扫描的时间间隔"},
                {"radar_loot_threshold", "物品阈值"},
                {"radar_loot_threshold_info", "物品高于此售卖价格会显示在雷达上"},
                {"radar_loot_per_slot", "按每格价值"},
                {"radar_boss_blip_color", "Boss颜色"},
                {"radar_scav_blip_color", "SCAV颜色"},
                {"radar_usec_blip_color", "USEC颜色"},
                {"radar_bear_blip_color", "BEAR颜色"},
                {"radar_corpse_blip_color", "尸体颜色"},
                {"radar_loot_blip_color", "物品颜色"},
                {"radar_background_blip_color", "背景颜色"},
            }},
            {"RU", new Dictionary<string, string>{
                {"radar_enable", "Радар"},
                {"radar_enable_shortcut","Кнопка для включения/выключения радара"},
                {"radar_pulse_enable", "Радарный импульс"},
                {"radar_pulse_enable_info","Добавляет эффект радарного сканирования."},
                {"radar_corpse_enable", "Обнаружение трупов"},
                {"radar_scav_corpse_enable", "Обнаружение трупов диких"},
                {"radar_corpse_shortcut","Кнопка для включения/выключения обнаружения трупа"},
                {"radar_loot_enable", "Обнаружение добычи"},
                {"radar_loot_shortcut","Кнопка для включения/отключения обнаружения добычи"},
                {"radar_fire_mode_enable","Режим стрельбы"},
                {"radar_fire_mode_enable_info","Показывать на радаре только атакующих врагов"},
                {"radar_compass_enable","Режим Компаса"},
                {"radar_compass_enable_info","Показывать радар на компасе"},
                {"radar_hud_size", "Размер HUD"},
                {"radar_hud_size_info","Размер радара Hud."},
                {"radar_blip_size", "Размер индикатора HUD"},
                {"radar_blip_size_info","Размер метки."},
                {"radar_distance_scale", "Смещение шкалы расстояния для меток HUD"},
                {"radar_distance_scale_info","Это масштабирует расстояние до игрока, эффективно увеличивая и уменьшая масштаб."},
                {"radar_y_height_threshold", "Порог высоты индикатора HUD"},
                {"radar_y_height_threshold_info","Этот порог расстояния определяет, что метки превращаются в стрелки вверх или вниз в зависимости от уровня роста врагов."},
                {"radar_x_position", "Смещение позиции X HUD"},
                {"radar_x_position_info", "Позиция X для радара."},
                {"radar_y_position", "Смещение позиции HUD по оси Y"},
                {"radar_y_position_info", "Положение Y для радара."},
                {"radar_outer_range", "Внешний диапазон радара"},
                {"radar_outer_range_info", "Диапазон, в пределах которого враги и добыча отображаются на радаре."},
                {"radar_inner_range", "Внутренний диапазон радара"},
                {"radar_inner_range_info", "Диапазон, за пределами которого враги отображаются на радаре."},
                {"radar_scan_interval", "Интервал сканирования радара"},
                {"radar_scan_interval_info","Интервал между двумя сканированиями."},
                {"radar_loot_threshold", "Порог добычи"},
                {"radar_loot_threshold_info", "Товар стоимостью выше этой будет отображаться на радаре."},
                {"radar_loot_per_slot", "Значение каждого слота"},
                {"radar_boss_blip_color", "Цвет босса"},
                {"radar_scav_blip_color", "Цвет SCAV"},
                {"radar_usec_blip_color", "Цвет USEC"},
                {"radar_bear_blip_color", "Цвет МЕДВЕДЯ"},
                {"radar_corpse_blip_color", "Цвет трупа"},
                {"radar_loot_blip_color", "Цвет добычи"},
                {"radar_background_blip_color", "Цвет фона"},
            }},
        };

        public static string GetTranslatedString(string key)
        {
            // Default to English if the selected language is not found
            string lang = Radar.radarLanguage.Value;
            if (!translations.ContainsKey(lang))
            {
                lang = "EN";
            }

            // Default to the original English text if the translation is not found
            if (translations[lang].ContainsKey(key))
            {
                return translations[lang][key];
            }
            else
            {
                return translations["EN"][key];
            }
        }
    }
}

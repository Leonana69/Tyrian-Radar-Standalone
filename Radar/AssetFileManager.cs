using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Loads the radar's prefabs and blip sprites out of the assembly's embedded resources.
    /// </summary>
    /// <remarks>
    /// Every blip set is a triple: level, above the player, below the player.
    /// </remarks>
    internal static class AssetFileManager
    {
        private const string ResourcePrefix = "Radar.bundle.";
        private const string BundleResource = ResourcePrefix + "radarhud.bundle";
        private const string HudPrefabPath = "Assets/Examples/Halo Reach/Hud/RadarHUD.prefab";
        private const string BlipPrefabPath = "Assets/Examples/Halo Reach/Hud/RadarBlipHUD.prefab";

        public static GameObject RadarHudPrefab { get; private set; } = null!;
        public static GameObject RadarBlipHudPrefab { get; private set; } = null!;

        public static Sprite[] NormalEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] BossEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] DeadEnemyBlips { get; private set; } = new Sprite[3];
        public static Sprite[] LootBlips { get; private set; } = new Sprite[3];
        public static Sprite[] BtrBlips { get; private set; } = new Sprite[3];
        public static Sprite[] MineBlips { get; private set; } = new Sprite[3];
        public static Sprite[] ExfiltrationBlips { get; private set; } = new Sprite[3];

        internal static bool Loaded { get; private set; }

        internal static void Load()
        {
            if (Loaded) return;

            using Stream? bundleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BundleResource);
            if (bundleStream == null)
            {
                RadarPlugin.Log.LogError($"Missing embedded AssetBundle: {BundleResource}");
                return;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(bundleStream);
                RadarHudPrefab = bundle.LoadAsset<GameObject>(HudPrefabPath)!;
                RadarBlipHudPrefab = bundle.LoadAsset<GameObject>(BlipPrefabPath)!;

                NormalEnemyBlips = LoadBlipSet("normal_enemy");
                BossEnemyBlips = LoadBlipSet("boss_enemy");
                DeadEnemyBlips = LoadBlipSet("dead_enemy");
                LootBlips = LoadBlipSet("loot");
                BtrBlips = LoadBlipSet("btr");
                MineBlips = LoadBlipSet("mine");
                ExfiltrationBlips = LoadBlipSet("exfiltraction");

                Loaded = true;
                RadarPlugin.Log.LogInfo("Radar assets loaded.");
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"Error loading assets: {e}");
            }
        }

        private static Sprite[] LoadBlipSet(string name) => new[]
        {
            LoadSprite($"{name}.png"),
            LoadSprite($"{name}_up.png"),
            LoadSprite($"{name}_down.png"),
        };

        private static Sprite LoadSprite(string fileName)
        {
            string resourceName = ResourcePrefix + fileName;

            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                RadarPlugin.Log.LogError($"Missing embedded resource: {resourceName}");
                return null!;
            }

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, buffer.ToArray(), true))
            {
                RadarPlugin.Log.LogError($"Failed to decode PNG resource: {resourceName}");
                return null!;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

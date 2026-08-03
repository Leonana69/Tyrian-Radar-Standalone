using EFT;
using EFT.Interactive;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Draws individual mines as blips and each minefield's footprint as a shaded region.
    /// </summary>
    internal sealed class MinefieldTracker
    {
        private const BindingFlags InstanceFields =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // Declared on BorderZone: protected up to 4.0.0, public from 4.1.1 - hence both binding flags.
        private static readonly FieldInfo? TriggerZoneSettingsField =
            typeof(BorderZone).GetField("_triggerZoneSettings", InstanceFields);

        private static readonly FieldInfo? ExtentsField =
            typeof(BorderZone).GetField("_extents", InstanceFields);

        private readonly GameWorld _gameWorld;
        private readonly List<BlipOther> _blips = new List<BlipOther>();
        private readonly List<RadarRegion> _regions = new List<RadarRegion>();

        public MinefieldTracker(GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
        }

        public void Rebuild()
        {
            Clear();

            if (!RadarConfig.MinefieldEnabled.Value)
                return;

            foreach (var mine in _gameWorld.MineManager.Mines)
                _blips.Add(new BlipOther(mine.GetInstanceID().ToString(), mine.transform, false, BlipKind.Mine));

            if (TriggerZoneSettingsField == null || ExtentsField == null)
            {
                // Individual mines still show; only the shaded footprint is lost.
                RadarPlugin.Log.LogWarning(
                    "BorderZone._triggerZoneSettings / _extents not found - minefield outlines disabled.");
                return;
            }

            foreach (Minefield zone in LocationScene.GetAllObjects<Minefield>())
                _regions.Add(new RadarRegion(BuildCorners(zone)));
        }

        public void Clear()
        {
            foreach (RadarRegion region in _regions)
                region.Destroy();

            foreach (BlipOther blip in _blips)
                blip.DestroyBlip();

            _regions.Clear();
            _blips.Clear();
        }

        public void Render()
        {
            foreach (BlipOther blip in _blips)
                blip.Update();

            foreach (RadarRegion region in _regions)
                region.UpdateVisual();
        }

        /// <summary>
        /// Projects a zone's local box onto the ground plane as four world-space corners.
        /// </summary>
        /// <remarks>
        /// _extents is the half-size of the zone box; _triggerZoneSettings holds a per-edge inset in
        /// world units - x/y trim the +X/-X edges, z/w trim the +Z/-Z edges - which is what makes the
        /// lethal area smaller than the collider.
        /// </remarks>
        private static Vector3[] BuildCorners(BorderZone zone)
        {
            var triggerZoneSettings = (Vector4)TriggerZoneSettingsField!.GetValue(zone);
            var extents = (Vector3)ExtentsField!.GetValue(zone);

            Transform transform = zone.transform;
            Vector3 scale = transform.lossyScale;

            float minX = -extents.x + triggerZoneSettings.y / scale.x;
            float maxX = extents.x - triggerZoneSettings.x / scale.x;
            float minZ = -extents.z + triggerZoneSettings.w / scale.z;
            float maxZ = extents.z - triggerZoneSettings.z / scale.z;

            return new[]
            {
                transform.TransformPoint(new Vector3(minX, 0, minZ)),
                transform.TransformPoint(new Vector3(minX, 0, maxZ)),
                transform.TransformPoint(new Vector3(maxX, 0, maxZ)),
                transform.TransformPoint(new Vector3(maxX, 0, minZ)),
            };
        }
    }
}

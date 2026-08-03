using UnityEngine;
using Object = UnityEngine.Object;

namespace Radar
{
    /// <summary>
    /// A quadrilateral world area (currently only minefields) shaded onto the radar face.
    /// Unlike a <see cref="Target"/> this projects all four corners, so the shape stays recognisable.
    /// </summary>
    public class RadarRegion
    {
        private const int CornerCount = 4;

        /// <summary>Regions start fading out this far inside the outer range, in metres.</summary>
        private const float FadeBand = 20f;

        public static Color FillColor { get; set; }

        private static Vector3 _playerPosition;

        public static void SetPlayerPosition(Vector3 playerPosition) => _playerPosition = playerPosition;

        private readonly GameObject _root;
        private readonly PolygonGraphic _fill;
        private readonly Vector3[] _worldCorners;
        private readonly Vector3 _center;

        /// <summary>Reused every frame so projecting the region does not allocate.</summary>
        private readonly Vector2[] _radarCorners = new Vector2[CornerCount];

        public RadarRegion(Vector3[] worldCorners)
        {
            _worldCorners = worldCorners;

            _center = Vector3.zero;
            foreach (Vector3 corner in worldCorners)
                _center += corner;
            _center /= worldCorners.Length;

            _root = new GameObject("RadarRegion", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(RadarHudLayout.BorderTransform, false);
            rootRect.localScale = Vector3.one;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;

            var fillObject = new GameObject("RadarRegionFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(PolygonGraphic));
            fillObject.transform.SetParent(_root.transform, false);

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = RadarHudLayout.BorderTransform.sizeDelta;

            _fill = fillObject.GetComponent<PolygonGraphic>();

            UpdateVisual();
        }

        public void UpdateVisual()
        {
            for (int i = 0; i < CornerCount; i++)
                _radarCorners[i] = WorldToRadarPosition(_worldCorners[i]);

            float dx = _playerPosition.x - _center.x;
            float dz = _playerPosition.z - _center.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            Color color = FillColor;
            color.a *= FadeAlpha(distance);
            _fill.color = color;

            _fill.UpdatePolygon(_radarCorners);
        }

        public void Destroy() => Object.Destroy(_root);

        /// <summary>Smoothstep fade so a region dissolves rather than popping as it leaves radar range.</summary>
        private static float FadeAlpha(float distance)
        {
            float fadeStart = Target.OuterRange - FadeBand;
            if (distance <= fadeStart) return 1f;

            float t = Mathf.Clamp01((distance - fadeStart) / FadeBand);
            return 1f - t * t * (3f - 2f * t);
        }

        private static Vector2 WorldToRadarPosition(Vector3 worldPosition)
        {
            Vector3 relative = worldPosition - _playerPosition;
            float distance = Mathf.Sqrt(relative.x * relative.x + relative.z * relative.z);
            Vector2 direction = new Vector2(relative.x, relative.z).normalized;

            return direction * RadarHudLayout.FaceRadius * Target.RangeToRadarFactor(distance);
        }
    }
}

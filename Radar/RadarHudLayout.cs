using UnityEngine;
using UnityEngine.UI;

namespace Radar
{
    /// <summary>
    /// Owns the radar HUD's transform hierarchy and the two ways it can be displayed: as a screen
    /// overlay, or projected onto the in-game compass glass.
    /// </summary>
    /// <remarks>
    /// Hierarchy: FPS Camera (or compass glass) -&gt; HUD root -&gt; Radar -&gt; RadarBorder.
    /// </remarks>
    internal sealed class RadarHudLayout
    {
        private const string CompassGlassName = "compas_glass_LOD0";

        /// <summary>Fraction of the radar face that blips and regions are allowed to occupy.</summary>
        private const float RadiusFraction = 0.68f;

        /// <summary>World scale that fits the radar onto the compass face.</summary>
        private const float CompassScale = 0.000123f;

        /// <summary>
        /// The rotating radar face. Blips and regions parent themselves to it, so it is exposed
        /// statically - there is only ever one radar HUD.
        /// </summary>
        public static RectTransform BorderTransform { get; private set; } = null!;

        /// <summary>Radius of the radar face in local UI units, accounting for the current HUD scale.</summary>
        public static float FaceRadius
        {
            get
            {
                Vector3 scale = BorderTransform.localScale;
                Vector2 size = BorderTransform.sizeDelta;
                return Mathf.Min(size.x * scale.x, size.y * scale.y) * RadiusFraction;
            }
        }

        public RectTransform PulseTransform { get; }

        private readonly Transform _hudRoot;
        private readonly GameObject _fpsCamera;
        private readonly RectTransform _baseTransform;
        private readonly GameObject _baseObject;
        private readonly Canvas _canvas;
        private readonly Image _borderImage;
        private readonly Image _pulseImage;
        private readonly Image _backgroundImage;
        private readonly Vector3 _initialScale;

        private GameObject? _compassGlass;
        private bool _compassVisible;

        public RadarHudLayout(Transform hudRoot, GameObject fpsCamera)
        {
            _hudRoot = hudRoot;
            _fpsCamera = fpsCamera;

            _baseTransform = (RectTransform)hudRoot.Find("Radar");
            _baseObject = _baseTransform.gameObject;
            _initialScale = _baseTransform.localScale;

            BorderTransform = (RectTransform)hudRoot.Find("Radar/RadarBorder");
            BorderTransform.SetAsLastSibling();
            _borderImage = BorderTransform.GetComponent<Image>();

            PulseTransform = (RectTransform)hudRoot.Find("Radar/RadarPulse");
            _pulseImage = PulseTransform.GetComponent<Image>();

            _backgroundImage = hudRoot.Find("Radar/RadarBackground").GetComponent<Image>();

            _canvas = hudRoot.GetComponentInChildren<Canvas>();

            SetBackgroundColor(RadarConfig.BackgroundColor.Value);
        }

        public void Apply()
        {
            if (RadarConfig.CompassEnabled.Value)
                ApplyCompassMode();
            else
                ApplyNormalMode();
        }

        public void SetBackgroundColor(Color color)
        {
            _borderImage.color = color;
            _pulseImage.color = color;
            _backgroundImage.color = color;
        }

        /// <summary>
        /// Applies the configured screen position. No-op in compass mode, where the HUD is positioned
        /// by the compass glass instead.
        /// </summary>
        public void ApplyPosition()
        {
            if (RadarConfig.CompassEnabled.Value) return;

            _baseTransform.position = new Vector2(RadarConfig.OffsetX.Value, RadarConfig.OffsetY.Value);
        }

        /// <summary>Applies the configured HUD size. No-op in compass mode, which has a fixed scale.</summary>
        public void ApplyScale()
        {
            if (RadarConfig.CompassEnabled.Value) return;

            _baseTransform.localScale = _initialScale * RadarConfig.HudSize.Value;
        }

        public void SetPulseVisible(bool visible) => PulseTransform.gameObject.SetActive(visible);

        public void SetPulseAngle(float degrees) => PulseTransform.localEulerAngles = new Vector3(0, 0, degrees);

        /// <summary>Keeps the radar face aligned with the player's heading in screen-overlay mode.</summary>
        public void UpdateBorderRotation() =>
            BorderTransform.eulerAngles = new Vector3(0, 0, _hudRoot.parent.eulerAngles.y);

        public void SetCompassParent(bool attachToCompass) =>
            _hudRoot.SetParent(attachToCompass && _compassGlass != null ? _compassGlass.transform : _fpsCamera.transform);

        /// <summary>
        /// Latches the radar onto the compass glass once it exists. The glass is spawned lazily by the
        /// game, so this has to keep looking until it turns up.
        /// </summary>
        public void TickCompass()
        {
            // Player.FirearmController.CurrentCompassState reports false even with the compass equipped,
            // so the radar is shown unconditionally while compass mode is on.
            if (!_compassVisible)
            {
                _compassVisible = true;
                _baseObject.SetActive(true);
                SetCompassParent(true);
            }

            if (_compassGlass != null) return;

            _compassGlass = GameObject.Find(CompassGlassName);
            if (_compassGlass == null || _hudRoot.parent == _compassGlass.transform) return;

            _hudRoot.SetParent(_compassGlass.transform, false);
            _hudRoot.localPosition = Vector3.zero;
            _hudRoot.localRotation = Quaternion.identity;
            _hudRoot.localScale = Vector3.one;
        }

        private void ApplyNormalMode()
        {
            RadarPlugin.Log.LogDebug("Radar HUD: screen overlay mode");

            _compassGlass = null;
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.worldCamera = null;
            }

            _hudRoot.SetParent(_fpsCamera.transform);
            _hudRoot.rotation = Quaternion.identity;

            ApplyPosition();
            _baseTransform.rotation = Quaternion.identity;
            ApplyScale();
            BorderTransform.rotation = Quaternion.identity;
            _baseObject.SetActive(true);
        }

        private void ApplyCompassMode()
        {
            RadarPlugin.Log.LogDebug("Radar HUD: compass mode");

            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
                _canvas.worldCamera = Camera.main;
            }

            SetCompassParent(true);
            _hudRoot.localPosition = Vector3.zero;
            _hudRoot.localRotation = Quaternion.identity;

            _baseTransform.localPosition = new Vector3(0, 0, 0.001f);
            _baseTransform.localRotation = Quaternion.Euler(0, -180, 0);
            _baseTransform.localScale = Vector3.one * CompassScale;
            BorderTransform.localRotation = Quaternion.identity;
        }
    }
}

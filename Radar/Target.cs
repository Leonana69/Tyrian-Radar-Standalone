using EFT;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Radar
{
    /// <summary>
    /// Base class for anything drawn as a single blip on the radar face. Owns the blip GameObject
    /// and the world-space to radar-space projection; subclasses decide what sprite and colour to use.
    /// </summary>
    public abstract class Target
    {
        /// <summary>Blips sit slightly in front of the radar face so they are not z-fought by it.</summary>
        private const float BlipDepth = -0.01f;

        /// <summary>Compass mode draws the radar rotated 30 degrees relative to the player.</summary>
        private const float CompassShiftAngle = 0.5235987f;

        /// <summary>How far past the outer range off-radar blips are pinned, in metres.</summary>
        private const float OutOfRangeOvershoot = 60f;

        /// <summary>Off-radar blips are only drawn while within this many degrees of the facing direction.</summary>
        private const float OutOfRangeVisibleArc = 20f;

        protected const float PlayerHeight = 1.8f;

        public static BifacialTransform PlayerTransform { get; private set; } = null!;
        public static float OuterRange { get; private set; }
        public static float InnerRange { get; private set; }

        public static void SetPlayerTransform(BifacialTransform playerTransform) => PlayerTransform = playerTransform;

        public static void SetRadarRange(float inner, float outer)
        {
            InnerRange = inner;
            OuterRange = outer;
        }

        /// <summary>
        /// Maps a world distance onto the [0,1] radius of the radar face. The exponent lets the user
        /// bias the mapping towards nearby (&lt;1) or distant (&gt;1) contacts.
        /// </summary>
        public static float RangeToRadarFactor(float distance)
        {
            float scale = RadarConfig.DistanceScale.Value;
            return Mathf.Pow(distance / OuterRange, 0.4f + scale * scale / 2.0f);
        }

        protected GameObject? blip;
        protected Image? blipImage;

        /// <summary>Target position relative to the player, in world axes.</summary>
        protected Vector3 blipPosition;

        public Vector3 TargetPosition { get; protected set; }

        protected Target()
        {
            blip = Object.Instantiate(AssetFileManager.RadarBlipHudPrefab);
            blip.transform.SetParent(RadarHudLayout.BorderTransform);
            blip.transform.SetAsLastSibling();
            blip.transform.localPosition = Vector3.zero;
            blip.transform.localRotation = Quaternion.identity;

            if (blip.transform.Find("Blip/RadarEnemyBlip") is RectTransform blipTransform)
            {
                blipImage = blipTransform.GetComponent<Image>();
                blipImage.color = Color.clear;
            }

            blip.SetActive(true);
        }

        public void DestroyBlip() => Object.Destroy(blip);

        protected void Hide()
        {
            if (blipImage != null)
                blipImage.color = Color.clear;
        }

        /// <summary>Recomputes <see cref="blipPosition"/> from the current player position.</summary>
        protected void RefreshRelativePosition()
        {
            Vector3 origin = PlayerTransform.position;
            blipPosition.x = TargetPosition.x - origin.x;
            blipPosition.y = TargetPosition.y - origin.y;
            blipPosition.z = TargetPosition.z - origin.z;
        }

        /// <summary>Squared horizontal distance to the target - avoids a square root when range testing.</summary>
        protected float SqrPlanarDistance => blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z;

        protected bool IsInRange
        {
            get
            {
                float sqrDistance = SqrPlanarDistance;
                return sqrDistance <= OuterRange * OuterRange && sqrDistance >= InnerRange * InnerRange;
            }
        }

        /// <summary>Chooses between the level, above and below sprite of a blip set.</summary>
        protected Sprite PickSpriteForHeight(Sprite[] blips)
        {
            float threshold = PlayerHeight * 1.5f * RadarConfig.HeightThreshold.Value;
            if (blipPosition.y > threshold) return blips[1];
            if (blipPosition.y < -threshold) return blips[2];
            return blips[0];
        }

        protected void SetBlipScale(float scale)
        {
            if (blip == null) return;
            float size = RadarConfig.BlipSize.Value * 3f * scale;
            blip.transform.localScale = new Vector3(size, size, size);
        }

        /// <param name="updatePosition">
        /// When false only the blip's own rotation is corrected, leaving it parked where it was.
        /// </param>
        /// <param name="showOutsideOfRange">Pin out-of-range blips to the rim instead of hiding them.</param>
        /// <param name="showInAllDirection">Keep pinned blips visible even when behind the player.</param>
        protected void UpdatePosition(bool updatePosition, bool showOutsideOfRange = false, bool showInAllDirection = false)
        {
            if (blip == null) return;

            RectTransform border = RadarHudLayout.BorderTransform;
            float eulerZ = border.rotation.eulerAngles.z;
            float shiftAngle;

            if (!RadarConfig.CompassEnabled.Value)
            {
                // Counter-rotate so the sprite stays upright while the radar face spins.
                blip.transform.localRotation = Quaternion.Euler(0, 0, 360f - eulerZ);
                shiftAngle = 0f;
            }
            else
            {
                eulerZ = PlayerTransform.rotation.eulerAngles.y;
                shiftAngle = CompassShiftAngle;
            }

            if (!updatePosition) return;

            float distance = Mathf.Sqrt(SqrPlanarDistance);
            bool exceedDistance = distance > OuterRange;
            if (showOutsideOfRange && exceedDistance)
                distance = OuterRange + OutOfRangeOvershoot;

            float offsetRadius = RangeToRadarFactor(distance);

            Vector3 rotatedDirection = border.rotation * Vector3.forward;
            float faceAngle = Mathf.Atan2(rotatedDirection.x, rotatedDirection.z);
            float targetAngle = Mathf.Atan2(blipPosition.x, blipPosition.z);

            if (exceedDistance && !showInAllDirection &&
                Mathf.Abs(eulerZ - targetAngle * Mathf.Rad2Deg) > OutOfRangeVisibleArc)
            {
                Hide();
                return;
            }

            float bearing = targetAngle - faceAngle - shiftAngle;
            blip.transform.localPosition =
                new Vector3(Mathf.Sin(bearing), Mathf.Cos(bearing), BlipDepth) * offsetRadius * RadarHudLayout.FaceRadius;
        }
    }
}

using UnityEngine;

namespace Radar
{
    /// <summary>What a <see cref="BlipOther"/> represents, which decides its sprite, colour and size.</summary>
    public enum BlipKind
    {
        Loot = 0,
        Mine = 1,
        WishlistLoot = 2,
        Exfil = 3,
        Transit = 4,
    }

    /// <summary>A blip for a static world object: loot, a mine, an extract or a transit.</summary>
    public class BlipOther : Target
    {
        /// <summary>Extracts and transits are drawn larger so they read as landmarks, not contacts.</summary>
        private const float LandmarkScale = 2.0f;

        private static readonly Color ExfilColor = new Color(0.5f, 1.0f, 0f);
        private static readonly Color TransitColor = new Color(1.0f, 0.65f, 0.24f);

        private readonly Transform _transform;
        private bool _lazyUpdate;

        public string Id { get; }
        public BlipKind Kind { get; private set; }

        /// <param name="lazyUpdate">
        /// Defer reading the transform until the first render. Loot spawned by the game is registered
        /// before its transform has settled, so sampling it immediately gives the wrong position.
        /// </param>
        public BlipOther(string id, Transform transform, bool lazyUpdate = false, BlipKind kind = BlipKind.Loot)
        {
            Id = id;
            Kind = kind;
            _transform = transform;
            _lazyUpdate = lazyUpdate;
            TargetPosition = transform.position;
        }

        public void SetKind(BlipKind kind)
        {
            if (Kind == kind) return;

            Kind = kind;
            if (blipImage != null)
                UpdateBlipImage(null);
        }

        public void Update(Color? blipColor = null)
        {
            if (_lazyUpdate)
            {
                TargetPosition = _transform.position;
                _lazyUpdate = false;
            }

            RefreshRelativePosition();

            // Extracts and transits stay pinned to the rim so the player can always find their way out.
            bool visible = IsInRange || Kind == BlipKind.Exfil || Kind == BlipKind.Transit;
            if (!visible)
            {
                Hide();
                return;
            }

            UpdateBlipImage(blipColor);
            UpdatePosition(updatePosition: true, showOutsideOfRange: true);
        }

        private void UpdateBlipImage(Color? blipColor)
        {
            if (blip == null || blipImage == null) return;

            blipImage.sprite = PickSpriteForHeight(SpritesFor(Kind));
            blipImage.color = blipColor ?? ColorFor(Kind);
            SetBlipScale(Kind == BlipKind.Exfil || Kind == BlipKind.Transit ? LandmarkScale : 1f);
        }

        private static Sprite[] SpritesFor(BlipKind kind) => kind switch
        {
            BlipKind.Mine => AssetFileManager.MineBlips,
            BlipKind.Exfil or BlipKind.Transit => AssetFileManager.ExfiltrationBlips,
            _ => AssetFileManager.LootBlips,
        };

        private static Color ColorFor(BlipKind kind) => kind switch
        {
            BlipKind.Mine => Color.black,
            BlipKind.WishlistLoot => RadarConfig.WishlistLootColor.Value,
            BlipKind.Exfil => ExfilColor,
            BlipKind.Transit => TransitColor,
            _ => RadarConfig.LootColor.Value,
        };
    }
}

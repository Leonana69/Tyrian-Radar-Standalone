using EFT;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Radar
{
    /// <summary>A blip tracking another player, PMC, scav, boss or BTR.</summary>
    public class BlipPlayer : Target
    {
        /// <summary>How long a contact stays lit after firing, in seconds.</summary>
        private const float FireModeFadeDuration = 3f;

        /// <summary>Fading is skipped below this interval, where it would just make blips flicker.</summary>
        private const float MinFadeInterval = 0.9f;

        private readonly Player _enemyPlayer;
        private Outline? _outline;
        private bool _isDead;
        private bool _visible;
        private float _lastUpdateTime = Time.time;

        public BlipPlayer(Player enemyPlayer)
        {
            _enemyPlayer = enemyPlayer;
        }

        public void UpdateLastFireTime(float lastFireTime) => _lastUpdateTime = lastFireTime;

        public void Update(bool updatePosition)
        {
            bool visible = false;

            if (_enemyPlayer != null)
            {
                if (updatePosition)
                {
                    // Reading the player's transform is the expensive part, so it is kept on the scan interval.
                    TargetPosition = _enemyPlayer.gameObject.transform.position;
                    RefreshRelativePosition();
                }

                visible = IsInRange;

                if (!_isDead && !_enemyPlayer.HealthController.IsAlive)
                    _isDead = true;

                if (_isDead)
                    visible &= RadarConfig.CorpseEnabled.Value;
            }

            if (_visible && !visible)
                Hide();

            _visible = visible;
            if (!visible) return;

            UpdateBlipImage();

            if (RadarConfig.FireModeEnabled.Value && !_isDead)
                UpdateAlpha(Time.time - _lastUpdateTime < FireModeFadeDuration, FireModeFadeDuration);
            else
                UpdateAlpha(updatePosition);

            UpdatePosition(updatePosition);
        }

        private void UpdateBlipImage()
        {
            if (blip == null || blipImage == null) return;

            Sprite[] targetBlips = AssetFileManager.NormalEnemyBlips;

            switch (_enemyPlayer.Profile.Info.Side)
            {
                case EPlayerSide.Savage:
                    switch (_enemyPlayer.Profile.Info.Settings.Role)
                    {
                        case WildSpawnType.assault:
                        case WildSpawnType.marksman:
                        case WildSpawnType.assaultGroup:
                            blipImage.color = RadarConfig.ScavColor.Value;
                            break;
                        case WildSpawnType.shooterBTR:
                            targetBlips = AssetFileManager.BtrBlips;
                            blipImage.color = Color.red;
                            break;
                        default:
                            targetBlips = AssetFileManager.BossEnemyBlips;
                            blipImage.color = RadarConfig.BossColor.Value;
                            break;
                    }
                    break;
                case EPlayerSide.Bear:
                    blipImage.color = RadarConfig.BearColor.Value;
                    break;
                case EPlayerSide.Usec:
                    blipImage.color = RadarConfig.UsecColor.Value;
                    break;
            }

            if (_isDead)
            {
                targetBlips = AssetFileManager.DeadEnemyBlips;
                // Corpses are all drawn in the corpse colour; the outline preserves which faction it was.
                UpdateCorpseOutline(blipImage.color);
                blipImage.color = RadarConfig.CorpseColor.Value;
            }

            blipImage.sprite = PickSpriteForHeight(targetBlips);
            SetBlipScale(1f);
        }

        private void UpdateCorpseOutline(Color factionColor)
        {
            if (blipImage == null) return;

            if (_outline == null && RadarConfig.CorpseTypeEnabled.Value)
            {
                _outline = blipImage.GetOrAddComponent<Outline>();
                _outline.effectDistance = new Vector2(1, -1);
            }
            else if (_outline != null && !RadarConfig.CorpseTypeEnabled.Value)
            {
                Object.Destroy(_outline);
                _outline = null;
            }

            if (_outline != null)
                _outline.effectColor = factionColor;
        }

        /// <summary>
        /// Fades the blip out over the scan interval so a contact visibly decays until the next sweep.
        /// </summary>
        /// <param name="refreshTimer">Whether an elapsed interval should restart the fade.</param>
        /// <param name="interval">Fade duration; negative means derive it from the scan interval.</param>
        private void UpdateAlpha(bool refreshTimer = false, float interval = -1)
        {
            if (blipImage == null) return;

            if (interval < 0)
                interval = Mathf.Min(RadarConfig.ScanInterval.Value, 3f);

            float alphaScale = 1f;
            if (interval > MinFadeInterval)
            {
                float timeDiff = Time.time - _lastUpdateTime;
                if (refreshTimer && timeDiff >= interval)
                    _lastUpdateTime = Time.time;

                float ratio = timeDiff / interval;
                alphaScale = Mathf.Max(0f, 1f - ratio * ratio);
            }

            Color color = blipImage.color;
            blipImage.color = new Color(color.r, color.g, color.b, color.a * alphaScale);
        }
    }
}

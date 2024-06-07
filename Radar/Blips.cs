using EFT.Interactive;
using EFT;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Item = EFT.InventoryLogic.Item;

namespace Radar
{
    public class BlipPlayer : Target
    {
        private readonly Player _enemyPlayer;
        private bool _isDead = false;
        public BlipPlayer(Player enemyPlayer)
        {
            _enemyPlayer = enemyPlayer;
        }

        private void UpdateBlipImage()
        {
            if (blip == null || blipImage == null) return;
            if (_isDead)
            {
                blipImage.sprite = AssetBundleManager.EnemyBlipDead;
                blipImage.color = Radar.corpseBlipColor.Value;
            }
            else
            {
                float totalThreshold = playerHeight * 1.5f * Radar.radarYHeightThreshold.Value;
                if (Mathf.Abs(blipPosition.y) <= totalThreshold)
                {
                    blipImage.sprite = AssetBundleManager.EnemyBlip;
                }
                else if (blipPosition.y > totalThreshold)
                {
                    blipImage.sprite = AssetBundleManager.EnemyBlipUp;
                }
                else if (blipPosition.y < -totalThreshold)
                {
                    blipImage.sprite = AssetBundleManager.EnemyBlipDown;
                }
                // set blip color
                switch (_enemyPlayer.Profile.Info.Side)
                {
                    case EPlayerSide.Savage:
                        switch (_enemyPlayer.Profile.Info.Settings.Role)
                        {
                            case WildSpawnType.assault:
                            case WildSpawnType.marksman:
                            case WildSpawnType.assaultGroup:
                                blipImage.color = Radar.scavBlipColor.Value;
                                break;
                            default:
                                blipImage.color = Radar.bossBlipColor.Value;
                                break;
                        }
                        break;
                    case EPlayerSide.Bear:
                        blipImage.color = Radar.bearBlipColor.Value;
                        break;
                    case EPlayerSide.Usec:
                        blipImage.color = Radar.usecBlipColor.Value;
                        break;
                    default:
                        break;
                }
            }
            float blipSize = Radar.radarBlipSizeConfig.Value * 3f;
            blip.transform.localScale = new Vector3(blipSize, blipSize, blipSize);
        }

        public void UpdateLastFireTime(float lastFireTime)
        {
            lastUpdateTime = lastFireTime;
        }

        public void Update(bool updatePosition)
        {
            bool _show = false;
            if (_enemyPlayer != null)
            {
                if (updatePosition)
                {
                    // this enemyPlayer read is expensive
                    GameObject enemyObject = _enemyPlayer.gameObject;
                    targetPosition = enemyObject.transform.position;
                    blipPosition.x = targetPosition.x - playerPosition.x;
                    blipPosition.y = targetPosition.y - playerPosition.y;
                    blipPosition.z = targetPosition.z - playerPosition.z;
                }

                var distance = blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z;
                _show = (distance > radarOuterRange * radarOuterRange || distance < radarInnerRange * radarInnerRange) ? false : true;

                if (!_isDead && _enemyPlayer.HealthController.IsAlive == _isDead)
                {
                    _isDead = true;
                }

                if (_isDead)
                {
                    _show = Radar.radarEnableCorpseConfig.Value && _show;
                }

                // don't show scav corpses
                if (Radar.radarEnableScavCorpseConfig.Value && _isDead && _enemyPlayer.Profile.Info.Side == EPlayerSide.Savage)
                {
                    if (
                        _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.assault
                        || _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.marksman
                        || _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.assaultGroup
                    )
                    {
                        _show = false;
                    }
                }
            }

            if (show && !_show && blipImage != null)
            {
                blipImage.color = new Color(0, 0, 0, 0);
            }

            show = _show;

            if (show)
            {
                UpdateBlipImage();
                if (Radar.radarEnableFireModeConfig.Value && !_isDead)
                    UpdateAlpha(Time.time - lastUpdateTime < 3, 3);
                else
                    UpdateAlpha(updatePosition);
                UpdatePosition(updatePosition);
            }
        }
    }

    public class BlipLoot : Target
    {
        public string _id;
        public Transform _transform;
        private bool _lazyUpdate;

        public BlipLoot(string id, Transform transform, bool lazyUpdate = false)
        {
            _id = id;
            _transform = transform;
            targetPosition = transform.position;
            _lazyUpdate = lazyUpdate;
        }

        private void UpdateBlipImage()
        {
            if (blip == null || blipImage == null)
                return;
            float totalThreshold = playerHeight * 1.5f * Radar.radarYHeightThreshold.Value;
            if (blipPosition.y > totalThreshold)
            {
                blipImage.sprite = AssetBundleManager.EnemyBlipUp;
            }
            else if (blipPosition.y < -totalThreshold)
            {
                blipImage.sprite = AssetBundleManager.EnemyBlipDown;
            }
            else
            {
                blipImage.sprite = AssetBundleManager.EnemyBlipDead;
            }
            blipImage.color = Radar.lootBlipColor.Value;

            float blipSize = Radar.radarBlipSizeConfig.Value * 3f;
            blip.transform.localScale = new Vector3(blipSize, blipSize, blipSize);
        }

        public void Update(bool _show)
        {
            if (_lazyUpdate)
            {
                targetPosition = _transform.position;
                _lazyUpdate = false;
            }

            blipPosition.x = targetPosition.x - playerPosition.x;
            blipPosition.y = targetPosition.y - playerPosition.y;
            blipPosition.z = targetPosition.z - playerPosition.z;

            if (!_show)
            {
                if (blipImage != null)
                    blipImage.color = new Color(0, 0, 0, 0);
            }
            else
            {
                UpdateBlipImage();
                //UpdateAlpha();
                UpdatePosition(true);
            }
        }
    }

    public class Target
    {
        public bool show = false;
        protected GameObject? blip;
        protected Image? blipImage;

        protected Vector3 blipPosition;
        public Vector3 targetPosition;
        public static Vector3 playerPosition;
        public static float radarOuterRange;
        public static float radarInnerRange;
        protected float lastUpdateTime = Time.time;

        protected float playerHeight = 1.8f;

        public void SetBlip()
        {
            var blipInstance = Object.Instantiate(AssetBundleManager.RadarBliphudPrefab);
            blip = blipInstance as GameObject;
            blip.transform.SetParent(HaloRadar.RadarBorderTransform.transform);
            blip.transform.SetAsLastSibling();
            blip.transform.localPosition = Vector3.zero;
            blip.transform.localRotation = Quaternion.identity;

            var blipTransform = blip.transform.Find("Blip/RadarEnemyBlip") as RectTransform;
            if (blipTransform != null)
            {
                blipImage = blipTransform.GetComponent<Image>();
                blipImage.color = Color.clear;
            }
            blip.SetActive(true);
        }

        public Target()
        {
            SetBlip();
        }

        public void DestoryBlip()
        {
            Object.Destroy(blip);
        }

        public static void setPlayerPosition(Vector3 playerPosition)
        {
            Target.playerPosition = playerPosition;
        }

        public static void setRadarRange(float inner, float outer)
        {
            Target.radarInnerRange = inner;
            Target.radarOuterRange = outer;
        }

        protected void UpdateAlpha(bool updatePosition = false, float interval = -1)
        {
            float r, g, b, a;
            if (blipImage != null)
            {
                r = blipImage.color.r;
                g = blipImage.color.g;
                b = blipImage.color.b;
                a = blipImage.color.a;
                float delta_a = 1;
                if (interval < 0)
                {
                    interval = Radar.radarScanInterval.Value;
                    if (interval > 3)
                        interval = 3;
                }

                if (interval > 0.9)
                {
                    float timeDiff = Time.time - lastUpdateTime;
                    if (interval <= timeDiff && updatePosition)
                    {
                        lastUpdateTime = Time.time;
                    }
                    float ratio = timeDiff / interval;
                    delta_a = 1 - ratio * ratio;
                    if (delta_a < 0)
                        delta_a = 0;
                }

                blipImage.color = new Color(r, g, b, a * delta_a);
            }
        }

        protected void UpdatePosition(bool updatePosition)
        {
            if (blip == null) return;
            blip.transform.localRotation = Quaternion.Euler(0, 0, 360f - HaloRadar.RadarBorderTransform.rotation.eulerAngles.z);

            if (!updatePosition)
            {
                return;
            }
            // Calculate the position based on the angle and distance
            float distance = Mathf.Sqrt(blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z);
            // Calculate the offset factor based on the distance
            float offsetRadius = Mathf.Pow(distance / radarOuterRange, 0.4f + Radar.radarDistanceScaleConfig.Value * Radar.radarDistanceScaleConfig.Value / 2.0f);
            // Calculate angle
            // Apply the rotation of the parent transform
            Vector3 rotatedDirection = HaloRadar.RadarBorderTransform.rotation * Vector3.forward;
            float angle = Mathf.Atan2(rotatedDirection.x, rotatedDirection.z) * Mathf.Rad2Deg;
            float angleInRadians = Mathf.Atan2(blipPosition.x, blipPosition.z);

            // Get the scale of the RadarBorderTransform
            Vector3 scale = HaloRadar.RadarBorderTransform.localScale;
            // Multiply the sizeDelta by the scale to account for scaling
            Vector2 scaledSizeDelta = HaloRadar.RadarBorderTransform.sizeDelta;
            scaledSizeDelta.x *= scale.x;
            scaledSizeDelta.y *= scale.y;
            // Calculate the radius of the circular boundary
            float graphicRadius = Mathf.Min(scaledSizeDelta.x, scaledSizeDelta.y) * 0.68f;

            // Set the local position of the blip
            blip.transform.localPosition = new Vector3(
                Mathf.Sin(angleInRadians - angle * Mathf.Deg2Rad),
                Mathf.Cos(angleInRadians - angle * Mathf.Deg2Rad), -0.01f)
                * offsetRadius * graphicRadius;
        }
    }
}

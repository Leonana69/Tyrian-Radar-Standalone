using EFT.Interactive;
using EFT;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Radar
{
    public class BlipPlayer : Target
    {
        private const float playerHeight = 1.8f;
        private Player enemyPlayer = null;
        private bool isDead = false;
        public BlipPlayer(Player enemyPlayer)
        {
            this.enemyPlayer = enemyPlayer;
        }

        private void UpdateBlipImage()
        {
            if (isDead)
            {
                blipImage.sprite = HaloRadar.EnemyBlipDead;
                blipImage.color = Radar.corpseBlipColor.Value;
            }
            else
            {
                float totalThreshold = playerHeight * 1.5f * Radar.radarYHeightThreshold.Value;
                if (Mathf.Abs(blipPosition.y) <= totalThreshold)
                {
                    blipImage.sprite = HaloRadar.EnemyBlip;
                }
                else if (blipPosition.y > totalThreshold)
                {
                    blipImage.sprite = HaloRadar.EnemyBlipUp;
                }
                else if (blipPosition.y < -totalThreshold)
                {
                    blipImage.sprite = HaloRadar.EnemyBlipDown;
                }
                // set blip color
                switch (enemyPlayer.Profile.Info.Side)
                {
                    case EPlayerSide.Savage:
                        switch (enemyPlayer.Profile.Info.Settings.Role)
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

        public void Update(bool updatePosition)
        {
            bool _show = false;
            if (enemyPlayer != null)
            {
                if (updatePosition)
                {
                    // this enemyPlayer read is expensive
                    GameObject enemyObject = enemyPlayer.gameObject;
                    blipPosition.x = enemyObject.transform.position.x - playerPosition.x;
                    blipPosition.y = enemyObject.transform.position.y - playerPosition.y;
                    blipPosition.z = enemyObject.transform.position.z - playerPosition.z;
                }

                _show = blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z
                     > radarRange * radarRange ? false : true;
                if (!isDead && enemyPlayer.HealthController.IsAlive == isDead)
                {
                    isDead = true;
                }

                if (isDead)
                {
                    _show = Radar.radarEnableCorpseConfig.Value && _show;
                }
            }

            if (show && !_show)
            {
                blipImage.color = new Color(0, 0, 0, 0);
            }

            show = _show;

            if (show)
            {
                UpdateAlpha();
                UpdateBlipImage();
                UpdatePosition(updatePosition);
            }
        }
    }

    public class BlipLoot : Target
    {
        public int price = 0;
        public string itemId;
        private Vector3 itemPosition;

        public BlipLoot(LootItem item)
        {
            this.itemId = item.ItemId;
            var offer = ItemExtensions.GetBestTraderOffer(item.Item);
            itemPosition = item.TrackableTransform.position;

            if (offer != null)
            {
                this.price = offer.Price;
            }
        }

        private void UpdateBlipImage()
        {
            blipImage.sprite = HaloRadar.EnemyBlipDead;
            blipImage.color = Radar.lootBlipColor.Value;

            float blipSize = Radar.radarBlipSizeConfig.Value * 3f;
            blip.transform.localScale = new Vector3(blipSize, blipSize, blipSize);
        }

        public void Update(bool updatePosition)
        {
            blipPosition.x = itemPosition.x - playerPosition.x;
            blipPosition.z = itemPosition.z - playerPosition.z;

            bool _show = Radar.radarEnableLootConfig.Value && this.price > Radar.radarLootThreshold.Value && blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z
                 < radarRange * radarRange;

            if (show && !_show)
            {
                blipImage.color = new Color(0, 0, 0, 0);
            }

            show = _show;
            if (show)
            {
                UpdateAlpha();
                UpdateBlipImage();
                UpdatePosition(updatePosition);
            }
        }

        public void DestoryLoot()
        {
            this.DestoryBlip();
        }
    }

    public class Target
    {
        public bool show = false;
        protected GameObject blip;
        protected Image blipImage;

        protected Vector3 blipPosition;
        public static Vector3 playerPosition;
        public static float radarRange;

        private void SetBlip()
        {
            var blipInstance = Object.Instantiate(HaloRadar.RadarBliphudPrefab,
                HaloRadar.radarHudBlipBasePosition.position, HaloRadar.radarHudBlipBasePosition.rotation);
            blip = blipInstance as GameObject;
            blip.transform.parent = HaloRadar.radarHudBlipBasePosition.transform;
            blip.transform.SetAsLastSibling();

            var blipTransform = blip.transform.Find("Blip/RadarEnemyBlip") as RectTransform;
            blipImage = blipTransform.GetComponent<Image>();
            blipImage.color = Color.clear;
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

        public static void setRadarRange(float radarRange)
        {
            Target.radarRange = radarRange;
        }

        protected void UpdateAlpha()
        {
            float r = blipImage.color.r, g = blipImage.color.g, b = blipImage.color.b, a = blipImage.color.a;
            float delta_a = 1;
            if (Radar.radarScanInterval.Value > 0.8)
            {
                float ratio = (Time.time - HaloRadar.radarLastUpdateTime) / Radar.radarScanInterval.Value;
                delta_a = 1 - ratio * ratio;
            }
            blipImage.color = new Color(r, g, b, a * delta_a);
        }

        protected void UpdatePosition(bool updatePosition)
        {
            Quaternion reverseRotation = Quaternion.Inverse(HaloRadar.radarHudBlipBasePosition.rotation);
            blip.transform.localRotation = reverseRotation;

            if (!updatePosition)
            {
                return;
            }
            // Calculate the position based on the angle and distance
            float distance = Mathf.Sqrt(blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z);
            // Calculate the offset factor based on the distance
            float offsetRadius = Mathf.Pow(distance / radarRange, 0.4f + Radar.radarDistanceScaleConfig.Value * Radar.radarDistanceScaleConfig.Value / 2.0f);
            // Calculate angle
            // Apply the rotation of the parent transform
            Vector3 rotatedDirection = HaloRadar.radarHudBlipBasePosition.rotation * Vector3.forward;
            float angle = Mathf.Atan2(rotatedDirection.x, rotatedDirection.z) * Mathf.Rad2Deg;
            float angleInRadians = Mathf.Atan2(blipPosition.x, blipPosition.z);

            // Get the scale of the radarHudBlipBasePosition
            Vector3 scale = HaloRadar.radarHudBlipBasePosition.localScale;
            // Multiply the sizeDelta by the scale to account for scaling
            Vector2 scaledSizeDelta = HaloRadar.radarHudBlipBasePosition.sizeDelta;
            scaledSizeDelta.x *= scale.x;
            scaledSizeDelta.y *= scale.y;
            // Calculate the radius of the circular boundary
            float graphicRadius = Mathf.Min(scaledSizeDelta.x, scaledSizeDelta.y) * 0.68f;

            // Set the local position of the blip
            blip.transform.localPosition = new Vector2(
                Mathf.Sin(angleInRadians - angle * Mathf.Deg2Rad),
                Mathf.Cos(angleInRadians - angle * Mathf.Deg2Rad))
                * offsetRadius * graphicRadius;
        }
    }
}

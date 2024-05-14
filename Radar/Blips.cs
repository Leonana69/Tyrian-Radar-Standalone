﻿using EFT.Interactive;
using EFT;
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Radar
{
    public class BlipPlayer : Target
    {
        private Player? _enemyPlayer = null;
        private bool _isDead = false;
        public BlipPlayer(Player enemyPlayer)
        {
            this._enemyPlayer = enemyPlayer;
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
                float totalThreshold = playerHeight * 1.2f * Radar.radarYHeightThreshold.Value;
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
                switch (_enemyPlayer?.Profile.Info.Side)
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

                _show = blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z
                     > radarRange * radarRange ? false : true;
                if (!_isDead && _enemyPlayer.HealthController.IsAlive == _isDead)
                {
                    _isDead = true;
                }

                if (_isDead)
                {
                    _show = Radar.radarEnableCorpseConfig.Value && _show;
                }

                // don't show scav corpses
                if (_isDead && _enemyPlayer.Profile.Info.Side == EPlayerSide.Savage && (
                        _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.assault
                        || _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.marksman
                        || _enemyPlayer.Profile.Info.Settings.Role == WildSpawnType.assaultGroup
                    )
                )
                {
                    _show = false;
                }
            }

            if (show && !_show && blipImage != null)
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
        public int _price = 0;
        public LootItem _item;
        public string _itemId;
        public Color _color;
        public string _tier;
        private bool _lazyUpdate;
        public int _key;

        public BlipLoot(LootItem item, bool lazyUpdate = false, int key = 0)
        {
            _item = item;
            _itemId = item.ItemId;
            _lazyUpdate = lazyUpdate;
            _key = key;
            var offer = ItemExtensions.GetBestTraderOffer(item.Item);
            targetPosition = item.TrackableTransform.position;

            if (offer != null)
            {
                _price = offer.Price;
            }
        }

        private void UpdateBlipImage()
        {
            if (blip == null || blipImage == null)
                return;
            float totalThreshold = playerHeight * 1.2f * Radar.radarYHeightThreshold.Value;
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
            // set blip color
            switch (_price)
            {
                case > 100000:
                    _tier = "BEST";
                    _color = Radar.bestLootBlipColor.Value;
                    break;
                case > 50000:
                    _tier = "BETTER";
                    _color = Radar.betterLootBlipColor.Value;
                    break;
                case > 25000:
                    _tier = "GOOD";
                    _color = Radar.goodLootBlipColor.Value;
                    break;
                default:
                    _tier = "DEFAULT";
                    _color = Radar.lootBlipColor.Value;
                    break;
            }
            blipImage.color = _color;

            float blipSize = Radar.radarBlipSizeConfig.Value * 3f;
            blip.transform.localScale = new Vector3(blipSize, blipSize, blipSize);
        }

        public void Update(bool updatePosition, bool _show = false)
        {
            if (_lazyUpdate)
            {
                targetPosition = _item.TrackableTransform.position;
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
                if (_price >= 45000)
                {
                    Radar.Log.LogInfo("Updating loot blip: " + _item.Name + " [" + _itemId + "] at " + blipPosition + " with price/slot " + _price + " and tier " + _tier);
                }
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
        protected GameObject? blip;
        protected Image? blipImage;

        protected Vector3 blipPosition;
        public Vector3 targetPosition;
        public static Vector3 playerPosition;
        public static float radarRange;

        protected float playerHeight = 1.8f;

        public void SetBlip()
        {
            var blipInstance = Object.Instantiate(AssetBundleManager.RadarBliphudPrefab,
                HaloRadar.RadarHudBlipBasePosition.position, HaloRadar.RadarHudBlipBasePosition.rotation);
            blip = blipInstance as GameObject;
            blip.transform.parent = HaloRadar.RadarHudBlipBasePosition.transform;
            blip.transform.SetAsLastSibling();

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
            float r, g, b, a;
            if (blipImage != null)
            {
                r = blipImage.color.r;
                g = blipImage.color.g;
                b = blipImage.color.b;
                a = blipImage.color.a;
                float delta_a = 1;
                if (Radar.radarScanInterval.Value > 0.8)
                {
                    float ratio = (Time.time - HaloRadar.RadarLastUpdateTime) / Radar.radarScanInterval.Value;
                    delta_a = 1 - ratio * ratio;
                }
                blipImage.color = new Color(r, g, b, a * delta_a);
            }
        }

        protected void UpdatePosition(bool updatePosition)
        {
            if (blip == null) return;
            Quaternion reverseRotation = Quaternion.Inverse(HaloRadar.RadarHudBlipBasePosition.rotation);
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
            Vector3 rotatedDirection = HaloRadar.RadarHudBlipBasePosition.rotation * Vector3.forward;
            float angle = Mathf.Atan2(rotatedDirection.x, rotatedDirection.z) * Mathf.Rad2Deg;
            float angleInRadians = Mathf.Atan2(blipPosition.x, blipPosition.z);

            // Get the scale of the RadarHudBlipBasePosition
            Vector3 scale = HaloRadar.RadarHudBlipBasePosition.localScale;
            // Multiply the sizeDelta by the scale to account for scaling
            Vector2 scaledSizeDelta = HaloRadar.RadarHudBlipBasePosition.sizeDelta;
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

using EFT;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Item = EFT.InventoryLogic.Item;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

            float totalThreshold = playerHeight * 1.5f * Radar.radarYHeightThreshold.Value;

            Sprite[] targetBlips = AssetFileManager.NormalEnemyBlips;

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
                        case WildSpawnType.shooterBTR:
                            targetBlips = AssetFileManager.BTRBlips;
                            blipImage.color = Color.red;
                            break;
                        default:
                            targetBlips = AssetFileManager.BossEnemyBlips;
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

            if (_isDead)
            {
                targetBlips = AssetFileManager.DeadEnemyBlips;
                if (outline == null && Radar.radarEnableCorpseTypeConfig.Value)
                {
                    outline = blipImage.GetOrAddComponent<Outline>();
                    outline.effectDistance = new Vector2(1, -1);
                } else if (outline != null && !Radar.radarEnableCorpseTypeConfig.Value)
                {
                    Object.Destroy(outline);
                    outline = null;
                }

                if (outline != null)
                {
                    outline.effectColor = blipImage.color;
                }
                
                blipImage.color = Radar.corpseBlipColor.Value;
            }

            if (blipPosition.y > totalThreshold)
            {
                blipImage.sprite = targetBlips[1];
            }
            else if (blipPosition.y < -totalThreshold)
            {
                blipImage.sprite = targetBlips[2];
            }
            else
            {
                blipImage.sprite = targetBlips[0];
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

    public class BlipOther : Target
    {
        public string _id;
        public Transform _transform;
        private bool _lazyUpdate;
        private int _type;

        public BlipOther(string id, Transform transform, bool lazyUpdate = false, int type = 0)
        {
            _id = id;
            _transform = transform;
            targetPosition = transform.position;
            _lazyUpdate = lazyUpdate;
            _type = type;
        }

        public void UpdatePriority(int newType)
        {
            if (_type != newType)
            {
                _type = newType;
                // Force visual update with the new priority
                if (blipImage != null)
                {
                    UpdateBlipImage();
                }
            }
        }

        private void UpdateBlipImage(Color? blipColor = null)
        {
            if (blip == null || blipImage == null)
                return;
            float totalThreshold = playerHeight * 1.5f * Radar.radarYHeightThreshold.Value;

            Sprite[] targetBlips = AssetFileManager.LootBlips;
            if (_type == 1)
            {
                targetBlips = AssetFileManager.MineBlips;
            }

            if (blipPosition.y > totalThreshold)
            {
                blipImage.sprite = targetBlips[1];
            }
            else if (blipPosition.y < -totalThreshold)
            {
                blipImage.sprite = targetBlips[2];
            }
            else
            {
                blipImage.sprite = targetBlips[0];
            }

            if (blipColor != null)
            {
                blipImage.color = blipColor.Value;
            }
            else
            {
                switch (_type)
                {
                    case 1:
                        blipImage.color = Color.black;
                        break;

                    case 2:
                        blipImage.color = Radar.wishListLootBlipColor.Value;
                        break;

                    default:
                        blipImage.color = Radar.lootBlipColor.Value;
                        break;
                }
            }

            float blipSize = Radar.radarBlipSizeConfig.Value * 3f;
            blip.transform.localScale = new Vector3(blipSize, blipSize, blipSize);
        }

        public void Update(bool _show, Color? blipColor = null)
        {
            if (_lazyUpdate)
            {
                targetPosition = _transform.position;
                _lazyUpdate = false;
            }

            blipPosition.x = targetPosition.x - playerPosition.x;
            blipPosition.y = targetPosition.y - playerPosition.y;
            blipPosition.z = targetPosition.z - playerPosition.z;

            var distance = blipPosition.x * blipPosition.x + blipPosition.z * blipPosition.z;
            _show = (distance > radarOuterRange * radarOuterRange || distance < radarInnerRange * radarInnerRange) ? false : true;
                
            if (!_show)
            {
                if (blipImage != null)
                    blipImage.color = new Color(0, 0, 0, 0);
            }
            else
            {
                UpdateBlipImage(blipColor);
                //UpdateAlpha();
                UpdatePosition(true);
            }
        }
    }

    public class Target
    {
        public bool show = false;
        protected GameObject? blip;
        protected Outline? outline;
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
            blip = Object.Instantiate(AssetFileManager.RadarBliphudPrefab);
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

        public void DestroyBlip()
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

    public class PolygonGraphic : Graphic
    {
        public List<Vector2> points = new();
        public float edgeFade = 8f; // Pixels for fade out
        public float outlineWidth = 0f; // Outline width in pixels

        protected override void OnPopulateMesh(Mesh mesh)
        {
            mesh.Clear();
            if (points.Count < 3) return;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();

            // Center of polygon
            Vector2 centroid = Vector2.zero;
            foreach (var p in points) centroid += p;
            centroid /= points.Count;

            // Add center vertex (fully opaque)
            vertices.Add(new Vector3(centroid.x, centroid.y, 0));
            colors.Add(color); // opaque center

            int centerIndex = 0;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % points.Count];

                Vector2 dir1 = (p1 - centroid).normalized * edgeFade;
                Vector2 dir2 = (p2 - centroid).normalized * edgeFade;

                Vector2 outer1 = p1 + dir1;
                Vector2 outer2 = p2 + dir2;

                int i0 = vertices.Count;
                vertices.Add(new Vector3(p1.x, p1.y, 0)); colors.Add(color);          // inner vertex
                vertices.Add(new Vector3(p2.x, p2.y, 0)); colors.Add(color);          // inner vertex
                vertices.Add(new Vector3(outer2.x, outer2.y, 0)); colors.Add(new Color(color.r, color.g, color.b, 0));  // outer transparent
                vertices.Add(new Vector3(outer1.x, outer1.y, 0)); colors.Add(new Color(color.r, color.g, color.b, 0));  // outer transparent

                // Center fan triangle
                triangles.Add(centerIndex);
                triangles.Add(i0);
                triangles.Add(i0 + 1);

                // Outer quad (two triangles for feathered edge)
                triangles.Add(i0 + 1);
                triangles.Add(i0 + 2);
                triangles.Add(i0 + 3);

                triangles.Add(i0 + 3);
                triangles.Add(i0);
                triangles.Add(i0 + 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
        }

        public void UpdatePolygon(List<Vector2> newPoints)
        {
            points = newPoints;
            SetVerticesDirty();  // Triggers mesh update
        }
    }

    public class RadarRegion
    {
        public static Vector3 playerPosition;
        public static Color initColor;
        private GameObject regionRoot;
        private List<RectTransform> cornerDots = new();
        private List<RectTransform> edgeLines = new();
        private PolygonGraphic? fillPolygon;

        private Vector3[] worldCorners = new Vector3[4];  // bottomLeft, bottomRight, topRight, topLeft
        private Vector3 center;


        public RadarRegion(Vector3[] _worldCorners)
        {
            worldCorners = _worldCorners;

            center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                center += worldCorners[i];
            }
            center /= 4f;

            regionRoot = new GameObject("RadarRegion", typeof(RectTransform));
            //regionRoot.transform.SetParent(HaloRadar.RadarBorderTransform, false);
            //regionRoot.transform.localScale = Vector3.one;

            // Add these lines:
            var rootRT = regionRoot.GetComponent<RectTransform>();
            rootRT.SetParent(HaloRadar.RadarBorderTransform, false);
            rootRT.localScale = Vector3.one;
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;

            //CreateCornerDots();
            //CreateEdgeLines();
            CreateFillPolygon();
            UpdateVisual();
        }

        public static void setPlayerPosition(Vector3 playerPosition)
        {
            RadarRegion.playerPosition = playerPosition;
        }

        private void CreateFillPolygon()
        {
            GameObject fillGO = new GameObject("RadarRegionFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(PolygonGraphic));
            fillGO.transform.SetParent(regionRoot.transform, false);

            var rect = fillGO.GetComponent<RectTransform>();

            // Match parent size and position
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = HaloRadar.RadarBorderTransform.sizeDelta;

            fillPolygon = fillGO.GetComponent<PolygonGraphic>();
            fillPolygon.color = new Color(1f, 0f, 0f, 0.3f);  // Semi-transparent red
        }


        private void CreateCornerDots()
        {
            for (int i = 0; i < 4; i++)
            {
                var dotGO = new GameObject($"Corner{i}", typeof(RectTransform), typeof(Image));
                dotGO.transform.SetParent(regionRoot.transform, false);

                var rect = dotGO.GetComponent<RectTransform>();
                var img = dotGO.GetComponent<Image>();
                rect.sizeDelta = new Vector2(6, 6);
                img.color = new Color(1f, 0.3f, 0.3f, 0.9f); // red-ish dot

                cornerDots.Add(rect);
            }
        }

        private void CreateEdgeLines()
        {
            for (int i = 0; i < 4; i++)
            {
                var edgeGO = new GameObject($"Edge{i}", typeof(RectTransform), typeof(Image));
                edgeGO.transform.SetParent(regionRoot.transform, false);

                var rect = edgeGO.GetComponent<RectTransform>();
                var img = edgeGO.GetComponent<Image>();
                rect.sizeDelta = new Vector2(2, 10); // Will stretch later
                img.color = new Color(1f, 0.3f, 0.3f, 0.5f); // transparent line

                edgeLines.Add(rect);
            }
        }

        private Vector2 WorldToRadarPosition(Vector3 worldPos)
        {
            Vector3 relative = worldPos - RadarRegion.playerPosition;

            float distance = Mathf.Sqrt(relative.x * relative.x + relative.z * relative.z);
            float scale = Mathf.Pow(
                distance / Target.radarOuterRange,
                0.4f + Radar.radarDistanceScaleConfig.Value * Radar.radarDistanceScaleConfig.Value / 2.0f
            );

            Vector3 borderScale = HaloRadar.RadarBorderTransform.localScale;
            Vector2 radarSize = HaloRadar.RadarBorderTransform.sizeDelta;
            radarSize.x *= borderScale.x;
            radarSize.y *= borderScale.y;
            float radarRadius = Mathf.Min(radarSize.x, radarSize.y) * 0.68f;

            Vector2 dir = new Vector2(relative.x, relative.z).normalized;
            return dir * radarRadius * scale;
        }

        public void UpdateVisual()
        {
            Vector2[] radarPositions = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                radarPositions[i] = WorldToRadarPosition(worldCorners[i]);
                //cornerDots[i].anchoredPosition = radarPositions[i];
            }

            // Calculate distance
            float dx = playerPosition.x - center.x;
            float dz = playerPosition.z - center.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            float alpha = 1.0f;
            if (distance > Target.radarOuterRange - 20)
            {
                float t = distance - (Target.radarOuterRange - 20.0f);
                t = Mathf.Clamp01(t / 20.0f);
                alpha = 1f - t * t * (3f - 2f * t);
            }

            if (fillPolygon != null)
            {
                Color c = initColor;
                c.a *= alpha;
                fillPolygon.color = c;
            }

            // Update edges: draw from i to (i+1)%4
            //for (int i = 0; i < 4; i++)
            //{
            //    Vector2 from = radarPositions[i];
            //    Vector2 to = radarPositions[(i + 1) % 4];
            //    Vector2 delta = to - from;

            //    RectTransform edge = edgeLines[i];
            //    edge.sizeDelta = new Vector2(2f, delta.magnitude);
            //    edge.anchoredPosition = (from + to) / 2;
            //    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            //    edge.localRotation = Quaternion.Euler(0, 0, angle - 90);
            //}


            fillPolygon?.UpdatePolygon(new List<Vector2>(radarPositions));
        }

        public void Destroy()
        {
            Object.Destroy(regionRoot);
        }
    }
}

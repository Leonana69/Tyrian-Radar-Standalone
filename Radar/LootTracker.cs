using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Decides which containers and loose items are worth a blip, keeps them in a quadtree, and works
    /// out which of them are close enough to draw on each sweep.
    /// </summary>
    internal sealed class LootTracker
    {
        /// <summary>Item owners whose root item starts with this id are player inventories.</summary>
        private const string PlayerInventoryPrefix = "55d7217a4bdc2d86028b456d";

        /// <summary>Drawers legitimately sit at identical positions, so they skip de-duplication.</summary>
        private const string DrawerPrefix = "578f87b7245977356274f2cd";

        /// <summary>Padding added around the sampled loot bounds when sizing the quadtree, in metres.</summary>
        private const float BoundsPaddingX = 5f;
        private const float BoundsPaddingZ = 2f;

        private readonly GameWorld _gameWorld;
        private readonly Player _player;
        private readonly IReadOnlyDictionary<MongoID, EWishlistGroup>? _wishlist;

        private readonly Dictionary<string, BlipOther> _blips = new Dictionary<string, BlipOther>();

        /// <summary>
        /// Containers we have subscribed to, by owner id. Never cleared during a raid: the add/remove
        /// handlers stay attached, so re-subscribing on a rebuild would double up the events.
        /// </summary>
        private readonly Dictionary<string, Transform> _containerTransforms = new Dictionary<string, Transform>();

        private Quadtree? _tree;

        // Double buffered so a sweep does not allocate once the lists have grown.
        private List<BlipOther> _visible = new List<BlipOther>();
        private List<BlipOther> _previouslyVisible = new List<BlipOther>();
        private readonly HashSet<BlipOther> _visibleSet = new HashSet<BlipOther>();
        private readonly List<BlipOther> _toHide = new List<BlipOther>();

        private float _lastScanTime;

        public LootTracker(GameWorld gameWorld, Player player)
        {
            _gameWorld = gameWorld;
            _player = player;
            _wishlist = player.Profile?.WishlistManager.GetWishlist();
        }

        /// <summary>Rescans every item owner in the world and rebuilds the spatial index.</summary>
        public void Rebuild()
        {
            Clear();

            float xMin = float.MaxValue, xMax = float.MinValue;
            float zMin = float.MaxValue, zMax = float.MinValue;

            // Reverse order keeps the last owner registered at a shared position, matching the game's
            // own draw order for stacked containers.
            var seenPositions = new HashSet<Vector3>();
            foreach (var entry in _gameWorld.ItemOwners.Reverse())
            {
                IItemOwner owner = entry.Key;
                Transform transform = entry.Value.Transform;

                if (transform == null || owner.RootItem.Name.StartsWith(PlayerInventoryPrefix))
                    continue;

                if (!owner.ContainerName.StartsWith(DrawerPrefix) && !seenPositions.Add(transform.position))
                    continue;

                Item rootItem = owner.Items.First();
                Add(owner.ID, rootItem, transform);

                if (rootItem.IsContainer && !_containerTransforms.ContainsKey(owner.ID))
                {
                    _containerTransforms[owner.ID] = transform;
                    owner.AddItemEvent += args => OnContainerAddItem(owner, args);
                    owner.RemoveItemEvent += args => OnContainerRemoveItem(owner, args);
                }

                Vector3 position = transform.position;
                xMin = Mathf.Min(xMin, position.x);
                xMax = Mathf.Max(xMax, position.x);
                zMin = Mathf.Min(zMin, position.z);
                zMax = Mathf.Max(zMax, position.z);
            }

            _tree = new Quadtree(Rect.MinMaxRect(
                xMin - BoundsPaddingX, zMin - BoundsPaddingZ,
                xMax + BoundsPaddingX, zMax + BoundsPaddingZ));

            foreach (BlipOther blip in _blips.Values)
                _tree.Insert(blip);

            // Show the new set on the next frame rather than waiting out a full scan interval.
            _lastScanTime = float.NegativeInfinity;
        }

        /// <summary>Registers an item if it clears the value or wishlist filter.</summary>
        /// <param name="lazyUpdate">
        /// Defer sampling the transform until the blip is first drawn. Items the game has just spawned
        /// have not settled yet, so reading their position immediately gives the wrong answer.
        /// </param>
        public void Add(string id, Item item, Transform transform, bool lazyUpdate = false)
        {
            if (item.Name.StartsWith(PlayerInventoryPrefix))
                return;

            bool wishlisted = RadarConfig.WishlistLootEnabled.Value && IsWishlisted(item);
            bool valuable = RadarConfig.LootEnabled.Value && IsValuable(item);
            if (!wishlisted && !valuable)
                return;

            // Re-registering the same id would otherwise strand the previous blip's GameObject.
            Remove(id);

            var blip = new BlipOther(id, transform, lazyUpdate, wishlisted ? BlipKind.WishlistLoot : BlipKind.Loot);
            _blips[id] = blip;
            _tree?.Insert(blip);
        }

        public void Remove(string id)
        {
            if (!_blips.TryGetValue(id, out BlipOther blip))
                return;

            _blips.Remove(id);
            _tree?.Remove(new Vector2(blip.TargetPosition.x, blip.TargetPosition.z), id);
            blip.DestroyBlip();
        }

        public void RemoveByKey(int key)
        {
            LootItem item = _gameWorld.LootItems.GetByKey(key);
            Remove(item.ItemId);
        }

        public void Clear()
        {
            foreach (BlipOther blip in _blips.Values)
                blip.DestroyBlip();

            _blips.Clear();
            _tree?.Clear();
            _tree = null;

            _visible.Clear();
            _previouslyVisible.Clear();
            _visibleSet.Clear();
            _toHide.Clear();
        }

        /// <summary>
        /// Re-queries which loot is in range. Runs on its own timer so it stays on the scan interval
        /// even when Fire Mode drives the player sweep at a much higher rate.
        /// </summary>
        public void Tick()
        {
            if (Time.time - _lastScanTime < RadarConfig.ScanInterval.Value)
                return;

            _lastScanTime = Time.time;

            (_previouslyVisible, _visible) = (_visible, _previouslyVisible);
            _visible.Clear();

            Vector3 position = _player.Transform.position;
            _tree?.QueryRange(new Vector2(position.x, position.z), RadarConfig.OuterRange.Value, _visible);

            _visibleSet.Clear();
            foreach (BlipOther blip in _visible)
                _visibleSet.Add(blip);

            _toHide.Clear();
            foreach (BlipOther blip in _previouslyVisible)
            {
                if (!_visibleSet.Contains(blip))
                    _toHide.Add(blip);
            }
        }

        public void Render()
        {
            // Blips that just left range still need a frame to clear themselves off the face.
            foreach (BlipOther blip in _toHide)
                blip.Update();

            foreach (BlipOther blip in _visible)
                blip.Update();
        }

        private void OnContainerAddItem(IItemOwner owner, AddItemEventArgs args)
        {
            bool wishlisted = IsWishlisted(args.Item);
            if (!wishlisted && !IsValuable(args.Item))
                return;

            if (!_blips.ContainsKey(owner.ID))
            {
                if (_containerTransforms.TryGetValue(owner.ID, out Transform transform))
                    Add(owner.ID, owner.Items.First(), transform);
            }
            else if (wishlisted && IsWishlisted(owner.Items.First()))
            {
                SetKind(owner.ID, BlipKind.WishlistLoot);
            }
        }

        private void OnContainerRemoveItem(IItemOwner owner, RemoveItemEventArgs args)
        {
            if (!IsWishlisted(args.Item) && !IsValuable(args.Item))
                return;

            Item root = owner.Items.First();
            bool stillWishlisted = IsWishlisted(root);
            bool stillValuable = IsValuable(root);

            if (!stillWishlisted && !stillValuable)
                Remove(owner.ID);
            else if (!stillWishlisted)
                SetKind(owner.ID, BlipKind.Loot);
        }

        private void SetKind(string id, BlipKind kind)
        {
            if (_blips.TryGetValue(id, out BlipOther blip))
                blip.SetKind(kind);
        }

        /// <summary>True when the item - or anything inside it - beats the configured price threshold.</summary>
        private static bool IsValuable(Item item)
        {
            if (!item.IsContainer)
                return PriceOf(item) > RadarConfig.LootThreshold.Value;

            int best = 0;
            foreach (Item subItem in item.GetAllItems())
                best = Mathf.Max(best, PriceOf(subItem));

            return best > RadarConfig.LootThreshold.Value;
        }

        private static int PriceOf(Item item)
        {
            int price = ItemPricing.GetBestPrice(item);
            if (!RadarConfig.LootValuePerSlot.Value)
                return price;

            var cellSize = item.CalculateCellSize();
            int slots = cellSize.X * cellSize.Y;
            return slots > 0 ? price / slots : price;
        }

        private bool IsWishlisted(Item item)
        {
            if (_wishlist == null)
                return false;

            if (!item.IsContainer)
                return IsTemplateWishlisted(item.TemplateId);

            foreach (Item subItem in item.GetAllItems())
            {
                if (IsTemplateWishlisted(subItem.TemplateId))
                    return true;
            }

            return false;
        }

        private bool IsTemplateWishlisted(MongoID templateId) =>
            _wishlist!.TryGetValue(templateId, out EWishlistGroup group) && group == EWishlistGroup.Other;
    }
}

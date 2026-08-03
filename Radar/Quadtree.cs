using System.Collections.Generic;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Point quadtree over the map's XZ plane, so each radar sweep only touches loot near the player
    /// instead of every tracked item on the map.
    /// </summary>
    public class Quadtree
    {
        private readonly QuadtreeNode _root;

        /// <summary>
        /// Loot outside the bounds sampled at raid start - dropped bags, airdrops, anything the game
        /// spawns later. Too rare to justify rebuilding the tree, so it is kept in a flat list that
        /// every query also scans.
        /// </summary>
        private readonly List<(Vector2 point, BlipOther blip)> _outOfBounds = new List<(Vector2, BlipOther)>();

        public Quadtree(Rect bounds, int maxDepth = 4)
        {
            _root = new QuadtreeNode(bounds, maxDepth);
        }

        public void Insert(BlipOther blip)
        {
            var point = new Vector2(blip.TargetPosition.x, blip.TargetPosition.z);
            if (!_root.Insert(point, blip))
                _outOfBounds.Add((point, blip));
        }

        public void Remove(Vector2 point, string id)
        {
            if (_root.Remove(point, id)) return;

            for (int i = 0; i < _outOfBounds.Count; i++)
            {
                if (_outOfBounds[i].blip.Id == id)
                {
                    _outOfBounds.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>Appends every blip within <paramref name="radius"/> of the centre to <paramref name="results"/>.</summary>
        public void QueryRange(Vector2 center, float radius, List<BlipOther> results)
        {
            var range = new Circle(center, radius);
            _root.QueryRange(range, results);

            foreach (var entry in _outOfBounds)
            {
                if (range.Contains(entry.point))
                    results.Add(entry.blip);
            }
        }

        public void Clear()
        {
            _root.Clear();
            _outOfBounds.Clear();
        }
    }

    internal class QuadtreeNode
    {
        private const int MaxObjectsBeforeSubdivide = 10;

        private readonly Rect _bounds;
        private readonly int _maxDepth;
        private readonly int _depth;
        private readonly List<(Vector2 point, BlipOther blip)> _objects = new List<(Vector2, BlipOther)>();
        private QuadtreeNode[]? _children;

        public QuadtreeNode(Rect bounds, int maxDepth, int depth = 0)
        {
            _bounds = bounds;
            _maxDepth = maxDepth;
            _depth = depth;
        }

        /// <returns>False if the point lies outside this node, so the caller can look elsewhere.</returns>
        public bool Insert(Vector2 point, BlipOther blip)
        {
            if (!_bounds.Contains(point)) return false;

            if (_children == null)
            {
                if (_objects.Count < MaxObjectsBeforeSubdivide || _depth == _maxDepth)
                {
                    _objects.Add((point, blip));
                    return true;
                }

                Subdivide();
            }

            foreach (QuadtreeNode child in _children!)
            {
                if (child.Insert(point, blip))
                    return true;
            }

            // The children tile this node exactly, so this only happens for a point landing on a
            // boundary that rounds the wrong way. Keeping it here is still correct - every read path
            // checks a node's own objects before descending into its children.
            _objects.Add((point, blip));
            return true;
        }

        public bool Remove(Vector2 point, string id)
        {
            if (!_bounds.Contains(point)) return false;

            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].blip.Id == id)
                {
                    _objects.RemoveAt(i);
                    return true;
                }
            }

            if (_children == null) return false;

            foreach (QuadtreeNode child in _children)
            {
                if (child.Remove(point, id))
                    return true;
            }

            return false;
        }

        public void QueryRange(Circle range, List<BlipOther> results)
        {
            if (!_bounds.Overlaps(range.GetBounds())) return;

            foreach (var entry in _objects)
            {
                if (range.Contains(entry.point))
                    results.Add(entry.blip);
            }

            if (_children == null) return;

            foreach (QuadtreeNode child in _children)
                child.QueryRange(range, results);
        }

        public void Clear()
        {
            _objects.Clear();
            _children = null;
        }

        private void Subdivide()
        {
            float halfWidth = _bounds.width / 2f;
            float halfHeight = _bounds.height / 2f;
            float x = _bounds.x;
            float y = _bounds.y;

            _children = new[]
            {
                new QuadtreeNode(new Rect(x, y + halfHeight, halfWidth, halfHeight), _maxDepth, _depth + 1),
                new QuadtreeNode(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight), _maxDepth, _depth + 1),
                new QuadtreeNode(new Rect(x, y, halfWidth, halfHeight), _maxDepth, _depth + 1),
                new QuadtreeNode(new Rect(x + halfWidth, y, halfWidth, halfHeight), _maxDepth, _depth + 1),
            };

            var pending = new List<(Vector2 point, BlipOther blip)>(_objects);
            _objects.Clear();

            foreach (var entry in pending)
            {
                bool placed = false;
                foreach (QuadtreeNode child in _children)
                {
                    if (child.Insert(entry.point, entry.blip))
                    {
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    _objects.Add(entry);
            }
        }
    }

    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public bool Contains(Vector2 point) => (point - center).sqrMagnitude <= radius * radius;

        public Rect GetBounds() => new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
    }
}

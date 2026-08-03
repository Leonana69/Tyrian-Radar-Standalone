using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Radar
{
    /// <summary>
    /// A filled UI polygon with a feathered edge, used to shade minefield footprints on the radar face.
    /// </summary>
    public class PolygonGraphic : Graphic
    {
        /// <summary>Width of the alpha falloff around the polygon, in pixels.</summary>
        public float edgeFade = 8f;

        /// <summary>Reused across rebuilds so a per-frame polygon update does not allocate.</summary>
        private readonly List<Vector2> _points = new List<Vector2>();

        public void UpdatePolygon(IList<Vector2> newPoints)
        {
            _points.Clear();
            for (int i = 0; i < newPoints.Count; i++)
                _points.Add(newPoints[i]);

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 3) return;

            Vector2 centroid = Vector2.zero;
            foreach (Vector2 point in _points)
                centroid += point;
            centroid /= _points.Count;

            Color32 solid = color;
            Color32 transparent = new Color(color.r, color.g, color.b, 0f);

            // Vertex 0 is the opaque centre that every edge quad fans back to.
            vh.AddVert(new Vector3(centroid.x, centroid.y, 0f), solid, Vector2.zero);

            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 p1 = _points[i];
                Vector2 p2 = _points[(i + 1) % _points.Count];

                // Push each edge outwards along its centroid ray to build the fade skirt.
                Vector2 outer1 = p1 + (p1 - centroid).normalized * edgeFade;
                Vector2 outer2 = p2 + (p2 - centroid).normalized * edgeFade;

                int i0 = vh.currentVertCount;
                vh.AddVert(new Vector3(p1.x, p1.y, 0f), solid, Vector2.zero);
                vh.AddVert(new Vector3(p2.x, p2.y, 0f), solid, Vector2.zero);
                vh.AddVert(new Vector3(outer2.x, outer2.y, 0f), transparent, Vector2.zero);
                vh.AddVert(new Vector3(outer1.x, outer1.y, 0f), transparent, Vector2.zero);

                vh.AddTriangle(0, i0, i0 + 1);           // centre fan
                vh.AddTriangle(i0 + 1, i0 + 2, i0 + 3);  // feathered skirt
                vh.AddTriangle(i0 + 3, i0, i0 + 1);
            }
        }
    }
}

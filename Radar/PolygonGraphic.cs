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
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Color32> _colors = new List<Color32>();

        public void UpdatePolygon(IList<Vector2> newPoints)
        {
            _points.Clear();
            for (int i = 0; i < newPoints.Count; i++)
                _points.Add(newPoints[i]);

            SetVerticesDirty();
        }

        [System.Obsolete("Required for compatibility with EFT's legacy uGUI mesh path.")]
        protected override void OnPopulateMesh(Mesh mesh)
        {
            mesh.Clear();
            if (_points.Count < 3) return;

            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();

            Vector2 centroid = Vector2.zero;
            foreach (Vector2 point in _points)
                centroid += point;
            centroid /= _points.Count;

            Color32 solid = color;
            Color32 transparent = new Color(color.r, color.g, color.b, 0f);

            // Vertex 0 is the opaque centre that every edge quad fans back to.
            AddVertex(centroid, solid);

            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 p1 = _points[i];
                Vector2 p2 = _points[(i + 1) % _points.Count];

                // Push each edge outwards along its centroid ray to build the fade skirt.
                Vector2 outer1 = p1 + (p1 - centroid).normalized * edgeFade;
                Vector2 outer2 = p2 + (p2 - centroid).normalized * edgeFade;

                int i0 = _vertices.Count;
                AddVertex(p1, solid);
                AddVertex(p2, solid);
                AddVertex(outer2, transparent);
                AddVertex(outer1, transparent);

                AddTriangle(0, i0, i0 + 1);           // centre fan
                AddTriangle(i0 + 1, i0 + 2, i0 + 3);  // feathered skirt
                AddTriangle(i0 + 3, i0, i0 + 1);
            }

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetColors(_colors);
        }

        private void AddVertex(Vector2 position, Color32 color)
        {
            _vertices.Add(new Vector3(position.x, position.y, 0f));
            _colors.Add(color);
        }

        private void AddTriangle(int index0, int index1, int index2)
        {
            _triangles.Add(index0);
            _triangles.Add(index1);
            _triangles.Add(index2);
        }
    }
}

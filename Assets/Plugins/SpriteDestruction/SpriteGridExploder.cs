using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDestruction
{
    public static class SpriteGridExploder
    {
        public static List<GameObject> GenerateGridPieces(GameObject source, float cellSize, Material mat = null)
        {
            List<GameObject> pieces = new();
            if (cellSize <= 0f)
            {
                Debug.LogWarning("SpriteGridExploder: cellSize must be greater than zero.");
                return pieces;
            }

            if (!mat)
            {
                mat = CreateFragmentMaterial(source);
            }

            Transform rootTransform = source.transform;
            Vector3 lossyScale = rootTransform.lossyScale;
            float cellSizeX = cellSize / Mathf.Max(Mathf.Abs(lossyScale.x), 1e-6f);
            float cellSizeY = cellSize / Mathf.Max(Mathf.Abs(lossyScale.y), 1e-6f);

            Vector2 origVelocity = source.GetComponent<Rigidbody2D>().linearVelocity;

            if (!TryGetColliderPoints(source, out List<Vector2> borderPoints))
            {
                borderPoints = new List<Vector2>();
            }

            Rect bounds = GetRendererRect(source);
            int columns = Mathf.Max(1, Mathf.FloorToInt(bounds.width / cellSizeX));
            int rows = Mathf.Max(1, Mathf.FloorToInt(bounds.height / cellSizeY));

            float gridWidth = columns * cellSizeX;
            float gridHeight = rows * cellSizeY;
            float startX = bounds.x + (bounds.width - gridWidth) * 0.5f;
            float startY = bounds.y + (bounds.height - gridHeight) * 0.5f;

            SpriteRenderer spriteRenderer = source.GetComponentInChildren<SpriteRenderer>();

            List<Vector2> cellCorners = new(4);
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    float x0 = startX + col * cellSizeX;
                    float y0 = startY + row * cellSizeY;
                    float x1 = x0 + cellSizeX;
                    float y1 = y0 + cellSizeY;

                    cellCorners.Clear();
                    cellCorners.Add(new Vector2(x0, y0));
                    cellCorners.Add(new Vector2(x0, y1));
                    cellCorners.Add(new Vector2(x1, y1));
                    cellCorners.Add(new Vector2(x1, y0));

                    List<List<Vector2>> clippedCells = GridPolygonClipper.Clip(borderPoints, cellCorners);
                    foreach (List<Vector2> clippedCell in clippedCells)
                    {
                        if (clippedCell.Count < 3)
                        {
                            continue;
                        }

                        pieces.Add(GeneratePiece(source, rootTransform, spriteRenderer, clippedCell, origVelocity, mat));
                    }
                }
            }

            return pieces;
        }

        private static GameObject GeneratePiece(
            GameObject source,
            Transform rootTransform,
            SpriteRenderer spriteRenderer,
            List<Vector2> polygon,
            Vector2 origVelocity,
            Material mat)
        {
            GameObject piece = new GameObject(source.name + " grid piece");
            piece.transform.SetPositionAndRotation(rootTransform.position, rootTransform.rotation);
            piece.transform.localScale = rootTransform.localScale;

            MeshFilter meshFilter = piece.AddComponent<MeshFilter>();
            piece.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[polygon.Count];
            for (int i = 0; i < polygon.Count; i++)
            {
                vertices[i] = new Vector3(polygon[i].x, polygon[i].y, 0f);
            }

            mesh.vertices = vertices;
            mesh.triangles = TriangulateFan(polygon.Count);

            mesh.uv = spriteRenderer
                ? SpriteDrawUV.Calculate(spriteRenderer, rootTransform, vertices)
                : CalcUV(vertices, source.GetComponentInChildren<MeshRenderer>(), rootTransform);

            Vector3 pivotDiff = CalcPivotCenterDiff(piece, vertices);
            CenterMeshPivot(piece, mesh, pivotDiff);
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            piece.GetComponent<MeshRenderer>().sharedMaterial = mat;

            PolygonCollider2D collider = piece.AddComponent<PolygonCollider2D>();
            Vector2[] colliderPoints = new Vector2[polygon.Count];
            for (int i = 0; i < polygon.Count; i++)
            {
                colliderPoints[i] = polygon[i] + (Vector2)pivotDiff;
            }

            collider.SetPath(0, colliderPoints);

            Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D>();
            rigidbody.linearVelocity = origVelocity;

            return piece;
        }

        private static int[] TriangulateFan(int vertexCount)
        {
            int[] triangles = new int[(vertexCount - 2) * 3];
            int index = 0;
            for (int i = 1; i < vertexCount - 1; i++)
            {
                triangles[index++] = 0;
                triangles[index++] = i;
                triangles[index++] = i + 1;
            }

            return triangles;
        }

        private static bool TryGetColliderPoints(GameObject source, out List<Vector2> points)
        {
            PolygonCollider2D polyCollider = source.GetComponent<PolygonCollider2D>();
            if (polyCollider)
            {
                points = GetPoints(polyCollider);
                return true;
            }

            BoxCollider2D boxCollider = source.GetComponent<BoxCollider2D>();
            if (boxCollider)
            {
                points = GetPoints(boxCollider);
                return true;
            }

            CircleCollider2D circleCollider = source.GetComponent<CircleCollider2D>();
            if (circleCollider)
            {
                points = GetPoints(circleCollider);
                return true;
            }

            points = null;
            return false;
        }

        private static List<Vector2> GetPoints(BoxCollider2D collider)
        {
            Vector2 center = collider.offset;
            Vector2 half = collider.size * 0.5f;
            return new List<Vector2>
            {
                new Vector2(center.x - half.x, center.y - half.y),
                new Vector2(center.x - half.x, center.y + half.y),
                new Vector2(center.x + half.x, center.y + half.y),
                new Vector2(center.x + half.x, center.y - half.y)
            };
        }

        private static List<Vector2> GetPoints(CircleCollider2D collider, int segments = 32)
        {
            List<Vector2> points = new(segments);
            Vector2 center = collider.offset;
            float radius = collider.radius;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            return points;
        }

        private static List<Vector2> GetPoints(PolygonCollider2D collider)
        {
            return collider.GetPath(0).ToList();
        }

        private static Rect GetRendererRect(GameObject source)
        {
            SpriteRenderer spriteRenderer = source.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer)
            {
                return SpriteDrawUV.GetRendererRectInRootSpace(source.transform, spriteRenderer);
            }

            Renderer renderer = source.GetComponentInChildren<Renderer>();
            Bounds localBounds = renderer.localBounds;
            return new Rect(
                localBounds.center.x - localBounds.extents.x,
                localBounds.center.y - localBounds.extents.y,
                localBounds.size.x,
                localBounds.size.y);
        }

        private static Vector2[] CalcUV(Vector3[] vertices, MeshRenderer meshRenderer, Transform rootTransform)
        {
            float texHeight = (meshRenderer.bounds.extents.y * 2f) / rootTransform.lossyScale.y;
            float texWidth = (meshRenderer.bounds.extents.x * 2f) / rootTransform.lossyScale.x;
            Vector3 bottomLeft = rootTransform.InverseTransformPoint(
                new Vector3(
                    meshRenderer.bounds.center.x - meshRenderer.bounds.extents.x,
                    meshRenderer.bounds.center.y - meshRenderer.bounds.extents.y,
                    0f));

            Vector2[] sourceUV = rootTransform.GetComponentInChildren<MeshFilter>().sharedMesh.uv;
            GetUVRange(sourceUV, out Vector2 uvMin, out Vector2 uvMax);

            Vector2[] uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                float x = ScaleRange((vertices[i].x - bottomLeft.x) / texWidth, 0f, 1f, uvMin.x, uvMax.x);
                float y = ScaleRange((vertices[i].y - bottomLeft.y) / texHeight, 0f, 1f, uvMin.y, uvMax.y);
                uv[i] = new Vector2(x, y);
            }

            return uv;
        }

        private static void GetUVRange(Vector2[] uv, out Vector2 min, out Vector2 max)
        {
            min = uv[0];
            max = uv[0];

            foreach (Vector2 point in uv)
            {
                if (point.x < min.x) min.x = point.x;
                if (point.x > max.x) max.x = point.x;
                if (point.y < min.y) min.y = point.y;
                if (point.y > max.y) max.y = point.y;
            }
        }

        private static float ScaleRange(float target, float oldMin, float oldMax, float newMin, float newMax)
        {
            return target / ((oldMax - oldMin) / (newMax - newMin)) + newMin;
        }

        private static Vector3 CalcPivotCenterDiff(GameObject target, Vector3[] vertices)
        {
            Vector3 sum = vertices.Aggregate(Vector3.zero, (current, t) => current + t);
            Vector3 center = sum / vertices.Length;
            Vector3 pivot = target.transform.InverseTransformPoint(target.transform.position);
            return pivot - center;
        }

        private static void CenterMeshPivot(GameObject target, Mesh mesh, Vector3 diff)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += diff;
            }

            mesh.vertices = vertices;

            Vector3 pivot = target.transform.InverseTransformPoint(target.transform.position);
            target.transform.localPosition = target.transform.TransformPoint(pivot - diff);
        }

        private static Material CreateFragmentMaterial(GameObject source)
        {
            SpriteRenderer spriteRenderer = source.GetComponentInChildren<SpriteRenderer>();
            if (!spriteRenderer)
            {
                return source.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            }
            
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            mat.color = spriteRenderer.color;
            SpriteDrawUV.ConfigureMaterial(spriteRenderer, mat);
            return mat;
        }
    }
}

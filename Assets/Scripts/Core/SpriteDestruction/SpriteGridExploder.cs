using System;
using System.Collections.Generic;
using AssetsSystem;
using Managers;
using UnityEngine;

namespace Core.SpriteDestruction
{
    public static class SpriteGridExploder
    {
        public const int MaxGridCellCount = 4096;

        private const int MaxGeometryCacheEntries = 64;
        private const int MaxMaterialCacheEntries = 64;

        private static GridFragment _piecePrefab;
        private static readonly List<Vector3> VerticesBuffer = new(16);
        private static readonly List<Vector2> UvBuffer = new(16);
        private static readonly List<int> TrianglesBuffer = new(24);
        private static readonly List<Vector2> CellCorners = new(4);
        private static readonly List<Rect> SplitRects = new(16);
        private static readonly GeometryData GeometryBuffer = new();
        private static readonly Dictionary<GeometryCacheKey, GeometryTemplate> GeometryCache = new();
        private static readonly Dictionary<MaterialCacheKey, Material> MaterialCache = new();

        public static List<GridFragment> GenerateGridPieces(
            GameObject source,
            float cellSize,
            Material mat = null,
            bool useColliderBounds = false,
            List<Vector3[]> rootLocalVertices = null,
            SpriteRenderer spriteSource = null)
        {
            List<GridFragment> pieces = new();
            if (!TryGetGridLayout(source, cellSize, useColliderBounds, out GridLayout layout))
            {
                return pieces;
            }

            if (!source.TryGetComponent(out Rigidbody2D sourceBody))
            {
                Debug.LogWarning("SpriteGridExploder: source requires a Rigidbody2D.", source);
                return pieces;
            }

            SpriteRenderer spriteRenderer = spriteSource
                ? spriteSource
                : source.GetComponentInChildren<SpriteRenderer>();
            bool ownsMaterial = false;
            if (!mat)
            {
                mat = CreateFragmentMaterial(spriteRenderer, source, Application.isPlaying, out ownsMaterial);
            }

            if (!mat)
            {
                Debug.LogWarning("SpriteGridExploder: unable to resolve a fragment material.", source);
                return pieces;
            }

            Transform rootTransform = source.transform;
            GeometryCacheKey cacheKey = default;
            bool canCache = Application.isPlaying
                && TryCreateGeometryCacheKey(
                    source,
                    spriteRenderer,
                    layout,
                    cellSize,
                    useColliderBounds,
                    out cacheKey);
            bool restoredFromCache = canCache && TryRestoreCachedGeometry(cacheKey, GeometryBuffer);
            SpriteDrawUV.SpriteUvContext uvContext = new SpriteDrawUV.SpriteUvContext(
                spriteRenderer,
                rootTransform,
                !restoredFromCache);
            if (!restoredFromCache)
            {
                CollectPieces(
                    source,
                    layout,
                    useColliderBounds,
                    uvContext,
                    GeometryBuffer);
                if (canCache)
                {
                    CacheGeometry(cacheKey, GeometryBuffer);
                }
            }

            if (GeometryBuffer.Pending.Count == 0)
            {
                if (ownsMaterial)
                {
                    DestroyMaterial(mat);
                }

                return pieces;
            }

            if (!CreatePieces(GeometryBuffer, pieces))
            {
                if (ownsMaterial)
                {
                    DestroyMaterial(mat);
                }

                return pieces;
            }

            Vector2 origVelocity = sourceBody.linearVelocity;
            int count = Mathf.Min(GeometryBuffer.Pending.Count, pieces.Count);
            for (int i = 0; i < count; i++)
            {
                GridFragment piece = FillPiece(
                    pieces[i],
                    rootTransform,
                    uvContext,
                    GeometryBuffer,
                    GeometryBuffer.Pending[i],
                    origVelocity,
                    mat,
                    rootLocalVertices);
                if (!piece)
                {
                    if (pieces[i])
                    {
                        pieces[i].Release();
                    }

                    pieces[i] = null;
                    continue;
                }

            }

            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                if (i >= count || !pieces[i])
                {
                    if (i >= count && pieces[i])
                    {
                        pieces[i].Release();
                    }

                    pieces.RemoveAt(i);
                }
            }

            if (ownsMaterial && !GridFragment.TransferMaterialOwnership(pieces, mat))
            {
                DestroyMaterial(mat);
            }

            return pieces;
        }

        public static bool TryGetGridLayout(
            GameObject source,
            float cellSize,
            bool useColliderBounds,
            out GridLayout layout)
        {
            layout = default;
            if (!source
                || !IsFinite(cellSize)
                || cellSize <= 1e-6f
                || !TryGetGridRect(source, useColliderBounds, out Rect bounds))
            {
                return false;
            }

            if (bounds.width <= 1e-6f || bounds.height <= 1e-6f)
            {
                return false;
            }

            float columnEstimate = bounds.width / cellSize;
            float rowEstimate = bounds.height / cellSize;
            if (!IsFinite(columnEstimate)
                || !IsFinite(rowEstimate)
                || columnEstimate > MaxGridCellCount
                || rowEstimate > MaxGridCellCount)
            {
                return false;
            }

            int columns = Mathf.Max(1, Mathf.RoundToInt(bounds.width / cellSize));
            int rows = Mathf.Max(1, Mathf.RoundToInt(bounds.height / cellSize));
            if ((long)columns * rows > MaxGridCellCount)
            {
                return false;
            }

            layout = new GridLayout(bounds, columns, rows);
            return true;
        }

        public static List<List<Vector2>> ClipCell(
            List<Vector2> cellCorners,
            float x0,
            float y0,
            float x1,
            float y1,
            bool useColliderBounds,
            bool hasCircle,
            CircleCollider2D circleCollider,
            List<Vector2> borderPoints)
        {
            if (hasCircle && circleCollider)
            {
                List<Vector2> clipped = GridPolygonClipper.ClipRectToCircle(
                    Rect.MinMaxRect(x0, y0, x1, y1),
                    circleCollider.offset,
                    circleCollider.radius);
                return clipped.Count >= 3
                    ? new List<List<Vector2>> { clipped }
                    : new List<List<Vector2>>();
            }

            if (useColliderBounds && borderPoints != null && borderPoints.Count >= 3)
            {
                return GridPolygonClipper.Clip(borderPoints, cellCorners);
            }

            return new List<List<Vector2>> { new (cellCorners) };
        }

        public readonly struct GridLayout
        {
            public readonly Rect Bounds;
            public readonly int Columns;
            public readonly int Rows;
            public readonly float CellWidth;
            public readonly float CellHeight;

            public GridLayout(Rect bounds, int columns, int rows)
            {
                Bounds = bounds;
                Columns = Mathf.Max(1, columns);
                Rows = Mathf.Max(1, rows);
                CellWidth = bounds.width / Columns;
                CellHeight = bounds.height / Rows;
            }

            public void GetCell(int column, int row, out float x0, out float y0, out float x1, out float y1)
            {
                x0 = Bounds.xMin + column * CellWidth;
                y0 = Bounds.yMin + row * CellHeight;
                x1 = column == Columns - 1 ? Bounds.xMax : x0 + CellWidth;
                y1 = row == Rows - 1 ? Bounds.yMax : y0 + CellHeight;
            }
        }

        private static void CollectPieces(
            GameObject source,
            GridLayout layout,
            bool useColliderBounds,
            SpriteDrawUV.SpriteUvContext uvContext,
            GeometryData geometry)
        {
            geometry.Clear(layout.Columns * layout.Rows);
            bool hasCircle = source.TryGetComponent(out CircleCollider2D circleCollider);
            List<Vector2> borderPoints = null;
            if (!hasCircle)
            {
                TryGetColliderPoints(source, out borderPoints);
            }

            Rect boxBounds = default;
            bool hasBoxBounds = false;
            if (!hasCircle && useColliderBounds)
            {
                hasBoxBounds = TryGetAxisAlignedBounds(borderPoints, out boxBounds);
            }

            bool needsClip = hasCircle
                || (useColliderBounds && borderPoints is { Count: >= 3 });
            for (int row = 0; row < layout.Rows; row++)
            {
                for (int col = 0; col < layout.Columns; col++)
                {
                    layout.GetCell(col, row, out float x0, out float y0, out float x1, out float y1);
                    if (hasCircle)
                    {
                        int relation = ClassifyRectVsCircle(
                            x0,
                            y0,
                            x1,
                            y1,
                            circleCollider.offset,
                            circleCollider.radius);
                        if (relation < 0)
                        {
                            continue;
                        }

                        if (relation > 0)
                        {
                            AppendRectPiece(geometry, uvContext, x0, y0, x1, y1);
                            continue;
                        }
                    }
                    else if (hasBoxBounds)
                    {
                        int relation = ClassifyRectVsRect(x0, y0, x1, y1, boxBounds);
                        if (relation < 0)
                        {
                            continue;
                        }

                        if (relation > 0)
                        {
                            AppendRectPiece(geometry, uvContext, x0, y0, x1, y1);
                            continue;
                        }
                    }
                    else if (!needsClip)
                    {
                        AppendRectPiece(geometry, uvContext, x0, y0, x1, y1);
                        continue;
                    }

                    CellCorners.Clear();
                    CellCorners.Add(new Vector2(x0, y0));
                    CellCorners.Add(new Vector2(x0, y1));
                    CellCorners.Add(new Vector2(x1, y1));
                    CellCorners.Add(new Vector2(x1, y0));

                    AppendClippedPiece(
                        geometry,
                        uvContext,
                        ClipCell(
                            CellCorners,
                            x0,
                            y0,
                            x1,
                            y1,
                            useColliderBounds,
                            hasCircle,
                            circleCollider,
                            borderPoints));
                }
            }
        }

        private static void AppendRectPiece(
            GeometryData geometry,
            SpriteDrawUV.SpriteUvContext uvContext,
            float x0,
            float y0,
            float x1,
            float y1)
        {
            int polygonStart = geometry.Polygons.Count;
            SplitRects.Clear();
            uvContext.SplitAxisAlignedRect(Rect.MinMaxRect(x0, y0, x1, y1), SplitRects);
            for (int i = 0; i < SplitRects.Count; i++)
            {
                AppendQuadPolygon(geometry, SplitRects[i]);
            }

            AddPendingRange(geometry, polygonStart);
        }

        private static void AppendClippedPiece(
            GeometryData geometry,
            SpriteDrawUV.SpriteUvContext uvContext,
            List<List<Vector2>> polygons)
        {
            if (polygons == null)
            {
                return;
            }

            int polygonStart = geometry.Polygons.Count;
            int pointStart = geometry.Points.Count;
            for (int i = 0; i < polygons.Count; i++)
            {
                List<Vector2> polygon = polygons[i];
                if (polygon == null || polygon.Count < 3)
                {
                    continue;
                }

                if (TryGetAxisAlignedRect(polygon, out Rect rect))
                {
                    SplitRects.Clear();
                    uvContext.SplitAxisAlignedRect(rect, SplitRects);
                    for (int j = 0; j < SplitRects.Count; j++)
                    {
                        AppendQuadPolygon(geometry, SplitRects[j]);
                    }
                }
                else
                {
                    AppendPolygon(geometry, polygon);
                }
            }

            if (!AddPendingRange(geometry, polygonStart))
            {
                geometry.Points.RemoveRange(pointStart, geometry.Points.Count - pointStart);
            }
        }

        private static bool AddPendingRange(GeometryData geometry, int polygonStart)
        {
            int polygonCount = geometry.Polygons.Count - polygonStart;
            if (polygonCount <= 0)
            {
                return false;
            }

            GridFragment.PolygonRange first = geometry.Polygons[polygonStart];
            Rect rect = default;
            bool isSimpleQuad = polygonCount == 1
                && TryGetAxisAlignedRect(geometry.Points, first.Start, first.Count, out rect);
            geometry.Pending.Add(new PendingRange(
                polygonStart,
                polygonCount,
                isSimpleQuad,
                isSimpleQuad ? rect : default));
            return true;
        }

        private static void AppendQuadPolygon(GeometryData geometry, Rect rect)
        {
            int start = geometry.Points.Count;
            geometry.Points.Add(new Vector2(rect.xMin, rect.yMin));
            geometry.Points.Add(new Vector2(rect.xMin, rect.yMax));
            geometry.Points.Add(new Vector2(rect.xMax, rect.yMax));
            geometry.Points.Add(new Vector2(rect.xMax, rect.yMin));
            geometry.Polygons.Add(new GridFragment.PolygonRange(start, 4));
        }

        private static void AppendPolygon(GeometryData geometry, List<Vector2> polygon)
        {
            int start = geometry.Points.Count;
            for (int i = 0; i < polygon.Count; i++)
            {
                geometry.Points.Add(polygon[i]);
            }

            geometry.Polygons.Add(new GridFragment.PolygonRange(start, polygon.Count));
        }

        private static GridFragment FillPiece(
            GridFragment fragment,
            Transform rootTransform,
            SpriteDrawUV.SpriteUvContext uvContext,
            GeometryData geometry,
            PendingRange pending,
            Vector2 origVelocity,
            Material mat,
            List<Vector3[]> rootLocalVertices)
        {
            if (!fragment)
            {
                return null;
            }

            Transform pieceTransform = fragment.transform;
            pieceTransform.SetPositionAndRotation(rootTransform.position, rootTransform.rotation);
            pieceTransform.localScale = rootTransform.localScale;

            VerticesBuffer.Clear();
            UvBuffer.Clear();
            TrianglesBuffer.Clear();

            if (pending.IsSimpleQuad)
            {
                AppendQuad(pending.Rect.xMin, pending.Rect.yMin, pending.Rect.xMax, pending.Rect.yMax);
                uvContext.AppendUVs(VerticesBuffer, 0, VerticesBuffer.Count, UvBuffer);
            }
            else
            {
                AppendPolygons(geometry, pending, uvContext);
            }

            int vertexCount = VerticesBuffer.Count;
            if (vertexCount < 3)
            {
                return null;
            }

            rootLocalVertices?.Add(VerticesBuffer.ToArray());

            Vector3 center = Vector3.zero;
            for (int i = 0; i < vertexCount; i++)
            {
                center += VerticesBuffer[i];
            }

            center /= vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                VerticesBuffer[i] -= center;
            }

            Vector3 pivotDiff = -center;
            pieceTransform.position = pieceTransform.TransformPoint(center);

            Bounds meshBounds = CalculateBounds(VerticesBuffer);
            fragment.ApplyRuntimeMesh(VerticesBuffer, TrianglesBuffer, UvBuffer, meshBounds);
            if (pending.IsSimpleQuad)
            {
                fragment.ConfigureBoxCollider(pending.Rect.center + (Vector2)pivotDiff, pending.Rect.size);
            }
            else
            {
                fragment.ConfigureCollider(
                    geometry.Points,
                    geometry.Polygons,
                    pending.PolygonStart,
                    pending.PolygonCount,
                    pivotDiff);
            }

            fragment.MeshRenderer.sharedMaterial = mat;
            fragment.Body.linearVelocity = origVelocity;
            fragment.Body.angularVelocity = 0f;

            return fragment;
        }

        private static void AppendQuad(float x0, float y0, float x1, float y1)
        {
            int vertexOffset = VerticesBuffer.Count;
            VerticesBuffer.Add(new Vector3(x0, y0, 0f));
            VerticesBuffer.Add(new Vector3(x0, y1, 0f));
            VerticesBuffer.Add(new Vector3(x1, y1, 0f));
            VerticesBuffer.Add(new Vector3(x1, y0, 0f));
            TrianglesBuffer.Add(vertexOffset);
            TrianglesBuffer.Add(vertexOffset + 1);
            TrianglesBuffer.Add(vertexOffset + 2);
            TrianglesBuffer.Add(vertexOffset);
            TrianglesBuffer.Add(vertexOffset + 2);
            TrianglesBuffer.Add(vertexOffset + 3);
        }

        private static void AppendPolygons(
            GeometryData geometry,
            PendingRange pending,
            SpriteDrawUV.SpriteUvContext uvContext)
        {
            bool perPolygonUv = uvContext.UsesPerPolygonUVs;
            int polygonEnd = pending.PolygonStart + pending.PolygonCount;
            for (int polygonIndex = pending.PolygonStart; polygonIndex < polygonEnd; polygonIndex++)
            {
                GridFragment.PolygonRange polygon = geometry.Polygons[polygonIndex];
                if (polygon.Count < 3)
                {
                    continue;
                }

                int vertexOffset = VerticesBuffer.Count;
                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 point = geometry.Points[polygon.Start + i];
                    VerticesBuffer.Add(new Vector3(point.x, point.y, 0f));
                }

                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    TrianglesBuffer.Add(vertexOffset);
                    TrianglesBuffer.Add(vertexOffset + i);
                    TrianglesBuffer.Add(vertexOffset + i + 1);
                }

                if (perPolygonUv)
                {
                    uvContext.AppendUVs(VerticesBuffer, vertexOffset, polygon.Count, UvBuffer);
                }
            }

            if (!perPolygonUv)
            {
                uvContext.AppendUVs(VerticesBuffer, 0, VerticesBuffer.Count, UvBuffer);
            }
        }

        private static bool CreatePieces(GeometryData geometry, List<GridFragment> pieces)
        {
            if (!TryGetPiecePrefab(out GridFragment prefab))
            {
                Debug.LogWarning("SpriteGridExploder: SpritePiece prefab is missing or has no GridFragment.");
                return false;
            }

            int count = geometry.Pending.Count;
            pieces.Clear();
            pieces.Capacity = Mathf.Max(pieces.Capacity, count);
            if (Application.isPlaying)
            {
                PoolManager.Instance.Get(prefab, null, count, pieces, false);
                return pieces.Count > 0;
            }

            for (int i = 0; i < count; i++)
            {
                pieces.Add(UnityEngine.Object.Instantiate(prefab));
            }

            return pieces.Count > 0;
        }

        private static void DestroyMaterial(Material mat)
        {
            if (!mat)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(mat);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }

        private readonly struct PendingRange
        {
            public readonly int PolygonStart;
            public readonly int PolygonCount;
            public readonly bool IsSimpleQuad;
            public readonly Rect Rect;

            public PendingRange(
                int polygonStart,
                int polygonCount,
                bool isSimpleQuad,
                Rect rect)
            {
                PolygonStart = polygonStart;
                PolygonCount = polygonCount;
                IsSimpleQuad = isSimpleQuad;
                Rect = rect;
            }
        }

        private static bool TryGetPiecePrefab(out GridFragment prefab)
        {
            if (!_piecePrefab)
            {
                _piecePrefab = AssetsManager.Instance.GetPrefab<GridFragment>(EPrefabNames.SpritePiece);
            }

            prefab = _piecePrefab;
            return prefab;
        }

        public static bool TryGetColliderPoints(GameObject source, out List<Vector2> points)
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

        private static List<Vector2> GetPoints(CircleCollider2D collider, int segments = 48)
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
            Vector2[] path = collider.GetPath(0);
            List<Vector2> points = new(path.Length);
            for (int i = 0; i < path.Length; i++)
            {
                points.Add(path[i]);
            }

            return points;
        }

        private static bool TryGetGridRect(GameObject source, bool useColliderBounds, out Rect rect)
        {
            if (useColliderBounds)
            {
                if (TryGetColliderRect(source, out rect) && IsValidRect(rect))
                {
                    return true;
                }

                return TryGetRendererRect(source, out rect) && IsValidRect(rect);
            }

            if (TryGetRendererRect(source, out rect) && IsValidRect(rect))
            {
                return true;
            }

            return TryGetColliderRect(source, out rect) && IsValidRect(rect);
        }

        private static bool IsValidRect(Rect rect)
        {
            return rect.width > 1e-6f && rect.height > 1e-6f
                && IsFinite(rect.x)
                && IsFinite(rect.y)
                && IsFinite(rect.width)
                && IsFinite(rect.height);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static Material CreateFragmentMaterial(SpriteRenderer spriteRenderer, GameObject source = null)
        {
            return CreateFragmentMaterial(spriteRenderer, source, false, out _);
        }

        private static Material CreateFragmentMaterial(
            SpriteRenderer spriteRenderer,
            GameObject source,
            bool useCache,
            out bool ownsMaterial)
        {
            ownsMaterial = false;
            if (!spriteRenderer)
            {
                MeshRenderer meshRenderer = source ? source.GetComponentInChildren<MeshRenderer>() : null;
                if (!meshRenderer || !meshRenderer.sharedMaterial)
                {
                    return null;
                }

                ownsMaterial = true;
                return new Material(meshRenderer.sharedMaterial);
            }

            MaterialCacheKey key = default;
            bool hasKey = useCache && TryCreateMaterialCacheKey(spriteRenderer, out key);
            if (hasKey && MaterialCache.TryGetValue(key, out Material cached) && cached)
            {
                return cached;
            }

            Material mat;
            if (spriteRenderer.sharedMaterial)
            {
                mat = new Material(spriteRenderer.sharedMaterial);
            }
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (!shader)
                {
                    return null;
                }

                mat = new Material(shader);
            }

            SpriteDrawUV.ConfigureMaterial(spriteRenderer, mat);
            if (hasKey && MaterialCache.Count < MaxMaterialCacheEntries)
            {
                MaterialCache[key] = mat;
                return mat;
            }

            ownsMaterial = true;
            return mat;
        }

        private static bool TryCreateMaterialCacheKey(SpriteRenderer spriteRenderer, out MaterialCacheKey key)
        {
            Sprite sprite = spriteRenderer.sprite;
            Texture texture = sprite ? sprite.texture : null;
            Shader shader = spriteRenderer.sharedMaterial ? spriteRenderer.sharedMaterial.shader : null;
            if (!texture || !shader)
            {
                key = default;
                return false;
            }

            key = new MaterialCacheKey(texture.GetHashCode(), shader.GetHashCode(), spriteRenderer.color);
            return true;
        }

        private static bool TryGetColliderRect(GameObject source, out Rect rect)
        {
            if (source.TryGetComponent(out CircleCollider2D circleCollider))
            {
                Vector2 center = circleCollider.offset;
                float radius = circleCollider.radius;
                rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
                return true;
            }

            if (source.TryGetComponent(out BoxCollider2D boxCollider))
            {
                Vector2 center = boxCollider.offset;
                Vector2 size = boxCollider.size;
                rect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
                return true;
            }

            if (source.TryGetComponent(out PolygonCollider2D polygonCollider) && polygonCollider.pathCount > 0)
            {
                Vector2[] path = polygonCollider.GetPath(0);
                if (path.Length > 0)
                {
                    Vector2 min = path[0];
                    Vector2 max = path[0];
                    foreach (Vector2 point in path)
                    {
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }

                    rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    return true;
                }
            }

            rect = default;
            return false;
        }

        private static bool TryGetRendererRect(GameObject source, out Rect rect)
        {
            SpriteRenderer spriteRenderer = source.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer && spriteRenderer.sprite)
            {
                rect = SpriteDrawUV.GetRendererRectInRootSpace(source.transform, spriteRenderer);
                return IsValidRect(rect);
            }

            Renderer renderer = source.GetComponentInChildren<Renderer>();
            if (!renderer)
            {
                rect = default;
                return false;
            }

            Bounds localBounds = renderer.localBounds;
            rect = new Rect(
                localBounds.center.x - localBounds.extents.x,
                localBounds.center.y - localBounds.extents.y,
                localBounds.size.x,
                localBounds.size.y);
            return IsValidRect(rect);
        }

        private static Bounds CalculateBounds(List<Vector3> vertices)
        {
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            for (int i = 1; i < vertices.Count; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            if (bounds.size.z <= 1e-6f)
            {
                Vector3 size = bounds.size;
                size.z = 0.001f;
                bounds.size = size;
            }

            return bounds;
        }

        private static int ClassifyRectVsCircle(
            float x0,
            float y0,
            float x1,
            float y1,
            Vector2 center,
            float radius)
        {
            float radiusSqr = radius * radius;
            bool allInside =
                IsInsideCircle(x0, y0, center, radiusSqr)
                && IsInsideCircle(x0, y1, center, radiusSqr)
                && IsInsideCircle(x1, y1, center, radiusSqr)
                && IsInsideCircle(x1, y0, center, radiusSqr);
            if (allInside)
            {
                return 1;
            }

            float closestX = Mathf.Clamp(center.x, x0, x1);
            float closestY = Mathf.Clamp(center.y, y0, y1);
            float dx = center.x - closestX;
            float dy = center.y - closestY;
            return dx * dx + dy * dy > radiusSqr ? -1 : 0;
        }

        private static bool IsInsideCircle(float x, float y, Vector2 center, float radiusSqr)
        {
            float dx = x - center.x;
            float dy = y - center.y;
            return dx * dx + dy * dy <= radiusSqr;
        }

        private static int ClassifyRectVsRect(float x0, float y0, float x1, float y1, Rect bounds)
        {
            if (x1 < bounds.xMin || x0 > bounds.xMax || y1 < bounds.yMin || y0 > bounds.yMax)
            {
                return -1;
            }

            if (x0 >= bounds.xMin && x1 <= bounds.xMax && y0 >= bounds.yMin && y1 <= bounds.yMax)
            {
                return 1;
            }

            return 0;
        }

        private static bool TryGetAxisAlignedBounds(List<Vector2> points, out Rect bounds)
        {
            bounds = default;
            if (points == null || points.Count != 4)
            {
                return false;
            }

            float minX = points[0].x;
            float maxX = points[0].x;
            float minY = points[0].y;
            float maxY = points[0].y;
            for (int i = 1; i < 4; i++)
            {
                minX = Mathf.Min(minX, points[i].x);
                maxX = Mathf.Max(maxX, points[i].x);
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            const float epsilon = 1e-4f;
            for (int i = 0; i < 4; i++)
            {
                bool onX = Mathf.Abs(points[i].x - minX) <= epsilon || Mathf.Abs(points[i].x - maxX) <= epsilon;
                bool onY = Mathf.Abs(points[i].y - minY) <= epsilon || Mathf.Abs(points[i].y - maxY) <= epsilon;
                if (!onX || !onY)
                {
                    return false;
                }
            }

            bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return bounds.width > epsilon && bounds.height > epsilon;
        }

        private static bool TryGetAxisAlignedRect(List<Vector2> polygon, out Rect rect)
        {
            return TryGetAxisAlignedRect(polygon, 0, polygon != null ? polygon.Count : 0, out rect);
        }

        private static bool TryGetAxisAlignedRect(
            List<Vector2> points,
            int start,
            int count,
            out Rect rect)
        {
            rect = default;
            if (points == null || count != 4 || start < 0 || start + count > points.Count)
            {
                return false;
            }

            float x0 = points[start].x;
            float x1 = points[start].x;
            float y0 = points[start].y;
            float y1 = points[start].y;
            for (int i = 1; i < count; i++)
            {
                Vector2 point = points[start + i];
                x0 = Mathf.Min(x0, point.x);
                x1 = Mathf.Max(x1, point.x);
                y0 = Mathf.Min(y0, point.y);
                y1 = Mathf.Max(y1, point.y);
            }

            const float epsilon = 1e-4f;
            int cornerMask = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = points[start + i];
                bool minX = Mathf.Abs(point.x - x0) <= epsilon;
                bool maxX = Mathf.Abs(point.x - x1) <= epsilon;
                bool minY = Mathf.Abs(point.y - y0) <= epsilon;
                bool maxY = Mathf.Abs(point.y - y1) <= epsilon;
                if ((!minX && !maxX) || (!minY && !maxY))
                {
                    return false;
                }

                int corner = (maxX ? 1 : 0) | (maxY ? 2 : 0);
                cornerMask |= 1 << corner;
            }

            if (cornerMask != 0xf || x1 - x0 <= epsilon || y1 - y0 <= epsilon)
            {
                return false;
            }

            rect = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        private static bool TryCreateGeometryCacheKey(
            GameObject source,
            SpriteRenderer renderer,
            GridLayout layout,
            float cellSize,
            bool useColliderBounds,
            out GeometryCacheKey key)
        {
            key = default;
            if (source.TryGetComponent(out PolygonCollider2D _))
            {
                return false;
            }

            int colliderKind = 0;
            Vector2 colliderOffset = default;
            Vector2 colliderSize = default;
            float colliderRadius = 0f;
            if (source.TryGetComponent(out CircleCollider2D circle))
            {
                colliderKind = 1;
                colliderOffset = circle.offset;
                colliderRadius = circle.radius;
            }
            else if (source.TryGetComponent(out BoxCollider2D box))
            {
                colliderKind = 2;
                colliderOffset = box.offset;
                colliderSize = box.size;
            }

            Sprite sprite = renderer ? renderer.sprite : null;
            int transformHash = 0;
            if (renderer && renderer.transform != source.transform)
            {
                Matrix4x4 rootToSprite = renderer.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                transformHash = rootToSprite.GetHashCode();
            }

            key = new GeometryCacheKey(
                sprite,
                renderer ? renderer.drawMode : SpriteDrawMode.Simple,
                renderer ? renderer.size : default,
                renderer && renderer.flipX,
                renderer && renderer.flipY,
                layout,
                cellSize,
                useColliderBounds,
                colliderKind,
                colliderOffset,
                colliderSize,
                colliderRadius,
                transformHash);
            return true;
        }

        private static bool TryRestoreCachedGeometry(GeometryCacheKey key, GeometryData geometry)
        {
            if (!GeometryCache.TryGetValue(key, out GeometryTemplate template))
            {
                return false;
            }

            geometry.CopyFrom(template);
            return true;
        }

        private static void CacheGeometry(GeometryCacheKey key, GeometryData geometry)
        {
            if (GeometryCache.Count >= MaxGeometryCacheEntries)
            {
                GeometryCache.Clear();
            }

            GeometryCache[key] = new GeometryTemplate(geometry);
        }

        private sealed class GeometryData
        {
            public readonly List<Vector2> Points = new(256);
            public readonly List<GridFragment.PolygonRange> Polygons = new(64);
            public readonly List<PendingRange> Pending = new(64);

            public void Clear(int pendingCapacity)
            {
                Points.Clear();
                Polygons.Clear();
                Pending.Clear();
                if (Pending.Capacity < pendingCapacity)
                {
                    Pending.Capacity = pendingCapacity;
                }
            }

            public void CopyFrom(GeometryTemplate template)
            {
                Clear(template.Pending.Length);
                Points.AddRange(template.Points);
                Polygons.AddRange(template.Polygons);
                Pending.AddRange(template.Pending);
            }
        }

        private sealed class GeometryTemplate
        {
            public readonly Vector2[] Points;
            public readonly GridFragment.PolygonRange[] Polygons;
            public readonly PendingRange[] Pending;

            public GeometryTemplate(GeometryData geometry)
            {
                Points = geometry.Points.ToArray();
                Polygons = geometry.Polygons.ToArray();
                Pending = geometry.Pending.ToArray();
            }
        }

        private readonly struct GeometryCacheKey : IEquatable<GeometryCacheKey>
        {
            private readonly Sprite _sprite;
            private readonly int _spriteHash;
            private readonly SpriteDrawMode _drawMode;
            private readonly Vector2 _rendererSize;
            private readonly bool _flipX;
            private readonly bool _flipY;
            private readonly Rect _gridBounds;
            private readonly int _columns;
            private readonly int _rows;
            private readonly float _cellSize;
            private readonly bool _useColliderBounds;
            private readonly int _colliderKind;
            private readonly Vector2 _colliderOffset;
            private readonly Vector2 _colliderSize;
            private readonly float _colliderRadius;
            private readonly int _transformHash;

            public GeometryCacheKey(
                Sprite sprite,
                SpriteDrawMode drawMode,
                Vector2 rendererSize,
                bool flipX,
                bool flipY,
                GridLayout layout,
                float cellSize,
                bool useColliderBounds,
                int colliderKind,
                Vector2 colliderOffset,
                Vector2 colliderSize,
                float colliderRadius,
                int transformHash)
            {
                _sprite = sprite;
                _spriteHash = sprite ? sprite.GetHashCode() : 0;
                _drawMode = drawMode;
                _rendererSize = rendererSize;
                _flipX = flipX;
                _flipY = flipY;
                _gridBounds = layout.Bounds;
                _columns = layout.Columns;
                _rows = layout.Rows;
                _cellSize = cellSize;
                _useColliderBounds = useColliderBounds;
                _colliderKind = colliderKind;
                _colliderOffset = colliderOffset;
                _colliderSize = colliderSize;
                _colliderRadius = colliderRadius;
                _transformHash = transformHash;
            }

            public bool Equals(GeometryCacheKey other)
            {
                return ReferenceEquals(_sprite, other._sprite)
                    && _drawMode == other._drawMode
                    && _rendererSize.Equals(other._rendererSize)
                    && _flipX == other._flipX
                    && _flipY == other._flipY
                    && _gridBounds.Equals(other._gridBounds)
                    && _columns == other._columns
                    && _rows == other._rows
                    && _cellSize.Equals(other._cellSize)
                    && _useColliderBounds == other._useColliderBounds
                    && _colliderKind == other._colliderKind
                    && _colliderOffset.Equals(other._colliderOffset)
                    && _colliderSize.Equals(other._colliderSize)
                    && _colliderRadius.Equals(other._colliderRadius)
                    && _transformHash == other._transformHash;
            }

            public override bool Equals(object obj)
            {
                return obj is GeometryCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _spriteHash;
                    hash = hash * 397 ^ (int)_drawMode;
                    hash = hash * 397 ^ _rendererSize.GetHashCode();
                    hash = hash * 397 ^ _flipX.GetHashCode();
                    hash = hash * 397 ^ _flipY.GetHashCode();
                    hash = hash * 397 ^ _gridBounds.GetHashCode();
                    hash = hash * 397 ^ _columns;
                    hash = hash * 397 ^ _rows;
                    hash = hash * 397 ^ _cellSize.GetHashCode();
                    hash = hash * 397 ^ _useColliderBounds.GetHashCode();
                    hash = hash * 397 ^ _colliderKind;
                    hash = hash * 397 ^ _colliderOffset.GetHashCode();
                    hash = hash * 397 ^ _colliderSize.GetHashCode();
                    hash = hash * 397 ^ _colliderRadius.GetHashCode();
                    hash = hash * 397 ^ _transformHash;
                    return hash;
                }
            }
        }

        private readonly struct MaterialCacheKey : IEquatable<MaterialCacheKey>
        {
            private readonly int _textureHash;
            private readonly int _shaderHash;
            private readonly Color _color;

            public MaterialCacheKey(int textureHash, int shaderHash, Color color)
            {
                _textureHash = textureHash;
                _shaderHash = shaderHash;
                _color = color;
            }

            public bool Equals(MaterialCacheKey other)
            {
                return _textureHash == other._textureHash
                    && _shaderHash == other._shaderHash
                    && _color.Equals(other._color);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _textureHash;
                    hash = hash * 397 ^ _shaderHash;
                    hash = hash * 397 ^ _color.GetHashCode();
                    return hash;
                }
            }
        }

    }
}

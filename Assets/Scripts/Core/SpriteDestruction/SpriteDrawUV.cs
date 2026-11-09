using System.Collections.Generic;
using UnityEngine;

namespace Core.SpriteDestruction
{
    /// <summary>
    /// UV mapping aligned with Unity SpriteRenderer draw modes, including 9-slice borders.
    /// </summary>
    internal static class SpriteDrawUV
    {
        private const float Epsilon = 1e-6f;

        private static Vector3[] VertexScratch = System.Array.Empty<Vector3>();
        private static Vector2[] UvScratch = System.Array.Empty<Vector2>();
        private static readonly Vector3[] RendererCorners = new Vector3[4];
        private static readonly int RendererColor = Shader.PropertyToID("_RendererColor");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        internal readonly struct SpriteUvContext
        {
            private readonly bool _sameSpace;
            private readonly Matrix4x4 _rootToSprite;
            private readonly Rect _drawRect;
            private readonly Vector2 _uvMin;
            private readonly Vector2 _uvMax;
            private readonly SpriteDrawMode _drawMode;
            private readonly SpriteBorderData _border;
            private readonly Vector2 _drawSize;
            private readonly bool _flipX;
            private readonly bool _flipY;
            private readonly float[] _tiledXCuts;
            private readonly float[] _tiledYCuts;

            public bool IsValid { get; }
            public bool UsesPerPolygonUVs => _drawMode == SpriteDrawMode.Tiled;

            public SpriteUvContext(
                SpriteRenderer spriteRenderer,
                Transform rootTransform,
                bool buildTiledCuts = true)
            {
                Transform spriteTransform = spriteRenderer ? spriteRenderer.transform : null;
                _sameSpace = !spriteRenderer || !rootTransform || rootTransform == spriteTransform;
                _rootToSprite = !_sameSpace
                    ? spriteTransform.worldToLocalMatrix * rootTransform.localToWorldMatrix
                    : Matrix4x4.identity;
                _drawMode = spriteRenderer ? spriteRenderer.drawMode : SpriteDrawMode.Simple;
                _border = default;
                _drawRect = default;
                _uvMin = default;
                _uvMax = default;
                _drawSize = spriteRenderer ? spriteRenderer.size : default;
                _flipX = spriteRenderer && spriteRenderer.flipX;
                _flipY = spriteRenderer && spriteRenderer.flipY;
                _tiledXCuts = null;
                _tiledYCuts = null;
                IsValid = spriteRenderer && spriteRenderer.sprite;
                if (!IsValid)
                {
                    return;
                }

                _drawRect = GetLocalDrawRect(spriteRenderer);
                GetSpriteUVRect(spriteRenderer.sprite, out _uvMin, out _uvMax);
                if (_drawMode != SpriteDrawMode.Simple)
                {
                    _border = SpriteBorderData.From(spriteRenderer);
                }

                if (buildTiledCuts && _drawMode == SpriteDrawMode.Tiled)
                {
                    _tiledXCuts = BuildTiledCuts(
                        _drawRect.xMin,
                        _drawSize.x,
                        _border.BorderLeft,
                        _border.BorderRight,
                        _border.CenterTileWidth);
                    _tiledYCuts = BuildTiledCuts(
                        _drawRect.yMin,
                        _drawSize.y,
                        _border.BorderBottom,
                        _border.BorderTop,
                        _border.CenterTileHeight);
                }
            }

            public void AppendUVs(List<Vector3> rootLocalVertices, int start, int count, List<Vector2> uv)
            {
                if (count <= 0)
                {
                    return;
                }

                if (!IsValid)
                {
                    for (int i = 0; i < count; i++)
                    {
                        uv.Add(Vector2.zero);
                    }

                    return;
                }

                EnsureScratch(count);
                for (int i = 0; i < count; i++)
                {
                    Vector3 rootLocal = rootLocalVertices[start + i];
                    VertexScratch[i] = _sameSpace
                        ? rootLocal
                        : _rootToSprite.MultiplyPoint3x4(rootLocal);
                }

                switch (_drawMode)
                {
                    case SpriteDrawMode.Tiled:
                        CalculateTiled(
                            VertexScratch,
                            count,
                            _drawRect,
                            _drawSize,
                            _flipX,
                            _flipY,
                            _border,
                            _uvMin,
                            _uvMax,
                            UvScratch);
                        break;
                    case SpriteDrawMode.Sliced:
                        CalculateSliced(
                            VertexScratch,
                            count,
                            _drawRect,
                            _drawSize,
                            _flipX,
                            _flipY,
                            _border,
                            _uvMin,
                            _uvMax,
                            UvScratch);
                        break;
                    default:
                        CalculateSimple(
                            VertexScratch,
                            count,
                            _drawRect,
                            _flipX,
                            _flipY,
                            _uvMin,
                            _uvMax,
                            UvScratch);
                        break;
                }

                for (int i = 0; i < count; i++)
                {
                    uv.Add(UvScratch[i]);
                }
            }

            /// <summary>
            /// Appends intersections with precomputed tiled boundaries to a reusable rect list.
            /// For non-tiled contexts, the input rect is appended unchanged.
            /// </summary>
            public void SplitAxisAlignedRect(Rect rect, List<Rect> output)
            {
                if (output == null)
                {
                    return;
                }

                if (_drawMode != SpriteDrawMode.Tiled || _tiledXCuts == null || _tiledYCuts == null)
                {
                    output.Add(rect);
                    return;
                }

                AppendTiledRect(rect, _tiledXCuts, _tiledYCuts, output);
            }
        }

        private static void EnsureScratch(int count)
        {
            if (VertexScratch.Length < count)
            {
                int size = Mathf.NextPowerOfTwo(Mathf.Max(count, 16));
                VertexScratch = new Vector3[size];
                UvScratch = new Vector2[size];
            }
        }

        private static Rect GetLocalDrawRect(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                Vector2 size = spriteRenderer.size;
                if (size is { x: > Epsilon, y: > Epsilon })
                {
                    Vector2 pivot = GetNormalizedPivot(spriteRenderer.sprite);
                    return new Rect(-pivot.x * size.x, -pivot.y * size.y, size.x, size.y);
                }
            }

            Bounds localBounds = spriteRenderer.localBounds;
            return new Rect(
                localBounds.center.x - localBounds.extents.x,
                localBounds.center.y - localBounds.extents.y,
                localBounds.size.x,
                localBounds.size.y);
        }

        public static Rect GetRendererRectInRootSpace(Transform rootTransform, SpriteRenderer spriteRenderer)
        {
            Rect localRect = GetLocalDrawRect(spriteRenderer);
            Transform spriteTransform = spriteRenderer.transform;
            RendererCorners[0] = new Vector3(localRect.xMin, localRect.yMin, 0f);
            RendererCorners[1] = new Vector3(localRect.xMin, localRect.yMax, 0f);
            RendererCorners[2] = new Vector3(localRect.xMax, localRect.yMax, 0f);
            RendererCorners[3] = new Vector3(localRect.xMax, localRect.yMin, 0f);

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 rootLocal = rootTransform.InverseTransformPoint(spriteTransform.TransformPoint(RendererCorners[i]));
                minX = Mathf.Min(minX, rootLocal.x);
                maxX = Mathf.Max(maxX, rootLocal.x);
                minY = Mathf.Min(minY, rootLocal.y);
                maxY = Mathf.Max(maxY, rootLocal.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Vector2[] Calculate(SpriteRenderer spriteRenderer, Transform rootTransform, Vector3[] rootLocalVertices)
        {
            if (!spriteRenderer || rootTransform == spriteRenderer.transform)
            {
                return Calculate(spriteRenderer, rootLocalVertices);
            }

            var spriteLocalVertices = new Vector3[rootLocalVertices.Length];
            Matrix4x4 rootToSprite =
                spriteRenderer.transform.worldToLocalMatrix * rootTransform.localToWorldMatrix;
            for (int i = 0; i < rootLocalVertices.Length; i++)
            {
                spriteLocalVertices[i] = rootToSprite.MultiplyPoint3x4(rootLocalVertices[i]);
            }

            return Calculate(spriteRenderer, spriteLocalVertices);
        }

        public static Vector2[] Calculate(SpriteRenderer spriteRenderer, Vector3[] localVertices)
        {
            if (!spriteRenderer || !spriteRenderer.sprite || localVertices == null)
            {
                return localVertices != null ? new Vector2[localVertices.Length] : System.Array.Empty<Vector2>();
            }

            Sprite sprite = spriteRenderer.sprite;
            GetSpriteUVRect(sprite, out Vector2 uvMin, out Vector2 uvMax);

            Rect drawRect = GetLocalDrawRect(spriteRenderer);
            SpriteBorderData borderData = SpriteBorderData.From(spriteRenderer);
            Vector2 drawSize = spriteRenderer.size;
            bool flipX = spriteRenderer.flipX;
            bool flipY = spriteRenderer.flipY;

            var uv = new Vector2[localVertices.Length];
            int count = localVertices.Length;

            switch (spriteRenderer.drawMode)
            {
                case SpriteDrawMode.Tiled:
                    CalculateTiled(
                        localVertices,
                        count,
                        drawRect,
                        drawSize,
                        flipX,
                        flipY,
                        borderData,
                        uvMin,
                        uvMax,
                        uv);
                    break;
                case SpriteDrawMode.Sliced:
                    CalculateSliced(
                        localVertices,
                        count,
                        drawRect,
                        drawSize,
                        flipX,
                        flipY,
                        borderData,
                        uvMin,
                        uvMax,
                        uv);
                    break;
                default:
                    CalculateSimple(localVertices, count, drawRect, flipX, flipY, uvMin, uvMax, uv);
                    break;
            }

            return uv;
        }

        public static void ConfigureMaterial(SpriteRenderer source, Material material)
        {
            if (!source || !material || !source.sprite)
            {
                return;
            }

            material.mainTexture = source.sprite.texture;
            material.SetTexture(MainTex, source.sprite.texture);
            material.color = source.color;
            if (material.HasProperty(RendererColor))
            {
                material.SetColor(RendererColor, source.color);
            }
        }

        private static void CalculateSimple(
            Vector3[] localVertices,
            int count,
            Rect drawRect,
            bool flipX,
            bool flipY,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            MapDrawSpaceLinear(localVertices, count, drawRect, flipX, flipY, uvMin, uvMax, uv);
        }

        /// <summary>
        /// Tiled: border regions keep fixed size; only the center repeats (Unity SpriteRenderer behaviour).
        /// </summary>
        private static void CalculateTiled(
            Vector3[] localVertices,
            int count,
            Rect drawRect,
            Vector2 drawSize,
            bool flipX,
            bool flipY,
            SpriteBorderData borderData,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            GetCentroid(localVertices, count, out float anchorX, out float anchorY);
            float localAnchorX = anchorX - drawRect.x;
            float localAnchorY = anchorY - drawRect.y;
            ApplyFlip(flipX, flipY, drawRect, ref localAnchorX, ref localAnchorY);
            float uvSpanX = uvMax.x - uvMin.x;
            float uvSpanY = uvMax.y - uvMin.y;

            for (int i = 0; i < count; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(flipX, flipY, drawRect, ref localX, ref localY);

                float normalizedU = MapTiledAxis(
                    localX,
                    drawSize.x,
                    borderData.BorderLeft,
                    borderData.BorderRight,
                    borderData.CenterTileWidth,
                    borderData.UvLeft,
                    borderData.UvRight,
                    localAnchorX);

                float normalizedV = MapTiledAxis(
                    localY,
                    drawSize.y,
                    borderData.BorderBottom,
                    borderData.BorderTop,
                    borderData.CenterTileHeight,
                    borderData.UvBottom,
                    borderData.UvTop,
                    localAnchorY);

                uv[i] = new Vector2(
                    uvMin.x + normalizedU * uvSpanX,
                    uvMin.y + normalizedV * uvSpanY);
            }
        }

        private static void CalculateSliced(
            Vector3[] localVertices,
            int count,
            Rect drawRect,
            Vector2 drawSize,
            bool flipX,
            bool flipY,
            SpriteBorderData borderData,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            float uvSpanX = uvMax.x - uvMin.x;
            float uvSpanY = uvMax.y - uvMin.y;
            for (int i = 0; i < count; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(flipX, flipY, drawRect, ref localX, ref localY);

                float normalizedU = MapSlicedAxis(
                    localX,
                    drawSize.x,
                    borderData.BorderLeft,
                    borderData.BorderRight,
                    borderData.UvLeft,
                    borderData.UvRight);

                float normalizedV = MapSlicedAxis(
                    localY,
                    drawSize.y,
                    borderData.BorderBottom,
                    borderData.BorderTop,
                    borderData.UvBottom,
                    borderData.UvTop);

                uv[i] = new Vector2(
                    uvMin.x + normalizedU * uvSpanX,
                    uvMin.y + normalizedV * uvSpanY);
            }
        }

        private static void MapDrawSpaceLinear(
            Vector3[] localVertices,
            int count,
            Rect drawRect,
            bool flipX,
            bool flipY,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            float invWidth = drawRect.width > Epsilon ? 1f / drawRect.width : 0f;
            float invHeight = drawRect.height > Epsilon ? 1f / drawRect.height : 0f;

            float uvSpanX = uvMax.x - uvMin.x;
            float uvSpanY = uvMax.y - uvMin.y;

            for (int i = 0; i < count; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(flipX, flipY, drawRect, ref localX, ref localY);

                uv[i] = new Vector2(
                    uvMin.x + localX * invWidth * uvSpanX,
                    uvMin.y + localY * invHeight * uvSpanY);
            }
        }

        /// <summary>
        /// One axis of Tiled mode: fixed border slices + repeating center using centerTileSize.
        /// </summary>
        private static float MapTiledAxis(
            float localPos,
            float drawSize,
            float borderStart,
            float borderEnd,
            float centerTileSize,
            float uvBorderStart,
            float uvBorderEnd,
            float anchorPos)
        {
            float uvCenterStart = uvBorderStart;
            float uvCenterEnd = 1f - uvBorderEnd;
            centerTileSize = Mathf.Max(centerTileSize, Epsilon);

            if (borderStart > Epsilon && anchorPos <= borderStart)
            {
                return borderStart > Epsilon ? Mathf.Clamp01(localPos / borderStart) * uvBorderStart : 0f;
            }

            if (borderEnd > Epsilon && anchorPos >= drawSize - borderEnd)
            {
                float borderLocal = localPos - (drawSize - borderEnd);
                return uvCenterEnd + Mathf.Clamp01(borderLocal / borderEnd) * uvBorderEnd;
            }

            float anchorCenter = Mathf.Max(anchorPos - borderStart, 0f);
            float tileIndex = Mathf.Floor(anchorCenter / centerTileSize);
            float tileStart = borderStart + tileIndex * centerTileSize;
            float t = Mathf.Clamp01((localPos - tileStart) / centerTileSize);
            return uvCenterStart + t * (uvCenterEnd - uvCenterStart);
        }

        private static void GetCentroid(Vector3[] vertices, int count, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (vertices == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                x += vertices[i].x;
                y += vertices[i].y;
            }

            x /= count;
            y /= count;
        }

        private static float MapSlicedAxis(
            float localPos,
            float drawSize,
            float borderStartSize,
            float borderEndSize,
            float uvBorderStart,
            float uvBorderEnd)
        {
            float centerDrawSize = drawSize - borderStartSize - borderEndSize;
            float uvCenterStart = uvBorderStart;
            float uvCenterEnd = 1f - uvBorderEnd;

            if (borderStartSize > Epsilon && localPos <= borderStartSize)
            {
                return (localPos / borderStartSize) * uvBorderStart;
            }

            if (borderEndSize > Epsilon && localPos >= drawSize - borderEndSize)
            {
                float borderLocal = localPos - (drawSize - borderEndSize);
                return uvCenterEnd + (borderLocal / borderEndSize) * uvBorderEnd;
            }

            float centerLocal = localPos - borderStartSize;
            float centerT = centerDrawSize > Epsilon ? centerLocal / centerDrawSize : 0f;
            return uvCenterStart + centerT * (uvCenterEnd - uvCenterStart);
        }

        private static void ApplyFlip(
            bool flipX,
            bool flipY,
            Rect drawRect,
            ref float localX,
            ref float localY)
        {
            if (flipX)
            {
                localX = drawRect.width - localX;
            }

            if (flipY)
            {
                localY = drawRect.height - localY;
            }
        }

        private static Vector2 GetNormalizedPivot(Sprite sprite)
        {
            if (!sprite || sprite.rect.width <= Epsilon || sprite.rect.height <= Epsilon)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        }

        private static void AppendTiledRect(Rect cell, float[] xCuts, float[] yCuts, List<Rect> output)
        {
            if (xCuts.Length < 2 || yCuts.Length < 2)
            {
                output.Add(cell);
                return;
            }

            GetOverlappingCutRange(xCuts, cell.xMin, cell.xMax, out int xStart, out int xEnd);
            GetOverlappingCutRange(yCuts, cell.yMin, cell.yMax, out int yStart, out int yEnd);
            for (int i = xStart; i <= xEnd; i++)
            {
                float x0 = Mathf.Max(cell.xMin, xCuts[i]);
                float x1 = Mathf.Min(cell.xMax, xCuts[i + 1]);
                if (x1 - x0 <= Epsilon)
                {
                    continue;
                }

                for (int j = yStart; j <= yEnd; j++)
                {
                    float y0 = Mathf.Max(cell.yMin, yCuts[j]);
                    float y1 = Mathf.Min(cell.yMax, yCuts[j + 1]);
                    if (y1 - y0 <= Epsilon)
                    {
                        continue;
                    }

                    output.Add(Rect.MinMaxRect(x0, y0, x1, y1));
                }
            }
        }

        private static void GetOverlappingCutRange(
            float[] cuts,
            float min,
            float max,
            out int start,
            out int end)
        {
            int last = cuts.Length - 2;
            start = 0;
            end = last;
            if (last < 0)
            {
                return;
            }

            int lo = 0;
            int hi = last;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (cuts[mid + 1] <= min + Epsilon)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            start = lo;
            lo = start;
            hi = last;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (cuts[mid] >= max - Epsilon)
                {
                    hi = mid - 1;
                }
                else
                {
                    lo = mid;
                }
            }

            end = lo;
        }

        private static readonly List<float> CutScratch = new(32);

        private static float[] BuildTiledCuts(
            float origin,
            float size,
            float borderStart,
            float borderEnd,
            float tileSize)
        {
            CutScratch.Clear();
            CutScratch.Add(origin);
            float end = origin + size;
            if (borderStart > Epsilon)
            {
                CutScratch.Add(origin + borderStart);
            }

            float centerEnd = end - Mathf.Max(borderEnd, 0f);
            float cursor = origin + Mathf.Max(borderStart, 0f);
            tileSize = Mathf.Max(tileSize, Epsilon);
            while (cursor + tileSize < centerEnd - Epsilon)
            {
                cursor += tileSize;
                CutScratch.Add(cursor);
            }

            if (borderEnd > Epsilon)
            {
                CutScratch.Add(centerEnd);
            }

            CutScratch.Add(end);
            int write = 1;
            for (int i = 1; i < CutScratch.Count; i++)
            {
                if (CutScratch[i] - CutScratch[write - 1] > Epsilon)
                {
                    CutScratch[write++] = CutScratch[i];
                }
            }

            float[] result = new float[write];
            for (int i = 0; i < write; i++)
            {
                result[i] = CutScratch[i];
            }

            return result;
        }

        private static void GetSpriteUVRect(Sprite sprite, out Vector2 min, out Vector2 max)
        {
            Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            min = new Vector2(outer.x, outer.y);
            max = new Vector2(outer.z, outer.w);
        }

        private readonly struct SpriteBorderData
        {
            public readonly float BorderLeft;
            public readonly float BorderRight;
            public readonly float BorderBottom;
            public readonly float BorderTop;
            public readonly float CenterTileWidth;
            public readonly float CenterTileHeight;
            public readonly float UvLeft;
            public readonly float UvRight;
            public readonly float UvBottom;
            public readonly float UvTop;

            public static SpriteBorderData From(SpriteRenderer spriteRenderer)
            {
                Sprite sprite = spriteRenderer.sprite;
                Vector4 border = sprite.border;
                float ppu = sprite.pixelsPerUnit;
                float invWidth = 1f / sprite.rect.width;
                float invHeight = 1f / sprite.rect.height;

                return new SpriteBorderData(
                    border.x / ppu,
                    border.z / ppu,
                    border.y / ppu,
                    border.w / ppu,
                    (sprite.rect.width - border.x - border.z) / ppu,
                    (sprite.rect.height - border.y - border.w) / ppu,
                    border.x * invWidth,
                    border.z * invWidth,
                    border.y * invHeight,
                    border.w * invHeight);
            }

            private SpriteBorderData(
                float borderLeft,
                float borderRight,
                float borderBottom,
                float borderTop,
                float centerTileWidth,
                float centerTileHeight,
                float uvLeft,
                float uvRight,
                float uvBottom,
                float uvTop)
            {
                BorderLeft = borderLeft;
                BorderRight = borderRight;
                BorderBottom = borderBottom;
                BorderTop = borderTop;
                CenterTileWidth = centerTileWidth;
                CenterTileHeight = centerTileHeight;
                UvLeft = uvLeft;
                UvRight = uvRight;
                UvBottom = uvBottom;
                UvTop = uvTop;
            }
        }
    }
}

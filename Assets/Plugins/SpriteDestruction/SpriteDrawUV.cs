using UnityEngine;

namespace SpriteDestruction
{
    /// <summary>
    /// UV mapping aligned with Unity SpriteRenderer draw modes, including 9-slice borders.
    /// </summary>
    internal static class SpriteDrawUV
    {
        private const float Epsilon = 1e-6f;

        public static Rect GetLocalDrawRect(SpriteRenderer spriteRenderer)
        {
            Bounds localBounds = spriteRenderer.localBounds;
            return new Rect(
                localBounds.center.x - localBounds.extents.x,
                localBounds.center.y - localBounds.extents.y,
                localBounds.size.x,
                localBounds.size.y);
        }

        public static Rect GetRendererRectInRootSpace(Transform rootTransform, SpriteRenderer spriteRenderer)
        {
            Bounds localBounds = spriteRenderer.localBounds;
            Transform spriteTransform = spriteRenderer.transform;
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            Vector3[] corners =
            {
                center + new Vector3(-extents.x, -extents.y, 0f),
                center + new Vector3(-extents.x, extents.y, 0f),
                center + new Vector3(extents.x, extents.y, 0f),
                center + new Vector3(extents.x, -extents.y, 0f)
            };

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (Vector3 corner in corners)
            {
                Vector3 rootLocal = rootTransform.InverseTransformPoint(spriteTransform.TransformPoint(corner));
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
            for (int i = 0; i < rootLocalVertices.Length; i++)
            {
                Vector3 worldPoint = rootTransform.TransformPoint(rootLocalVertices[i]);
                spriteLocalVertices[i] = spriteRenderer.transform.InverseTransformPoint(worldPoint);
            }

            return Calculate(spriteRenderer, spriteLocalVertices);
        }

        public static Vector2[] Calculate(SpriteRenderer spriteRenderer, Vector3[] localVertices)
        {
            Sprite sprite = spriteRenderer.sprite;
            GetUVRange(sprite.uv, out Vector2 uvMin, out Vector2 uvMax);

            Rect drawRect = GetLocalDrawRect(spriteRenderer);
            SpriteBorderData borderData = SpriteBorderData.From(spriteRenderer);

            var uv = new Vector2[localVertices.Length];

            switch (spriteRenderer.drawMode)
            {
                case SpriteDrawMode.Tiled:
                    CalculateTiled(spriteRenderer, localVertices, drawRect, borderData, uvMin, uvMax, uv);
                    break;
                case SpriteDrawMode.Sliced:
                    CalculateSliced(spriteRenderer, localVertices, drawRect, borderData, uvMin, uvMax, uv);
                    break;
                default:
                    CalculateSimple(spriteRenderer, localVertices, drawRect, uvMin, uvMax, uv);
                    break;
            }

            return uv;
        }

        public static void ConfigureMaterial(SpriteRenderer source, Material material)
        {
            if (!source || !material || !material.mainTexture)
            {
                return;
            }

            material.mainTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private static void CalculateSimple(
            SpriteRenderer spriteRenderer,
            Vector3[] localVertices,
            Rect drawRect,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            MapDrawSpaceLinear(spriteRenderer, localVertices, drawRect, uvMin, uvMax, uv);
        }

        /// <summary>
        /// Tiled: border regions keep fixed size; only the center repeats (Unity SpriteRenderer behaviour).
        /// </summary>
        private static void CalculateTiled(
            SpriteRenderer spriteRenderer,
            Vector3[] localVertices,
            Rect drawRect,
            SpriteBorderData borderData,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            Vector2 drawSize = spriteRenderer.size;

            for (int i = 0; i < localVertices.Length; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(spriteRenderer, drawRect, ref localX, ref localY);

                float normalizedU = MapTiledAxis(
                    localX,
                    drawSize.x,
                    borderData.BorderLeft,
                    borderData.BorderRight,
                    borderData.CenterTileWidth,
                    borderData.UvLeft,
                    borderData.UvRight);

                float normalizedV = MapTiledAxis(
                    localY,
                    drawSize.y,
                    borderData.BorderBottom,
                    borderData.BorderTop,
                    borderData.CenterTileHeight,
                    borderData.UvBottom,
                    borderData.UvTop);

                uv[i] = new Vector2(
                    uvMin.x + normalizedU * (uvMax.x - uvMin.x),
                    uvMin.y + normalizedV * (uvMax.y - uvMin.y));
            }
        }

        private static void CalculateSliced(
            SpriteRenderer spriteRenderer,
            Vector3[] localVertices,
            Rect drawRect,
            SpriteBorderData borderData,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            Vector2 drawSize = spriteRenderer.size;

            for (int i = 0; i < localVertices.Length; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(spriteRenderer, drawRect, ref localX, ref localY);

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
                    uvMin.x + normalizedU * (uvMax.x - uvMin.x),
                    uvMin.y + normalizedV * (uvMax.y - uvMin.y));
            }
        }

        private static void MapDrawSpaceLinear(
            SpriteRenderer spriteRenderer,
            Vector3[] localVertices,
            Rect drawRect,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector2[] uv)
        {
            float invWidth = drawRect.width > Epsilon ? 1f / drawRect.width : 0f;
            float invHeight = drawRect.height > Epsilon ? 1f / drawRect.height : 0f;

            float uvSpanX = uvMax.x - uvMin.x;
            float uvSpanY = uvMax.y - uvMin.y;

            for (int i = 0; i < localVertices.Length; i++)
            {
                float localX = localVertices[i].x - drawRect.x;
                float localY = localVertices[i].y - drawRect.y;

                ApplyFlip(spriteRenderer, drawRect, ref localX, ref localY);

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
            float uvBorderEnd)
        {
            float uvCenterStart = uvBorderStart;
            float uvCenterEnd = 1f - uvBorderEnd;
            centerTileSize = Mathf.Max(centerTileSize, Epsilon);

            if (borderStart > Epsilon && localPos <= borderStart)
            {
                return (localPos / borderStart) * uvBorderStart;
            }

            if (borderEnd > Epsilon && localPos >= drawSize - borderEnd)
            {
                float borderLocal = localPos - (drawSize - borderEnd);
                return uvCenterEnd + (borderLocal / borderEnd) * uvBorderEnd;
            }

            float centerLocal = Mathf.Max(localPos - borderStart, 0f);
            float frac = centerLocal / centerTileSize - Mathf.Floor(centerLocal / centerTileSize);
            return uvCenterStart + frac * (uvCenterEnd - uvCenterStart);
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

        private static void ApplyFlip(SpriteRenderer spriteRenderer, Rect drawRect, ref float localX, ref float localY)
        {
            if (spriteRenderer.flipX)
            {
                localX = drawRect.width - localX;
            }

            if (spriteRenderer.flipY)
            {
                localY = drawRect.height - localY;
            }
        }

        private static void GetUVRange(Vector2[] spriteUV, out Vector2 min, out Vector2 max)
        {
            min = spriteUV[0];
            max = spriteUV[0];

            foreach (Vector2 point in spriteUV)
            {
                if (point.x < min.x) min.x = point.x;
                if (point.x > max.x) max.x = point.x;
                if (point.y < min.y) min.y = point.y;
                if (point.y > max.y) max.y = point.y;
            }
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

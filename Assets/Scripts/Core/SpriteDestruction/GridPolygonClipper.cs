using System.Collections.Generic;
using UnityEngine;

namespace Core.SpriteDestruction
{
    internal static class GridPolygonClipper
    {
        private const float Epsilon = 1e-4f;
        private const float EpsilonSqr = Epsilon * Epsilon;
        private const int CircleArcSamples = 64;
        private static readonly Vector2[] UnitCircleSamples = CreateUnitCircleSamples();
        private static readonly List<Vector2> ClipScratchA = new(16);
        private static readonly List<Vector2> ClipScratchB = new(16);
        private static readonly AngleComparer SharedAngleComparer = new();

        private sealed class AngleComparer : IComparer<Vector2>
        {
            public Vector2 Centroid;

            public int Compare(Vector2 a, Vector2 b)
            {
                float angleA = Mathf.Atan2(a.y - Centroid.y, a.x - Centroid.x);
                float angleB = Mathf.Atan2(b.y - Centroid.y, b.x - Centroid.x);
                return angleA.CompareTo(angleB);
            }
        }

        public static List<Vector2> ClipRectToCircle(Rect rect, Vector2 center, float radius)
        {
            List<Vector2> points = new(12);
            float radiusSqr = radius * radius;
            Vector2[] corners =
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.yMin)
            };

            int insideCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if ((corners[i] - center).sqrMagnitude <= radiusSqr + Epsilon)
                {
                    points.Add(corners[i]);
                    insideCount++;
                }
            }

            if (insideCount == 4)
            {
                return EnsureCounterClockwise(points, true);
            }

            for (int i = 0; i < 4; i++)
            {
                AddSegmentCircleIntersections(corners[i], corners[(i + 1) % 4], center, radius, points);
            }

            for (int i = 0; i < CircleArcSamples; i++)
            {
                Vector2 point = center + UnitCircleSamples[i] * radius;
                if (ContainsInclusive(rect, point))
                {
                    points.Add(point);
                }
            }

            Deduplicate(points);
            if (points.Count < 3)
            {
                return new List<Vector2>();
            }

            Vector2 centroid = Vector2.zero;
            foreach (Vector2 point in points)
            {
                centroid += point;
            }

            centroid /= points.Count;
            SharedAngleComparer.Centroid = centroid;
            points.Sort(SharedAngleComparer);

            return Mathf.Abs(SignedArea(points)) >= Epsilon ? points : new List<Vector2>();
        }

        public static List<List<Vector2>> Clip(List<Vector2> boundary, List<Vector2> subject)
        {
            List<List<Vector2>> results = new();
            if (boundary == null || subject == null || boundary.Count < 3 || subject.Count < 3)
            {
                return results;
            }

            List<Vector2> clipPolygon = EnsureCounterClockwise(boundary, false);
            List<Vector2> subjectPolygon = EnsureCounterClockwise(subject, false);
            if (ClipScratchA.Capacity < subject.Count)
            {
                ClipScratchA.Capacity = subject.Count;
            }

            if (ClipScratchB.Capacity < subject.Count + 1)
            {
                ClipScratchB.Capacity = subject.Count + 1;
            }

            if (IsConvexPolygon(clipPolygon) && IsConvexPolygon(subjectPolygon))
            {
                List<Vector2> clipped = ClipConvex(
                    clipPolygon,
                    subjectPolygon,
                    ClipScratchA,
                    ClipScratchB);
                if (clipped.Count >= 3 && Mathf.Abs(SignedArea(clipped)) >= Epsilon)
                {
                    results.Add(clipped);
                }

                return results;
            }

            List<List<Vector2>> triangles = Triangulate(clipPolygon);
            if (triangles.Count == 0)
            {
                return results;
            }

            foreach (List<Vector2> triangle in triangles)
            {
                List<Vector2> clipped = ClipConvex(
                    subjectPolygon,
                    triangle,
                    ClipScratchA,
                    ClipScratchB);
                if (clipped.Count < 3 || Mathf.Abs(SignedArea(clipped)) < Epsilon)
                {
                    continue;
                }

                MergeIntoResults(results, clipped);
            }

            return results;
        }

        private static void MergeIntoResults(List<List<Vector2>> results, List<Vector2> piece)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (TryMergePolygons(results[i], piece, out List<Vector2> merged))
                {
                    results[i] = merged;
                    return;
                }
            }

            results.Add(piece);
        }

        private static List<Vector2> ClipConvex(
            List<Vector2> subject,
            List<Vector2> convexClip,
            List<Vector2> scratchA,
            List<Vector2> scratchB)
        {
            scratchA.Clear();
            scratchA.AddRange(subject);
            List<Vector2> input = scratchA;
            List<Vector2> output = scratchB;

            for (int i = 0; i < convexClip.Count; i++)
            {
                Vector2 a = convexClip[i];
                Vector2 b = convexClip[(i + 1) % convexClip.Count];
                output.Clear();
                ClipAgainstEdge(input, a, b, output);
                if (output.Count == 0)
                {
                    break;
                }

                List<Vector2> swap = input;
                input = output;
                output = swap;
            }

            return RemoveDuplicateVertices(output.Count == 0 ? output : input);
        }

        private static void ClipAgainstEdge(
            List<Vector2> input,
            Vector2 edgeStart,
            Vector2 edgeEnd,
            List<Vector2> output)
        {
            if (input.Count == 0)
            {
                return;
            }

            for (int i = 0; i < input.Count; i++)
            {
                Vector2 current = input[i];
                Vector2 previous = input[(i + input.Count - 1) % input.Count];

                bool currentInside = IsInsideHalfPlane(current, edgeStart, edgeEnd);
                bool previousInside = IsInsideHalfPlane(previous, edgeStart, edgeEnd);

                if (currentInside)
                {
                    if (!previousInside && TrySegmentIntersection(previous, current, edgeStart, edgeEnd, out Vector2 enter))
                    {
                        output.Add(enter);
                    }

                    output.Add(current);
                    continue;
                }
                
                if (previousInside && TrySegmentIntersection(previous, current, edgeStart, edgeEnd, out Vector2 exit))
                {
                    output.Add(exit);
                }
            }
        }

        private static bool IsInsideHalfPlane(Vector2 point, Vector2 edgeStart, Vector2 edgeEnd)
        {
            return Cross(edgeEnd - edgeStart, point - edgeStart) >= -Epsilon;
        }

        private static bool TryMergePolygons(List<Vector2> a, List<Vector2> b, out List<Vector2> merged)
        {
            merged = null;
            if (a == null || b == null || a.Count < 3 || b.Count < 3)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                Vector2 a0 = a[i];
                Vector2 a1 = a[(i + 1) % a.Count];

                for (int j = 0; j < b.Count; j++)
                {
                    Vector2 b0 = b[j];
                    Vector2 b1 = b[(j + 1) % b.Count];

                    if (!SharesEdge(a0, a1, b0, b1))
                    {
                        continue;
                    }

                    merged = JoinPolygons(a, i, b, j);
                    merged = RemoveDuplicateVertices(merged);
                    return merged.Count >= 3;
                }
            }

            return false;
        }

        private static bool SharesEdge(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            return (Approximately(a0, b1) && Approximately(a1, b0))
                || (Approximately(a0, b0) && Approximately(a1, b1));
        }

        private static List<Vector2> JoinPolygons(List<Vector2> a, int edgeIndexA, List<Vector2> b, int edgeIndexB)
        {
            Vector2 a0 = a[edgeIndexA];
            Vector2 a1 = a[(edgeIndexA + 1) % a.Count];
            Vector2 b0 = b[edgeIndexB];
            Vector2 b1 = b[(edgeIndexB + 1) % b.Count];

            List<Vector2> result = new(a.Count + b.Count);
            if (Approximately(a0, b1) && Approximately(a1, b0))
            {
                for (int k = 0; k < a.Count; k++)
                {
                    result.Add(a[(edgeIndexA + 1 + k) % a.Count]);
                }

                for (int k = 1; k < b.Count; k++)
                {
                    result.Add(b[(edgeIndexB + k) % b.Count]);
                }
            }
            else if (Approximately(a0, b0) && Approximately(a1, b1))
            {
                for (int k = 0; k < a.Count; k++)
                {
                    result.Add(a[(edgeIndexA + 1 + k) % a.Count]);
                }

                for (int k = 1; k < b.Count; k++)
                {
                    result.Add(b[(edgeIndexB + 1 + k) % b.Count]);
                }
            }

            return result;
        }

        private static List<List<Vector2>> Triangulate(List<Vector2> polygon)
        {
            List<List<Vector2>> triangles = new();
            if (polygon.Count < 3)
            {
                return triangles;
            }

            if (polygon.Count == 3)
            {
                triangles.Add(new List<Vector2>(polygon));
                return triangles;
            }

            var indices = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                indices.Add(i);
            }

            int guard = 0;
            while (indices.Count > 3 && guard++ < polygon.Count * polygon.Count)
            {
                bool earFound = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = indices[(i - 1 + indices.Count) % indices.Count];
                    int curr = indices[i];
                    int next = indices[(i + 1) % indices.Count];

                    if (!IsConvex(polygon[prev], polygon[curr], polygon[next]))
                    {
                        continue;
                    }

                    if (ContainsPointInTriangle(polygon, indices, prev, curr, next))
                    {
                        continue;
                    }

                    triangles.Add(new List<Vector2>
                    {
                        polygon[prev],
                        polygon[curr],
                        polygon[next]
                    });
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.Add(new List<Vector2>
                {
                    polygon[indices[0]],
                    polygon[indices[1]],
                    polygon[indices[2]]
                });
            }

            if (triangles.Count == 0)
            {
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    triangles.Add(new List<Vector2>
                    {
                        polygon[0],
                        polygon[i],
                        polygon[i + 1]
                    });
                }
            }

            return triangles;
        }

        private static bool ContainsPointInTriangle(
            List<Vector2> polygon,
            List<int> indices,
            int prev,
            int curr,
            int next)
        {
            Vector2 a = polygon[prev];
            Vector2 b = polygon[curr];
            Vector2 c = polygon[next];

            foreach (int index in indices)
            {
                if (index == prev || index == curr || index == next)
                {
                    continue;
                }

                if (IsPointInTriangle(polygon[index], a, b, c))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float cross1 = Cross(b - a, point - a);
            float cross2 = Cross(c - b, point - b);
            float cross3 = Cross(a - c, point - c);
            bool hasNeg = cross1 < -Epsilon || cross2 < -Epsilon || cross3 < -Epsilon;
            bool hasPos = cross1 > Epsilon || cross2 > Epsilon || cross3 > Epsilon;
            return !(hasNeg && hasPos);
        }

        private static bool IsConvexPolygon(List<Vector2> polygon)
        {
            if (polygon.Count < 3)
            {
                return false;
            }

            bool? positive = null;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
                Vector2 curr = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                float cross = Cross(curr - prev, next - curr);
                if (Mathf.Abs(cross) < Epsilon)
                {
                    continue;
                }

                bool isPositive = cross > 0f;
                if (positive == null)
                {
                    positive = isPositive;
                    continue;
                }

                if (positive != isPositive)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsConvex(Vector2 prev, Vector2 curr, Vector2 next)
        {
            return Cross(curr - prev, next - curr) > Epsilon;
        }

        private static List<Vector2> EnsureCounterClockwise(List<Vector2> polygon, bool ownsInput)
        {
            if (SignedArea(polygon) < 0f)
            {
                List<Vector2> result = ownsInput ? polygon : new List<Vector2>(polygon);
                result.Reverse();
                return result;
            }

            return polygon;
        }

        private static Vector2[] CreateUnitCircleSamples()
        {
            Vector2[] samples = new Vector2[CircleArcSamples];
            for (int i = 0; i < CircleArcSamples; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleArcSamples;
                samples[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return samples;
        }

        private static bool ContainsInclusive(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin - Epsilon
                && point.x <= rect.xMax + Epsilon
                && point.y >= rect.yMin - Epsilon
                && point.y <= rect.yMax + Epsilon;
        }

        private static void AddSegmentCircleIntersections(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            float radius,
            List<Vector2> points)
        {
            Vector2 direction = end - start;
            Vector2 offset = start - center;
            float a = Vector2.Dot(direction, direction);
            if (a < EpsilonSqr)
            {
                return;
            }

            float b = 2f * Vector2.Dot(offset, direction);
            float c = Vector2.Dot(offset, offset) - radius * radius;
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
            {
                return;
            }

            float sqrt = Mathf.Sqrt(Mathf.Max(0f, discriminant));
            float inv = 0.5f / a;
            float t1 = (-b - sqrt) * inv;
            float t2 = (-b + sqrt) * inv;
            if (t1 >= -Epsilon && t1 <= 1f + Epsilon)
            {
                points.Add(start + direction * Mathf.Clamp01(t1));
            }

            if (t2 >= -Epsilon && t2 <= 1f + Epsilon && Mathf.Abs(t2 - t1) > Epsilon)
            {
                points.Add(start + direction * Mathf.Clamp01(t2));
            }
        }

        private static void Deduplicate(List<Vector2> points)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (Approximately(points[i], points[j]))
                    {
                        points.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static List<Vector2> RemoveDuplicateVertices(List<Vector2> polygon)
        {
            if (polygon.Count == 0)
            {
                return polygon;
            }

            List<Vector2> cleaned = new(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                if (!Approximately(current, next))
                {
                    cleaned.Add(current);
                }
            }

            if (cleaned.Count > 1 && Approximately(cleaned[0], cleaned[^1]))
            {
                cleaned.RemoveAt(cleaned.Count - 1);
            }

            return cleaned;
        }

        private static bool TrySegmentIntersection(
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            Vector2 p4,
            out Vector2 intersection)
        {
            intersection = Vector2.zero;
            Vector2 d1 = p2 - p1;
            Vector2 d2 = p4 - p3;
            float denominator = Cross(d1, d2);
            if (Mathf.Abs(denominator) < Epsilon)
            {
                return false;
            }

            float t = Cross(p3 - p1, d2) / denominator;
            float u = Cross(p3 - p1, d1) / denominator;
            if (t < -Epsilon || t > 1f + Epsilon || u < -Epsilon || u > 1f + Epsilon)
            {
                return false;
            }

            intersection = p1 + d1 * t;
            return true;
        }

        private static float SignedArea(List<Vector2> polygon)
        {
            double area = 0d;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += (double)a.x * b.y - (double)b.x * a.y;
            }

            return (float)(area * 0.5d);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude <= EpsilonSqr;
        }
    }
}

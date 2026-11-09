using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.SpriteDestruction
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GridFragment : PoolComponent
    {
        [SerializeField] private Vector3[] _uvVertices;
        [SerializeField] private Vector3[] _meshVertices;
        [SerializeField] private int[] _triangles;
        [SerializeField] private Vector3 _restLocalPosition;
        [SerializeField] private Quaternion _restLocalRotation;
        [SerializeField] private Vector3 _restLocalScale;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private Collider2D _collider;

        private readonly List<Vector2> _colliderPoints = new(16);
        private Mesh _runtimeMesh;
        private SharedMaterialLease _materialLease;

        public Rigidbody2D Body
        {
            get
            {
                Cache();
                return _body;
            }
        }

        public MeshRenderer MeshRenderer
        {
            get
            {
                Cache();
                return _meshRenderer;
            }
        }

        public override void Initialize(IPool pool, EntityId templateId)
        {
            base.Initialize(pool, templateId);
            Cache();
            EnsureRuntimeMesh();
        }

        public override void BackToPool()
        {
            Cache();
            ClearRuntimeState();
            base.BackToPool();
        }

        public void Release()
        {
            if (InPool)
            {
                return;
            }

            if (Application.isPlaying && Pool != null)
            {
                BackToPool();
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void SetBakeData(Vector3[] uvVertices, Mesh mesh)
        {
            _uvVertices = uvVertices;
            _meshVertices = mesh.vertices;
            _triangles = mesh.triangles;
        }

        public void ApplyRuntimeMesh(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uv,
            Bounds bounds)
        {
            Cache();
            EnsureRuntimeMesh();
            _runtimeMesh.Clear();
            _runtimeMesh.SetVertices(vertices);
            _runtimeMesh.SetTriangles(triangles, 0, false);
            _runtimeMesh.SetUVs(0, uv);
            _runtimeMesh.bounds = bounds;
            _meshFilter.sharedMesh = _runtimeMesh;
        }

        public void ConfigureBoxCollider(Vector2 offset, Vector2 size)
        {
            BoxCollider2D boxCollider = EnsureCollider<BoxCollider2D>();
            if (!boxCollider)
            {
                return;
            }

            boxCollider.offset = offset;
            boxCollider.size = size;
        }

        public void ConfigureCollider(
            List<Vector2> points,
            List<PolygonRange> polygons,
            int polygonStart,
            int polygonCount,
            Vector3 pivotDiff)
        {
            PolygonCollider2D polygonCollider = EnsureCollider<PolygonCollider2D>();
            if (!polygonCollider)
            {
                return;
            }

            SetPolygonCollider(polygonCollider, points, polygons, polygonStart, polygonCount, pivotDiff);
        }

#if UNITY_EDITOR
        public void DestroyRuntimeMeshImmediate()
        {
            Cache();
            _meshFilter.sharedMesh = null;
            if (_runtimeMesh)
            {
                DestroyImmediate(_runtimeMesh);
                _runtimeMesh = null;
            }
        }
#endif

        public static bool TransferMaterialOwnership(IEnumerable<GridFragment> fragments, Material material)
        {
            if (!material || fragments == null)
            {
                return false;
            }

            SharedMaterialLease lease = new(material);
            foreach (GridFragment fragment in fragments)
            {
                if (!fragment)
                {
                    continue;
                }

                fragment._materialLease?.Release();
                fragment._materialLease = lease;
                lease.Retain();
            }

            return lease.HasOwners;
        }

        public void CaptureRestPose()
        {
            Transform fragmentTransform = transform;
            _restLocalPosition = fragmentTransform.localPosition;
            _restLocalRotation = fragmentTransform.localRotation;
            _restLocalScale = fragmentTransform.localScale;
        }

        public void EnsureMeshInstance()
        {
            Cache();
            if (_runtimeMesh || _meshVertices == null || _meshVertices.Length < 3 || _triangles == null)
            {
                return;
            }

            _runtimeMesh = new Mesh { name = name };
            _runtimeMesh.vertices = _meshVertices;
            _runtimeMesh.triangles = _triangles;
            _runtimeMesh.RecalculateBounds();
            _meshFilter.sharedMesh = _runtimeMesh;
        }

        private void OnDestroy()
        {
            if (_runtimeMesh)
            {
                if (Application.isPlaying)
                {
                    Destroy(_runtimeMesh);
                }
                else
                {
                    DestroyImmediate(_runtimeMesh);
                }

                _runtimeMesh = null;
            }

            _materialLease?.Release();
            _materialLease = null;
        }

        public void ApplySprite(SpriteRenderer spriteRenderer, Transform root)
        {
            EnsureMeshInstance();
            if (!_runtimeMesh || spriteRenderer == null || _uvVertices == null || _uvVertices.Length == 0)
            {
                return;
            }

            _runtimeMesh.uv = SpriteDrawUV.Calculate(spriteRenderer, root, _uvVertices);
        }

        public void Launch(Transform worldParent, Vector2 inheritedVelocity)
        {
            Cache();
            transform.SetParent(worldParent, true);
            _body.simulated = true;
            _body.linearVelocity = inheritedVelocity;
            _body.angularVelocity = 0f;
        }

        public void Restore(Transform fragmentsRoot)
        {
            Cache();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.simulated = false;
            _body.Sleep();

            transform.SetParent(fragmentsRoot, false);
            transform.localPosition = _restLocalPosition;
            transform.localRotation = _restLocalRotation;
            transform.localScale = _restLocalScale;
        }

        private void ClearRuntimeState()
        {
            if (_meshFilter)
            {
                _meshFilter.sharedMesh = null;
            }

            if (_meshRenderer)
            {
                _meshRenderer.sharedMaterial = null;
            }

            if (_body)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0f;
                _body.simulated = true;
            }

            _materialLease?.Release();
            _materialLease = null;
        }

        private T EnsureCollider<T>() where T : Collider2D
        {
            Cache();
            if (_collider is T existing)
            {
                return existing;
            }

            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i])
                {
                    DestroyImmediate(colliders[i]);
                }
            }

            T added = gameObject.AddComponent<T>();
            _collider = added;
            return added;
        }

        private void SetPolygonCollider(
            PolygonCollider2D collider,
            List<Vector2> points,
            List<PolygonRange> polygons,
            int polygonStart,
            int polygonCount,
            Vector3 pivotDiff)
        {
            int pathIndex = 0;
            int polygonEnd = Mathf.Min(polygonStart + polygonCount, polygons.Count);
            for (int polygonIndex = polygonStart; polygonIndex < polygonEnd; polygonIndex++)
            {
                PolygonRange polygon = polygons[polygonIndex];
                if (polygon.Count < 3 || polygon.Start < 0 || polygon.Start + polygon.Count > points.Count)
                {
                    continue;
                }

                _colliderPoints.Clear();
                if (_colliderPoints.Capacity < polygon.Count)
                {
                    _colliderPoints.Capacity = polygon.Count;
                }

                for (int i = 0; i < polygon.Count; i++)
                {
                    _colliderPoints.Add(points[polygon.Start + i] + (Vector2)pivotDiff);
                }

                collider.pathCount = pathIndex + 1;
                collider.SetPath(pathIndex, _colliderPoints);
                pathIndex++;
            }

            collider.pathCount = pathIndex;
        }

        private void EnsureRuntimeMesh()
        {
            if (_runtimeMesh)
            {
                return;
            }

            _runtimeMesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt16
            };
            _runtimeMesh.MarkDynamic();
        }

        private void Cache()
        {
            if (!_meshFilter)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (!_meshRenderer)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }

            if (!_body)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            if (!_collider)
            {
                _collider = GetComponent<Collider2D>();
            }
        }

        public readonly struct PolygonRange
        {
            public readonly int Start;
            public readonly int Count;

            public PolygonRange(int start, int count)
            {
                Start = start;
                Count = count;
            }
        }

        private sealed class SharedMaterialLease
        {
            private Material _material;
            private int _ownerCount;

            public bool HasOwners => _ownerCount > 0;

            public SharedMaterialLease(Material material)
            {
                _material = material;
            }

            public void Retain()
            {
                _ownerCount++;
            }

            public void Release()
            {
                if (_ownerCount <= 0)
                {
                    return;
                }

                _ownerCount--;
                if (_ownerCount > 0 || !_material)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(_material);
                }
                else
                {
                    Object.DestroyImmediate(_material);
                }

                _material = null;
            }
        }
    }
}

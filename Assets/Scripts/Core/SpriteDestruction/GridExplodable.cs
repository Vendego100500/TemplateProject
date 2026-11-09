
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace Core.SpriteDestruction
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GridExplodable : MonoBehaviour
    {
        private const string FragmentsRootName = "BakedFragments";

        [SerializeField] private bool _allowRuntimeFragmentation = true;
        [SerializeField] private bool _useColliderBounds;
        [SerializeField, Min(0.01f)] private float _cellSize = 0.25f;
        [SerializeField] private string _fragmentLayer = "Default";
        [SerializeField] private int _orderInLayer;
        [SerializeField] private SpriteRenderer _spriteSource;

        [Header("Explosion")]
        [SerializeField, Min(0f)] private float _force = 0.5f;
        [SerializeField, Min(0.01f)] private float _radius = 1.5f;

        [Header("Baked Fragments")]
        [SerializeField] private Transform _fragmentsRoot;
        [SerializeField] private GridFragment[] _bakedFragments;
        [SerializeField] private Material _fragmentMaterial;

        private List<GridFragment> _runtimeFragments = new();
        private bool _bakedPrepared;
        private bool _hasExploded;
        private bool _ownsFragmentMaterial;
        private int _fragmentLayerIndex;

        public int BakedFragmentCount
        {
            get
            {
                if (_bakedFragments == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (GridFragment fragment in _bakedFragments)
                {
                    if (fragment)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasBakedFragments => BakedFragmentCount > 0;

        private void Awake()
        {
            _fragmentLayerIndex = gameObject.layer;
            PrepareBakedMeshes();
        }

        private void OnValidate()
        {
            if (!IsFinite(_cellSize) || _cellSize < 0.01f)
            {
                _cellSize = 0.01f;
            }

            if (!IsFinite(_force) || _force < 0f)
            {
                _force = 0f;
            }

            if (!IsFinite(_radius) || _radius < 0.01f)
            {
                _radius = 0.01f;
            }
        }

        private void OnDestroy()
        {
            if (_ownsFragmentMaterial && _fragmentMaterial)
            {
                if (GridFragment.TransferMaterialOwnership(_bakedFragments, _fragmentMaterial))
                {
                    _ownsFragmentMaterial = false;
                }
                else
                {
                    Destroy(_fragmentMaterial);
                }
            }
        }

        public void Explode(Vector3 collisionPoint, float force, Vector2 impactDirection, bool destroySourceObject = false)
        {
            if (_hasExploded || !ValidateExplosion(collisionPoint, force, impactDirection))
            {
                return;
            }

            bool exploded;
            if (HasBakedFragments)
            {
                ExplodeBaked(collisionPoint, force, impactDirection);
                exploded = true;
            }
            else
            {
                exploded = ExplodeRuntime(collisionPoint, force, impactDirection);
            }

            if (!exploded)
            {
                return;
            }

            _hasExploded = true;
            if (destroySourceObject)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        public void DeleteFragments()
        {
            DeleteRuntimeFragments();
            RestoreBakedFragments();
            _hasExploded = false;
        }

#if UNITY_EDITOR
        public bool BakeFragments()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("GridExplodable: fragments cannot be baked during Play Mode.", this);
                return false;
            }

            if (!ValidateFragmentGeneration())
            {
                return false;
            }

            SpriteRenderer spriteRenderer = GetSpriteSource();
            Material newMaterial = SpriteGridExploder.CreateFragmentMaterial(spriteRenderer, gameObject);
            List<Vector3[]> uvVertices = new();
            List<GridFragment> pieces = SpriteGridExploder.GenerateGridPieces(
                gameObject,
                _cellSize,
                newMaterial,
                _useColliderBounds,
                uvVertices,
                spriteRenderer);

            if (pieces.Count == 0)
            {
                DestroyGeneratedBake(null, pieces, newMaterial);
                Debug.LogError("GridExplodable: bake produced no fragments; previous bake was preserved.", this);
                return false;
            }

            GameObject newRootObject = new GameObject(FragmentsRootName);
            SceneManager.MoveGameObjectToScene(newRootObject, gameObject.scene);
            Transform newRoot = newRootObject.transform;
            newRoot.SetParent(transform, false);

            int fragmentLayer = _fragmentLayerIndex;
            List<GridFragment> fragments = new(pieces.Count);
            try
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    GridFragment fragment = pieces[i];
                    if (!fragment)
                    {
                        continue;
                    }

                    GameObject piece = fragment.gameObject;
                    if (PrefabUtility.IsPartOfPrefabInstance(piece))
                    {
                        PrefabUtility.UnpackPrefabInstance(piece, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }

                    piece.name = $"Fragment_{i:00}";
                    piece.layer = fragmentLayer;
                    SceneManager.MoveGameObjectToScene(piece, newRootObject.scene);
                    piece.transform.SetParent(newRoot, true);

                    MeshRenderer meshRenderer = fragment.MeshRenderer;
                    meshRenderer.sortingOrder = _orderInLayer;
                    if (spriteRenderer)
                    {
                        meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                    }
                    meshRenderer.sharedMaterial = newMaterial;

                    fragment.SetBakeData(uvVertices[i], piece.GetComponent<MeshFilter>().sharedMesh);
                    fragment.CaptureRestPose();
                    fragment.DestroyRuntimeMeshImmediate();
                    fragment.Body.simulated = false;
                    fragments.Add(fragment);
                }

                if (fragments.Count == 0)
                {
                    DestroyGeneratedBake(newRootObject, pieces, newMaterial);
                    Debug.LogError("GridExplodable: bake produced no valid fragments; previous bake was preserved.", this);
                    return false;
                }

                newRootObject.SetActive(false);
            }
            catch
            {
                DestroyGeneratedBake(newRootObject, pieces, newMaterial);
                throw;
            }

            Transform previousRoot = _fragmentsRoot;
            Material previousMaterial = _fragmentMaterial;
            _fragmentsRoot = newRoot;
            _bakedFragments = fragments.ToArray();
            _fragmentMaterial = newMaterial;

            if (previousRoot)
            {
                DestroyImmediate(previousRoot.gameObject);
            }

            if (previousMaterial)
            {
                DestroyImmediate(previousMaterial);
            }

            return true;
        }

        public void ClearBakedFragments()
        {
            if (_fragmentsRoot)
            {
                DestroyEditorObject(_fragmentsRoot.gameObject);
                _fragmentsRoot = null;
            }

            _bakedFragments = System.Array.Empty<GridFragment>();

            if (_fragmentMaterial)
            {
                DestroyEditorObject(_fragmentMaterial);
                _fragmentMaterial = null;
            }
        }

        private static void DestroyGeneratedBake(
            GameObject root,
            List<GridFragment> pieces,
            Material material)
        {
            if (root)
            {
                DestroyImmediate(root);
            }

            foreach (GridFragment piece in pieces)
            {
                if (piece)
                {
                    DestroyImmediate(piece.gameObject);
                }
            }

            if (material)
            {
                DestroyImmediate(material);
            }
        }

        private static void DestroyEditorObject(Object value)
        {
            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
#endif

        private void ExplodeBaked(Vector3 collisionPoint, float force, Vector2 impactDirection)
        {
            PrepareBakedMeshes();
            RestoreBakedFragments();

            SpriteRenderer spriteRenderer = GetSpriteSource();
            ApplySprite(spriteRenderer);

            Vector2 inheritedVelocity = GetComponent<Rigidbody2D>().linearVelocity;
            Transform worldParent = transform.parent;
            foreach (GridFragment fragment in _bakedFragments)
            {
                if (!fragment)
                {
                    continue;
                }

                fragment.Launch(worldParent, inheritedVelocity);
                AddExplosionForce(fragment.Body, collisionPoint, force, impactDirection);
            }
        }

        private bool ExplodeRuntime(Vector3 collisionPoint, float force, Vector2 impactDirection)
        {
            if (_runtimeFragments.Count > 0)
            {
                DeleteRuntimeFragments();
            }

            if (!_allowRuntimeFragmentation)
            {
                Debug.LogWarning("GridExplodable: no baked fragments and runtime fragmentation is disabled.", this);
                return false;
            }

            if (!ValidateFragmentGeneration())
            {
                return false;
            }

            GenerateRuntimeFragments();
            if (_runtimeFragments.Count == 0)
            {
                Debug.LogWarning("GridExplodable: runtime fragmentation produced no fragments.", this);
                return false;
            }

            foreach (GridFragment fragment in _runtimeFragments)
            {
                if (!fragment)
                {
                    continue;
                }

                AddExplosionForce(fragment.Body, collisionPoint, force, impactDirection);
            }

            return true;
        }

        private void GenerateRuntimeFragments()
        {
            SpriteRenderer spriteRenderer = GetSpriteSource();
            _runtimeFragments = SpriteGridExploder.GenerateGridPieces(
                gameObject,
                _cellSize,
                null,
                _useColliderBounds,
                null,
                spriteRenderer);

            int fragmentLayer = _fragmentLayerIndex;

            foreach (GridFragment fragment in _runtimeFragments)
            {
                if (!fragment)
                {
                    continue;
                }

                fragment.gameObject.layer = fragmentLayer;
                MeshRenderer meshRenderer = fragment.MeshRenderer;
                if (meshRenderer)
                {
                    meshRenderer.sortingOrder = _orderInLayer;
                    if (spriteRenderer)
                    {
                        meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                    }
                }

                fragment.gameObject.SetActive(true);
                if (fragment.Body)
                {
                    fragment.Body.WakeUp();
                }
            }
        }

        private void DeleteRuntimeFragments()
        {
            foreach (GridFragment fragment in _runtimeFragments)
            {
                if (fragment)
                {
                    fragment.Release();
                }
            }

            _runtimeFragments.Clear();
        }

        private void RestoreBakedFragments()
        {
            if (!HasBakedFragments)
            {
                return;
            }

            foreach (GridFragment fragment in _bakedFragments)
            {
                if (fragment)
                {
                    fragment.Restore(_fragmentsRoot);
                }
            }

            if (_fragmentsRoot)
            {
                _fragmentsRoot.gameObject.SetActive(false);
            }
        }

        private void PrepareBakedMeshes()
        {
            if (_bakedPrepared || !HasBakedFragments)
            {
                return;
            }

            if (_fragmentMaterial)
            {
                _fragmentMaterial = new Material(_fragmentMaterial);
                _ownsFragmentMaterial = true;
            }

            foreach (GridFragment fragment in _bakedFragments)
            {
                if (!fragment)
                {
                    continue;
                }

                fragment.EnsureMeshInstance();
                if (_fragmentMaterial)
                {
                    fragment.GetComponent<MeshRenderer>().sharedMaterial = _fragmentMaterial;
                }
            }

            _bakedPrepared = true;
        }

        private void ApplySprite(SpriteRenderer spriteRenderer)
        {
            if (!spriteRenderer || !spriteRenderer.sprite)
            {
                return;
            }

            if (!_fragmentMaterial)
            {
                _fragmentMaterial = SpriteGridExploder.CreateFragmentMaterial(spriteRenderer);
                _ownsFragmentMaterial = true;
                foreach (GridFragment fragment in _bakedFragments)
                {
                    if (!fragment)
                    {
                        continue;
                    }

                    fragment.GetComponent<MeshRenderer>().sharedMaterial = _fragmentMaterial;
                }
            }

            _fragmentMaterial.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            _fragmentMaterial.color = spriteRenderer.color;
            SpriteDrawUV.ConfigureMaterial(spriteRenderer, _fragmentMaterial);

            Transform rootTransform = transform;
            foreach (GridFragment fragment in _bakedFragments)
            {
                if (fragment)
                {
                    fragment.ApplySprite(spriteRenderer, rootTransform);
                }
            }
        }

        private SpriteRenderer GetSpriteSource()
        {
            if (_spriteSource)
            {
                return _spriteSource;
            }

            return GetComponentInChildren<SpriteRenderer>();
        }

        private void AddExplosionForce(Rigidbody2D body, Vector3 forcePoint, float force, Vector2 impactDirection)
        {
            if (!body)
            {
                return;
            }

            Vector2 punch = impactDirection.sqrMagnitude > 1e-6f ? impactDirection.normalized : Vector2.zero;
            Vector2 origin = forcePoint;
            if (punch != Vector2.zero)
            {
                origin -= punch * 0.35f;
            }

            Vector2 offset = body.worldCenterOfMass - origin;
            float distance = offset.magnitude;
            Vector2 radial = distance > 1e-4f ? offset / distance : punch;
            if (radial.sqrMagnitude < 1e-6f)
            {
                radial = Vector2.up;
            }

            Vector2 direction = punch.sqrMagnitude > 1e-6f
                ? (radial + punch).normalized
                : radial;

            float radius = Mathf.Max(_radius, 0.01f);
            float falloff = Mathf.Clamp01(1f - distance / radius);

            float impulse = _force * force * falloff;
            body.AddForce(direction * impulse, ForceMode2D.Impulse);

            float torque = radial.x * direction.y - radial.y * direction.x;
            if (Mathf.Abs(torque) < 0.05f)
            {
                torque = (body.GetEntityId().GetHashCode() & 1) == 0 ? 1f : -1f;
            }

            body.AddTorque(torque * impulse * 0.12f, ForceMode2D.Impulse);
        }

        private bool ValidateExplosion(Vector3 collisionPoint, float force, Vector2 impactDirection)
        {
            if (!IsFinite(collisionPoint) || !IsFinite(impactDirection) || !IsFinite(force) || force < 0f)
            {
                Debug.LogWarning("GridExplodable: explosion arguments must be finite and force must be non-negative.", this);
                return false;
            }

            if (!IsFinite(_force) || _force < 0f || !IsFinite(_radius) || _radius <= 0f)
            {
                Debug.LogWarning("GridExplodable: explosion force and radius configuration is invalid.", this);
                return false;
            }

            return true;
        }

        private bool ValidateFragmentGeneration()
        {
            SpriteRenderer spriteRenderer = GetSpriteSource();
            if (!spriteRenderer || !spriteRenderer.sprite)
            {
                Debug.LogWarning("GridExplodable: a SpriteRenderer with a sprite is required to generate fragments.", this);
                return false;
            }

            if (!IsFinite(_cellSize) || _cellSize < 0.01f)
            {
                Debug.LogWarning("GridExplodable: cell size must be finite and at least 0.01.", this);
                return false;
            }

            if (_useColliderBounds
                && (!SpriteGridExploder.TryGetColliderPoints(gameObject, out List<Vector2> points)
                    || points == null
                    || points.Count < 3))
            {
                Debug.LogWarning("GridExplodable: collider bounds require a valid CircleCollider2D, BoxCollider2D, or PolygonCollider2D.", this);
                return false;
            }

            if (!SpriteGridExploder.TryGetGridLayout(
                    gameObject,
                    _cellSize,
                    _useColliderBounds,
                    out _))
            {
                Debug.LogWarning(
                    $"GridExplodable: grid layout is invalid or exceeds {SpriteGridExploder.MaxGridCellCount} cells.",
                    this);
                return false;
            }

            _fragmentLayerIndex = LayerMask.NameToLayer(_fragmentLayer);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isEditor || _cellSize <= 0f)
            {
                return;
            }

            if (!SpriteGridExploder.TryGetGridLayout(gameObject, _cellSize, _useColliderBounds, out SpriteGridExploder.GridLayout layout))
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            bool hasCircle = TryGetComponent(out CircleCollider2D circleCollider);
            SpriteGridExploder.TryGetColliderPoints(gameObject, out List<Vector2> borderPoints);
            List<Vector2> cellCorners = new(4);
            Gizmos.color = new Color(0.15f, 1f, 0.55f, 1f);
            for (int row = 0; row < layout.Rows; row++)
            {
                for (int col = 0; col < layout.Columns; col++)
                {
                    layout.GetCell(col, row, out float x0, out float y0, out float x1, out float y1);
                    cellCorners.Clear();
                    cellCorners.Add(new Vector2(x0, y0));
                    cellCorners.Add(new Vector2(x0, y1));
                    cellCorners.Add(new Vector2(x1, y1));
                    cellCorners.Add(new Vector2(x1, y0));

                    List<List<Vector2>> clippedCells = SpriteGridExploder.ClipCell(
                        cellCorners,
                        x0,
                        y0,
                        x1,
                        y1,
                        _useColliderBounds,
                        hasCircle,
                        circleCollider,
                        borderPoints);
                    foreach (List<Vector2> clippedCell in clippedCells)
                    {
                        DrawPolygon(clippedCell);
                    }
                }
            }
        }

        private static void DrawPolygon(List<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 2)
            {
                return;
            }

            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                Gizmos.DrawLine(polygon[i], polygon[next]);
            }
        }
    }
}

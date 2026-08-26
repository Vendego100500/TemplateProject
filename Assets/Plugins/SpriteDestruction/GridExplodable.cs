using System.Collections.Generic;
using UnityEngine;

namespace SpriteDestruction
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GridExplodable : MonoBehaviour
    {
        [SerializeField] private bool _allowRuntimeFragmentation = true;
        [SerializeField, Min(0.01f)] private float _cellSize = 0.25f;
        [SerializeField] private string _fragmentLayer = "Default";
        [SerializeField] private int _orderInLayer;
        
        [Header("Explosion")]
        [SerializeField] private float _force = 25;

        private readonly List<List<Vector2>> _gridLines = new();
        private List<GameObject> _fragments = new();

        
        public void Explode(Vector3 collisionPoint, float force, bool destroySourceObject = false)
        {
            if (_fragments.Count > 0)
            {
                DeleteFragments();
            }
            
            GenerateFragments();

            foreach (GameObject fragment in _fragments)
            {
                AddExplosionForce(fragment.GetComponent<Rigidbody2D>(), collisionPoint, force);
            }
            
            if (destroySourceObject)
            {
                Destroy(gameObject);
                return;
            }
            
            gameObject.SetActive(false);
        }

        public void DeleteFragments()
        {
            foreach (GameObject fragment in _fragments)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(fragment);
                }
                else
                {
                    Destroy(fragment);
                }
            }

            _fragments.Clear();
            _gridLines.Clear();
        }

        private void GenerateFragments()
        {
            _fragments = SpriteGridExploder.GenerateGridPieces(gameObject, _cellSize);

            foreach (GameObject fragment in _fragments)
            {
                if (!fragment)
                {
                    continue;
                }

                fragment.layer = LayerMask.NameToLayer(_fragmentLayer);
                fragment.GetComponent<Renderer>().sortingOrder = _orderInLayer;
            }
        }
        
        private void AddExplosionForce(Rigidbody2D body, Vector3 forcePoint, float force)
        {
            Vector3 dir = body.transform.position - forcePoint;
            Vector3 baseForce = dir.normalized * _force * force * Mathf.Max(0, 1 - dir.magnitude);
            body.AddForce(baseForce);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isEditor)
            {
                return;
            }

            Renderer rndrr = GetComponentInChildren<Renderer>();
            if (!rndrr || _cellSize <= 0f)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (_gridLines.Count > 0)
            {
                foreach (List<Vector2> polygon in _gridLines)
                {
                    DrawPolygon(polygon);
                }

                return;
            }

            Bounds localBounds = rndrr.localBounds;
            Vector3 lossyScale = transform.lossyScale;
            float cellSizeX = _cellSize / Mathf.Max(Mathf.Abs(lossyScale.x), 1e-6f);
            float cellSizeY = _cellSize / Mathf.Max(Mathf.Abs(lossyScale.y), 1e-6f);

            Rect rect;
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer)
            {
                rect = SpriteDrawUV.GetRendererRectInRootSpace(transform, spriteRenderer);
            }
            else
            {
                rect = new Rect(
                    localBounds.center.x - localBounds.extents.x,
                    localBounds.center.y - localBounds.extents.y,
                    localBounds.size.x,
                    localBounds.size.y);
            }

            int columns = Mathf.Max(1, Mathf.FloorToInt(rect.width / cellSizeX));
            int rows = Mathf.Max(1, Mathf.FloorToInt(rect.height / cellSizeY));

            float gridWidth = columns * cellSizeX;
            float gridHeight = rows * cellSizeY;
            float startX = rect.x + (rect.width - gridWidth) * 0.5f;
            float startY = rect.y + (rect.height - gridHeight) * 0.5f;

            for (int row = 0; row <= rows; row++)
            {
                float y = startY + row * cellSizeY;
                Gizmos.DrawLine(new Vector3(startX, y, 0f), new Vector3(startX + gridWidth, y, 0f));
            }

            for (int col = 0; col <= columns; col++)
            {
                float x = startX + col * cellSizeX;
                Gizmos.DrawLine(new Vector3(x, startY, 0f), new Vector3(x, startY + gridHeight, 0f));
            }
        }

        private static void DrawPolygon(List<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                Gizmos.DrawLine(polygon[i], polygon[next]);
            }
        }
    }
}

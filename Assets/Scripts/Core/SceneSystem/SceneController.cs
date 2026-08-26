
using Managers;
using UnityEngine;

namespace Core.SceneSystem
{
    public abstract class SceneController : MonoBehaviour
    {
        protected Camera _camera;

        protected virtual void Awake()
        {
            _camera = Camera.main;
        }

        protected virtual void Start()
        {
            OpenLevel();

            GlobalTimer.Instance.Tick += Tick;
        }

        protected virtual void OnDestroy()
        {
            if (GlobalTimer.Instance != null)
            {
                GlobalTimer.Instance.Tick -= Tick;
            }
        }

        public Vector2 ScreenToWorldPoint(Vector2 screenPosition)
        {
            Vector3 point = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            return new Vector2(point.x, point.y);
        }

        protected virtual void Tick(float deltaTime) { }

        protected virtual void OpenLevel() { }
    }
}

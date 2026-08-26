
using UnityEngine;
using UnityEngine.InputSystem;
using InputDevice = Core.Input.InputDevice;

namespace Core
{
    public class CameraController
    {
        private const float Speed = 100f;
        private const float ZoomSpeed = 5f;
        private const float MinZoom = 100f;
        private const float MaxZoom = 400f;
        
        private readonly Camera _camera;
        private readonly Transform _transform;
        private readonly InputDevice _inputDevice;
        
        private Vector2 _lastMousePos;

        public CameraController(Camera camera, InputDevice inputDevice)
        {
            _camera = camera;
            _inputDevice = inputDevice;
            _transform = camera.transform;
        }

        public void Tick()
        {
            if (_inputDevice.MiddleButton.State.IsPress)
            {
                _lastMousePos = Mouse.current.position.ReadValue();
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (_inputDevice.MiddleButton.State.IsDown)
            {
                Vector3 delta = mousePos - _lastMousePos;
                _transform.position -= delta * (Speed * Time.deltaTime);
                
                _lastMousePos = mousePos;
            }
            
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll == 0) return;
            
            Vector3 mouseWorldBefore = _camera.ScreenToWorldPoint(mousePos);
            
            _camera.orthographicSize -= scroll * ZoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, MinZoom, MaxZoom);
            
            Vector3 mouseWorldAfter = _camera.ScreenToWorldPoint(mousePos);
            
            _transform.position += mouseWorldBefore - mouseWorldAfter;
        }
    }
}
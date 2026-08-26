
using Core.Input;
using UnityEngine;
using ViewSystem;

namespace Core
{
    public class SceneController
    {
        public Transform Transform { get; }
        
        private readonly Camera _camera;
        private readonly CameraController _cameraController;
        
        public SceneController(GameObject scene, InputDevice inputDevice)
        {
            Transform = scene.transform;
            
            _camera  = scene.GetComponentInChildren<Camera>();
            _cameraController = new CameraController(_camera, inputDevice);
            
            HUD.Instance.Init(_camera);
        }
        
        public void Tick()
        {
            _cameraController.Tick();
        }
    }
}
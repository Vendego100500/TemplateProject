
using System;
using Core;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using static Utils.Utils;

namespace ViewSystem
{
    public class HUD : MonoBehaviourSingleton<HUD>
    {
        private Camera _camera;
        private CanvasScaler _canvasScaler;
        private float _currentAspectRatio;


        public void Init(Camera inCamera)
        {
            _camera = inCamera;

            Canvas canvas = GetComponentInChildren<Canvas>();
            if (!canvas)
            {
                throw new ArgumentException("HUD canvas not found");
            }
            
            _canvasScaler =  canvas.GetComponent<CanvasScaler>();
            if (!_canvasScaler)
            {
                throw new ArgumentException($"Canvas {canvas} must contains CanvasScaler");
            }
        }

        public void Tick()
        {
#if UNITY_EDITOR
            float currentAspect = _camera.aspect;
            if (!Mathf.Approximately(currentAspect, _currentAspectRatio))
            {
                SetScaleMatching(currentAspect);
            }
#endif
        }

        private void SetScaleMatching(float aspectRatio)
        {
            _currentAspectRatio = aspectRatio;
            _canvasScaler.matchWidthOrHeight = _currentAspectRatio switch
            {
                >= ASPECT_RATIO_16x9 => 1,
                >= ASPECT_RATIO_4x3 => 0,
                _ => _canvasScaler.matchWidthOrHeight
            };
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            Game.ApplicationQuitingInvoke();
        }
    }
}
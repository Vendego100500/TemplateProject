
using System;
using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
     public sealed class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [Header("Slider setup")] 
        [SerializeField, Range(0, 1f)] private float _sliderValue;
        [SerializeField] private Image _background;
        [SerializeField] private Color _onColor = Color.green;
        [SerializeField] private Color _offColor = Color.red;

        [Header("Animation")] 
        [SerializeField, Range(0, 1f)] private float _animationDuration = 0.5f;
        [SerializeField] private AnimationCurve _slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Events")] 
        [SerializeField] private UnityEvent<bool> OnToggleChanged;

        public bool CurrentValue { get; private set; } = true;

        private Action _transitionEffect;
        private Slider _slider;
        private Coroutine _animateSliderCoroutine;
        
        
        private void OnValidate()
        {
            SetupToggleComponents();

            _slider.value = _sliderValue;
        }

        private void SetupToggleComponents()
        {
            if (_slider)
            {
                return;
            }

            SetupSliderComponent();
        }

        private void SetupSliderComponent()
        {
            _slider = GetComponent<Slider>();

            if (!_slider)
            {
                Debug.Log("No slider found!", this);
                return;
            }

            _slider.interactable = false;
            ColorBlock sliderColors = _slider.colors;
            sliderColors.disabledColor = Color.white;
            _slider.colors = sliderColors;
            _slider.transition = Selectable.Transition.None;
        }


        private void Awake()
        {
            SetupSliderComponent();
        }
        

        public void OnPointerClick(PointerEventData eventData)
        {
            SetStateAndStartAnimation(!CurrentValue);
        }

        public void SetValue(bool state, bool notify = false)
        {
            CurrentValue = state;
            UpdateVisualState();

            if (notify)
            {
                OnToggleChanged.Invoke(CurrentValue);
            }
        }

        private void UpdateVisualState()
        {
            _background.color = CurrentValue ? _onColor : _offColor;
            _sliderValue = CurrentValue ? 1 : 0;
            _slider.value = _sliderValue;
        }
        
        private void SetStateAndStartAnimation(bool state)
        {
            if (state == CurrentValue)
            {
                return;
            }
            
            _background.color = state ? _onColor : _offColor;
            
            SfxManager.Instance.Play(ESfxId.ui_button_click);
            
            CurrentValue = state;
            OnToggleChanged.Invoke(CurrentValue);

            if (_animateSliderCoroutine != null)
            {
                StopCoroutine(_animateSliderCoroutine);
            }
            _animateSliderCoroutine = StartCoroutine(AnimateSlider());
        }


        private IEnumerator AnimateSlider()
        {
            float startValue = _slider.value;
            float endValue = CurrentValue ? 1 : 0;

            float time = 0;
            if (_animationDuration > 0)
            {
                while (time < _animationDuration)
                {
                    time += Time.deltaTime;

                    float lerpFactor = _slideEase.Evaluate(time / _animationDuration);
                    _slider.value = _sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                    _transitionEffect?.Invoke();
                        
                    yield return null;
                }
            }

            _slider.value = endValue;
        }
    }
}

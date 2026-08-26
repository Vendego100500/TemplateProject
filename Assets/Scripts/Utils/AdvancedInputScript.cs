
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utils
{
    public class AdvancedInputScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler
    {
        public const float SINGLE_CLICK_DELTA_MS = 0.15f;
        public const float LONG_START_CLICK_DELTA_MS = 0.16f;
        public const float LONG_CLICK_DELTA_MS = 0.45f;

        public EventDataParam OnTap;
        public EventDataParam OnLongTapStart;
        public EventDataParam OnLongTap;
        public EventDataParam OnLongTapEnd;
        public EventDataParam OnDoubleTap;
        
        private bool _isLongTapInProgress;
        private PointerEventData _lastDownData;
        private Coroutine _longClickCoroutine;
        private Coroutine _singleClickCoroutine;

        public void OnBeginDrag(PointerEventData eventData)
        {
            CancelAllCoroutines();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isLongTapInProgress = false;

            _lastDownData = eventData;
            if (_longClickCoroutine != null || _singleClickCoroutine != null)
            {
                CancelAllCoroutines();
                DoubleClick();

                return;
            }

            if (_longClickCoroutine != null)
            {
                StopCoroutine(_longClickCoroutine);
                _longClickCoroutine = null;
            }

            _longClickCoroutine = StartCoroutine(LongClickCoroutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if ((eventData.position - eventData.pressPosition).magnitude >= 5.0f)
            {
                CancelAllCoroutines();
                return;
            }

            if (_isLongTapInProgress)
            {
                _isLongTapInProgress = false;
                OnLongTapEnd.Invoke(eventData);
            }

            if (_longClickCoroutine == null)
            {
                return;
            }

            _singleClickCoroutine ??= StartCoroutine(SingleClickCoroutine());

            StopCoroutine(_longClickCoroutine); // передовать в кансел с нулом
            _longClickCoroutine = null;
        }

        public void LongTapStart()
        {
            OnLongTapStart.Invoke(_lastDownData);
        }

        public void SingleClick()
        {
            OnTap.Invoke(_lastDownData);
        }

        public void DoubleClick()
        {
            OnDoubleTap.Invoke(_lastDownData);
        }

        public void LongClick()
        {
            OnLongTap.Invoke(_lastDownData);
        }


        private IEnumerator SingleClickCoroutine()
        {
            yield return new WaitForSeconds(SINGLE_CLICK_DELTA_MS);

            _singleClickCoroutine = null;
            
            SingleClick();
            
            yield return null;
        }

        private IEnumerator LongClickCoroutine()
        {
            yield return new WaitForSeconds(LONG_START_CLICK_DELTA_MS);
            
            LongTapStart();

            yield return new WaitForSeconds(LONG_CLICK_DELTA_MS);
            
            _isLongTapInProgress = true;
            _longClickCoroutine = null;
            
            LongClick();

            yield return null;
        }

        private void CancelAllCoroutines()
        {
            if (_singleClickCoroutine != null)
            {
                StopCoroutine(_singleClickCoroutine);
            }
            _singleClickCoroutine = null;

            if (_longClickCoroutine != null)
            {
                StopCoroutine(_longClickCoroutine);
            }
            _longClickCoroutine = null;
        }

        [Serializable]
        public class EventDataParam : UnityEvent<PointerEventData>
        {
        }
    }
}
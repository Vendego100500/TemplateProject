using Managers.SoundManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Common
{
    public class MultiGraphicButton : Button
    {
        [SerializeField] private Graphic[] _graphics;
        [SerializeField] private ESfxId _sound = ESfxId.ui_button_click;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (IsActive() && IsInteractable() && _sound != ESfxId.None)
            {
                SfxManager.Instance.Play(_sound);
            }

            base.OnPointerClick(eventData);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            var targetColor =
                state == SelectionState.Disabled ? colors.disabledColor :
                state == SelectionState.Highlighted ? colors.highlightedColor :
                state == SelectionState.Normal ? colors.normalColor :
                state == SelectionState.Pressed ? colors.pressedColor :
                state == SelectionState.Selected ? colors.selectedColor : Color.white;

            var graphics = _graphics.Length > 0 ? _graphics : GetComponentsInChildren<Graphic>();
            foreach (var graphic in graphics)
            {
                graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }
    }
}

using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class MultiGraphicButton : Button
    {
        [SerializeField] private Graphic[] _graphics;
        [SerializeField] private ESfxId _sound = ESfxId.ui_button_click;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (IsActive() && IsInteractable() && _sound != ESfxId.None)
            {
                SfxManager.InvokePlay(_sound);
            }

            base.OnPointerClick(eventData);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            Color color = state switch
            {
                SelectionState.Disabled => colors.disabledColor,
                SelectionState.Highlighted => colors.highlightedColor,
                SelectionState.Normal => colors.normalColor,
                SelectionState.Pressed => colors.pressedColor,
                SelectionState.Selected => colors.selectedColor,
                _ => Color.white
            };
            
            UpdateGraphicColor(color, instant ? 0f : colors.fadeDuration);
        }

        public void UpdateGraphicColor(Color color, float fadeDuration)
        {
            var graphics = _graphics.Length > 0 ? _graphics : GetComponentsInChildren<Graphic>();
            foreach (var graphic in graphics)
            {
                graphic.CrossFadeColor(color, fadeDuration, true, true);
            }
        }
    }
}
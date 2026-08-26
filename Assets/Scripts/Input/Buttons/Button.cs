
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Input.Buttons
{
    public class Button : BaseButton<ButtonState>
    {
        private const float SingleClickDeltaMs = 0.29f;

        public event Action OnDown;
        public event Action OnUp;
        public event Action OnHold;
        public event Action OnPress;


        public Button(InputAction action)
            : base(action)
        {
        }

        public override void Tick()
        {
            if (_pressed)
            {
                OnPress.InvokeSafe();

                if (State.IsReleased)
                {
                    OnDown.InvokeSafe();
                    State = ButtonState.DownState;
                    return;
                }

                float deltaTime = Time.time - _pressTime;
                if (deltaTime > SingleClickDeltaMs)
                {
                    OnHold.InvokeSafe();
                    State = ButtonState.HoldState;
                    return;
                }

                State = ButtonState.PressedState;
                return;
            }

            if (State.IsReleased)
            {
                return;
            }
            
            OnUp.InvokeSafe();
            State = ButtonState.ReleasedState;
            
        }
    }
}


using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input.Buttons
{
    public class SimpleButton : BaseButton<bool>
    {
        private const float SpamProtectionTime = 0.1f;

        public SimpleButton(InputAction action)
            : base(action)
        {
        }

        public override void Tick()
        {
            if (_down)
            {
                float deltaTime = Time.time - _pressTime;
                if (deltaTime > SpamProtectionTime)
                {
                    _pressTime = Time.time;
                    State = true;
                    return;
                }
            }

            State = false;
        }
    }
}

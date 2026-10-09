// Заглушка только для проверки компиляции: те члены Input System, что использует GameInput.
// В Unity берётся настоящий пакет com.unity.inputsystem.
using UnityEngine;

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
    }

    public class KeyControl : ButtonControl { }

    public class Vector2Control
    {
        public Vector2 ReadValue() => Vector2.zero;
    }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;

    public enum Key { None, Space, A, C, D, E, F, I, K, Q, R, S, W, Escape, Digit1, LeftArrow, RightArrow, UpArrow, DownArrow }

    public class Keyboard
    {
        public static Keyboard current => null;
        public KeyControl this[Key key] => null;
    }

    public class Mouse
    {
        public static Mouse current => null;
        public ButtonControl leftButton => null;
        public ButtonControl rightButton => null;
        public Vector2Control position => null;
    }
}

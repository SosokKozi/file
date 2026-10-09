using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AshenCanvas.Game.Input
{
    public enum Act { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6, Potion, Interact, Inventory, Character, Skills, Menu }

    /// <summary>
    /// Ввод для клавиатуры и мыши. Работает и с новой системой ввода (Input System),
    /// и со старой — смотря что включено в Project Settings → Player → Active Input Handling.
    /// </summary>
    public static class GameInput
    {
        public static readonly string[] SkillKeyNames = { "ЛКМ", "ПКМ", "Q", "E", "R", "F" };

#if ENABLE_INPUT_SYSTEM
        static Key KeyOf(Act a)
        {
            switch (a)
            {
                case Act.Skill3: return Key.Q;
                case Act.Skill4: return Key.E;
                case Act.Skill5: return Key.R;
                case Act.Skill6: return Key.F;
                case Act.Potion: return Key.Digit1;
                case Act.Interact: return Key.Space;
                case Act.Inventory: return Key.I;
                case Act.Character: return Key.C;
                case Act.Skills: return Key.K;
                case Act.Menu: return Key.Escape;
                default: return Key.None;
            }
        }

        public static bool Down(Act a)
        {
            var m = Mouse.current;
            if (a == Act.Skill1) return m != null && m.leftButton.wasPressedThisFrame;
            if (a == Act.Skill2) return m != null && m.rightButton.wasPressedThisFrame;
            var kb = Keyboard.current;
            return kb != null && kb[KeyOf(a)].wasPressedThisFrame;
        }

        public static bool Held(Act a)
        {
            var m = Mouse.current;
            if (a == Act.Skill1) return m != null && m.leftButton.isPressed;
            if (a == Act.Skill2) return m != null && m.rightButton.isPressed;
            var kb = Keyboard.current;
            return kb != null && kb[KeyOf(a)].isPressed;
        }

        public static Vector2 Move
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return Vector2.zero;
                float x = (kb[Key.D].isPressed || kb[Key.RightArrow].isPressed ? 1f : 0f) - (kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed ? 1f : 0f);
                float y = (kb[Key.W].isPressed || kb[Key.UpArrow].isPressed ? 1f : 0f) - (kb[Key.S].isPressed || kb[Key.DownArrow].isPressed ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        static KeyCode KeyOf(Act a)
        {
            switch (a)
            {
                case Act.Skill3: return KeyCode.Q;
                case Act.Skill4: return KeyCode.E;
                case Act.Skill5: return KeyCode.R;
                case Act.Skill6: return KeyCode.F;
                case Act.Potion: return KeyCode.Alpha1;
                case Act.Interact: return KeyCode.Space;
                case Act.Inventory: return KeyCode.I;
                case Act.Character: return KeyCode.C;
                case Act.Skills: return KeyCode.K;
                case Act.Menu: return KeyCode.Escape;
                default: return KeyCode.None;
            }
        }

        public static bool Down(Act a)
        {
            if (a == Act.Skill1) return UnityEngine.Input.GetMouseButtonDown(0);
            if (a == Act.Skill2) return UnityEngine.Input.GetMouseButtonDown(1);
            return UnityEngine.Input.GetKeyDown(KeyOf(a));
        }

        public static bool Held(Act a)
        {
            if (a == Act.Skill1) return UnityEngine.Input.GetMouseButton(0);
            if (a == Act.Skill2) return UnityEngine.Input.GetMouseButton(1);
            return UnityEngine.Input.GetKey(KeyOf(a));
        }

        public static Vector2 Move
        {
            get
            {
                float x = (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                        - (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                        - (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static Vector2 MousePosition => UnityEngine.Input.mousePosition;
#endif

        public static Act SkillAct(int slot) => (Act)((int)Act.Skill1 + slot);
    }
}

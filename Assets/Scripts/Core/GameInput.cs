using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoBot
{
    /// <summary>
    /// Ponto único de leitura dos controles (teclado e gamepad).
    /// Os scripts do jogo nunca leem o teclado diretamente: perguntam para o GameInput.
    /// Funciona com o Input Manager clássico ou com o novo Input System.
    /// </summary>
    public static class GameInput
    {
        const float DeadZone = 0.35f;

#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        static bool Key(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        static bool KeyDown(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        static float Axis(string name)
        {
            try { return Input.GetAxisRaw(name); }
            catch (System.ArgumentException) { return 0f; } // eixo não configurado no projeto
        }

        public static float Horizontal
        {
            get
            {
                float h = 0f;
                if (Key(KeyCode.A, KeyCode.LeftArrow)) h -= 1f;
                if (Key(KeyCode.D, KeyCode.RightArrow)) h += 1f;
                if (h == 0f) h = Axis("Horizontal");
                return Mathf.Abs(h) < DeadZone ? 0f : Mathf.Sign(h);
            }
        }

        public static float Vertical
        {
            get
            {
                float v = 0f;
                if (Key(KeyCode.S, KeyCode.DownArrow)) v -= 1f;
                if (Key(KeyCode.W, KeyCode.UpArrow)) v += 1f;
                if (v == 0f) v = Axis("Vertical");
                return Mathf.Abs(v) < DeadZone ? 0f : Mathf.Sign(v);
            }
        }

        public static bool JumpPressed => KeyDown(KeyCode.Space, KeyCode.K, KeyCode.Z, KeyCode.JoystickButton0);
        public static bool JumpHeld => Key(KeyCode.Space, KeyCode.K, KeyCode.Z, KeyCode.JoystickButton0);
        public static bool RunHeld => Key(KeyCode.LeftShift, KeyCode.RightShift, KeyCode.J, KeyCode.X, KeyCode.JoystickButton2);
        public static bool PausePressed => KeyDown(KeyCode.Escape, KeyCode.P, KeyCode.JoystickButton7);
        public static bool CancelPressed => KeyDown(KeyCode.Escape, KeyCode.Backspace, KeyCode.JoystickButton1);
        public static bool MutePressed => KeyDown(KeyCode.M);
        public static bool ScreenshotPressed => KeyDown(KeyCode.F12);
#else
        static Keyboard Kb => Keyboard.current;
        static Gamepad Pad => Gamepad.current;

        public static float Horizontal
        {
            get
            {
                float h = 0f;
                if (Kb != null)
                {
                    if (Kb.aKey.isPressed || Kb.leftArrowKey.isPressed) h -= 1f;
                    if (Kb.dKey.isPressed || Kb.rightArrowKey.isPressed) h += 1f;
                }
                if (h == 0f && Pad != null) h = Pad.leftStick.x.ReadValue() + Pad.dpad.x.ReadValue();
                return Mathf.Abs(h) < DeadZone ? 0f : Mathf.Sign(h);
            }
        }

        public static float Vertical
        {
            get
            {
                float v = 0f;
                if (Kb != null)
                {
                    if (Kb.sKey.isPressed || Kb.downArrowKey.isPressed) v -= 1f;
                    if (Kb.wKey.isPressed || Kb.upArrowKey.isPressed) v += 1f;
                }
                if (v == 0f && Pad != null) v = Pad.leftStick.y.ReadValue() + Pad.dpad.y.ReadValue();
                return Mathf.Abs(v) < DeadZone ? 0f : Mathf.Sign(v);
            }
        }

        public static bool JumpPressed =>
            (Kb != null && (Kb.spaceKey.wasPressedThisFrame || Kb.kKey.wasPressedThisFrame || Kb.zKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        public static bool JumpHeld =>
            (Kb != null && (Kb.spaceKey.isPressed || Kb.kKey.isPressed || Kb.zKey.isPressed)) ||
            (Pad != null && Pad.buttonSouth.isPressed);

        public static bool RunHeld =>
            (Kb != null && (Kb.leftShiftKey.isPressed || Kb.rightShiftKey.isPressed || Kb.jKey.isPressed || Kb.xKey.isPressed)) ||
            (Pad != null && (Pad.buttonWest.isPressed || Pad.rightTrigger.isPressed));

        public static bool PausePressed =>
            (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.pKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.startButton.wasPressedThisFrame);

        public static bool CancelPressed =>
            (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame);

        public static bool MutePressed => Kb != null && Kb.mKey.wasPressedThisFrame;
        public static bool ScreenshotPressed => Kb != null && Kb.f12Key.wasPressedThisFrame;
#endif
    }
}

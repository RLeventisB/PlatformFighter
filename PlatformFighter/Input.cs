#if ANDROID
using Android.Views;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PlatformFighter
{
    public static class Input
    {
        public static readonly char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray(), upperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), lowerLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray(), numbers = "0123456789".ToCharArray();
        public static MouseState mouseState;
        public static ExposedList<GamepadInfo> Gamepads;
        private static Vector2 mousePosition;
        public static bool ShiftPressed, ControlPressed, AltPressed, WindowsPressed,
            MouseLeft, MouseRight, MouseMiddle, FirstMouseLeft, FirstMouseRight, FirstMouseMiddle;
        public static float MouseScroll, SmoothMouseScroll;
        public static Vector2 MouseRawPosition = Vector2.Zero;
        public static Vector2 MouseOldPosition;
        private static bool lastPressedKeyFrameHolder;
        static Input()
        {
            Keyboard.OnKeyDown += delegate(Keys key)
            {
                LastPressedKey = key;
                HasLastPressedKeyBeenThisFrame = true;
                lastPressedKeyFrameHolder = true;
            };
        }
        public static Keys LastPressedKey { get; private set; }
        public static bool HasLastPressedKeyBeenThisFrame { get; private set; }
        public static Vector2 MousePosition
        {
            get => mousePosition;
            set => Mouse.SetPosition((int)value.X, (int)value.Y);
        }
        public static IEnumerable<KeyData> GetKeyboardEnumerable() => new KeyboardEnumerable();
        public static void Update()
        {
            if (Main.instance.IsActive)
            {
                MouseState newState = Mouse.GetState();
                MouseScroll = (mouseState.ScrollWheelValue - newState.ScrollWheelValue) / 120f;

                mouseState = newState;
                for (byte i = 0; i < Gamepads.Count; i++)
                {
                    ref GamepadInfo info = ref Gamepads.items[i];
                    info.State = GamePad.GetState(info.Index);
                }
            }
            else
            {
                for (byte i = 0; i < Gamepads.Count; i++)
                {
                    Gamepads.items[i].State = new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, Array.Empty<Buttons>());
                }
                mouseState = new MouseState();
                MouseScroll = 0;
            }
            MouseRawPosition.X = mouseState.X;
            MouseRawPosition.Y = mouseState.Y;
            MouseOldPosition = mousePosition;
            mousePosition = Vector2.Transform(MouseRawPosition, Renderer.InvWindowMatrix);
            SmoothMouseScroll = MathHelper.Lerp(SmoothMouseScroll, MouseScroll, 0.1f * Renderer.TimeDelta);

            bool mousePressed = mouseState.LeftButton == ButtonState.Pressed;
            FirstMouseLeft = mousePressed && !MouseLeft;
            MouseLeft = mousePressed;

            mousePressed = mouseState.RightButton == ButtonState.Pressed;
            FirstMouseRight = mousePressed && !MouseRight;
            MouseRight = mousePressed;

            mousePressed = mouseState.MiddleButton == ButtonState.Pressed;
            FirstMouseMiddle = mousePressed && !MouseMiddle;
            MouseMiddle = mousePressed;

            ShiftPressed = Keyboard.IsKeyPressed(Keys.LeftShift) || Keyboard.IsKeyPressed(Keys.RightShift);
            ControlPressed = Keyboard.IsKeyPressed(Keys.LeftControl) || Keyboard.IsKeyPressed(Keys.RightControl);
            AltPressed = Keyboard.IsKeyPressed(Keys.LeftAlt) || Keyboard.IsKeyPressed(Keys.RightAlt);
            WindowsPressed = Keyboard.IsKeyPressed(Keys.LeftWindows) || Keyboard.IsKeyPressed(Keys.RightWindows);
            if (!lastPressedKeyFrameHolder)
                HasLastPressedKeyBeenThisFrame = false;
            lastPressedKeyFrameHolder = false;
        }
        public static void BuildGamepadDictionary()
        {
            Gamepads.Clear();
#if DESKTOPGL
            for (int i = 0; i < GamePad.GamepadCount; i++)
            {
                PlayerIndex index = (PlayerIndex)i;
                if (Gamepads.Any(v => v.Index == index)) continue;

                GamepadInfo info = new GamepadInfo(index);

                info.State = GamePad.GetState(info.Index);
                info.Capabilities = GamePad.GetCapabilities(info.Index);
                info.DevicePointer = GamePad.GetGamepadAt(i).Device;
                info.ControllerType = Sdl.GameController.GameControllerGetType(info.DevicePointer);
                info.Name = Sdl.GameController.GetName(info.DevicePointer);
                Gamepads.Add(info);
            }
#endif
        }
    }
    public class GamepadInfo
    {
        public GamepadInfo(PlayerIndex index)
        {
            Index = index;
            State = default;
            Capabilities = default;
            DevicePointer = nint.Zero;
            ControllerType = default;
            Name = null;
        }
        public PlayerIndex Index { get; internal set; }
        public GamePadState State { get; internal set; }
        public GamePadCapabilities Capabilities { get; internal set; }
        public 
#if DESKTOPGL
            Sdl.GameController.GameControllerType
#elif ANDROID
            InputDevice
#endif
            ControllerType { get; internal set; }
        public IntPtr DevicePointer { get; internal set; }
        public string Name { get; internal set; }
    }
    public class KeyboardEnumerable : IEnumerable<KeyData>
    {
        public IEnumerator<KeyData> GetEnumerator() => new KeyboardEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public unsafe class KeyboardEnumerator : IEnumerator<KeyData>
        {
            private KeyData _current;
            public byte index;
            public bool skipReleased = false;
            public KeyboardEnumerator()
            {
                Reset();
            }
            public KeyData Current => _current;
            object IEnumerator.Current => _current;
            public void Dispose()
            {

            }
            public bool MoveNext()
            {
                if (index == 255)
                {
                    return false;
                }
                do
                {
                    index++;
                } while ((_current = Keyboard.KeysPointer[index]).TotalRepeatCount != 0 && skipReleased);
                return true;
            }
            public void Reset()
            {
                index = 0;
                _current = Keyboard.KeysPointer[0];
                if (skipReleased && _current.TotalRepeatCount != 0)
                {
                    MoveNext();
                }
            }
        }
    }
}
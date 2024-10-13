using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using PlatformFighter.Rendering;

using System;
using System.Collections;
using System.Collections.Generic;
#if ANDROID
using Android.Views;
#endif

namespace PlatformFighter
{
	public static class Input
	{
		public static readonly char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray(), upperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), lowerLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray(), numbers = "0123456789".ToCharArray();
		public static MouseState mouseState;
		public static GamepadInfo[] Gamepads = new GamepadInfo[GamePad.MaximumGamePadCount];
		private static Vector2 mousePosition;
		public static bool ShiftPressed, ControlPressed, AltPressed, WindowsPressed,
			MouseLeft, MouseRight, MouseMiddle, FirstMouseLeft, FirstMouseRight, FirstMouseMiddle;
		public static float MouseScroll, SmoothMouseScroll;
		public static Vector2 MouseRawPosition = Vector2.Zero;
		public static Vector2 MouseOldPosition;
		private static bool lastPressedKeyFrameHolder;
		private static int lastGamepadCount;

		static Input()
		{
			Keyboard.OnKeyDown += delegate(Keys key)
			{
				LastPressedKey = key;
				HasLastPressedKeyBeenThisFrame = true;
				lastPressedKeyFrameHolder = true;
			};

			for (byte i = 0; i < Gamepads.Length; i++)
			{
				Gamepads[i] = new GamepadInfo(i);
			}
		}

		public static KeyboardState KeyboardState { get; private set; }
		public static KeyboardState PreviousKeyboardState { get; private set; }
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
			PreviousKeyboardState = KeyboardState;

			if (Main.instance.IsActive)
			{
				MouseState newState = Mouse.GetState();
				MouseScroll = (mouseState.ScrollWheelValue - newState.ScrollWheelValue) / 120f;
				KeyboardState = Keyboard.GetState();

				mouseState = newState;
				UpdateGamepadLogic();
			}
			else
			{
				for (byte i = 0; i < Gamepads.Length; i++)
				{
					GamePadState state = Gamepads[i].State;
					state.GetType().GetProperty("Buttons").SetValue(state, new GamePadButtons());
					state.GetType().GetProperty("DPad").SetValue(state, new GamePadDPad());
					state.GetType().GetProperty("ThumbSticks").SetValue(state, new GamePadThumbSticks());
					state.GetType().GetProperty("Triggers").SetValue(state, new GamePadTriggers());
					Gamepads[i].State = state;
				}

				KeyboardState = new KeyboardState();
				mouseState = new MouseState();
				MouseScroll = 0;
			}

			MouseRawPosition.X = mouseState.X;
			MouseRawPosition.Y = mouseState.Y;
			MouseOldPosition = mousePosition;
			mousePosition = Vector2.Transform(MouseRawPosition, Renderer.InvWindowMatrix);
			SmoothMouseScroll = MathHelper.Lerp(SmoothMouseScroll, MouseScroll, 0.1f);

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

		public static void UpdateGamepadLogic()
		{
			for (byte i = 0; i < Gamepads.Length; i++)
			{
				ref GamepadInfo info = ref Gamepads[i];
				bool oldConnected = info.State.IsConnected;
				info.State = GamePad.GetState(info.Index);
				bool newConnected = info.State.IsConnected;

				if (oldConnected == newConnected)
					continue;

				// detected change!!!
				if (newConnected)
				{
					info.Load();
				}
				else
				{
					info.Unload();
				}
			}
		}
	}
	public class GamepadInfo
	{
		public GamepadInfo(int index)
		{
			Index = index;
			State = GamePadState.Default;
			PreviousState = GamePadState.Default;
			Capabilities = default;
			DevicePointer = nint.Zero;
			ControllerType = default;
			Name = string.Empty;
		}

		public int Index { get; init; }
		public bool IsConnected => State.IsConnected;
		public GamePadState State { get; internal set; }
		public GamePadState PreviousState { get; internal set; }
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

		public void Load()
		{
			PreviousState = State;
			State = GamePad.GetState(Index);
			Capabilities = GamePad.GetCapabilities(Index);
#if DESKTOPGL
			DevicePointer = GamePad.GetGamepadAt(Index).Device;
			ControllerType = Sdl.GameController.GameControllerGetType(DevicePointer);
			Name = Sdl.GameController.GetName(DevicePointer);
#elif ANDROID
			DevicePointer = GamePad.GetGamepadAt(Index)._deviceId;
#endif

			Logger.LogMessage($"Detected gamepad connected, pointer: {DevicePointer}, name: {Name}, index: {Index}");
		}

		public void Unload()
		{
			Logger.LogMessage($"Detected gamepad disconnected, old pointer: {DevicePointer}, old name: {Name}, index: {Index}");

			PreviousState = State;
			State = GamePadState.Default;
			Capabilities = default;
			DevicePointer = nint.Zero;
			ControllerType = default;
			Name = string.Empty;
		}
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
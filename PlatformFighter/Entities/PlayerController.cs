using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

namespace PlatformFighter.Entities
{
	public static class PlayerController
	{
		public static Dictionary<ushort, IPlayerControlDataReceiver> registeredControllers = new Dictionary<ushort, IPlayerControlDataReceiver>();
		public static ushort nextId;

		public static bool UnregisterPlayerController(ushort id) => registeredControllers.Remove(id);

		public static ushort RegisterPlayerController(IPlayerControlDataReceiver dataReceiver)
		{
			ushort id = nextId;

			registeredControllers.Add(nextId, dataReceiver);

			nextId++;

			return id;
		}
	}
	public struct GamepadDataReceiver : IPlayerControlDataReceiver
	{
		public readonly int gamepadIndex;

		public GamepadDataReceiver(int index)
		{
			gamepadIndex = index;
		}

		public GamepadInfo GetGamepadInfo => Input.Gamepads[gamepadIndex];
		public bool IsConnected => GetGamepadInfo.IsConnected;
		public ControlState Left => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadLeft | Buttons.LeftThumbstickLeft));
		public ControlState Right => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadRight | Buttons.LeftThumbstickRight));
		public ControlState Up => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadUp | Buttons.LeftThumbstickUp));
		public ControlState Down => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadDown | Buttons.LeftThumbstickDown));
		public ControlState Jump => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.A));
		public ControlState MeleeAttack => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.X));
		public ControlState ShotAttack => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.Y));
		public ControlState SpecialToggle => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.LeftTrigger));
		public ControlState Dash => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightTrigger));
		public ControlState Shield => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.LeftShoulder));
		public ControlState CycleMeterUp => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightThumbstickRight));
		public ControlState CycleMeterDown => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightThumbstickLeft));
		public ControlState ActivateMeter => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightStick));
		public ControlState Pause => IPlayerControlDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.Back));
	}
	public struct KeyboardDataReceiver : IPlayerControlDataReceiver
	{
		public bool IsConnected => true;
		public ControlState Left => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Left));
		public ControlState Right => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Right));
		public ControlState Up => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Up));
		public ControlState Down => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Down));
		public ControlState Jump => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Space));
		public ControlState MeleeAttack => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Z));
		public ControlState ShotAttack => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.X));
		public ControlState SpecialToggle => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.C));
		public ControlState Dash => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.LeftControl) || v.IsKeyDown(Keys.RightControl));
		public ControlState Shield => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.LeftShift));
		public ControlState CycleMeterUp => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.S));
		public ControlState CycleMeterDown => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.A));
		public ControlState ActivateMeter => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.D));
		public ControlState Pause => IPlayerControlDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Escape));
	}
	public interface IPlayerControlDataReceiver
	{
		public bool IsConnected { get; }
		public ControlState Left { get; }
		public ControlState Right { get; }
		public ControlState Up { get; }
		public ControlState Down { get; }
		public ControlState Jump { get; }
		public ControlState MeleeAttack { get; }
		public ControlState ShotAttack { get; }
		public ControlState SpecialToggle { get; }
		public ControlState Dash { get; }
		public ControlState Shield { get; }
		public ControlState CycleMeterUp { get; }
		public ControlState CycleMeterDown { get; }
		public ControlState ActivateMeter { get; }
		public ControlState Pause { get; }
		
		public ControlState Select => MeleeAttack;
		public ControlState Cancel => ShotAttack;
		public ControlState Extra => SpecialToggle;

		public static ControlState GetState<T>(T oldState, T newState, Func<T, bool> checker)
		{
			bool oldCheck = checker(oldState);
			bool newCheck = checker(newState);

			if (oldCheck && newCheck)
				return ControlState.Pressed;
			if (!oldCheck && newCheck)
				return ControlState.JustPressed;
			if (!oldCheck && !newCheck)
				return ControlState.Released;
			return ControlState.Releasing;
		}

		public AttackDirection GetAppropiateAttackDirection()
		{
			// priority is Neutral > Down > Side
			return Up ? AttackDirection.Neutral : Down ? AttackDirection.Down : AttackDirection.Side;
		}
	}
	public class ControlState
	{
		public static readonly ControlState Released = 0, JustPressed = 1, Pressed = 2, Releasing = 3;
		public readonly byte _state;

		private ControlState(byte value) // walmart java enum
		{
			_state = value;
		}

		public static implicit operator ControlState(byte value) => new ControlState(value);

		public static implicit operator bool(ControlState value) => value == JustPressed || value == Pressed;
	}
	public enum AttackDirection
	{
		Neutral, Side, Down
	}
}
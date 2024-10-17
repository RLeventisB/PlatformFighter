using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

namespace PlatformFighter.Entities
{
	public static class PlayerController
	{
		public static Dictionary<ushort, IPlayerDataReceiver> registeredControllers = new Dictionary<ushort, IPlayerDataReceiver>();
		public static ushort nextId;

		public static bool UnregisterController(ushort id) => registeredControllers.Remove(id);

		public static ushort RegisterController(IPlayerDataReceiver dataReceiver)
		{
			ushort id = nextId;

			registeredControllers.Add(nextId, dataReceiver);

			nextId++;

			return id;
		}
	}
	public struct GamepadDataReceiver : IPlayerDataReceiver
	{
		public readonly int gamepadIndex;

		public GamepadDataReceiver(int index)
		{
			gamepadIndex = index;
		}

		public GamepadInfo GetGamepadInfo => Input.Gamepads[gamepadIndex];
		public bool IsConnected => GetGamepadInfo.IsConnected;
		public ControlState Left => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadLeft | Buttons.LeftThumbstickLeft));
		public ControlState Right => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadRight | Buttons.LeftThumbstickRight));
		public ControlState Up => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadUp | Buttons.LeftThumbstickUp));
		public ControlState Down => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.DPadDown | Buttons.LeftThumbstickDown));
		public ControlState Jump => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.A));
		public ControlState MeleeAttack => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.X));
		public ControlState ShotAttack => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.Y));
		public ControlState SpecialToggle => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.LeftTrigger));
		public ControlState Dash => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightTrigger));
		public ControlState Shield => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.LeftShoulder));
		public ControlState CycleMeterUp => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightThumbstickRight));
		public ControlState CycleMeterDown => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightThumbstickLeft));
		public ControlState ActivateMeter => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.RightStick));
		public ControlState Pause => IPlayerDataReceiver.GetState(GetGamepadInfo.PreviousState, GetGamepadInfo.State, v => v.IsButtonDown(Buttons.Back));
	}
	public struct KeyboardDataReceiver : IPlayerDataReceiver
	{
		public bool IsConnected => true;
		public ControlState Left => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Left));
		public ControlState Right => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Right));
		public ControlState Up => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Up));
		public ControlState Down => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Down));
		public ControlState Jump => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Space));
		public ControlState MeleeAttack => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Z));
		public ControlState ShotAttack => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.X));
		public ControlState SpecialToggle => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.C));
		public ControlState Dash => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.LeftControl) || v.IsKeyDown(Keys.RightControl));
		public ControlState Shield => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.LeftShift));
		public ControlState CycleMeterUp => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.S));
		public ControlState CycleMeterDown => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.A));
		public ControlState ActivateMeter => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.D));
		public ControlState Pause => IPlayerDataReceiver.GetState(Input.PreviousKeyboardState, Input.KeyboardState, v =>v.IsKeyDown(Keys.Escape));
	}
	public interface IPlayerDataReceiver
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
	public struct InactiveDataReceiver : IPlayerDataReceiver
	{
		public bool IsConnected => true;
		public ControlState Left => ControlState.Released;
		public ControlState Right => ControlState.Released;
		public ControlState Up => ControlState.Released;
		public ControlState Down => ControlState.Released;
		public ControlState Jump => ControlState.Released;
		public ControlState MeleeAttack => ControlState.Released;
		public ControlState ShotAttack => ControlState.Released;
		public ControlState SpecialToggle => ControlState.Released;
		public ControlState Dash => ControlState.Released;
		public ControlState Shield => ControlState.Released;
		public ControlState CycleMeterUp => ControlState.Released;
		public ControlState CycleMeterDown => ControlState.Released;
		public ControlState ActivateMeter => ControlState.Released;
		public ControlState Pause => ControlState.Released;
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
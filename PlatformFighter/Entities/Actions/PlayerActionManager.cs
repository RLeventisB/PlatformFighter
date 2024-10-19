using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Characters;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;

using System;

namespace PlatformFighter.Entities.Actions
{
	public class PlayerActionManager
	{
		public int AnimationFrame;
		private bool ChangedActionThisFrame;
		public ActionBase<Player> CurrentAction;
		public bool Dashing;
		public FacingDirection FacingDirection = FacingDirection.Right;
		public Vector2 Impulse;
		public QueuedActionList QueuedAction = new QueuedActionList();
		public int RecoveryFrames;
		public int ShieldBreakStun;
		public AnalogValue<sbyte>
			Shielding = new AnalogValue<sbyte>(0, 10, 1);
		public ushort WalkTime, TurningTime, AirTime, DashTime;

		public PlayerActionManager(Player player)
		{
			Player = player;
		}

		public Player Player { get; init; }

		public void SetDefaults()
		{
			Impulse = Vector2.Zero;
		}

		public void Update()
		{
			IPlayerDataReceiver controller = Player.GetController();
			Impulse = Vector2.Zero;
			if (controller.Up)
				Impulse.Y -= 1;

			if (controller.Down)
				Impulse.Y += 1;

			if (controller.Left)
				Impulse.X -= 1;

			if (controller.Right)
				Impulse.X += 1;

			QueuedAction.AddActions(controller);

			if (RecoveryFrames > 0)
			{
				RecoveryFrames--;

				if (CurrentAction is null)
					return;

				CurrentAction.UpdateOnRecovery();
				if(!CurrentAction.RunUpdateOnRecovery)
					return;
			}

			QueuedAction.TickActions();

			CurrentAction ??= Player.CharacterData.Definition.ResolveIdleAction(Player, Player.Grounded);

			do
			{
				ChangedActionThisFrame = false;

				CurrentAction.ProcessQueue(QueuedAction);
			} while (ChangedActionThisFrame);

			CurrentAction.Update();
		}

		public static void TickActionsDefault(PlayerActionManager actionManager, IPlayerDataReceiver controller, bool ignoreRecoveryFrames = false)
		{
			if (actionManager.RecoveryFrames > 0 && !ignoreRecoveryFrames)
				return;
			
			Type currentActionType = actionManager.CurrentAction?.GetType();

			Direction collidedDirections = Collision.GetCollidingDirection(actionManager.Player.MovableObject, actionManager.Impulse * 1.5f);

			bool left = collidedDirections.HasFlag(Direction.Left);

			if (collidedDirections.HasFlag(Direction.Left) || collidedDirections.HasFlag(Direction.Right))
			{
				actionManager.SetAction(new ElmoWallAction(actionManager.Player, left ? FacingDirection.Right : FacingDirection.Left));
			}
			
			if (currentActionType != actionManager.CurrentAction?.GetType())
				return;

			while (actionManager.QueuedAction.HasActions)
			{
				QueuedAction action = actionManager.QueuedAction.DequeueAction();

				ProcessActionDefault(actionManager, controller, action);
			}

			if (currentActionType != actionManager.CurrentAction?.GetType())
				return;

			if (controller.Left && actionManager.Player.Grounded)
			{
				actionManager.DoWalkAction(FacingDirection.Left);
			}
			else if (controller.Right && actionManager.Player.Grounded)
			{
				actionManager.DoWalkAction(FacingDirection.Right);
			}
		}

		public static void ProcessActionDefault(PlayerActionManager actionManager, IPlayerDataReceiver controller, QueuedAction action)
		{
			switch (action.ActionType)
			{
				case ActionType.Jump:
					actionManager.SetAction(new ElmoJumpAction(actionManager.Player, null));

					break;
				case ActionType.DownStart when actionManager.Player.Grounded:
					actionManager.SetAction(new ElmoCrouchAction(actionManager.Player));

					break;
				case ActionType.Dash when actionManager.Impulse != Vector2.Zero && (actionManager.Impulse.Y <= 0 || !actionManager.Player.Grounded):
					actionManager.UpdateFacingToInputs();
					actionManager.SetAction(new ElmoDashStartAction(actionManager.Player, actionManager.FacingDirection));

					break;
			}
		}

		public void DoWalkAction(FacingDirection facingDirection)
		{
			SetAction(new ElmoWalkAction(Player, facingDirection));
		}

		public bool SetAction(ActionBase<Player> action)
		{
			if (CurrentAction is not null && !CurrentAction.CanChangeTo(action))
				return false;

			CurrentAction?.OnEnd();
			CurrentAction = action;
			CurrentAction?.OnStart();
			ChangedActionThisFrame = true;

			return true;
		}

		public bool UpdateFacingToInputs()
		{
			bool inputChange = Utils.GetFacingDirectionMult(FacingDirection) != Impulse.X && Impulse.X != 0;
			if (inputChange)
				FacingDirection = Utils.GetFacingDirectionFrom(Impulse.X);

			return inputChange;
		}

		public void Draw()
		{
			CurrentAction ??= Player.CharacterData.Definition.ResolveIdleAction(Player, Player.Grounded);
			CurrentAction.Draw();
		}

		public void Reset()
		{
			CurrentAction = Player.CharacterData.Definition.ResolveIdleAction(Player, Player.Grounded);
		}

		public bool CurrentActionHasFlag(ActionTags tags) => CurrentAction is not null && CurrentAction.Tags.HasFlag(tags);
	}
	public struct QueuedAction : IComparable<QueuedAction>
	{
		public readonly ActionType ActionType;
		public ushort BufferTime;
		public const ushort DefaultBufferTime = 14;

		public QueuedAction(ActionType actionType)
		{
			ActionType = actionType;
			BufferTime = DefaultBufferTime;
		}

		public bool TickBufferAndIsExpired() => BufferTime-- == 0;

		public int CompareTo(QueuedAction other) => ActionType.CompareTo(other.ActionType);
	}
	public struct QueuedActionList
	{
		public ExposedList<QueuedAction> ActionQueue = new ExposedList<QueuedAction>();

		public QueuedActionList()
		{
		}

		public bool HasActions => ActionQueue.Count != 0;

		public bool DequeueAction(ActionType type, out QueuedAction action)
		{
			int index = ActionQueue.FindIndex(v => v.ActionType == type);

			if (index != -1)
			{
				action = DequeueAction(index);

				return true;
			}

			action = default;

			return false;
		}

		public bool ConsumeAction(ActionType type)
		{
			int index = ActionQueue.FindIndex(v => v.ActionType == type);

			if (index == -1)
				return false;

			ActionQueue.RemoveAt(index);

			return true;
		}

		public QueuedAction DequeueAction(int index = 0)
		{
			QueuedAction action = ActionQueue[index];
			ActionQueue.RemoveAt(index);

			return action;
		}

		public void TickActions()
		{
			// tick all actions that weren't processed
			for (int i = 0; i < ActionQueue.Count; i++)
			{
				ref QueuedAction action = ref ActionQueue.items[i];

				if (!action.TickBufferAndIsExpired())
					continue;

				ActionQueue.RemoveAt(i);
				i--;
			}
		}

		public void AddActions(IPlayerDataReceiver controller)
		{
			if (controller.Left == ControlState.JustPressed)
			{
				AddAction(ActionType.LeftStart);
			}

			if (controller.Left == ControlState.Releasing)
			{
				AddAction(ActionType.LeftEnd);
			}

			if (controller.Right == ControlState.JustPressed)
			{
				AddAction(ActionType.RightStart);
			}

			if (controller.Right == ControlState.Releasing)
			{
				AddAction(ActionType.RightEnd);
			}

			if (controller.Up == ControlState.JustPressed)
			{
				AddAction(ActionType.UpStart);
			}

			if (controller.Up == ControlState.Releasing)
			{
				AddAction(ActionType.UpEnd);
			}

			if (controller.Down == ControlState.JustPressed)
			{
				AddAction(ActionType.DownStart);
			}

			if (controller.Down == ControlState.Releasing)
			{
				AddAction(ActionType.DownEnd);
			}

			if (controller.ActivateMeter == ControlState.JustPressed)
			{
				AddAction(ActionType.Meter);
			}

			if (controller.Shield == ControlState.JustPressed)
			{
				AddAction(ActionType.BlockStart);
			}

			if (controller.Shield == ControlState.Releasing)
			{
				AddAction(ActionType.BlockEnd);
			}

			if (controller.Dash == ControlState.JustPressed)
			{
				AddAction(ActionType.Dash);
			}

			if (controller.Jump == ControlState.JustPressed)
			{
				AddAction(ActionType.Jump);
			}

			if (controller.SpecialToggle == ControlState.JustPressed)
			{
				AddAction(ActionType.SpecialOn);
			}

			if (controller.SpecialToggle == ControlState.Releasing)
			{
				AddAction(ActionType.SpecialOff);
			}

			if (controller.MeleeAttack == ControlState.JustPressed)
			{
				AddAction(ActionType.AttackMelee);
			}

			if (controller.ShotAttack == ControlState.JustPressed)
			{
				AddAction(ActionType.AttackShot);
			}
		}

		public void AddAction(ActionType actionType)
		{
			int index = ActionQueue.FindIndex(v => v.ActionType == actionType);

			if (index == -1)
			{
				ActionQueue.Add(new QueuedAction(actionType));
			}
			else
			{
				ActionQueue.items[index].BufferTime = QueuedAction.DefaultBufferTime;
			}

			ActionQueue.Sort();
		}
	}
	public enum FacingDirection : byte
	{
		Left, Right
	}
	public enum ActionType
	{
		Dash,
		LeftStart,
		RightStart,
		UpStart,
		DownStart,
		LeftEnd,
		RightEnd,
		UpEnd,
		DownEnd,
		Jump,
		BlockStart,
		BlockEnd,
		AttackMelee,
		AttackShot,
		SpecialOn,
		SpecialOff,
		Meter
	}
	public static class ActionTypeUtils
	{
		public static int GetPriority(ActionType action)
		{
			switch (action)
			{
				case ActionType.Dash:
					return 95;
				case ActionType.LeftStart:
				case ActionType.RightStart:
				case ActionType.UpStart:
				case ActionType.DownStart:
				case ActionType.LeftEnd:
				case ActionType.RightEnd:
				case ActionType.UpEnd:
				case ActionType.DownEnd:
					return 100;
				case ActionType.Jump:
					return 90;
				case ActionType.BlockEnd:
				case ActionType.BlockStart:
					return 110;
				case ActionType.AttackMelee:
				case ActionType.AttackShot:
					return 89;
				case ActionType.SpecialOn:
				case ActionType.SpecialOff:
					return 120;
				case ActionType.Meter:
					return 70;
			}

			return 0;
		}
	}
}
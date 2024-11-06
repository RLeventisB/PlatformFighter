using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Characters;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;

using System;

namespace PlatformFighter.Entities.Actions
{
	public class PlayerActionManager
	{
		public ushort ActionId; // this increases for every new action
		private bool ChangedActionThisFrame;
		public ActionBase<Player> CurrentAction;
		public FacingDirection FacingDirection = FacingDirection.Right;
		public bool HasThisActionCollided, HasThisActionHit;
		public Vector2 Impulse;
		public int JumpCount;
		public QueuedActionList QueuedAction = new QueuedActionList();
		public int RecoveryFrames, HitStun;
		public int ShieldBreakStun;

		public PlayerActionManager(Player player)
		{
			Player = player;
		}

		public Player Player { get; init; }

		public bool IsOnRecovery => RecoveryFrames > 0;
		public bool IsOnHitstun => HitStun > 0;

		public void SetDefaults()
		{
			Impulse = Vector2.Zero;
			QueuedAction.Clear();
			HasThisActionCollided = false;
			HasThisActionHit = false;
			ActionId = 0;
			ChangedActionThisFrame = false;
			RecoveryFrames = 0;
			JumpCount = 0;
		}

		public void Update()
		{
			IPlayerDataReceiver controller = Player.GetController();
			Impulse = Vector2.Zero;

			if (IsOnRecovery)
			{
				QueuedAction.Clear();
				RecoveryFrames--;
			}
			else
			{
				Impulse = GetImpulseFromController(controller);

				QueuedAction.AddActions(controller);
			}

			if (HitStun > 0)
			{
				HitStun--;
			}

			QueuedAction.TickActions();

			CurrentAction ??= Player.CharacterData.Definition.ResolveIdleAction(Player, Player.Grounded);

			if (CurrentAction is null)
				return;

			int iterations = 0;

			do
			{
				ChangedActionThisFrame = false;
				iterations++;

				CurrentAction.ProcessQueue(QueuedAction);

				if (ChangedActionThisFrame)
					continue;

				CurrentAction.Update();
			} while (ChangedActionThisFrame && iterations <= 10);

			CurrentAction.AddHitboxes();
		}

		public Vector2 GetImpulseFromController(IPlayerDataReceiver controller)
		{
			Vector2 impulse = Vector2.Zero;
			if (controller.Up)
				impulse.Y -= 1;

			if (controller.Down)
				impulse.Y += 1;

			if (controller.Left)
				impulse.X -= 1;

			if (controller.Right)
				impulse.X += 1;

			return impulse;
		}

		public void RestoreJumpCount()
		{
			JumpCount = Player.CharacterData.Definition.MaxJumpCount;
		}

		public void DoDefaultLogic(IPlayerDataReceiver controller, bool doWallLogic = true, bool doAttackLogic = true, bool doJumpLogic = true, bool doWalkingLogic = true, bool doOnlyDash = false)
		{
			if (doWallLogic)
			{
				DoWallLogic();

				if (ChangedActionThisFrame)
					return;
			}

			if (doAttackLogic)
			{
				DoAttackLogic();

				if (ChangedActionThisFrame)
					return;
			}

			if (doJumpLogic)
			{
				DoJumpLogic();

				if (ChangedActionThisFrame)
					return;
			}

			if (doWalkingLogic)
				DoWalkingLogic(controller, doOnlyDash);
		}

		public void DoWallLogic()
		{
			Direction collidedDirections = Collision.GetCollidingDirection(Player.MovableObject, Player.MovableObject.Velocity);

			bool left = collidedDirections.HasFlag(Direction.Left);

			if (collidedDirections.HasFlag(Direction.Left) || collidedDirections.HasFlag(Direction.Right))
			{
				SetAction(new ElmoWallAction(Player, left ? FacingDirection.Right : FacingDirection.Left));
			}
		}

		public void DoWalkingLogic(IPlayerDataReceiver controller, bool onlyDash = false)
		{
			if (IsOnRecovery || DoDashLogic() || onlyDash)
				return;

			if (controller.Left)
			{
				DoWalkAction(FacingDirection.Left);
			}
			else if (controller.Right)
			{
				DoWalkAction(FacingDirection.Right);
			}
		}

		public bool DoDashLogic()
		{
			if (Impulse != Vector2.Zero && QueuedAction.DequeueAction(InputType.Dash))
			{
				UpdateFacingToInputs();

				if (Impulse.Y > 0 && Player.Grounded)
				{
					return true;
				}

				SetAction(new ElmoDashStartAction(Player, FacingDirection));

				return true;
			}

			return false;
		}

		public void DoJumpLogic()
		{
			if (QueuedAction.DequeueAction(InputType.Jump))
				SetAction(new ElmoJumpAction(Player, null));
		}

		public void DoAttackLogic()
		{
			if (QueuedAction.DequeueAction(InputType.AttackMelee))
				DoAttackAction(false);

			if (QueuedAction.DequeueAction(InputType.AttackShot))
				DoAttackAction(true);
		}

		public void DoAttackAction(bool isShot)
		{
			UpdateFacingToInputs();
			SetAction(Player.CharacterData.Definition.ResolveAttackAction(Player, GetAttackDirection(), isShot, Player.GetController().SpecialToggle));
		}

		public AttackDirection GetAttackDirection()
		{
			IPlayerDataReceiver controller = Player.GetController();

			if (controller.Up)
			{
				return AttackDirection.Neutral;
			}

			if (controller.Down)
			{
				return AttackDirection.Down;
			}

			if (controller.Left || controller.Right)
			{
				return AttackDirection.Side;
			}

			return AttackDirection.Neutral;
		}

		public void DoWalkAction(FacingDirection facingDirection)
		{
			FacingDirection = facingDirection;
			SetAction(new ElmoWalkAction(Player, facingDirection));
		}

		public bool SetAction(ActionBase<Player> action)
		{
			if (CurrentAction is not null && CurrentAction.CanChangeTo(action) && action is not null && action.CanBeSetAsActive(Player))
			{
				CurrentAction?.OnEnd();
				CurrentAction = action;
				CurrentAction?.OnStart();
				ChangedActionThisFrame = true;
				HasThisActionCollided = false;
				HasThisActionHit = false;
				ActionId++;

				return false;
			}

			return false;
		}

		/// <summary>
		/// Update the current <see cref="PlayerActionManager"/>'s <see cref="FacingDirection"/> to match the used Impulse
		/// Vector (this is dependant of <paramref name="ignoreRecovery"/>).<br/>
		/// If the used Impulse Vector's X value is not zero, <see cref="FacingDirection"/> will be set to the result of
		/// <see cref="Utils.GetFacingDirectionFrom"/>, using the used Impulse Vector's X value as the parameter.
		/// </summary>
		/// <param name="ignoreRecovery">
		/// Whether to use the <see cref="Impulse"/> value, or recalculate the impulse from the
		/// controller.<br/>This gives the effect of ignoring <see cref="RecoveryFrames"/> setting <see cref="Impulse"/> as
		/// <see cref="Vector2.Zero"/>
		/// </param>
		/// <returns>True if there was a direction change</returns>
		public bool UpdateFacingToInputs(bool ignoreRecovery = false)
		{
			IPlayerDataReceiver controller = Player.GetController();
			Vector2 impulse = Impulse;

			if (ignoreRecovery)
			{
				impulse = GetImpulseFromController(controller);
			}

			bool inputChange = Utils.GetFacingDirectionMult(FacingDirection) != impulse.X && impulse.X != 0;
			if (inputChange)
				FacingDirection = Utils.GetFacingDirectionFrom(impulse.X);

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

		public void ReceiveHit(LaunchType launchType)
		{
			SetAction(Player.CharacterData.Definition.ResolveHitAction(Player, launchType));
		}
	}
	public struct QueuedAction
	{
		public readonly InputType InputType;
		public ushort RemainingTime;
		public const ushort DefaultBufferTime = 14;

		public QueuedAction(InputType inputType)
		{
			InputType = inputType;
			RemainingTime = DefaultBufferTime;
		}

		/// <summary>
		/// Ticks the <see cref="RemainingTime"/> of the current <see cref="QueuedAction"/>, reducing it by one frame.<br/>
		/// <see cref="RemainingTime"/> can be set as 0 after this method was invoked, meaning the current <see cref="QueuedAction"/> will be expired on the next invocation of <see cref="TickBufferAndIsExpired"/>.
		/// </summary>
		/// <returns>True if <see cref="RemainingTime"/> was zero before being ticked.</returns>
		public bool TickBufferAndIsExpired() => RemainingTime-- == 0;
	}
	public struct QueuedActionList
	{
		public ExposedList<QueuedAction> ActionQueue = new ExposedList<QueuedAction>();

		public QueuedActionList()
		{
		}

		public bool HasActions => ActionQueue.Count != 0;

		public bool DequeueAction(InputType type, out QueuedAction action)
		{
			int index = ActionQueue.FindIndex(v => v.InputType == type);

			if (index != -1)
			{
				action = DequeueAction(index);

				return true;
			}

			action = default;

			return false;
		}

		/// <summary>
		/// Checks if an specific <see cref="InputType"/> is on the queue.
		/// </summary>
		/// <param name="type">The type of <see cref="InputType"/> to find.</param>
		/// <returns>True if an <see cref="InputType"/> <paramref name="type"/> was found on the queue.</returns>
		public bool HasAction(InputType type)
		{
			return ActionQueue.FindIndex(v => v.InputType == type) != -1;
		}

		/// <summary>
		/// Dequeues an specific <see cref="InputType"/> from the queue.
		/// </summary>
		/// <param name="type">The <see cref="InputType"/> to dequeue.</param>
		/// <returns>True if there was an action of the specified type that has ben dequeued.</returns>
		public bool DequeueAction(InputType type)
		{
			int index = ActionQueue.FindIndex(v => v.InputType == type);

			if (index == -1)
				return false;

			ActionQueue.RemoveAt(index);

			return true;
		}

		/// <summary>
		/// Dequeues the first action that is contained by <paramref name="types"/>.
		/// </summary>
		/// <param name="dequeuedType">The type that was dequeued and is contained by <paramref name="types"/></param>
		/// <param name="types">The list of types to dequeue, this accepts multiple of the same type at no actual benefit, why would you do this? idk</param>
		/// <returns>True if an action was found and dequeued, false otherwise.</returns>
		public bool ConsumeFirstOfAction(out InputType dequeuedType, params InputType[] types)
		{
			dequeuedType = InputType.Dash;
			int index = -1;

			foreach (InputType type in types)
			{
				int typeIndex = ActionQueue.FindIndex(v => v.InputType == type);

				if (typeIndex == -1)
					continue;

				if (index == -1 || index > typeIndex)
					index = typeIndex;
			}

			if (index == -1)
				return false;

			ActionQueue.RemoveAt(index);

			return true;
		}

		/// <summary>
		/// Dequeues an <see cref="QueuedAction"/> at the specified <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index to dequeue at.</param>
		/// <returns>The <see cref="QueuedAction"/> found at the given <paramref name="index"/></returns>
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
				AddAction(InputType.LeftStart);
			}

			if (controller.Left == ControlState.Releasing)
			{
				AddAction(InputType.LeftEnd);
			}

			if (controller.Right == ControlState.JustPressed)
			{
				AddAction(InputType.RightStart);
			}

			if (controller.Right == ControlState.Releasing)
			{
				AddAction(InputType.RightEnd);
			}

			if (controller.Up == ControlState.JustPressed)
			{
				AddAction(InputType.UpStart);
			}

			if (controller.Up == ControlState.Releasing)
			{
				AddAction(InputType.UpEnd);
			}

			if (controller.Down == ControlState.JustPressed)
			{
				AddAction(InputType.DownStart);
			}

			if (controller.Down == ControlState.Releasing)
			{
				AddAction(InputType.DownEnd);
			}

			if (controller.ActivateMeter == ControlState.JustPressed)
			{
				AddAction(InputType.Meter);
			}

			if (controller.Shield == ControlState.JustPressed)
			{
				AddAction(InputType.BlockStart);
			}

			if (controller.Shield == ControlState.Releasing)
			{
				AddAction(InputType.BlockEnd);
			}

			if (controller.Dash == ControlState.JustPressed)
			{
				AddAction(InputType.Dash);
			}

			if (controller.Jump == ControlState.JustPressed)
			{
				AddAction(InputType.Jump);
			}

			if (controller.SpecialToggle == ControlState.JustPressed)
			{
				AddAction(InputType.SpecialOn);
			}

			if (controller.SpecialToggle == ControlState.Releasing)
			{
				AddAction(InputType.SpecialOff);
			}

			if (controller.MeleeAttack == ControlState.JustPressed)
			{
				AddAction(InputType.AttackMelee);
			}

			if (controller.ShotAttack == ControlState.JustPressed)
			{
				AddAction(InputType.AttackShot);
			}

			if (controller.MeleeAttack == ControlState.Releasing)
			{
				AddAction(InputType.AttackMeleeEnd);
			}

			if (controller.ShotAttack == ControlState.Releasing)
			{
				AddAction(InputType.AttackShotEnd);
			}
		}

		/// <summary>
		/// Adds an action to the end of the queue. If there is an existing action of the same type, it gets removed and added to the end.
		/// </summary>
		/// <param name="inputType">The type of action to add</param>
		public void AddAction(InputType inputType)
		{
			int index = ActionQueue.FindIndex(v => v.InputType == inputType);

			if (index != -1)
			{
				ActionQueue.RemoveAt(index);
			}

			ActionQueue.Add(new QueuedAction(inputType));
		}

		public void Clear()
		{
			ActionQueue.Clear();
		}
	}
	public enum FacingDirection : byte
	{
		/// <summary>
		/// Left direction.<br/>
		/// This means an negative x coordinate. And <see cref="Utils.GetFacingDirectionMult"/> returns -1.
		/// </summary>
		Left,
		/// <summary>
		/// Right direction.<br/>
		/// This means an positive x coordinate. And <see cref="Utils.GetFacingDirectionMult"/> returns 1.
		/// </summary>
		Right
	}
	public enum InputType
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
		AttackMeleeEnd,
		AttackShot,
		AttackShotEnd,
		SpecialOn,
		SpecialOff,
		Meter
	}
	[Flags]
	public enum ActionType : uint
	{
		None = 0,
		Dash = 1 << 0,
		Jump = 1 << 1,
		Walk = 1 << 2,
		AttackMeleeNeutral = 1 << 3,
		AttackMeleeSide = 1 << 4,
		AttackMeleeDown = 1 << 5,
		AttackShotNeutral = 1 << 6,
		AttackShotSide = 1 << 7,
		AttackShotDown = 1 << 8,
		SpecialMeleeNeutral = 1 << 9,
		SpecialMeleeSide = 1 << 10,
		SpecialMeleeDown = 1 << 11,
		SpecialShotNeutral = 1 << 12,
		SpecialShotSide = 1 << 13,
		SpecialShotDown = 1 << 14,
		Block = 1 << 15,
		HighJump = 1 << 16,
		Crouch = 1 << 17,
		Meter = 1 << 20,
		Custom1 = 1 << 21,
		Custom2 = 1 << 22,
		Custom3 = 1 << 23,
		Custom4 = 1 << 24,
		Custom5 = 1 << 25,
		Custom6 = 1 << 26,
		Custom7 = 1 << 27,
		Custom8 = 1 << 28,
		Custom9 = 1 << 29,
		AllMeleeAttacks = AttackMeleeNeutral | AttackMeleeSide | AttackMeleeDown,
		AllShotAttacks = AttackShotNeutral | AttackShotSide | AttackShotDown,
		AllSpecialMeleeAttacks = SpecialMeleeNeutral | SpecialMeleeSide | SpecialMeleeDown,
		AllSpecialShotAttacks = SpecialShotNeutral | SpecialShotSide | SpecialShotDown,
		AllAttacks = AllMeleeAttacks | AllShotAttacks | AllSpecialMeleeAttacks | AllSpecialShotAttacks,
		AllMovement = Walk | Jump | Dash | Crouch | Block,
		AllCustom = Custom1 | Custom2 | Custom3 | Custom4 | Custom5 | Custom6 | Custom7 | Custom8 | Custom9,
		AllActions = AllAttacks | AllMovement | AllCustom | Meter
	}
}
using Microsoft.Xna.Framework;

using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformFighter.Entities.Actions
{
	public class PlayerActionManager
	{
		public ExposedList<QueuedAction> ActionQueue = new ExposedList<QueuedAction>();
		public ActionBase<Player> CurrentAction;
		public bool Shielding;
		public float HorizontalImpulse;
		public sbyte DirectionAnimation;
		public bool FacingRight;
		public JumpData JumpData = new JumpData();
		public int AnimationFrame;

		public void Update(Player player)
		{
			IPlayerControlDataReceiver controller = player.GetController();
			AddActions(controller);

			HorizontalImpulse = 0;

			TickActions(player, controller);
			
			if (DirectionAnimation < 10) // tick change direction animation
			{
				DirectionAnimation++;
			}

			if (player.Grounded)
			{
				if (HorizontalImpulse == 0) // apply friction if not moving
				{
					player.MovableObject.VelocityX *= player.CharacterData.Definition.FloorFriction;
				}
				else if (Math.Abs(player.MovableObject.VelocityX) < player.CharacterData.Definition.WalkMaxSpeed || Math.Sign(player.MovableObject.VelocityX) != Math.Sign(HorizontalImpulse))
				{
					player.MovableObject.VelocityX += player.CharacterData.Definition.WalkAcceleration * HorizontalImpulse;
				}
			}
			
			if (HorizontalImpulse == 1) // if only pressing right, reset animation, etc
			{
				if (!FacingRight)
					DirectionAnimation = 0;
				FacingRight = true;
			}

			if (HorizontalImpulse == -1)
			{
				if (FacingRight)
					DirectionAnimation = 0;
				FacingRight = false;
			}

			JumpData.Tick(player, controller);

			CurrentAction?.Update();
		}
		public void TickActions(Player player, IPlayerControlDataReceiver controller)
		{
			if (CurrentAction is null)
			{
				while (ActionQueue.Count != 0)
				{
					QueuedAction action = ActionQueue[0];
					ProcessAction(player, action.ActionType);
					ActionQueue.RemoveAt(0);
				}

				// tick all actions that werent able to be processed
				for (int i = 0; i < ActionQueue.Count; i++)
				{
					QueuedAction action = ActionQueue[i];

					if (!action.TickBufferAndReturnIfExpired())
						continue;

					ActionQueue.RemoveAt(i);
					i--;
				}
			}
			else
			{
				CurrentAction.ProcessQueue(ActionQueue);
			}
		}

		public void ProcessAction(Player player, ActionType actionType)
		{
			switch (actionType)
			{
				case ActionType.ActionLeftValue:
					HorizontalImpulse -= 1; 
					break;
				case ActionType.ActionRightValue:
					HorizontalImpulse += 1; 
					break;
				case ActionType.JumpValue: // this works because jump is processed after moving
					JumpData.InitializeJump(player, HorizontalImpulse, player.Grounded, false);
					break;
			}
		}

		public void AddActions(IPlayerControlDataReceiver controller)
		{
			if (controller.ActivateMeter)
			{
				AddAction(ActionType.Meter);
			}
			if (controller.Shield)
			{
				AddAction(ActionType.Block);
			}
			if (controller.Left)
			{
				AddAction(ActionType.ActionLeft);
			}
			if (controller.Right)
			{
				AddAction(ActionType.ActionRight);
			}
			if (controller.Up)
			{
				AddAction(ActionType.ActionUp);
			}
			if (controller.Down)
			{
				AddAction(ActionType.ActionDown);
			}
			if (controller.Jump == ControlState.JustPressed)
			{
				AddAction(ActionType.Jump);
			}
			if (controller.SpecialToggle)
			{
				AddAction(ActionType.Special);
			}
			if (controller.MeleeAttack == ControlState.JustPressed)
			{
				AddAction(ActionType.AttackMelee);
			}
			if (controller.ShotAttack == ControlState.JustPressed)
			{
				AddAction(ActionType.AttackShot);
			}

			return;

			void AddAction(ActionType actionType)
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

		public void Draw(Player player)
		{
			if (CurrentAction is null)
			{
				AnimationData data = null;
				int usedFrame = AnimationFrame;

				if (JumpData.JumpFrame is not null)
				{
					data = AnimationRenderer.GetAnimation("elmojump");
					AnimationFrame = JumpData.JumpFrame.Value;
				}
				if (HorizontalImpulse == 0 && JumpData.JumpFrame is null)
				{
					data = AnimationRenderer.GetAnimation("elmoidle");
					AnimationFrame++;
				}

				Vector2 scale = player.Scale;
				scale.X *= FacingRight.GetDirection();
				if(data is not null)
					AnimationRenderer.DrawJsonData(Main.spriteBatch, data.JsonData, usedFrame, player.MovableObject.Center, scale);

			}
			else
			{
				CurrentAction.Draw();
			}
		}

		public void Reset()
		{
			CurrentAction = null;
		}
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

		public bool TickBufferAndReturnIfExpired()
		{
			return BufferTime-- == 0;
		}

		public int CompareTo(QueuedAction other) => ActionType.CompareTo(other.ActionType);
	}
	public struct JumpData
	{
		public int? JumpFrame;
		public float JumpDirection;
		public bool WasGrounded;
		public bool WallJump;
		public Vector2 UsedVelocity;
		
		public void Tick(Player player, IPlayerControlDataReceiver controller)
		{
			if (!JumpFrame.HasValue)
				return;

			if (JumpFrame.Value >= player.CharacterData.Definition.JumpStartupFrames)
			{
				if (JumpFrame.Value == player.CharacterData.Definition.JumpStartupFrames || controller.Jump && JumpFrame.Value < player.CharacterData.Definition.JumpStartupFrames + player.CharacterData.Definition.JumpHoldMaxFrames)
				{
					player.MovableObject.Velocity = UsedVelocity;
				}
				else
				{
					JumpFrame = null;
				}
			}
			
			JumpFrame++;
		}

		public void InitializeJump(Player player, float direction, bool wasGrounded, bool wallJump)
		{
			if (JumpFrame.HasValue)
				return;
			
			JumpFrame = 0;
			JumpDirection = direction;
			WasGrounded = wasGrounded;
			WallJump = wallJump;
			bool neutralJump = direction == 0;
			CharacterDefinition definition = player.CharacterData.Definition;
			UsedVelocity = wallJump ? definition.WallJumpVelocity : wasGrounded ? neutralJump ? definition.GroundJumpVelocity : definition.GroundSideJumpVelocity : neutralJump ? definition.AirborneJumpVelocity : definition.AirborneSideJumpVelocity;
			UsedVelocity.X *= player.PlayerActionManager.FacingRight.GetDirection();
		}
	}
	public readonly struct ActionType : IComparable<ActionType>
	{
		public static readonly ActionType // order is special toggle > block > move actions > jump > meter > attack melee > attack shot
			ActionLeft = (ActionLeftValue, 100),
			ActionRight = (ActionRightValue, 100),
			ActionUp = (ActionUpValue, 100),
			ActionDown = (ActionDownValue, 100),
			Jump = (JumpValue, 90), 
			Block = (BlockValue, 110),
			AttackMelee = (AttackMeleeValue, 80),
			AttackShot = (AttackShotValue, 79),
			Special = (SpecialValue, 120),
			Meter = (MeterValue, 89);
		public const byte 
			ActionLeftValue = 0,
			ActionRightValue =1, 
			ActionUpValue =2, 
			ActionDownValue =3, 
			JumpValue =4, 
			BlockValue =5, 
			AttackMeleeValue =6, 
			AttackShotValue =7, 
			SpecialValue =8, 
			MeterValue =9; 
		public readonly byte Value;
		public readonly byte Priority;

		public ActionType(byte value, byte priority)
		{
			Value = value;
			Priority = priority;
		}

		public static implicit operator ActionType((byte value, byte priority) what) => new ActionType(what.value, what.priority);
		public static implicit operator byte(ActionType action) => action.Value;

		public int CompareTo(ActionType other) => other.Priority.CompareTo(Priority); // reversed so higher priority is located first
	}
}
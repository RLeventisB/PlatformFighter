using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;
using PlatformFighter.Rendering;

using System;

namespace PlatformFighter.Entities.Characters
{
	public class ElmoDefinition : CharacterDefinition
	{
		public override string FighterName => "Elmo";
		public override float WalkAcceleration => 0.12f;
		public override float DashTurningAngle => 1f;
		public override float AirAcceleration => 0.01f;
		public override float WalkMaxSpeed => 2.5f;
		public override float DashSpeed => 4f;
		public override float AirMaxSpeed => 1f;
		public override float FloorFriction => 0.9f;
		public override float HigherSpeedSlowingValue => 0.9f;
		public override float FallingGravity => 0.15f;
		public override float FallingGravityMax => 5f;
		public override int MaxJumpCount => 3;
		public override float FastFallAcceleration => 0.2f;
		public override float FastFallMaxSpeed => 6f;
		public override float Tankiness => 1f;
		public override int JumpStartupFrames => 7;
		public override int DashStartupFrames => 7;
		public override Vector2 GroundJumpVelocity => new Vector2(0, -4);
		public override Vector2 DashGroundJumpVelocity => new Vector2(0, -5).RotateRad(MathHelper.ToRadians(35f));
		public override Vector2 GroundSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(40f));
		public override Vector2 AirborneJumpVelocity => GroundJumpVelocity;
		public override Vector2 AirborneSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(40f));
		public override Vector2 WallJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(50f));
		public override int JumpHoldMaxFrames => 30;
		public override Vector2 CollisionSize => new Vector2(40, 80);
		public override float WallGravity => 0.044f;
		public override float WallMaxFallSpeed => 2f;

		public override ActionBase<Player> ResolveIdleAction(Player player, bool grounded) => grounded ? new ElmoIdleAction(player) : new ElmoFallingAction(player);

		public override ActionBase<Player> ResolveAttackAction(Player player, AttackDirection attackDirection, bool isShot, bool isSpecial)
		{
			if (player.Grounded)
			{
				if (isShot)
				{
				}
				else
				{
					switch (attackDirection)
					{
						case AttackDirection.Neutral:
							return new ElmoNeutralMeleeGrounded(player);
						case AttackDirection.Side:
							return new ElmoSideMeleeGrounded(player);
						case AttackDirection.Down:
							return new ElmoDownMeleeGrounded(player);
					}
				}

				return new ElmoNeutralMeleeGrounded(player);
			}

			if (isShot)
			{
			}
			else
			{
				return new ElmoNeutralMeleeGrounded(player);
			}

			return new ElmoNeutralMeleeGrounded(player);
		}

		public override ActionBase<Player> ResolveHitAction(Player player, LaunchType launchType) => new ElmoHitAction(player, launchType);
	}
	public class ElmoHitAction : AnimationActionBase
	{
		public ElmoHitAction(Player entity, LaunchType hitType) : base(entity, "elmohitair")
		{
			HitType = hitType;
		}

		public LaunchType HitType { get; }

		public override ActionTags Tags => ActionTags.Hit | ActionTags.NoGravity;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			if (Entity.ActionManager.HitStun > 0)
				return;

			if (Entity.ActionManager.QueuedAction.DequeueAction(InputType.Jump))
			{
				ChangeTo(new ElmoJumpAction(Entity, Utils.GetFacingDirectionFrom(Entity.ActionManager.GetImpulseFromController(Entity.GetController()).X), true));
			}

			ChangeTo(new ElmoFallingAction(Entity));
		}

		public override void Update()
		{
			if (Shortcuts.IsGrounded(Entity))
			{
				// ChangeTo(new ElmoLandAction(Entity));
			}

			Entity.AddAcceleration(ref Entity.MovableObject.VelocityY, FacingDirection.Right, 0.15f, Entity.CharacterData.Definition.FallingGravityMax, 0.9f);

			base.Update();
		}

		public override void OnEnd()
		{
			Entity.Health.ComboTracker.Reset();
		}
	}
	public class ElmoIdleAction : AnimationActionBase
	{
		public ElmoIdleAction(Player entity) : base(entity, "elmoidle")
		{
		}

		public override ActionTags Tags => ActionTags.None;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller);

			if (controller.Down)
			{
				ChangeTo(new ElmoCrouchAction(Entity));
			}
		}

		public override void Update()
		{
			if (!Entity.Grounded)
				ChangeTo(new ElmoFallingAction(Entity));

			Entity.ActionManager.RestoreJumpCount();
			Frame %= AnimationData.LastFrame;

			base.Update();
		}
	}
	public class ElmoFastFallingAction : StateAction
	{
		public ElmoFastFallingAction(Player entity) : base(entity, "elmofastfall")
		{
		}

		public override ActionTags Tags => ActionTags.NoGravity | ActionTags.Aerial;
		public override bool State => Entity.GetController().Down;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller);
		}

		public override void Update()
		{
			if (Shortcuts.IsGrounded(Entity))
				ChangeTo(new ElmoLandAction(Entity));

			if (Entity.GetController().Down && Entity.MovableObject.VelocityY < Entity.CharacterData.Definition.FastFallMaxSpeed)
			{
				Entity.MovableObject.VelocityY += Entity.CharacterData.Definition.FastFallAcceleration;
			}

			base.Update();
		}

		public override void OnTimeEnd()
		{
			ChangeTo(new ElmoFallingAction(Entity));
		}
	}
	public class ElmoFallingAction : AnimationActionBase
	{
		public ElmoFallingAction(Player entity) : base(entity, "elmofalling")
		{
		}

		public override ActionTags Tags => ActionTags.Aerial;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller);

			if (controller.Down)
			{
				ChangeTo(new ElmoFastFallingAction(Entity));
			}
		}

		public override void Update()
		{
			if (Entity.Grounded)
				ChangeTo(new ElmoLandAction(Entity));

			base.Update();
		}
	}
	public class ElmoNeutralMeleeGrounded : EndingActionToIdle
	{
		public ElmoNeutralMeleeGrounded(Player entity) : base(entity, "elmogroundneutralmelee")
		{
		}

		public override ActionTags Tags => ActionTags.Attack;
	}
	public class ElmoDownMeleeGrounded : EndingActionToIdle
	{
		public ElmoDownMeleeGrounded(Player entity) : base(entity, "elmogrounddownmelee")
		{
		}

		public override ActionTags Tags => ActionTags.Attack;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			if (Entity.ActionManager.HasThisActionCollided)
			{
				Entity.ActionManager.DoDefaultLogic(Entity.GetController(), false, false, true, false);
			}
		}

		public override void OnTimeEnd()
		{
			ChangeTo(new ElmoCrouchAction(Entity) { Frame = Duration });
		}
	}
	public class ElmoSideMeleeGrounded : EndingActionToIdle
	{
		public ElmoSideMeleeGrounded(Player entity) : base(entity, "elmogroundsidemelee")
		{
		}

		public override ActionTags Tags => ActionTags.Attack;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			if (!HasActionHit)
				return;

			if (queuedAction.DequeueAction(InputType.AttackMelee))
			{
				ChangeTo(new ElmoSideMeleeGroundedFinisher(Entity));
			}
		}
	}
	public class ElmoSideMeleeGroundedFinisher : EndingActionToIdle
	{
		public ElmoSideMeleeGroundedFinisher(Player entity) : base(entity, "elmogroundsidemeleefinisher")
		{
		}

		public override ActionTags Tags => ActionTags.Attack;
	}
	public class ElmoLandAction : EndingActionToIdle
	{
		public ElmoLandAction(Player entity) : base(entity, "elmoland")
		{
		}

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller, false);
			Entity.ActionManager.RestoreJumpCount();
		}

		public override void OnStart()
		{
			Entity.ActionManager.RecoveryFrames = 10;
		}

		public override void Update()
		{
			Entity.MovableObject.VelocityX *= 0.9f;
			base.Update();
		}

		public override bool CanChangeTo(ActionBase<Player> action) => true;

		public override void OnTimeEnd()
		{
			ChangeTo(new ElmoIdleAction(Entity));
		}
	}
	public class ElmoWalkAction : AnimationActionBase
	{
		public ElmoWalkAction(Player entity, FacingDirection direction) : base(entity, "elmowalkstart")
		{
			WalkStartFrames = AnimationRenderer.GetAnimation("elmowalkstart").LastFrame;
			TurnStartFrames = AnimationRenderer.GetAnimation("elmoturn").LastFrame;

			WalkingDirection = direction;
		}

		public int WalkStartFrames { get; init; }
		public int TurnStartFrames { get; init; }
		public int TurnFrames { get; internal set; }
		public int WalkTime { get; internal set; }
		public FacingDirection WalkingDirection { get; internal set; }
		public bool DoingStart => WalkTime < TurnStartFrames;
		public override ActionTags Tags => (Entity.ActionManager.Impulse.X != 0 ? ActionTags.NoFriction : ActionTags.None) | ActionTags.Movement;

		public override void OnStart()
		{
			ProcessDirection(WalkingDirection);
		}

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller, doWalkingLogic: false);

			if (controller.Down)
				ChangeTo(new ElmoCrouchAction(Entity));

			if (Entity.ActionManager.QueuedAction.DequeueAction(InputType.Dash))
			{
				Entity.ActionManager.UpdateFacingToInputs();
				ChangeTo(new ElmoDashStartAction(Entity, Entity.ActionManager.FacingDirection));
			}
		}

		private void ProcessDirection(FacingDirection newDirection)
		{
			if (Entity.ActionManager.FacingDirection != newDirection)
				TurnFrames = TurnStartFrames;

			Entity.ActionManager.RestoreJumpCount();
			WalkingDirection = Entity.ActionManager.FacingDirection = newDirection;
		}

		public override bool CanChangeTo(ActionBase<Player> action) => action is not ElmoWalkAction;

		public override void Update()
		{
			if (!Entity.Grounded)
			{
				ChangeTo(new ElmoFallingAction(Entity));

				return;
			}

			if (TurnFrames > 0)
			{
				Entity.AddWalkAcceleration(WalkingDirection);
				TurnFrames--;

				if (TurnFrames > 0)
				{
					AnimationData = AnimationRenderer.GetAnimation("elmoturn");

					Frame = AnimationData.LastFrame - TurnFrames;

					return;
				}
			}

			AnimationData = AnimationRenderer.GetAnimation(DoingStart ? "elmowalkstart" : "elmowalk");

			if (Entity.ActionManager.Impulse.X != 0)
			{
				FacingDirection updatedDirection = Utils.GetFacingDirectionFrom(Entity.ActionManager.Impulse.X);

				if (updatedDirection != WalkingDirection)
				{
					ProcessDirection(updatedDirection);
				}

				if (WalkTime < WalkStartFrames)
				{
					WalkTime++;

					if (!DoingStart)
					{
						Frame = WalkTime;
					}
				}

				Entity.AddWalkAcceleration(WalkingDirection);
				Frame++;

				if (!DoingStart)
				{
					Frame %= AnimationData.LastFrame;
				}
			}
			else
			{
				if (WalkTime > 0)
					WalkTime--;

				Frame = WalkTime;

				if (WalkTime <= 0)
				{
					ChangeTo(new ElmoIdleAction(Entity));
				}
			}
		}

		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, Frame, Entity.MovableObject.Center, Entity.GetScaleWithFacing, Entity.Rotation);
		}
	}
	public class ElmoDashStartAction : EndingActionToIdle
	{
		public ElmoDashStartAction(Player entity, FacingDirection dashDirection) : base(entity, entity.Grounded ? "elmodashgroundstart" : "elmodashairstart")
		{
			Grounded = entity.Grounded;
			DashDirection = dashDirection;
			Duration = entity.CharacterData.Definition.DashStartupFrames;
			entity.ActionManager.FacingDirection = dashDirection;
		}

		public override ActionTags Tags => ActionTags.NoGravity;
		public FacingDirection DashDirection { get; internal set; }
		public float DashAngle { get; internal set; }
		public bool Grounded { get; internal set; }

		public override void Update()
		{
			Direction collidedDirections = Collision.GetCollidingDirection(Entity.MovableObject, new Vector2(0, Entity.CharacterData.Definition.FallingGravity));
			Grounded = collidedDirections.HasFlag(Direction.Up);

			if (Frame < 5)
			{
				DashAngle = Entity.ActionManager.Impulse.ToAngle();
				Entity.ActionManager.UpdateFacingToInputs();
				DashDirection = Entity.ActionManager.FacingDirection;
			}

			Entity.MovableObject.Velocity *= 0.9f;
			base.Update();
		}

		public override void OnTimeEnd()
		{
			Entity.ActionManager.SetAction(new ElmoDashAction(Entity, DashDirection, DashAngle, Grounded));
		}
	}
	public class ElmoDashAction : AnimationActionBase
	{
		public ElmoDashAction(Player entity, FacingDirection dashDirection, float initialDashAngle, bool grounded) : base(entity, grounded ? "elmodashground" : "elmodashair")
		{
			Grounded = grounded;
			DashDirection = dashDirection;
			DashAngle = initialDashAngle;
		}

		public float DashAngle { get; internal set; }
		public FacingDirection DashDirection { get; init; }

		public override ActionTags Tags => ActionTags.NoFriction | ActionTags.NoGravity | ActionTags.Movement;
		public int NoImpulseFrames { get; private set; }
		public bool Grounded { get; internal set; }

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			Entity.ActionManager.DoDefaultLogic(Entity.GetController(), doWalkingLogic: false);
		}

		public override void Update()
		{
			bool oldGrounded = Grounded;
			Grounded = Shortcuts.IsGrounded(Entity);

			Entity.ActionManager.RestoreJumpCount();

			if (oldGrounded != Grounded)
			{
				AnimationData = AnimationRenderer.GetAnimation(Grounded ? "elmodashground" : "elmodashair");
			}

			Frame %= AnimationData.LastFrame;

			float targetAngle = Entity.ActionManager.Impulse.ToAngle();
			float difference = Utils.WrapAngle(targetAngle - DashAngle);
			bool groundedAndOppositeImpulse = Math.Abs(difference) > 179 && Grounded;

			if (groundedAndOppositeImpulse)
			{
				difference = 0;
				targetAngle = DashAngle;
			}

			if (Entity.ActionManager.Impulse == Vector2.Zero || !Entity.GetController().Dash || groundedAndOppositeImpulse)
			{
				NoImpulseFrames++;

				if (NoImpulseFrames > 5)
				{
					Entity.ActionManager.SetAction(new ElmoDashEndAction(Entity, DashDirection, Grounded));

					return;
				}
			}
			else
			{
				NoImpulseFrames = 0;
			}

			if (Entity.ActionManager.Impulse != Vector2.Zero)
			{
				if (difference > 0)
				{
					DashAngle += Entity.CharacterData.Definition.DashTurningAngle;
					difference = Utils.WrapAngle(targetAngle - DashAngle);
					if (difference < 0)
						DashAngle = targetAngle;
				}
				else if (difference < 0)
				{
					DashAngle -= Entity.CharacterData.Definition.DashTurningAngle;
					difference = Utils.WrapAngle(targetAngle - DashAngle);
					if (difference > 0)
						DashAngle = targetAngle;
				}
			}

			DashAngle = Utils.WrapAngle(DashAngle);
			if (DashAngle is not -90 or 90)
				Entity.ActionManager.FacingDirection = DashAngle is > -90 and < 90 ? FacingDirection.Right : FacingDirection.Left;

			Entity.Rotation = MathHelper.ToRadians(Entity.ActionManager.FacingDirection == FacingDirection.Left ? -DashAngle + 180 : DashAngle);
			Entity.MovableObject.Velocity = new Vector2(Entity.CharacterData.Definition.DashSpeed, 0).Rotate(DashAngle);

			Entity.ActionManager.DoWallLogic();
			Direction collidedDirections = Collision.GetCollidingDirection(Entity.MovableObject, Entity.MovableObject.Velocity);

			if (collidedDirections.HasFlag(Direction.Up))
			{
				ChangeTo(new ElmoLandAction(Entity));
			}

			base.Update();
		}

		public override void OnEnd()
		{
			Entity.Rotation = 0;
		}
	}
	public class ElmoDashEndAction : EndingActionToIdle
	{
		public ElmoDashEndAction(Player entity, FacingDirection dashDirection, bool grounded) : base(entity, grounded ? "elmodashgroundend" : "elmodashairend")
		{
			DashDirection = dashDirection;
		}

		public FacingDirection DashDirection { get; init; }

		public override void OnStart()
		{
			Entity.ActionManager.RecoveryFrames += Shortcuts.IsGrounded(Entity) ? 17 : 40;
		}

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller);
		}

		public override void OnTimeEnd()
		{
			Entity.ActionManager.SetAction(new ElmoIdleAction(Entity));
		}
	}
	public class ElmoJumpEndAction : EndingActionToIdle
	{
		public ElmoJumpEndAction(Player entity) : base(entity, "elmojump", 30)
		{
		}

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.DoDefaultLogic(controller);

			if (controller.Down)
			{
				ChangeTo(new ElmoFastFallingAction(Entity));
			}
		}

		public override void OnTimeEnd()
		{
			ChangeTo(new ElmoFallingAction(Entity));
		}

		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, Frame + 30, Entity.MovableObject.Center, Entity.GetScaleWithFacing, Entity.Rotation);
		}
	}
	public class ElmoCrouchAction : StateAction
	{
		public ElmoCrouchAction(Player entity) : base(entity, "elmocrouch")
		{
		}

		public override bool State => Entity.GetController().Down;
		public override int Frequency => 2;

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.UpdateFacingToInputs();
			Entity.ActionManager.DoAttackLogic();
			Entity.ActionManager.DoJumpLogic();
			Entity.ActionManager.DoWalkingLogic(controller, true);
		}

		public override void Update()
		{
			if (!Entity.Grounded)
				ChangeTo(new ElmoFallingAction(Entity));
			else
				base.Update();
		}

		public override void OnTimeEnd()
		{
			ChangeTo(new ElmoIdleAction(Entity));
		}
	}
	public class ElmoWallAction : AnimationActionBase
	{
		public ElmoWallAction(Player entity, FacingDirection wallDirection) : base(entity, "elmowall")
		{
			WallDirection = wallDirection;
		}

		public override ActionTags Tags => ActionTags.Wall | ActionTags.NoGravity;
		public FacingDirection WallDirection { get; set; }

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			IPlayerDataReceiver controller = Entity.GetController();

			Entity.ActionManager.RestoreJumpCount();

			if (WallDirection == FacingDirection.Right)
			{
				Entity.MovableObject.VelocityX = -0.01f;

				if (controller.Right)
				{
					Entity.MovableObject.VelocityX = 1f;
					Entity.ActionManager.SetAction(new ElmoFallingAction(Entity));
				}
			}

			if (WallDirection == FacingDirection.Left)
			{
				Entity.MovableObject.VelocityX = 0.01f;

				if (controller.Left)
				{
					Entity.MovableObject.VelocityX = -1f;
					Entity.ActionManager.SetAction(new ElmoFallingAction(Entity));
				}
			}

			if (Entity.ActionManager.QueuedAction.DequeueAction(InputType.Jump))
			{
				Entity.ActionManager.SetAction(new ElmoJumpAction(Entity, WallDirection));
			}
		}

		public override void Update()
		{
			Entity.ActionManager.FacingDirection = WallDirection;

			if (Entity.MovableObject.VelocityY < Entity.CharacterData.Definition.WallMaxFallSpeed)
			{
				Entity.MovableObject.VelocityY += Entity.CharacterData.Definition.WallGravity;
			}

			Collision.CollisionPrecalculation precalculation = new Collision.CollisionPrecalculation(Entity.MovableObject, new Vector2(-Utils.GetFacingDirectionMult(WallDirection) * 10, 0));
			Collision.GetCollidingObjects(Entity.MovableObject, in precalculation, out Collision.CollisionData[] collidedObjects);
			Direction collidingDirections = Collision.ResolveCollisions(ref Entity.MovableObject, in precalculation, in collidedObjects);

			if (collidingDirections.HasFlag(Direction.Left) && WallDirection == FacingDirection.Right ||
			    collidingDirections.HasFlag(Direction.Right) && WallDirection == FacingDirection.Left)
				return;

			ChangeTo(new ElmoFallingAction(Entity));
		}
	}
	public class ElmoJumpAction : AnimationActionBase
	{
		private readonly bool IsDashJump;
		public readonly FacingDirection? WallJumpDirection;

		public ElmoJumpAction(Player entity, FacingDirection? wallJumpDirection, bool dash = false) : base(entity, "elmojump")
		{
			JumpStartup = Entity.CharacterData.Definition.JumpStartupFrames;
			Duration = JumpStartup + Entity.CharacterData.Definition.JumpHoldMaxFrames;
			WallJumpDirection = wallJumpDirection;
			IsDashJump = dash;
		}

		public int JumpStartup { get; init; }
		public override ActionTags Tags => ActionTags.NoGravity | ActionTags.Movement;

		public override bool CanBeSetAsActive(Player player) => player.ActionManager.JumpCount > 0;

		public override void OnStart()
		{
			Entity.ActionManager.JumpCount--;
		}

		public override void ProcessQueue(QueuedActionList queuedAction)
		{
			if (Frame < JumpStartup + 5)
				return;

			Entity.ActionManager.DoJumpLogic();
			Entity.ActionManager.DoAttackLogic();
			Entity.ActionManager.DoWallLogic();
		}

		public override void Update()
		{
			FacingDirection? JumpDirection = Utils.GetFacingDirectionFromWithZero(Entity.ActionManager.Impulse.X);
			if (IsDashJump)
				JumpDirection = Entity.ActionManager.FacingDirection;

			if (Frame < JumpStartup && Math.Abs(Entity.MovableObject.VelocityY) > 0.1f && !Entity.Grounded)
			{
				Entity.MovableObject.Velocity *= 0.9f;
			}
			else if (Frame == JumpStartup)
			{
				if (WallJumpDirection is not null)
					JumpDirection = WallJumpDirection;

				if (JumpDirection is not null)
					Entity.ActionManager.FacingDirection = JumpDirection.Value;

				bool neutralJump = JumpDirection == null;
				bool grounded = Collision.GetCollidingDirection(Entity.MovableObject, new Vector2(0, 1)).HasFlag(Direction.Up);
				CharacterDefinition definition = Entity.CharacterData.Definition;
				Vector2 UsedVelocity = WallJumpDirection is not null ? definition.WallJumpVelocity : IsDashJump ? definition.DashGroundJumpVelocity : grounded ? neutralJump ? definition.GroundJumpVelocity : definition.GroundSideJumpVelocity : neutralJump ? definition.AirborneJumpVelocity : definition.AirborneSideJumpVelocity;

				UsedVelocity.X *= Utils.GetFacingDirectionMult(JumpDirection);
				Entity.MovableObject.VelocityX = Entity.MovableObject.VelocityX * 0.4f + UsedVelocity.X;
				Entity.MovableObject.VelocityY = UsedVelocity.Y;
			}

			if (Frame > JumpStartup && !Entity.GetController().Jump || Frame > Duration)
			{
				ChangeTo(new ElmoJumpEndAction(Entity));
			}

			base.Update();
		}
	}
}
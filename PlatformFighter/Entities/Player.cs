using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;

using System;

namespace PlatformFighter.Entities
{
	public class Player : GameEntity
	{
		public PlayerActionManager ActionManager;
		public CharacterData CharacterData = new CharacterData();
		public ushort ControllerId;
		public bool Grounded;
		public HealthHandler Health;
		public Collision.CollisionData[] LastFrameCollidedData;
		public Direction LastFrameCollidedDirections;
		public float MoveDelta;
		public Team PlayerTeam;
		public int Stocks;

		public Player()
		{
			ActionManager = new PlayerActionManager(this);
			Health = new HealthHandler(this);
		}

		public Vector2 GetScaleWithFacing => new Vector2(Scale.X * Utils.GetFacingDirectionMult(ActionManager.FacingDirection), Scale.Y);

		public override void ResetValues()
		{
			CharacterData.SetDefinition(0);
			CharacterData.ApplyDefaults(this);

			ActionManager.Reset();

			PlayerTeam = Team.Default;
			Stocks = 3;
		}

		public override void Kill()
		{
			GameWorld.Players.Return(this);
		}

		public override void Update()
		{
			MoveDelta = 1;
			if (GameWorld.IntroTimer > 0)
				ActionManager.SetDefaults();
			else
				ActionManager.Update();

			DoPhysics();
			Health.Update();
		}

		public void DoPhysics()
		{
			if (Grounded)
			{
				// if (ActionManager.CurrentAction is ElmoCrouchAction or ElmoShieldAction or ElmoDashAction)
				// {
				// MovableObject.VelocityX *= 0.9f;
				// MoveDelta = 0.9f;
				// }
				/*else*/
				if (!ActionManager.CurrentActionHasFlag(ActionTags.NoFriction))
				{
					MovableObject.VelocityX *= CharacterData.Definition.FloorFriction;
				}
			}

			MovableObject.Velocity *= 0.999f;
		}

		public void AddWalkAcceleration(FacingDirection direction)
		{
			AddAcceleration(ref MovableObject.VelocityX, direction, CharacterData.Definition.WalkAcceleration, CharacterData.Definition.WalkMaxSpeed, CharacterData.Definition.HigherSpeedSlowingValue);
		}

		public void AddAcceleration(ref float value, FacingDirection direction, float acceleration, float maxSpeed, float higherSpeedPenalty)
		{
			bool lowerThanMaxSpeed = Math.Abs(value) < maxSpeed;

			if (lowerThanMaxSpeed || Math.Sign(value) != Math.Sign(Utils.GetFacingDirectionMult(direction)))
			{
				value += acceleration * Utils.GetFacingDirectionMult(direction);
			}

			if (!lowerThanMaxSpeed)
			{
				value *= higherSpeedPenalty;
			}
		}

		public override void PostUpdate()
		{
			AddEnvironmentVelocities();

			Collision.CollisionPrecalculation precalculation = Collision.GetCollisionCalculations(MovableObject);
			Collision.GetCollidingObjects(MovableObject, in precalculation, out LastFrameCollidedData);

			LastFrameCollidedDirections = Collision.ResolveCollisions(ref MovableObject, in precalculation, LastFrameCollidedData);

			MovableObject.Position += MovableObject.Velocity * MoveDelta;
			Grounded = LastFrameCollidedDirections.HasFlag(Direction.Up);
		}

		private void AddEnvironmentVelocities()
		{
			if (!ActionManager.CurrentActionHasFlag(ActionTags.NoGravity))
			{
				if (MovableObject.VelocityY <= CharacterData.Definition.FallingGravityMax)
					MovableObject.VelocityY += CharacterData.Definition.FallingGravity;
				else
				{
					MovableObject.VelocityY *= CharacterData.Definition.HigherSpeedSlowingValue;
				}
			}
		}

		public override void Draw()
		{
			ActionManager.Draw();
			Main.spriteBatch.DrawRectangle(MovableObject.Rectangle);
		}

		public IPlayerDataReceiver GetController() => PlayerController.registeredControllers[ControllerId];

		public void Die()
		{
			Stocks--;
			Health.OnRespawn();
			MovableObject.Position = GameWorld.CurrentStage.GetSpawnPosition(this, true);

			if (Stocks == 0)
			{
				// Environment.Exit(0);
			}
		}
	}
}
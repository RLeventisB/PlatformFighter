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
		public Direction CollidedDirections;
		public ushort ControllerId;
		public bool Grounded;
		public HealthHandler Health = new HealthHandler();
		public int HitStun = 0;
		public Collision.CollisionData[] LastFrameCollidedData;
		public float MoveDelta;
		public TemporaryStateBoolean NoGravityFrames = new TemporaryStateBoolean();
		public Team PlayerTeam;
		public int Stocks;
		public Vector2 GetScaleWithFacing => new Vector2(Scale.X * Utils.GetFacingDirectionMult(ActionManager.FacingDirection), Scale.Y);

		public Player()
		{
			ActionManager = new PlayerActionManager(this);
		}
		public override void ResetValues()
		{
			ActionManager.Reset();

			CharacterData.SetDefinition(0);
			CharacterData.ApplyDefaults(this);
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
				if (ActionManager.Impulse.X == 0) // apply friction if not moving
				{
					MovableObject.VelocityX *= CharacterData.Definition.FloorFriction;
				}
			}

			MovableObject.Velocity *= 0.999f;
		}

		public void AddWalkAcceleration(FacingDirection direction)
		{
			if (Math.Abs(MovableObject.VelocityX) < CharacterData.Definition.WalkMaxSpeed || Math.Sign(MovableObject.VelocityX) != Math.Sign(Utils.GetFacingDirectionMult(direction)))
			{
				MovableObject.VelocityX += CharacterData.Definition.WalkAcceleration * Utils.GetFacingDirectionMult(direction);
			}
		}
		public override void PostUpdate()
		{
			AddEnvironmentVelocities();

			Collision.CollisionPrecalculation precalculation = Collision.GetCollisionCalculations(MovableObject);
			Collision.GetCollidingObjects(MovableObject, in precalculation, out LastFrameCollidedData);

			CollidedDirections = Collision.ResolveCollisions(ref MovableObject, in precalculation, LastFrameCollidedData);

			MovableObject.Position += MovableObject.Velocity * MoveDelta;
			Grounded = CollidedDirections.HasFlag(Direction.Up);
		}

		private void AddEnvironmentVelocities()
		{
			if (!NoGravityFrames.HasFrames)
			{
				if (MovableObject.VelocityY <= CharacterData.Definition.FallingGravityMax)
					MovableObject.VelocityY += CharacterData.Definition.FallingGravity;
			}
			else
			{
				NoGravityFrames.Tick();
			}
		}

		public override void Draw()
		{
			ActionManager.Draw(this);
			Main.spriteBatch.DrawRectangle(MovableObject.Rectangle);
		}

		public IPlayerDataReceiver GetController() => PlayerController.registeredControllers[ControllerId];
	}
}
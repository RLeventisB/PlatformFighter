using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;

namespace PlatformFighter.Entities
{
	public class Player : GameEntity
	{
		public Collision.CollisionData[] LastFrameCollidedData;
		public PlayerActionManager PlayerActionManager = new PlayerActionManager();
		public bool ApplyGravity = true;
		public CharacterData CharacterData = new CharacterData();
		public Direction CollidedDirections;
		public ushort ControllerId;
		public bool Grounded;
		public int HitStun = 0;
		public Team PlayerTeam;
		public int Stocks;
		public HealthHandler Health = new HealthHandler();

		public override void ResetValues()
		{
			PlayerActionManager.Reset();

			CharacterData.SetDefinition(0);
			CharacterData.ApplyDefaults(this);
			PlayerTeam = Team.Default;
			Stocks = 3;
		}

		public override void Kill(DeathReason killReason = DeathReason.NotSpecified)
		{
			GameWorld.Players.Return(this);
		}

		public override void Update()
		{
			PlayerActionManager.Update(this);
		}

		public override void PostUpdate()
		{
			AddEnvironmentVelocities();

			Collision.CollisionPrecalculation precalculation = Collision.GetCollisionCalculations(MovableObject);
			Collision.GetCollidingObjects(MovableObject, in precalculation, out LastFrameCollidedData);

			CollidedDirections = Collision.ResolveCollisions(ref MovableObject, in precalculation, LastFrameCollidedData);
			
			MovableObject.Position += MovableObject.Velocity;
			Grounded = CollidedDirections.HasFlag(Direction.Up);
		}

		private void AddEnvironmentVelocities()
		{
			if (ApplyGravity)
			{
				if (MovableObject.VelocityY <= CharacterData.Definition.FallingGravityMax)
					MovableObject.VelocityY += CharacterData.Definition.FallingGravity;
			}
		}

		public override void Draw()
		{
			PlayerActionManager.Draw(this);
			Main.spriteBatch.DrawRectangle(MovableObject.Rectangle);
		}

		public IPlayerControlDataReceiver GetController() => PlayerController.registeredControllers[ControllerId];
	}
}
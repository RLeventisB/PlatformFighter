using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

namespace PlatformFighter.Entities
{
	public abstract class CharacterDefinition
	{
		public abstract string FighterName { get; }
		public abstract float WalkAcceleration { get; }
		public abstract float AirAcceleration { get; }
		public abstract float WalkMaxSpeed { get; }
		public abstract float AirMaxSpeed { get; }
		public abstract float FloorFriction { get; }
		public abstract float JumpVelocity { get; }
		public abstract float FallingGravity { get; }
		public abstract float FallingGravityMax { get; }
		public abstract int MaxAirJumpCount { get; }
		public abstract float FastFallAcceleration { get; }
		public abstract float FastFallMaxSpeed { get; }
		public abstract float Tankiness { get; }
		public abstract int JumpStartupFrames { get; }
		public abstract int DashStartupFrames { get; }
		
		public abstract Vector2 GroundJumpVelocity { get; }
		public abstract Vector2 GroundSideJumpVelocity { get; }
		public abstract Vector2 AirborneJumpVelocity { get; }
		public abstract Vector2 AirborneSideJumpVelocity { get; }
		public abstract Vector2 WallJumpVelocity { get; }
		public abstract int JumpHoldMaxFrames { get; }

		public abstract Vector2 CollisionSize { get; }
		public abstract ActionBase<Player> ResolveAttackAction(Player player, AttackDirection attackDirection, bool isShot, bool isSpecial);

	}
}
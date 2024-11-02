using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;

namespace PlatformFighter.Entities
{
	public abstract class CharacterDefinition
	{
		public abstract string FighterName { get; }
		public abstract float WalkAcceleration { get; }
		public abstract float AirAcceleration { get; }
		public abstract float WalkMaxSpeed { get; }
		public abstract float DashTurningAngle { get; }
		public abstract float DashSpeed { get; }
		public abstract float AirMaxSpeed { get; }
		public abstract float FloorFriction { get; }
		public abstract float HigherSpeedSlowingValue { get; }
		public abstract float FallingGravity { get; }
		public abstract float FallingGravityMax { get; }
		public abstract int MaxJumpCount { get; }
		public abstract float FastFallAcceleration { get; }
		public abstract float FastFallMaxSpeed { get; }
		public abstract float Tankiness { get; }
		public abstract int JumpStartupFrames { get; }
		public abstract int DashStartupFrames { get; }

		public abstract Vector2 GroundJumpVelocity { get; }
		public abstract Vector2 DashGroundJumpVelocity { get; }
		public abstract Vector2 GroundSideJumpVelocity { get; }
		public abstract Vector2 AirborneJumpVelocity { get; }
		public abstract Vector2 AirborneSideJumpVelocity { get; }
		public abstract Vector2 WallJumpVelocity { get; }
		public abstract int JumpHoldMaxFrames { get; }

		public abstract Vector2 CollisionSize { get; }
		public abstract float WallGravity { get; }
		public abstract float WallMaxFallSpeed { get; }

		public abstract ActionBase<Player> ResolveIdleAction(Player player, bool grounded);

		public abstract ActionBase<Player> ResolveAttackAction(Player player, AttackDirection attackDirection, bool isShot, bool isSpecial);

		public abstract ActionBase<Player> ResolveHitAction(Player player, LaunchType launchType);
	}
}
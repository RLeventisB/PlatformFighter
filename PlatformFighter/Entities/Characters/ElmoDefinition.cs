using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

namespace PlatformFighter.Entities.Characters
{
	public class ElmoDefinition : CharacterDefinition
	{
		public override string FighterName => "Elmo";
		public override float WalkAcceleration => 0.12f;
		public override float AirAcceleration => 0.01f;
		public override float WalkMaxSpeed => 3f;
		public override float AirMaxSpeed => 1f;
		public override float FloorFriction => 0.9f;
		public override float JumpVelocity => 2f;
		public override float FallingGravity => 0.1f;
		public override float FallingGravityMax => 4f;
		public override int MaxAirJumpCount => 2;
		public override float FastFallAcceleration => 0.2f;
		public override float FastFallMaxSpeed => 6f;
		public override float Tankiness => 1f;
		public override int JumpStartupFrames => 7;
		public override int DashStartupFrames => 7;
		public override Vector2 GroundJumpVelocity => new Vector2(0, -4);
		public override Vector2 GroundSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(40f));
		public override Vector2 AirborneJumpVelocity => GroundJumpVelocity;
		public override Vector2 AirborneSideJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(60f));
		public override Vector2 WallJumpVelocity => GroundJumpVelocity.RotateRad(MathHelper.ToRadians(50f));
		public override int JumpHoldMaxFrames => 30;
		public override Vector2 CollisionSize => new Vector2(40, 80);
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
						default:
							return new ElmoNeutralMeleeGrounded(player);
					}
				}
				return new ElmoNeutralMeleeGrounded(player);
			}
			else
			{
				if (isShot)
				{
					
				}
				else
				{
					return new ElmoNeutralMeleeGrounded(player);
				}
				return new ElmoNeutralMeleeGrounded(player);
			}
		}
	}
	public class ElmoNeutralMeleeGrounded : ActionBase<Player>
	{
		public ElmoNeutralMeleeGrounded(Player entity) : base(entity)
		{
		}

		public override ActionTags Tags => ActionTags.Attack;
		public AnimationData GetAnimationData => AnimationRenderer.GetAnimation("elmogroundneutralmelee");
		public override int Duration => GetAnimationData.LastFrame;

		public override void Update()
		{
			Frame++;
			Frame %= GetAnimationData.LastFrame;
		}

		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, GetAnimationData.JsonData, Frame, Entity.MovableObject.Center, Entity.Scale);
		}
	}
}
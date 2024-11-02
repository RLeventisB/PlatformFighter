using PlatformFighter.Entities.Actions;

using System;

namespace PlatformFighter
{
	public struct HitData
	{
		public HitData(FacingDirection Direction, ushort? owner, ushort target, HitboxData hitbox, HurtboxData targetHurtbox, int attackHashCode)
		{
			this.Direction = Direction;
			Owner = owner;
			Target = target;
			Hitbox = hitbox;
			TargetHurtbox = targetHurtbox;
		}

		public FacingDirection Direction { get; init; }
		public ushort? Owner { get; init; }
		public ushort Target { get; init; }
		public HitboxData Hitbox { get; init; }
		public HurtboxData TargetHurtbox { get; init; }
		public int AttackHashCode { get; init; }
	}
}
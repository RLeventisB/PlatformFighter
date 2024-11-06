using PlatformFighter.Entities.Actions;

namespace PlatformFighter
{
	public struct HitData
	{
		public HitData(HitboxStateData stateData, FacingDirection Direction, ushort owner, ushort target, HitboxData hitbox, HurtboxData targetHurtbox, int attackHashCode)
		{
			StateData = stateData;
			this.Direction = Direction;
			Owner = owner;
			Target = target;
			Hitbox = hitbox;
			TargetHurtbox = targetHurtbox;
			AttackHashCode = attackHashCode;
		}

		public HitboxStateData StateData { get; }
		public FacingDirection Direction { get; init; }
		public ushort Owner { get; init; }
		public ushort Target { get; init; }
		public HitboxData Hitbox { get; init; }
		public HurtboxData TargetHurtbox { get; init; }
		public int AttackHashCode { get; init; }
	}
}
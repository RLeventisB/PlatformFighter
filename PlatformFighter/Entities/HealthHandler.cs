using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PlatformFighter.Entities
{
	public struct HealthHandler
	{
		public bool IsHit;
		public float Damage = 0;
		public Player Player { get; init; }
		public ComboTracker ComboTracker = new ComboTracker();
		public HitImmunityManager HitManager = new HitImmunityManager();

		public HealthHandler(Player player)
		{
			Player = player;
		}

		public bool Hit(HitData data)
		{
			if (HitManager.HasRegistered(data.AttackHashCode))
				return false;

			if (Player.ActionManager.CurrentActionHasFlag(ActionTags.Shield))
			{
				ApplyKnockback(new Vector2(data.Hitbox.ShieldPotency, 0).Rotate(data.Hitbox.ShieldLaunchAngle), data.Direction, data.Hitbox.ShieldStun);

				return false;
			}

			// NOTE: hitstun is before damage is added, which could make hits less "safe"
			ushort hitstun = data.Hitbox.Hitstun.GetValue(Damage);

			RegisterHit(data.Hitbox.Damage, data.Hitbox.Rate, data.Hitbox.LaunchType);

			Vector2 attackVector = data.StateData.GetLaunchVector(data.Hitbox, data.TargetHurtbox, Damage);
			ApplyKnockback(attackVector, data.Hitbox.LaunchDataIsAngle ? data.Direction : FacingDirection.Right, hitstun);

			HitManager.Register(data.AttackHashCode, data.Hitbox.ImmunityAfterHit);

			return true;
		}

		private void ApplyKnockback(Vector2 attackVector, FacingDirection direction, ushort hitstun)
		{
			attackVector.X *= Utils.GetFacingDirectionMult(direction);
			Player.MovableObject.Velocity = attackVector * Player.CharacterData.Definition.Tankiness;
			Player.ActionManager.HitStun = hitstun;
		}

		public void RegisterHit(float damage, float hitRate, LaunchType launchType)
		{
			Damage += ComboTracker.RegisterAndCalculateDamage(damage, hitRate, launchType);
			Player.ActionManager.ReceiveHit(launchType);
			IsHit = true;
		}

		public void Update()
		{
			HitManager.Tick();
		}

		public void OnRespawn()
		{
			HitManager.Clear();
			ComboTracker.Reset();
			Damage = 0;
		}
	}
	public record HitImmunityManager
	{
		private readonly Dictionary<int, ushort> attackTracker = new Dictionary<int, ushort>();

		public void Register(int attackHashCode, ushort immunityTime)
		{
			attackTracker.Add(attackHashCode, immunityTime);
		}

		public bool HasRegistered(int attackHashCode) => attackTracker.ContainsKey(attackHashCode);

		public void Clear()
		{
			attackTracker.Clear();
		}

		public void Tick()
		{
			foreach (int attackId in attackTracker.Keys)
			{
				ref ushort time = ref CollectionsMarshal.GetValueRefOrNullRef(attackTracker, attackId);

				if (time == 0)
				{
					attackTracker.Remove(attackId);
				}

				time--;
			}
		}
	}
	public struct ComboTracker
	{
		public ushort HitCount;
		public float TotalDamage;
		public float TotalRate;
		public LaunchType LaunchType;
		public List<ushort> AttackersId = new List<ushort>();

		public ComboTracker()
		{
			Reset();
		}

		public void Reset()
		{
			HitCount = 0;
			TotalDamage = 0f;
			TotalRate = 1f;
			LaunchType = LaunchType.TorsoHit;
			AttackersId.Clear();
		}

		public float RegisterAndCalculateDamage(float damage, float rate, LaunchType launchType)
		{
			float effectiveDamage = damage * TotalRate;
			HitCount++;
			TotalDamage += effectiveDamage;
			TotalRate *= rate;
			LaunchType = launchType;

			return effectiveDamage;
		}
	}
}
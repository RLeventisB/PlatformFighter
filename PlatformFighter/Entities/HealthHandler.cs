using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Generic;

namespace PlatformFighter.Entities
{
	public struct HealthHandler
	{
		public bool IsHit;
		public float Damage = 0;
		public Player Player { get; set; }
		public ComboTracker ComboTracker = new ComboTracker();

		public HealthHandler(Player player)
		{
			Player = player;
		}

		public void Hit(HitData data)
		{
			if (Player.ActionManager.Shielding)
			{
				Player.MovableObject.Velocity = new Vector2(0, data.Hitbox.ShieldPotency).Rotate(data.Hitbox.ShieldLaunchAngle);
				Player.HitStun = data.Hitbox.ShieldStun;
			}
			else
			{
				// NOTE: hitstun is before damage is added, which could make hits less "safe"
				ushort hitstun = (ushort) CalculateGrowingValue(data.Hitbox.Hitstun, data.Hitbox.HitstunGrowth, data.Hitbox.MaxHitstun);
				Player.HitStun = hitstun;
				
				RegisterHit(data.Hitbox.Damage, data.Hitbox.Hitstun, data.Hitbox.LaunchType);
				float launchPotency = CalculateGrowingValue(data.Hitbox.LaunchPotency, data.Hitbox.LaunchPotencyGrowth, data.Hitbox.LaunchPotencyMax);
				
				Player.MovableObject.Velocity = new Vector2(0, launchPotency).Rotate(data.Hitbox.ShieldLaunchAngle);
				Player.HitStun = data.Hitbox.ShieldStun;
			}
		}

		public void RegisterHit(float damage, float rate, LaunchType launchType)
		{
			Damage += ComboTracker.RegisterAndCalculateDamage(damage, rate, launchType);
			IsHit = true;
		}

		public float CalculateGrowingValue(float baseValue, float growthPerDamage, float max)
		{
			return Math.Min(max, baseValue + growthPerDamage * Damage);
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
			TotalDamage += effectiveDamage;
			TotalRate *= rate;
			LaunchType = launchType;
			
			return effectiveDamage;
		}
	}
}
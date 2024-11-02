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
		public Player Player { get; set; }
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
				Player.MovableObject.Velocity = new Vector2(0, data.Hitbox.ShieldPotency).Rotate(data.Hitbox.ShieldLaunchAngle);
				Player.HitStun = data.Hitbox.ShieldStun;

				return false;
			}

			// NOTE: hitstun is before damage is added, which could make hits less "safe"
			ushort hitstun = data.Hitbox.Hitstun.GetValue(Damage);
			Player.HitStun = hitstun;

			RegisterHit(data.Hitbox.Damage, data.Hitbox.Rate, data.Hitbox.LaunchType);
			float launchPotency = data.Hitbox.LaunchPotency.GetValue(Damage);

			Vector2 attackVector = new Vector2(launchPotency, 0).Rotate(data.Hitbox.GetLaunchAngle(Player.MovableObject.Center));
			attackVector.X *= Utils.GetFacingDirectionMult(data.Direction);

			Player.MovableObject.Velocity = attackVector;
			Player.HitStun = data.Hitbox.ShieldStun;

			HitManager.Register(data.AttackHashCode, data.Hitbox.ImmunityAfterHit);

			return true;
		}

		public void RegisterHit(float damage, float rate, LaunchType launchType)
		{
			Damage += ComboTracker.RegisterAndCalculateDamage(damage, rate, launchType);
			Player.ActionManager.ReceiveHit(launchType);
			IsHit = true;
		}

		public void Update()
		{
			HitManager.Tick();
		}
	}
	public record HitImmunityManager
	{
		public Dictionary<int, ushort> attackTracker = new Dictionary<int, ushort>();
		public void Register(int attackHashCode, ushort immunityTime)
		{
			attackTracker.Add(attackHashCode, immunityTime);
		}

		public bool HasRegistered(int attackHashCode)
		{
			return attackTracker.ContainsKey(attackHashCode);
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
			TotalDamage += effectiveDamage;
			TotalRate *= rate;
			LaunchType = launchType;

			return effectiveDamage;
		}
	}
}
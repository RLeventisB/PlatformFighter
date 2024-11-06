using Editor.Objects;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Entities;
using PlatformFighter.Entities.Actions;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;
using PlatformFighter.Rendering;
using PlatformFighter.Stages;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PlatformFighter
{
	public static class GameWorld
	{
		public static bool Paused;
		public static int IntroTimer, HitLagTimer = 0;
		public static Pool<Player> Players = new Pool<Player>(64);
		public static Stage CurrentStage;
		public static FrozenDictionary<Player, HitboxStateData> CollidingHitboxData;
		public static List<HitData> Hits = new List<HitData>();

		public static void StartGame(ushort stageId)
		{
			ResetWorld();

			// IntroTimer = 600;
			CurrentStage = InstanceManager.Stages.CreateInstance(stageId);
			CurrentStage.Load();
			TheGameState.PlayingMatch = true;
			Paused = false;
		}

		public static bool CreatePlayer(Vector2 position, ushort characterDefinitionId, ushort controllerId, out Player player)
		{
			if (!Players.Get(out player))
				return false;

			player.MovableObject.Center = position;
			player.CharacterData.SetDefinition(characterDefinitionId);
			player.CharacterData.ApplyDefaults(player);

			player.ControllerId = controllerId;

			return true;
		}

		public static void AfterPlayerCreation()
		{
			Dictionary<Player, HitboxStateData> data = new Dictionary<Player, HitboxStateData>();

			foreach (Player player in Players)
			{
				data.Add(player, new HitboxStateData(player.whoAmI));
			}

			CollidingHitboxData = data.ToFrozenDictionary();
		}

		public static void ResetWorld()
		{
			Players.Clear(true);
			CollidingHitboxData = null;
		}

		public static void UpdateWorld(GameTime gameTime)
		{
			CurrentStage.Update();

			foreach (HitboxStateData stateData in CollidingHitboxData.Values)
			{
				stateData.Clear();
			}

			Hits.Clear();

			foreach (Player player in Players)
			{
				player.Update();
			}

			foreach (Player player in Players)
			{
				player.PostUpdate();
			}

			foreach (Player player in Players)
			{
				if (CurrentStage.IsPlayerOnBlastZone(player))
				{
					player.Die();
				}
			}

			foreach (HitboxStateData stateData in CollidingHitboxData.Values)
			{
				foreach (HitboxStateData otherStateData in CollidingHitboxData.Values)
				{
					if (stateData.PlayerId == otherStateData.PlayerId)
						continue;

					if (stateData.HasCollided(otherStateData, out HitboxData usedHitbox, out HurtboxData hitHurtbox))
					{
						Player player = Players[stateData.PlayerId];
						player.ActionManager.HasThisActionCollided = true;

						Hits.Add(new HitData(stateData, player.ActionManager.FacingDirection, stateData.PlayerId, otherStateData.PlayerId, usedHitbox, hitHurtbox, stateData.GetAttackHashCode()));
					}
				}
			}

			foreach (HitData hit in Hits)
			{
				bool clash = Hits.Any(v => v.Target == hit.Owner);

				if (!clash)
				{
					Player target = Players[hit.Target];
					if (target.Health.Hit(hit))
						Players[hit.Owner].ActionManager.HasThisActionHit = true;
				}
			}

			if (IntroTimer > 0)
				IntroTimer--;

			Camera.Update();
		}

		public static void RenderWorld(GameTime gameTime)
		{
			Main.spriteBatch.Begin(SpriteSortMode.FrontToBack, Renderer.PixelBlendState, Renderer.PixelSamplerState, DepthStencilState.Default, RasterizerState.CullNone, transformMatrix: Camera.ViewMatrix * Renderer.windowMatrix);

			foreach (Player player in Players)
			{
				player.Draw();
			}

			foreach (HitboxStateData state in CollidingHitboxData.Values)
			{
				foreach (HitboxData hitbox in state.Hitboxes)
				{
					Main.spriteBatch.DrawRectangleCentered(hitbox.Rectangle, hitbox.Rotation);
				}

				foreach (HurtboxData hurtbox in state.Hurtboxes)
				{
					Main.spriteBatch.DrawRectangleCentered(hurtbox.Rectangle, hurtbox.Rotation, 1f, Color.Green, 1f);
				}
			}

			CurrentStage?.Draw();

			Main.spriteBatch.End();
		}

		public static void RegisterHitboxes(Player owner, HitboxAnimationObject[] variedBoxes, int frame)
		{
			CollidingHitboxData[owner].Register(owner.MovableObject.Center, owner.Scale, owner.Rotation, variedBoxes, frame, owner.ActionManager.FacingDirection, owner.ActionManager.ActionId);
		}
	}
	public struct HitboxStateData
	{
		public Player Player => GameWorld.Players[PlayerId];
		public ushort PlayerId, ActionId;
		public List<HurtboxData> Hurtboxes = new List<HurtboxData>();
		public List<HitboxData> Hitboxes = new List<HitboxData>();

		public HitboxStateData(ushort id)
		{
			PlayerId = id;
		}

		public void Register(Vector2 center, Vector2 scale, float rotation, HitboxAnimationObject[] variedBoxes, int frame, FacingDirection facingDirection, ushort actionId)
		{
			foreach (HitboxAnimationObject box in variedBoxes)
			{
				if (!box.IsOnFrame(frame))
					continue;

				switch (box.Type)
				{
					case HitboxType.Hitbox:
						Hitboxes.Add(new HitboxData(box, frame, center, scale, rotation, facingDirection));

						break;
					case HitboxType.Hurtbox:
						Hurtboxes.Add(new HurtboxData(box, frame, center, scale, rotation, facingDirection));

						break;
				}
			}

			ActionId = actionId;

			// Hitboxes.Sort(); // hitboxes have priority and this sorts them because of the comparator thingy, but, since the check that is literally 20 lines below exists, idk if this is needed
		}

		public bool HasCollided(HitboxStateData other, out HitboxData usedHitbox, out HurtboxData hitHurtbox)
		{
			usedHitbox = default;
			hitHurtbox = default;
			bool hit = false;

			foreach (HitboxData hitbox in Hitboxes)
			{
				foreach (HurtboxData hurtbox in other.Hurtboxes)
				{
					if (!Collision.AreRotatedRectanglesColliding(hurtbox.Rectangle, hurtbox.Rotation, hitbox.Rectangle, hitbox.Rotation))
						continue;

					if (hitbox.Priority < usedHitbox.Priority) // this selects the higher priority hitbox!!!
					{
						continue;
					}

					usedHitbox = hitbox;
					hitHurtbox = hurtbox;
					hit = true;
				}
			}

			return hit;
		}

		public Vector2 GetLaunchVector(HitboxData hitbox, HurtboxData hurtbox, float damage)
		{
			float angle = hitbox.GetLaunchAngle(hurtbox.Rectangle.Center);
			float potency = hitbox.LaunchPotency.GetValue(damage);
			Vector2 launchVector = new Vector2(potency, 0).Rotate(angle);

			if (hitbox.MovementInfluence != 0)
			{
				launchVector += Player.MovableObject.Velocity * hitbox.MovementInfluence;
			}

			return launchVector;
		}

		public int GetAttackHashCode() => (PlayerId << 16 | ActionId);

		public void Clear()
		{
			Hitboxes.Clear();
			Hurtboxes.Clear();
		}
	}
	public interface BoxData
	{
		public MovableObjectRectangle Rectangle { get; init; }
		public float Rotation { get; init; }
	}
	public struct HurtboxData : BoxData
	{
		public HurtboxData(HitboxAnimationObject hitboxObject, int frame, Vector2 center, Vector2 scale, float rotation, FacingDirection facingDirection)
		{
			Vector2 pos = hitboxObject.Position.Interpolate(frame).RotateRad(rotation) * scale;
			pos.X *= facingDirection == FacingDirection.Right ? 1 : -1f;

			Rectangle = new MovableObjectRectangle(center + pos, hitboxObject.Size.Interpolate(frame) * scale);
			Rotation = rotation;
		}

		public MovableObjectRectangle Rectangle { get; init; }
		public float Rotation { get; init; }
	}
	public struct HitboxData : BoxData, IComparable<HitboxData>
	{
		public HitboxData(HitboxAnimationObject hitboxObject, int frame, Vector2 center, Vector2 scale, float rotation, FacingDirection facingDirection)
		{
			Vector2 pos = hitboxObject.Position.Interpolate(frame).RotateRad(rotation) * scale;
			pos.X *= facingDirection == FacingDirection.Right ? 1 : -1f;
			Rectangle = new MovableObjectRectangle(center + pos, hitboxObject.Size.Interpolate(frame) * scale);
			Rotation = rotation;

			Damage = hitboxObject.Damage;
			Rate = hitboxObject.Rate;
			Type = hitboxObject.Type;
			LaunchType = hitboxObject.LaunchType;
			Conditions = hitboxObject.Conditions;
			MovementInfluence = hitboxObject.MovementInfluence;
			LaunchAngleData = hitboxObject.LaunchPoint != Vector2.Zero ? LaunchPoint + center : new Vector2(hitboxObject.LaunchAngle, float.NaN);
			Hitstun = new UshortScalableValue(hitboxObject.Hitstun, hitboxObject.MaxHitstun, hitboxObject.HitstunGrowth);
			ShieldStun = hitboxObject.ShieldStun;
			DuelGameLag = hitboxObject.DuelGameLag;
			AttackId = hitboxObject.AttackId;
			ImmunityAfterHit = hitboxObject.ImmunityAfterHit;
			Priority = hitboxObject.Priority;
			LaunchPotency = new FloatScalableValue(hitboxObject.LaunchPotency, hitboxObject.LaunchPotencyMax, hitboxObject.LaunchPotencyGrowth);
			ShieldLaunchAngle = hitboxObject.ShieldLaunchAngle;
			ShieldPotency = hitboxObject.ShieldPotency;
		}

		public MovableObjectRectangle Rectangle { get; init; }
		public float Rotation { get; init; }
		public float Damage { get; init; }
		public UshortScalableValue Hitstun;
		public ushort ShieldStun;
		public ushort DuelGameLag;
		public ushort AttackId;
		public ushort ImmunityAfterHit;
		public ushort Priority;
		public FloatScalableValue LaunchPotency;
		public float ShieldLaunchAngle;
		public float ShieldPotency;
		public float Rate;
		public float MovementInfluence;
		public Vector2 LaunchPoint;
		public HitboxType Type;
		public LaunchType LaunchType;
		public HitboxConditions Conditions;
		public Vector2 LaunchAngleData;
		public bool LaunchDataIsAngle => float.IsNaN(LaunchAngleData.Y);

		public float GetLaunchAngle(Vector2 otherCenter) => LaunchDataIsAngle ? LaunchAngleData.X : (otherCenter - LaunchAngleData).ToAngle();

		public int CompareTo(HitboxData other) => Priority.CompareTo(other.Priority);
	}
}
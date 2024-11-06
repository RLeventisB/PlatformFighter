using PlatformFighter.Rendering;

using System;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Entities.Actions
{
	public abstract class ActionBase<T> where T : Player
	{
		public ActionBase(T entity)
		{
			Entity = entity;
		}

		public T Entity { get; init; }
		public int Frame { get; set; }
		public abstract ActionTags Tags { get; }

		public virtual string ActionName => GetType().Name;

		public bool HasActionCollided => Entity.ActionManager.HasThisActionCollided;
		public bool HasActionHit => Entity.ActionManager.HasThisActionHit;

		public virtual void OnStart() { }

		public virtual void OnEnd() { }

		public virtual void Update()
		{
			Frame++;
		}

		/// <summary>
		/// Method that runs after Update, dedicated to adding hitboxes, if the action is changed while
		/// </summary>
		public virtual void AddHitboxes()
		{
		}

		public abstract void Draw();

		/// <summary>
		/// Shortcut for SetAction for the current Entity's ActionManager
		/// </summary>
		/// <param name="newAction"></param>
		public void ChangeTo(ActionBase<T> newAction)
		{
			Entity.ActionManager.SetAction(Unsafe.As<ActionBase<Player>>(newAction));
		}

		/// <summary>
		/// Method that runs before Update, if the action is changed while running the method, this method runs again for the
		/// new action
		/// </summary>
		/// <param name="queuedAction"></param>
		public virtual void ProcessQueue(QueuedActionList queuedAction)
		{
		}

		/// <summary>
		/// Check for when another action is being set
		/// </summary>
		/// <param name="action">The new action to be set</param>
		/// <returns>True if the current action accepts being changed to the new <paramref name="action"/></returns>
		public virtual bool CanChangeTo(ActionBase<Player> action)
		{
			if (action.GetType() == GetType())
			{
				Logger.LogMessage("Tried to change to another action of the same type.");

				return false;
			}

			return true;
		}

		/// <summary>
		/// Check for when the current action instance is being set as another player's action
		/// </summary>
		/// <param name="player">The player for the new action to be set to</param>
		/// <returns>True if the current action accepts being set as the player's action</returns>
		public virtual bool CanBeSetAsActive(Player player) => true;
	}
	public abstract class AnimationActionBase : ActionBase<Player>
	{
		public AnimationActionBase(Player entity, string animName, int duration) : base(entity)
		{
			AnimationData = AnimationRenderer.GetAnimation(animName);
			Duration = duration;
		}

		public AnimationActionBase(Player entity, string animName) : base(entity)
		{
			AnimationData = AnimationRenderer.GetAnimation(animName);
			Duration = AnimationData.LastFrame;
		}

		public virtual bool RunUpdateOnRecovery => true;
		public int Duration { get; internal set; }
		public override ActionTags Tags => ActionTags.None;
		public AnimationData AnimationData { get; internal set; }

		public override void AddHitboxes()
		{
			GameWorld.RegisterHitboxes(Entity, AnimationData.JsonData.hitboxObjects, Frame);
		}

		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, Frame, Entity.MovableObject.Center, Entity.GetScaleWithFacing, Entity.Rotation);
		}
	}
	public abstract class EndingActionToIdle : AnimationActionBase
	{
		public EndingActionToIdle(Player entity, string animName, int duration) : base(entity, animName, duration)
		{
		}

		public EndingActionToIdle(Player entity, string animName) : base(entity, animName)
		{
		}

		public override void Update()
		{
			if (Frame >= Duration)
			{
				OnTimeEnd();
			}

			base.Update();
		}

		public virtual void OnTimeEnd()
		{
			ChangeTo(Entity.CharacterData.Definition.ResolveIdleAction(Entity, Entity.Grounded));
		}
	}
	public abstract class StateAction : AnimationActionBase
	{
		public StateAction(Player entity, string animName, int duration) : base(entity, animName, duration)
		{
		}

		public StateAction(Player entity, string animName) : base(entity, animName)
		{
		}

		public abstract bool State { get; }
		public virtual int Frequency => 1;

		public override void Update()
		{
			if (State)
			{
				if (Frame < Duration)
					Frame += Frequency;
			}
			else
			{
				if (Frame == 0)
					OnTimeEnd();

				Frame -= Frequency;
			}
		}

		public virtual void OnTimeEnd()
		{
			ChangeTo(null);
		}
	}

	[Flags]
	public enum ActionTags : ushort
	{
		None = 0,
		NoFriction = 1,
		NoGravity = 2,
		Attack = 8,
		Shield = 16,
		Movement = 32,
		DontDrawFlipped = 64,
		Aerial = 128,
		Wall = 256,
		Crouch = 512,
		Shot = 1024,
		Special = 2048,
		Meter = 4096,
		Hit = 8192
	}
}
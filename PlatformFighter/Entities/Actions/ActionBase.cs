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

		public virtual bool RunUpdateOnRecovery => false;

		public virtual void UpdateOnRecovery()
		{
			
		}
		public virtual void OnStart() { }

		public virtual void OnEnd() { }

		public virtual void Update()
		{
			Frame++;
		}

		public abstract void Draw();

		public void ChangeTo(ActionBase<T> newAction)
		{
			Entity.ActionManager.SetAction(Unsafe.As<ActionBase<Player>>(newAction));
		}

		public virtual void ProcessQueue(QueuedActionList queuedAction)
		{
		}

		public virtual bool CanChangeTo(ActionBase<Player> action) => true;
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

		public virtual float FrameToDraw => Frame;
		public override bool RunUpdateOnRecovery => true;
		public int Duration { get; internal set; }
		public override ActionTags Tags => ActionTags.None;
		public AnimationData AnimationData { get; internal set; }

		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, (int)FrameToDraw, Entity.MovableObject.Center, Entity.GetScaleWithFacing, Entity.Rotation);
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
			ChangeTo(null);
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
		Meter = 4096
	}
}
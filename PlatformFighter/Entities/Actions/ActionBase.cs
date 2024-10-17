using Editor.Objects;

using PlatformFighter.Miscelaneous;
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
		public virtual bool OverrideActions => true;
		public abstract ActionTags Tags { get; }

		public virtual string ActionName => GetType().Name;

		public virtual void OnStart() { }

		public virtual void OnEnd() { }

		public virtual void Update()
		{
			Frame++;
		}

		public abstract void Draw();
		public void ChangeTo(ActionBase<T> newAction)
		{
			Entity.ActionManager.CurrentAction = Unsafe.As<ActionBase<Player>>(newAction);
		}
		
		public virtual void ProcessQueue(QueuedActionList queuedAction)
		{
			
		}
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
		public int Duration { get; internal set; }
		public override void Update()
		{
			if (Frame >= Duration)
			{
				OnTimeEnd();
			}
			base.Update();
		}
		public abstract void OnTimeEnd();
		public override void Draw()
		{
			AnimationRenderer.DrawJsonData(Main.spriteBatch, AnimationData.JsonData, (int)FrameToDraw, Entity.MovableObject.Center, Entity.GetScaleWithFacing);
		}
		public override ActionTags Tags => ActionTags.Sustain;
		public AnimationData AnimationData { get; internal set; }
	}
	public class EndingActionToIdle : AnimationActionBase
	{
		public EndingActionToIdle(Player entity, string animName, int duration) : base(entity, animName, duration)
		{
		}
		public EndingActionToIdle(Player entity, string animName) : base(entity, animName)
		{
		}
		public override void OnTimeEnd()
		{
			ChangeTo(null);
		}
	}
	
	[Flags]
	public enum ActionTags : ushort
	{
		Idle = 0,
		Start = 2,
		Sustain = 4,
		Attack = 8,
		Walk = 16,
		Dash = 32,
		Jump = 64,
		Aerial = 128,
		Wall = 256,
		Crouch = 512,
		Shot = 1024,
		Special = 2048,
		Meter = 4096
	}
}
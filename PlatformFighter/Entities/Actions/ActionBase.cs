using PlatformFighter.Miscelaneous;

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
		public abstract int Duration { get; }
		public abstract ActionTags Tags { get; }

		public virtual string ActionName => GetType().Name;

		public virtual void OnStart() { }

		public virtual void OnEnd() { }

		public virtual void Update()
		{
			if (Frame >= Duration)
			{
				OnTimeEnd();
			}

			Frame++;
		}

		public abstract void Draw();

		public virtual void OnTimeEnd()
		{
			Entity.PlayerActionManager.CurrentAction = null;
		}

		public void ChangeTo(ActionBase<T> newAction)
		{
			Entity.PlayerActionManager.CurrentAction = Unsafe.As<ActionBase<Player>>(newAction);
		}

		public virtual void ProcessQueue(ExposedList<QueuedAction> actionQueue)
		{
			
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
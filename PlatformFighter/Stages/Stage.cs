using PlatformFighter.Entities;

using System.Collections.Generic;

namespace PlatformFighter.Stages
{
	public abstract class Stage
	{
		public List<WorldObject> objects = new List<WorldObject>();

		public abstract void Load();

		public virtual void Update()
		{
			foreach (WorldObject worldObject in objects)
			{
				worldObject.Update();
			}
		}

		public virtual void Draw()
		{
			foreach (WorldObject worldObject in objects)
			{
				worldObject.Draw();
			}
		}

		public abstract void Unload();
	}
}
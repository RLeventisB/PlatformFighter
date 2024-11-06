using Microsoft.Xna.Framework;

using PlatformFighter.Entities;
using PlatformFighter.Physics;

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

		public abstract bool IsPlayerOnBlastZone(Player player);

		public abstract bool IsRectangleDespawnable(MovableObjectRectangle rectangle);

		public abstract void Unload();

		public Vector2 GetSpawnPosition(Player player, bool respawn = false)
		{
			if (respawn)
			{
				return new Vector2(0, -200);
			}

			return Vector2.Zero;
		}
	}
}
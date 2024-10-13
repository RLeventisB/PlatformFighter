using Microsoft.Xna.Framework;

using PlatformFighter.Physics;

namespace PlatformFighter.Stages
{
	public abstract class WorldObject
	{
		public WorldObject(Vector2 position)
		{
			MovableObject = new CompressedMovableObject(position, 0, 0);
		}

		public IMovableObject<float> MovableObject;

		public abstract void Update();

		public abstract void Draw();
	}
}
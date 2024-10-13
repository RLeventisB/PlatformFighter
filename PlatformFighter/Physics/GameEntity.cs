using Microsoft.Xna.Framework;

using PlatformFighter.Physics;

namespace PlatformFighter
{
	public abstract class GameEntity : PoolObject
	{
		public IMovableObject<float> MovableObject = new CompressedMovableObject();
		public float Rotation = 0;
		public Vector2 Scale = Vector2.One;

		public virtual void Update()
		{
		}

		public virtual void PostUpdate()
		{
		}

		public virtual void Draw()
		{
		}
	}
	public class MovementManager
	{
	}
}
using Microsoft.Xna.Framework;

using PlatformFighter.Entities;
using PlatformFighter.Physics;

namespace PlatformFighter.Stages
{
	public class DefaultStage : Stage
	{
		public override void Load()
		{
			objects.Add(new MainPlatform(Vector2.Zero));
		}

		public override void Unload()
		{
		}

		public class MainPlatform : WorldObject
		{
			public MainPlatform(Vector2 position) : base(position)
			{
				MovableObject.Rectangle.Inflate(400, 160);
				MovableObject.PositionY += 150;
			}

			public override void Update()
			{
			}

			public override void Draw()
			{
				Main.spriteBatch.Draw(Assets.Textures["DefaultPlatform"], MovableObject.Rectangle, null, Color.White);
			}
		}
	}
}
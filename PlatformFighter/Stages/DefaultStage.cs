using Microsoft.Xna.Framework;

using PlatformFighter.Entities;
using PlatformFighter.Physics;

namespace PlatformFighter.Stages
{
	public class DefaultStage : Stage
	{
		public readonly MovableObjectRectangle BlastZoneRectangle = MovableObjectRectangle.FromCenter(0, 0, VirtualWidth + 100, VirtualHeight + 100);

		public override void Load()
		{
			objects.Add(new MainPlatform(Vector2.Zero));
		}

		public override bool IsPlayerOnBlastZone(Player player) => IsRectangleDespawnable(player.MovableObject.Rectangle);

		public override bool IsRectangleDespawnable(MovableObjectRectangle rectangle) => !BlastZoneRectangle.Intersects(rectangle);

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
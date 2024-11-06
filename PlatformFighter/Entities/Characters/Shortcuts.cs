using Microsoft.Xna.Framework;

using PlatformFighter.Miscelaneous;
using PlatformFighter.Physics;

namespace PlatformFighter.Entities.Characters
{
	public static class Shortcuts
	{
		public static bool IsGrounded(Player player)
		{
			Direction collidedDirections = Collision.GetCollidingDirection(player.MovableObject, new Vector2(0, player.CharacterData.Definition.FallingGravity));

			return collidedDirections.HasFlag(Direction.Up);
		}
	}
}
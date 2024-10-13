using Microsoft.Xna.Framework;

namespace PlatformFighter.Entities
{
	public record Team(Color color, bool friendlyFire = false)
	{
		public static readonly Team Default = new Team(Color.White, true);
	}
}
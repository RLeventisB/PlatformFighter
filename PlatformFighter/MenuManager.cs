using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Menus;
using PlatformFighter.Rendering;

namespace PlatformFighter
{
	public static class MenuManager
	{
		public static GameMenu CurrentMenu;

		public static void Load(GameMenu menu)
		{
			CurrentMenu?.Unload();
			CurrentMenu = menu;
			CurrentMenu?.Load();
		}
		public static void Render(GameTime gameTime)
		{
			Main.spriteBatch.Begin(SpriteSortMode.FrontToBack, Renderer.PixelBlendState, Renderer.PixelSamplerState, null, RasterizerState.CullNone, transformMatrix: Renderer.windowMatrix);

			CurrentMenu?.Draw();
			
			Main.spriteBatch.End();
		}

		public static void Update(GameTime gameTime)
		{
			CurrentMenu?.Update();
		}
	}
	public interface GameMenu
	{
		public void Load();
		public void Unload();
		public void Draw();

		public void Update();

		public void UnloadAndSetBlank()
		{
			Unload();
			MenuManager.Load(null);
		}
	}
}
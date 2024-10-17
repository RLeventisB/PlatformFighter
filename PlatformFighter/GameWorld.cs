using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Entities;
using PlatformFighter.Menus;
using PlatformFighter.Rendering;
using PlatformFighter.Stages;

namespace PlatformFighter
{
	public static class GameWorld
	{
		public static int IntroTimer = 0;
		public static Pool<Player> Players = new Pool<Player>(64);
		public static Stage CurrentStage;

		public static void StartGame(ushort stageId)
		{
			ResetWorld();
			// IntroTimer = 600;
			CurrentStage = InstanceManager.Stages.CreateInstance(stageId);
			CurrentStage.Load();
			TheGameState.PlayingMatch = true;
		}

		public static bool CreatePlayer(Vector2 position, ushort characterDefinitionId, ushort controllerId, out Player player)
		{
			if (!Players.Get(out player))
				return false;

			player.MovableObject.Center = position;
			player.CharacterData.SetDefinition(characterDefinitionId);
			player.CharacterData.ApplyDefaults(player);

			player.ControllerId = controllerId;
			return true;
		}

		public static void ResetWorld()
		{
			Players.Clear(true);
		}

		public static void UpdateWorld(GameTime gameTime)
		{
			CurrentStage.Update();
			foreach (Player player in Players)
			{
				player.Update();
			}

			foreach (Player player in Players)
			{
				player.PostUpdate();
			}

			if(IntroTimer > 0)
				IntroTimer--;
			Camera.Update();
		}

		public static void RenderWorld(GameTime gameTime)
		{
			Main.spriteBatch.Begin(SpriteSortMode.FrontToBack, Renderer.PixelBlendState, Renderer.PixelSamplerState, DepthStencilState.Default, RasterizerState.CullNone, transformMatrix: Camera.ViewMatrix * Renderer.windowMatrix);

			foreach (Player player in Players)
			{
				player.Draw();
			}
			
			CurrentStage?.Draw();

			Main.spriteBatch.End();
		}
	}
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Entities;
using PlatformFighter.Rendering;
using PlatformFighter.Stages;

namespace PlatformFighter
{
	public static class GameWorld
	{
		public static Pool<Player> Players = new Pool<Player>(64);
		public static Stage CurrentStage;

		public static void StartGame(ushort stageId)
		{
			ResetWorld();
			CurrentStage = InstanceManager.Stages.CreateInstance(stageId);
			CurrentStage.Load();
			TheGameState.PlayingMatch = true;
		}

		public static bool CreatePlayer(Vector2 position, ushort characterDefinitionId, int? gamepadIndex, out Player player)
		{
			if (!Players.Get(out player))
				return false;

			player.MovableObject.Center = position;
			player.CharacterData.SetDefinition(characterDefinitionId);
			player.CharacterData.ApplyDefaults(player);

			if (gamepadIndex is null)
			{
				player.ControllerId = PlayerController.RegisterPlayerController(new KeyboardDataReceiver());
			}
			else
			{
				player.ControllerId = PlayerController.RegisterPlayerController(new GamepadDataReceiver(gamepadIndex.Value));
			}
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
			
			Camera.Update();
		}

		public static void RenderWorld(GameTime gameTime)
		{
			Main.spriteBatch.Begin(SpriteSortMode.FrontToBack, Renderer.PixelBlendState, Renderer.PixelSamplerState, transformMatrix: Camera.ViewMatrix);

			foreach (Player player in Players)
			{
				player.Draw();
			}
			
			CurrentStage?.Draw();

			Main.spriteBatch.End();
		}
	}
}
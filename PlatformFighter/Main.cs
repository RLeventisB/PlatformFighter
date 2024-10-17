global using static PlatformFighter.Miscelaneous.Constants;
global using static PlatformFighter.InstanceManager;

using Editor.Objects;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Menus;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;
using PlatformFighter.Stages;

using System;
using System.Collections.Frozen;
using System.Diagnostics;

namespace PlatformFighter
{
	public class Main : Game
	{
		public static Color ClearColor = new Color(0, 0, 0, 0);

		public static GameRandom mainRandom = new GameRandom(), cosmeticRandom = new GameRandom();
		public static Main instance;
		public static GraphicsDeviceManager graphics;
		public static SpriteBatch spriteBatch;

		internal static FrozenDictionary<string, ReadonlyVector> staticVector2s;
		internal static FrozenDictionary<string, Rectangle> staticRectangles;

		public static RenderTarget2D BackgroundTarget, WorldTarget, GUITarget, MergedTarget, PauseTarget;

		public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

		public static bool GamePaused;

		public Main()
		{
			instance = this;
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		public static GraphicsDevice Graphics => instance.GraphicsDevice;

		protected override void Initialize()
		{
			Window.Title = cosmeticRandom.NextBoolean(0.01) ? "smash Touhalla 12.3 ultimate - La yuca viene a por ti" : "Platform Fighter debug version";
			Renderer._maxRes.X = GraphicsDevice.DisplayMode.Width;
			Renderer._maxRes.Y = GraphicsDevice.DisplayMode.Height;
			Renderer.PixelSamplerState = SamplerState.PointClamp;
			Renderer.PixelBlendState = new BlendState
			{
				ColorSourceBlend = Blend.SourceAlpha,
				AlphaSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.InverseSourceAlpha,
				AlphaDestinationBlend = Blend.InverseSourceAlpha
			};
			
			GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

			// GraphicsDevice.Viewport.MinDepth = -1000f;
			// GraphicsDevice.Viewport.MaxDepth = 1000f;

			KeyframeableValue.CacheValueOnInterpolate = false;
			TargetElapsedTime = TimeSpan.FromSeconds(1d / 120);
			IsFixedTimeStep = true;
			graphics.SynchronizeWithVerticalRetrace = false;
			InitializeRenderTargets();

#if DEBUG
			if (Debugger.IsAttached)
			{
				"Detectado Debugger!!!".Log();
			}
#endif
			InstanceManager.Initialize();

			MenuManager.Load(GameMenus.CreateInstance<SelectMenu>());

			base.Initialize();
		}

		public void InitializeRenderTargets()
		{
			if (BackgroundTarget is null || BackgroundTarget.IsDisposed)
				BackgroundTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
				{
					Name = "BackgroundTarget"
				};

			if (WorldTarget is null || WorldTarget.IsDisposed)
				WorldTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
				{
					Name = "WorldTarget"
				};

			if (GUITarget is null || GUITarget.IsDisposed)
				GUITarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
				{
					Name = "ScreenTarget"
				};

			if (MergedTarget is null || MergedTarget.IsDisposed)
				MergedTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
				{
					Name = "MergedTarget"
				};

			if (PauseTarget is null || PauseTarget.IsDisposed)
				PauseTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents)
				{
					Name = "PauseTarget"
				};
		}

		protected override void LoadContent()
		{
			Assets.LoadAssets();
			ShadersInfo.Initialize();
			spriteBatch = new SpriteBatch(GraphicsDevice);
			TextRenderer.TextureInfo = Assets.Textures["PixelFont"];
			TextRenderer.Initialize();
			GameText.Initialize();
		}

		protected override void Update(GameTime gameTime)
		{
			Renderer.Update(gameTime);
			Input.Update();

			if (TheGameState.PlayingMatch)
			{
				GameWorld.UpdateWorld(gameTime);
			}
			else
			{
				MenuManager.Update(gameTime);
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			ClearAllTargets();
			GraphicsDevice.Clear(Color.CornflowerBlue);
			if (TheGameState.PlayingMatch)
			{
				// GraphicsDevice.SetRenderTarget(WorldTarget);

				GameWorld.RenderWorld(gameTime);
			}
			else
			{
				// GraphicsDevice.SetRenderTarget(GUITarget);
				
				MenuManager.Render(gameTime);
			}
			/*
			GraphicsDevice.SetRenderTarget(MergedTarget);
			GraphicsDevice.Clear(ClearColor);

			// DrawRenderTarget(ref BackgroundTarget, ShaderType.Background);
			DrawRenderTarget(ref WorldTarget, ShaderType.Screen);
			DrawRenderTarget(ref GUITarget, ShaderType.Menu);

			GraphicsDevice.SetRenderTarget(null);
			GraphicsDevice.Clear(Color.CornflowerBlue);
			GraphicsDevice.BlendState = BlendState.Additive;
			spriteBatch.Begin(SpriteSortMode.Immediate, Renderer.PixelBlendState, Renderer.PixelSamplerState);
			Graphics.Textures[0] = MergedTarget;

			foreach (ShadersInfo.ShaderData shader in ShadersInfo.shadersOrdered[ShaderType.Merged])
			{
				if (shader.Progress > 0)
					continue;

				shader.Effect.Parameters["screenTexture"]?.SetValue(MergedTarget);
				shader.Apply();
				spriteBatch.Draw(MergedTarget, Renderer.ScreenRectangle, Color.White);
			}

			if (Renderer.MaintainAspectRatio)
			{
				GraphicsDevice.Clear(Color.Black);
				spriteBatch.Draw(MergedTarget, Renderer.WindowAspectRatioRectangle, Color.White);
			}
			else
			{
				spriteBatch.Draw(MergedTarget, Renderer.ScreenRectangle, Color.White);
			}

			spriteBatch.End();
			*/
			base.Draw(gameTime);
		}
		public void ClearAllTargets()
		{
			GraphicsDevice.SetRenderTarget(BackgroundTarget);
			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(WorldTarget);
			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(GUITarget);
			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(MergedTarget);
			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(PauseTarget);
			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(null);
		}

		public static void DrawRenderTarget(ref RenderTarget2D renderTarget, ShaderType shaderType)
		{
			spriteBatch.Begin(SpriteSortMode.Immediate, Renderer.PixelBlendState, Renderer.PixelSamplerState);
			Graphics.Textures[0] = renderTarget;

			foreach (ShadersInfo.ShaderData shader in ShadersInfo.shadersOrdered[shaderType])
			{
				if (shader.Progress == 0)
					continue;

				shader.Effect.Parameters["screenTexture"]?.SetValue(renderTarget);
				shader.Apply();
			}

			spriteBatch.Draw(renderTarget, Vector2.Zero, null, Color.White);
			spriteBatch.End();
		}
	}
}
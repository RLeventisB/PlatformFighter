global using static PlatformFighter.Miscelaneous.Constants;
global using static PlatformFighter.InstanceManager;

using Editor.Objects;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

namespace PlatformFighter
{
    public class Main : Game
    {
        public static Color ClearColor = new Color(0, 0, 0, 0);
        
        public static GameRandom mainRandom = new GameRandom(), cosmeticRandom = new GameRandom();
        public static Main instance;
        public static GraphicsDevice Graphics => instance.GraphicsDevice;
        public static GraphicsDeviceManager graphics;
        public static SpriteBatch spriteBatch;

        internal static FrozenDictionary<string, ReadonlyVector> staticVector2s;
        internal static FrozenDictionary<string, Rectangle> staticRectangles;

        public static RenderTarget2D BackgroundTarget, WorldTarget, GUITarget, MergedTarget, PauseTarget;

        public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static bool GamePaused;
        private int selectedAnimation = 1;

        public Main()
        {
            instance = this;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

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
#pragma warning disable
            InstanceManager.Initialize();
#pragma warning restore
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

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            Renderer.Update(gameTime);
            Input.Update();
            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.IsKeyPressed(Keys.Escape))
                Exit();

            if (Keyboard.IsKeyPressedWithDelay(Keys.Left))
            {
                selectedAnimation--;

                if (selectedAnimation < 0)
                {
                    selectedAnimation = AnimationRenderer.loadedAnimations.Count - 1;
                }
            }
            if (Keyboard.IsKeyPressedWithDelay(Keys.Right))
            {
                selectedAnimation++;

                if (selectedAnimation >= AnimationRenderer.loadedAnimations.Count)
                {
                    selectedAnimation = 0;
                }
            }
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // if (TheGameState.PlayingMatch)
            {
                RenderWorld();
            }
            // else
            {
                // RenderGui();
            }
            
            GraphicsDevice.SetRenderTarget(MergedTarget);
            GraphicsDevice.Clear(ClearColor);
            
            // DrawRenderTarget(ref BackgroundTarget, ShaderType.Background);
            DrawRenderTarget(ref WorldTarget, ShaderType.Screen);
            // DrawRenderTarget(ref GUITarget, ShaderType.Menu);

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

            base.Draw(gameTime);
        }

        private void RenderGui()
        {
            
        }

        public void RenderWorld()
        {
            GraphicsDevice.SetRenderTarget(WorldTarget);
            
            GraphicsDevice.Clear(ClearColor);
            Matrix matrix = Matrix.CreateScale(Renderer.Resolution.X / VirtualWidth,  Renderer.Resolution.Y / VirtualHeight, 1) * Matrix.CreateTranslation(VirtualWidth / 2, VirtualHeight / 2, 0);
            spriteBatch.Begin(SpriteSortMode.Deferred, Renderer.PixelBlendState, Renderer.PixelSamplerState, transformMatrix:matrix);

            ImmutableArray<AnimationData> loadedAnimations = AnimationRenderer.loadedAnimations.Values;
            AnimationData data = loadedAnimations[selectedAnimation];
            AnimationRenderer.DrawJsonData(spriteBatch, data.JsonData, (int)(Renderer.gameTime.TotalGameTime.TotalSeconds / Renderer.GameTimeDelta) % data.LastFrame, Vector2.Zero);

            spriteBatch.End();
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
using Editor.Objects;
using ExtraProcessors.GameTexture;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PlatformFighter.Rendering
{
    public static class Renderer
    {
        public static class FPSCounter
        {
            private static bool _count;
            internal static ushort _fps;
            public static readonly Stopwatch stopwatch = new Stopwatch();
            private static readonly Queue<ushort> storedFps = new Queue<ushort>(10);
            public static ushort FPS => storedFps.Count > 0 ? storedFps.Last() : (ushort)0;
            public static ushort MaxFPS => storedFps.Count > 0 ? storedFps.MaxBy(v => v) : (ushort)0;
            public static ushort MinFPS => storedFps.Count > 0 ? storedFps.MinBy(v => v) : (ushort)0;
            public static bool Count
            {
                get => _count;
                set
                {
                    _count = value;
                    if (_count)
                    {
                        stopwatch.Start();
                    }
                    else
                    {
                        stopwatch.Reset();
                    }
                }
            }
            public static void Update()
            {
                _fps++;
                if (stopwatch.ElapsedMilliseconds >= 1000)
                {
                    stopwatch.Restart();
                    if (storedFps.Count == 10)
                        storedFps.Dequeue();
                    storedFps.Enqueue(_fps);
                    _fps = 0;
                }
            }
        }
        public static Point[] supportedResolutions = Main.Graphics.Adapter.SupportedDisplayModes.Select(v => new Point(v.Width, v.Height)).Append(VirtualResolution).ToArray();
        private static Vector2 _resolution = Vector2.Zero;
        internal static Vector2 _maxRes;
        private static bool? _focusToggle;
        private static WindowType _windowType = WindowType.Fullscreen;
        public static Vector2 MaximumResolution => _maxRes;
        public static Vector2 Resolution
        {
            get => _resolution;
            set
            {
#if DESKTOPGL
                if (_resolution != value)
                {
                    ChangeResolution(value.ToPoint());
                    _resolution = value;
                    needApply = true;
                }
#endif
            }
        }
        public static bool? FocusToggle => _focusToggle;
        public static float WorldTimeWarp { get; set; } = 1;
#if DESKTOPGL
        public static WindowType WindowType
        {
            get => _windowType;
            set
            {
#if DESKTOPGL
                SetWindowType(value);
#endif
            }
        }
#endif
        public const float GameTimeDelta = 1 / 120f;
        public static bool MaintainAspectRatio { get; set; }
        public static Rectangle WindowAspectRatioRectangle => _windowAspectRatioRect;
        private static Rectangle _windowAspectRatioRect;
        public static Matrix windowMatrix = Matrix.Identity, InvWindowMatrix = Matrix.Identity;
        public static GameTime gameTime;
        public static bool hasFocus, needApply, hasSetResolutionWithoutResize = true;
        public static Vector2 MouseWorld = Vector2.Zero, resolutionScale = Vector2.One;
        public static Vector2? PreFullscreenResolution;
        public static SamplerState PixelSamplerState;
        public static BlendState PixelBlendState;
        public static Rectangle ScreenRectangle = new Rectangle(0, 0, 0, 0);
        public static Viewport NormalViewport;
        public static readonly Action OnWindowTypeChanged;
        static Renderer()
        {
#if DESKTOPGL
            SdlGamePlatform.OnEventDictionary.GetReference(Sdl.EventType.WindowEvent).BeforeEvent += (Sdl.Event evt) =>
            {
                if (evt.Type == Sdl.EventType.WindowEvent)
                {
                    if (evt.Window.EventID == Sdl.Window.EventId.Maximized)
                    {
                        _windowType = WindowType.Maximized;
                        OnWindowTypeChanged?.Invoke();
                    }
                    if (evt.Window.EventID == Sdl.Window.EventId.Minimized || evt.Window.EventID == Sdl.Window.EventId.Restored)
                    {
                        _windowType = WindowType.Windowed;
                        OnWindowTypeChanged?.Invoke();
                    }
                    if (evt.Window.EventID == Sdl.Window.EventId.FocusLost)
                    {
                        _focusToggle = false;
                    }
                    if (evt.Window.EventID == Sdl.Window.EventId.FocusGained)
                    {
                        _focusToggle = true;
                    }
                }
            };
#endif
        }
        public static readonly Action<Vector2> OnResolutionChanged = delegate(Vector2 setRes)
        {
            NormalViewport = Main.Graphics.Viewport;
            Resolution = setRes;
            resolutionScale = setRes / VirtualResolution;
            ScreenRectangle.Width = (int)setRes.X;
            ScreenRectangle.Height = (int)setRes.Y;
            const float aspectRatio = (float)VirtualWidth / VirtualHeight; // debes er un valor mayor a 1 :(
            float currentAspectRatio = setRes.X / setRes.Y;
            currentAspectRatio /= aspectRatio;
            float xRatio = VirtualWidth / setRes.X;
            float yRatio = VirtualHeight / setRes.Y;
            if (xRatio < yRatio) // muy bien eres normal 
            {
                _windowAspectRatioRect.Width = (int)(setRes.X / currentAspectRatio);
                _windowAspectRatioRect.Height = (int)setRes.Y;

                _windowAspectRatioRect.X = (int)(setRes.X / 2 - (_windowAspectRatioRect.Width >> 1));
                _windowAspectRatioRect.Y = 0;
            }
            else // en el 0% donde el jugador tiene la ventana en vertical (algo como 1080x1920 lol)
            {
                _windowAspectRatioRect.Width = (int)setRes.X;
                _windowAspectRatioRect.Height = (int)(setRes.Y * currentAspectRatio);

                _windowAspectRatioRect.X = 0;
                _windowAspectRatioRect.Y = (int)(setRes.Y / 2 - (_windowAspectRatioRect.Height >> 1));
            }
            if (MaintainAspectRatio)
            {
                if (_windowAspectRatioRect.Y != 0)
                {
                    resolutionScale.X = setRes.X / VirtualWidth;
                    resolutionScale.Y = _windowAspectRatioRect.Height / (float)VirtualHeight;
                }
                else
                {
                    resolutionScale.X = _windowAspectRatioRect.Width / (float)VirtualWidth;
                    resolutionScale.Y = setRes.Y / VirtualHeight;
                }
                Matrix scale = Matrix.CreateScale(resolutionScale.X, resolutionScale.Y, 1);
                Matrix translation = Matrix.CreateTranslation(_windowAspectRatioRect.X / resolutionScale.X, _windowAspectRatioRect.Y / resolutionScale.Y, 0);
                windowMatrix = translation * scale;
                InvWindowMatrix = Matrix.Invert(windowMatrix);
            }
            else
            {
                windowMatrix = Matrix.CreateScale(resolutionScale.X, resolutionScale.Y, 1);
                InvWindowMatrix = Matrix.Invert(windowMatrix);
            }
        };
        public static void UpdateEssentials()
        {
            _focusToggle = null;
            Vector2 curRes = Main.graphics.GraphicsDevice.Viewport.Bounds.Size();
            MouseWorld = Input.MousePosition + Camera.Position - VirtualMidResolution;
            if (hasSetResolutionWithoutResize)
            {
                OnResolutionChanged(Resolution);
                hasSetResolutionWithoutResize = false;
            }
            else if (curRes != Resolution)
            {
                OnResolutionChanged(curRes);
                hasSetResolutionWithoutResize = false;
            }
        }
        public static void Update(GameTime gameTime)
        {
            hasFocus = Main.instance.IsActive;
            Renderer.gameTime = gameTime;

            FPSCounter.Update();
            UpdateEssentials();
            if (needApply)
            {
                Main.graphics.ApplyChanges();
                needApply = false;
            }
        }
        [Conditional("DESKTOPGL")]
        public static void SetWindowType(WindowType type)
        {
#if DESKTOPGL
            if (_windowType != type)
            {
                nint windowHandle = Main.instance.Window.Handle;

                switch (type)
                {
                    case WindowType.Windowed:
                        if (_windowType == WindowType.Maximized)
                        {
                            Sdl.Window.RestoreWindow(windowHandle);
                        }
                        if (PreFullscreenResolution.HasValue)
                            Resolution = PreFullscreenResolution.Value;
                        PreFullscreenResolution = null;
                        Sdl.Window.SetFullscreen(windowHandle, Sdl.Window.State.Minimized);
                        break;
                    case WindowType.Maximized:
                        if (!PreFullscreenResolution.HasValue)
                            PreFullscreenResolution = Resolution;
                        Sdl.Window.MaximizeWindow(windowHandle);
                        //Sdl.Window.SetPosition(windowHandle, 0, 25);
                        Sdl.Window.SetFullscreen(windowHandle, Sdl.Window.State.Maximized);
                        break;
                    case WindowType.Fullscreen:
                        if (!PreFullscreenResolution.HasValue)
                            PreFullscreenResolution = Resolution;
                        Sdl.Window.SetFullscreen(windowHandle, Sdl.Window.State.Fullscreen);
                        break;
                    case WindowType.BorderlessFullscreen:
                        if (!PreFullscreenResolution.HasValue)
                            PreFullscreenResolution = Resolution;
                        Sdl.Window.SetFullscreen(windowHandle, Sdl.Window.State.FullscreenDesktop);
                        Main.graphics.shouldApplyChanges = true;
                        //("Error activating borderless fullscreen: " + Sdl.GetError());
                        break;
                }
                _windowType = type;
                OnWindowTypeChanged?.Invoke();
                Main.graphics.ApplyChanges();
            }
#endif
        }
        [Conditional("DESKTOPGL")]
        public static void ChangeResolution(Point res)
        {
            Main.graphics.PreferredBackBufferWidth = res.X;
            Main.graphics.PreferredBackBufferHeight = res.Y;
            hasSetResolutionWithoutResize = true;
        }
    }
    public enum WindowType : byte
    {
        Null, Windowed, Maximized, Fullscreen, BorderlessFullscreen
    }
    public class AnimationRenderer
    {
        public static Dictionary<TextureFrame, IAsset<GameTexture>> jsonDataTextureDictionary = new Dictionary<TextureFrame, IAsset<GameTexture>>();
        public static FrozenDictionary<string, JsonData> loadedAnimations;
        public static JsonData GetAnimation(string name)
        {
            return loadedAnimations[name];
        }
        public static bool GetAnimation(string name, out JsonData data)
        {
            return loadedAnimations.TryGetValue(name, out data);
        }
        public static void DrawJsonData(SpriteBatch spriteBatch, JsonData data, int frame, Vector2 center, Vector2? scale = null)
        {
            Vector2 finalScale = scale ?? Vector2.One;

            foreach(var graphicObject in data.graphicObjects)
            {
				Color color = new Color(1f, 1f, 1f, graphicObject.Transparency.Interpolate(frame));

				if (color.A == 0)
					continue;

				Vector2 position = center + graphicObject.Position.Interpolate(frame) * finalScale;
				int frameIndex = graphicObject.FrameIndex.Interpolate(frame);
				float rotation = graphicObject.Rotation.Interpolate(frame);
				Vector2 localScale = graphicObject.Scale.Interpolate(frame) * finalScale;
				SpriteEffects effects = SpriteEffects.None;

				if (localScale.X < 0)
				{
					localScale.X = -localScale.X;
					effects |= SpriteEffects.FlipHorizontally;
				}

				if (localScale.Y < 0)
				{
					localScale.Y = -localScale.Y;
					effects |= SpriteEffects.FlipVertically;
				}

				TextureFrame texture = ResolveTexture(data, graphicObject.TextureName);
				int framesX = texture.Width / texture.FrameSize.X;
				if (framesX == 0)
					framesX = 1;

				int x = frameIndex % framesX;
				int y = frameIndex / framesX;

				Rectangle sourceRect = new Rectangle(texture.FramePosition.X + x * texture.FrameSize.X, texture.FramePosition.Y + y * texture.FrameSize.Y,
					texture.FrameSize.X, texture.FrameSize.Y);

				spriteBatch.Draw(texture, position, sourceRect, color,
					rotation, texture.Pivot,
					localScale, effects, graphicObject.ZIndex.CachedValue);
            }
        }
        public static TextureFrame ResolveTexture(JsonData data, string name)
        {
            return data.textures.First(v => v.Name == name);
        }
        public static void LoadAnimations(string contentPath)
        {
            Dictionary<string, JsonData> loadedAnimations = new Dictionary<string, JsonData>();
            foreach(var file in Directory.GetFiles(Path.Combine(contentPath, "Animations"), "*.anim"))
            {
                try
                {
                    JsonData data = JsonData.LoadFromPath(file);
                    data.Fixup();

                    loadedAnimations.Add(Path.GetFileNameWithoutExtension(file), data);

                    foreach(var texture in data.textures)
                    {
                        jsonDataTextureDictionary.Add(texture, Assets.Textures.dictionary.Values.First(v => v.FilePath == texture.Path));
                    }
                }
                catch(Exception e)
                {
                    Logger.LogMessage(e);
                }
            }
        }
    }
}
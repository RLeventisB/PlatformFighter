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
		public const float GameTimeDelta = 1 / 120f;
		public static Point[] supportedResolutions = Main.Graphics.Adapter.SupportedDisplayModes.Select(v => new Point(v.Width, v.Height)).Append(VirtualResolution).ToArray();
		private static Vector2 _resolution = Vector2.Zero;
		internal static Vector2 _maxRes;
		private static WindowType _windowType = WindowType.Fullscreen;
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
				windowMatrix = Matrix.Identity * Matrix.CreateScale(resolutionScale.X, resolutionScale.Y, 1);
				InvWindowMatrix = Matrix.Invert(windowMatrix);
			}
		};

		static Renderer()
		{
#if DESKTOPGL
			SdlGamePlatform.OnEventDictionary.GetReference(Sdl.EventType.WindowEvent).BeforeEvent += evt =>
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
						FocusToggle = false;
					}

					if (evt.Window.EventID == Sdl.Window.EventId.FocusGained)
					{
						FocusToggle = true;
					}
				}
			};
#endif
		}

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
		public static bool? FocusToggle { get; private set; }
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
		public static bool MaintainAspectRatio { get; set; }
		public static Rectangle WindowAspectRatioRectangle => _windowAspectRatioRect;

		public static void UpdateEssentials()
		{
			FocusToggle = null;
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
	}
	public enum WindowType : byte
	{
		Null, Windowed, Maximized, Fullscreen, BorderlessFullscreen
	}
	public class AnimationRenderer
	{
		public static FrozenDictionary<string, AnimationData> loadedAnimations;

		public static AnimationData GetAnimation(string name) => loadedAnimations[name];

		public static bool GetAnimation(string name, out AnimationData data) => loadedAnimations.TryGetValue(name, out data);

		public static void DrawJsonData(SpriteBatch spriteBatch, JsonData data, int frame, Vector2 center, Vector2? scale = null, float rotation = 0)
		{
			Vector2 finalScale = scale ?? Vector2.One;
			bool flipped = scale.HasValue && scale.Value.X < 0;

			foreach (TextureAnimationObject graphicObject in data.graphicObjects)
			{
				Color color = new Color(1f, 1f, 1f, graphicObject.Transparency.Interpolate(frame));

				if (color.A == 0)
					continue;

				Vector2 position = center + graphicObject.Position.Interpolate(frame).RotateRad(rotation) * finalScale;
				int frameIndex = graphicObject.FrameIndex.Interpolate(frame);
				float localRotation = graphicObject.Rotation.Interpolate(frame) + rotation;

				if (flipped)
				{
					localRotation = -localRotation;
				}

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

				TextureFrame textureFrame = ResolveTexture(data, graphicObject.TextureName);
				GameTexture texture = TextureFrameManager.GetTexture(textureFrame.TextureId);
				int framesX = texture.Width / textureFrame.FrameSize.X;
				if (framesX == 0)
					framesX = 1;

				int x = frameIndex % framesX;
				int y = frameIndex / framesX;
				Rectangle sourceRect = new Rectangle(textureFrame.FramePosition.X + x * textureFrame.FrameSize.X, textureFrame.FramePosition.Y + y * textureFrame.FrameSize.Y,
					textureFrame.FrameSize.X, textureFrame.FrameSize.Y);

				Vector2 pivot = textureFrame.Pivot;

				if (flipped)
				{
					pivot.X = textureFrame.FrameSize.X - pivot.X;
				}

				spriteBatch.Draw(texture, position, sourceRect, color,
					localRotation, pivot,
					localScale, effects, graphicObject.ZIndex.CachedValue);
			}
		}

		public static TextureFrame ResolveTexture(JsonData data, string name)
		{
			return data.textures.First(v => v.Name == name);
		}

		public static void LoadAnimations(string contentPath)
		{
			Dictionary<string, AnimationData> loadedAnimations = new Dictionary<string, AnimationData>();

			foreach (string file in Directory.GetFiles(Path.Combine(contentPath, "Animations"), "*.anim"))
			{
				try
				{
					JsonData data = JsonData.LoadFromPath(file);
					data.Fixup();

					loadedAnimations.Add(Path.GetFileNameWithoutExtension(file), new AnimationData(data));

					foreach (TextureAnimationObject graphicObject in data.graphicObjects)
					{
						RemoveInvalidLinks(graphicObject);
					}

					foreach (HitboxAnimationObject hitboxObject in data.hitboxObjects)
					{
						RemoveInvalidLinks(hitboxObject);
					}

					foreach (TextureFrame texture in data.textures)
					{
						texture.TextureId = TextureFrameManager.GetId(texture.Path);
					}
				}
				catch (Exception e)
				{
					Logger.LogMessage(e.ToString());
				}
			}

			AnimationRenderer.loadedAnimations = loadedAnimations.ToFrozenDictionary();
		}

		public static void RemoveInvalidLinks(IAnimationObject animationObject)
		{
			foreach (KeyframeableValue value in animationObject.EnumerateKeyframeableValues())
			{
				for (int index = 0; index < value.links.Count; index++)
				{
					KeyframeLink link = value.links[index];
					link.SanitizeValues();
					List<int> frames = link.Frames.ToList();
					frames.RemoveAll(v => !value.HasKeyframeAtFrame(v));
					link = new KeyframeLink(link.ContainingValue, frames);

					if (link.Count >= 2)
						continue;

					value.links.RemoveAt(index);
					index--;
				}
			}
		}

		public static class TextureFrameManager
		{
			private static int _id;
			private static readonly Dictionary<string, nint> idMap = new Dictionary<string, nint>();
			private static readonly Dictionary<nint, string> reverseIdMap = new Dictionary<nint, string>();

			public static nint GetId(string texturePath)
			{
				string key = Path.GetFileNameWithoutExtension(texturePath);

				if (!Assets.Textures.ContainsKey(key))
					throw new NotSupportedException();

				if (idMap.TryGetValue(key, out nint idToReturn))
				{
					return idToReturn;
				}

				idToReturn = _id;

				idMap.Add(key, idToReturn);
				reverseIdMap.Add(idToReturn, key);
				_id++;

				return idToReturn;
			}

			public static GameTexture GetTexture(nint id) => Assets.Textures[reverseIdMap[id]];
		}
	}
	public record AnimationData(JsonData JsonData, int LastFrame)
	{
		public AnimationData(JsonData jsonData) : this(jsonData, -1)
		{
			int lastFrame = 1;

			foreach (TextureAnimationObject graphicObject in jsonData.graphicObjects)
			{
				foreach (KeyframeableValue value in graphicObject.EnumerateKeyframeableValues())
				{
					IOrderedEnumerable<int> orderedFrames = value.keyframes.Select(v => v.Frame).OrderByDescending(v => v);

					if (!orderedFrames.Any())
						continue;

					int lastLocalFrame = orderedFrames.First();
					if (lastFrame < lastLocalFrame)
						lastFrame = lastLocalFrame;
				}
			}

			foreach (HitboxAnimationObject hitboxObject in jsonData.hitboxObjects)
			{
				foreach (KeyframeableValue value in hitboxObject.EnumerateKeyframeableValues())
				{
					IOrderedEnumerable<int> orderedFrames = value.keyframes.Select(v => v.Frame).OrderByDescending(v => v);

					if (!orderedFrames.Any())
						continue;

					int lastLocalFrame = orderedFrames.First();
					if (lastFrame < lastLocalFrame)
						lastFrame = lastLocalFrame;
				}
			}

			LastFrame = lastFrame;
		}
	}
}
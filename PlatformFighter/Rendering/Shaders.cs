using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Rendering
{
    public static class ShadersInfo
    {
        public delegate void ShaderUpdateDelegate(ref ShaderData data);
        public static FrozenDictionary<string, ShaderData> shaders;
        public static FrozenDictionary<ShaderType, ShaderData[]> shadersOrdered;
        public static void Initialize()
        {
            Dictionary<string, ShaderData> shadersLocal = new Dictionary<string, ShaderData>();
            Dictionary<ShaderType, ShaderData[]> shadersOrderedLocal = new Dictionary<ShaderType, ShaderData[]>();


            shadersOrderedLocal.Add(ShaderType.Background, shadersLocal.Values.Where(v => v.type.HasFlag(ShaderType.Background)).ToArray());
            shadersOrderedLocal.Add(ShaderType.Screen, shadersLocal.Values.Where(v => v.type.HasFlag(ShaderType.Screen)).ToArray());
            shadersOrderedLocal.Add(ShaderType.Menu, shadersLocal.Values.Where(v => v.type.HasFlag(ShaderType.Menu)).ToArray());
            shadersOrderedLocal.Add(ShaderType.Merged, shadersLocal.Values.Where(v => v.type.HasFlag(ShaderType.Merged)).ToArray());

            shaders = shadersLocal.ToFrozenDictionary();
            shadersOrdered = shadersOrderedLocal.ToFrozenDictionary();
        }
        public static void Execute(ShaderUpdateDelegate everyShader)
        {
            foreach (ShaderData item in shaders.Values)
                everyShader(ref Unsafe.AsRef(in item));
        }
        public class ShaderData
        {
            public bool active;
            public string passName;
            public float progress;
            public Effect shader;
            public ShaderType type;
            public ShaderData(Effect effect, string passName, ShaderType type)
            {
                shader = effect;
                this.passName = passName;
                this.type = type;
            }
            public void Update()
            {
                progress += Renderer.TimeDelta / 60 * active.GetDirection();
                progress = MathHelper.Clamp01(progress);
            }
            public void Apply()
            {
                shader.TrySetValue("progress", progress);
                shader.TrySetValue("time", (float)Renderer.gameTime.TotalGameTime.TotalSeconds);
                shader.TrySetValue("winResolution", Renderer.Resolution);
                shader.CurrentTechnique.Passes[passName].Apply();
            }
        }
    }
    [Flags]
    public enum ShaderType : byte
    {
        None = 0, Background = 1, Screen = 2, Menu = 4, Merged = 8
    }
}
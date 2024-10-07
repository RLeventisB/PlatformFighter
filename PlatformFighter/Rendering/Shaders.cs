using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace PlatformFighter.Rendering
{
    public static class ShadersInfo
    {
        public delegate void ShaderUpdateDelegate(ref ShaderData data);
        public static ShaderData[] shaders;
        public static FrozenDictionary<ShaderType, ShaderData[]> shadersOrdered;
        public static void Initialize()
        {
            List<ShaderData> addedShaders = new List<ShaderData>();
            
            AddShaders(addedShaders);

            shaders = addedShaders.ToArray();

            Dictionary<ShaderType, ShaderData[]> shadersOrderedLocal = new Dictionary<ShaderType, ShaderData[]>
            {
                {
                    ShaderType.Background, addedShaders.Where(v => v.Type.HasFlag(ShaderType.Background)).ToArray()
                },
                {
                    ShaderType.Screen, addedShaders.Where(v => v.Type.HasFlag(ShaderType.Screen)).ToArray()
                },
                {
                    ShaderType.Menu, addedShaders.Where(v => v.Type.HasFlag(ShaderType.Menu)).ToArray()
                },
                {
                    ShaderType.Merged, addedShaders.Where(v => v.Type.HasFlag(ShaderType.Merged)).ToArray()
                }
            };

            shadersOrdered = shadersOrderedLocal.ToFrozenDictionary();
        }

        public static void AddShaders(List<ShaderData> addedShaders)
        {
            
        }

        public static void Execute(ShaderUpdateDelegate everyShader)
        {
            for (int index = 0; index < shaders.Length; index++)
            {
                ref ShaderData item = ref shaders[index];
                everyShader(ref item);
            }
        }
        public class ShaderData
        {
            public readonly string Name, PassName;
            public readonly Effect Effect;
            public readonly ShaderType Type;
            public bool Active;
            public float Progress;
            public ShaderData(string shaderName, Effect effect, string passName, ShaderType type)
            {
                Name = shaderName;
                Effect = effect;
                PassName = passName;
                Type = type;
            }
            public void Update()
            {
                Progress += Renderer.GameTimeDelta * Active.GetDirection();
                Progress = MathHelper.Clamp01(Progress);
            }
            public void Apply()
            {
                Effect.TrySetValue("progress", Progress);
                Effect.TrySetValue("time", (float)Renderer.gameTime.TotalGameTime.TotalSeconds);
                Effect.TrySetValue("winResolution", Renderer.Resolution);
                Effect.CurrentTechnique.Passes[PassName].Apply();
            }
        }
    }
    [Flags]
    public enum ShaderType : byte
    {
        None = 0, Background = 1, Screen = 2, Menu = 4, Merged = 8
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PlatformFighter.Rendering
{
    public abstract class BaseVertexBatch<T> : IDisposable where T : struct, IVertexType
    {
        public VertexDeclaration declaration;
        public Effect effect;
        public EffectPass pass;
        public EffectParameter matrixParameter;

        public GraphicsDevice GraphicsDevice;
        public int numPrimitives;
        public ExposedList<T> vertices;
        public ExposedList<short> indices;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ManageIndices(int start, int length)
        {
            int i = 0;
            while (i < length)
            {
                bool flag = i + 2 < length;
                if (!flag)
                {
                    break;
                }
                ManageIndices(i);
                ManageIndices(i + 1);
                ManageIndices(i + 2);
                i += 3;
                ManageIndices(i);
                ManageIndices(i - 1);
                ManageIndices(i - 2);
                i--;
                void ManageIndices(int index)
                {
                    if (index >= 0)
                    {
                        indices.Add((short)(index + start));
                    }
                }
                numPrimitives += 2;
            }
        }
        public abstract void AddTrail(Vector2[] positions, float[] rotations, Color color, float width, int? requiredLength = null);
        public abstract void AddTrail(Vector2[] positions, float[] rotations, Func<float, Color> colorFunction, Func<float, float> widthFunction, int? requiredLength = null);
        public abstract void AddVertexWithSides(Vector2 pos, float rotation, Color color, float width);
        public abstract void AddVertex(Vector2 pos, Color color);
        public virtual unsafe void DrawTrail()
        {
            if (vertices.Count >= 3)
            {
                GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                Viewport viewport = GraphicsDevice.Viewport;
                float num = 2.0f / viewport.Width;
                float num2 = -2.0f / viewport.Height;
                float* ptr = (float*)matrixParameter.data.ToPointer();
                *ptr = num * Camera.ViewMatrix.M11 - Camera.ViewMatrix.M14;
                ptr[1] = num * Camera.ViewMatrix.M21 - Camera.ViewMatrix.M24;
                ptr[2] = num * Camera.ViewMatrix.M31 - Camera.ViewMatrix.M34;
                ptr[3] = num * Camera.ViewMatrix.M41 - Camera.ViewMatrix.M44;
                ptr[4] = num2 * Camera.ViewMatrix.M12 + Camera.ViewMatrix.M14;
                ptr[5] = num2 * Camera.ViewMatrix.M22 + Camera.ViewMatrix.M24;
                ptr[6] = num2 * Camera.ViewMatrix.M32 + Camera.ViewMatrix.M34;
                ptr[7] = num2 * Camera.ViewMatrix.M42 + Camera.ViewMatrix.M44;
                ptr[8] = Camera.ViewMatrix.M13;
                ptr[9] = Camera.ViewMatrix.M23;
                ptr[10] = Camera.ViewMatrix.M33;
                ptr[11] = Camera.ViewMatrix.M43;
                ptr[12] = Camera.ViewMatrix.M14;
                ptr[13] = Camera.ViewMatrix.M24;
                ptr[14] = Camera.ViewMatrix.M34;
                ptr[15] = Camera.ViewMatrix.M44;
                matrixParameter.AdvanceState();

                pass.Apply();
                PreDraw();
                GraphicsDevice.DrawUserIndexedPrimitives(0, vertices.items, 0, vertices.Count, indices.items, 0, numPrimitives, declaration);
            }
            vertices.Clear();
            indices.Clear();
            numPrimitives = 0;
        }
        public virtual void PreDraw()
        {

        }
        public void Dispose()
        {
            Array.Clear(vertices.items);
            Array.Clear(indices.items);
        }
    }
    public class VertexColorBatch : BaseVertexBatch<VertexColorBatch.VertexType>
    {
        public VertexColorBatch(GraphicsDevice device)
        {
            effect = new Effect(GraphicsDevice = device, EffectResource.SpriteEffect.Bytecode);
            matrixParameter = effect.Parameters["MatrixTransform"];
            pass = effect.CurrentTechnique.Passes[0];
            vertices = new ExposedList<VertexType>();
            indices = new ExposedList<short>();
            declaration = VertexType.declaration;
        }
        public override void AddTrail(Vector2[] positions, float[] rotations, Color color, float width, int? requiredLength = null)
        {
            int length = requiredLength ?? positions.Length;
            int arrayLength = length;
            length <<= 1;
            if (length > 3)
            {
                int start = vertices.Count;
                for (int i = 0; i < arrayLength; i++)
                {
                    float progress = i / (length - 1f);
                    AddVertexWithSides(positions[i], rotations[i], color, width * progress);
                }
                ManageIndices(start, length);
            }
        }
        public void AddTrail(Vector2[] positions, float[] rotations, Func<float, Color> colorFunction1, Func<float, Color> colorFunction2, Func<float, float> widthFunction, int? requiredLength = null)
        {
            int length = requiredLength ?? positions.Length;
            int arrayLength = length;
            length <<= 1;
            if (length > 3)
            {
                int start = vertices.Count;
                for (int i = 0; i < arrayLength; i++)
                {
                    float progress = i / (arrayLength - 1f);
                    AddVertexWithSides(positions[i], rotations[i], colorFunction1(progress), colorFunction2(progress), widthFunction(progress));
                }
                ManageIndices(start, length);
            }
        }
        public void AddVertexWithSides(Vector2 pos, float rotation, Color color1, Color color2, float width)
        {
            Vector2 sideAdd = new Vector2(width, 0).RotateRad(rotation);
            AddVertex(pos + sideAdd, color1);
            AddVertex(pos - sideAdd, color2);
        }
        public override void AddTrail(Vector2[] positions, float[] rotations, Func<float, Color> colorFunction, Func<float, float> widthFunction, int? requiredLength = null)
        {
            int length = requiredLength ?? positions.Length;
            int arrayLength = length;
            length <<= 1;
            if (length > 3)
            {
                int start = vertices.Count;
                for (int i = 0; i < arrayLength; i++)
                {
                    float progress = i / (arrayLength - 1f);
                    AddVertexWithSides(positions[i], rotations[i], colorFunction(progress), widthFunction(progress));
                }
                ManageIndices(start, length);
            }
        }
        public override void AddVertexWithSides(Vector2 pos, float rotation, Color color, float width)
        {
            Vector2 sideAdd = new Vector2(width, 0).RotateRad(rotation);
            AddVertex(pos + sideAdd, color);
            AddVertex(pos - sideAdd, color);
        }
        public override void AddVertex(Vector2 pos, Color color) => vertices.Add(new VertexType(pos, color));
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VertexType : IVertexType
        {
            public Vector2 Position;
            public Color Color;
            public static readonly VertexDeclaration declaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
            public VertexDeclaration VertexDeclaration => declaration;
            public VertexType(Vector2 position, Color color)
            {
                Position = position;
                Color = color;
            }
        }
    }
    public class VertexTextureBatch : BaseVertexBatch<VertexTextureBatch.VertexType>
    {
        public Texture2D Texture;
        private Vector2 CurrentCoord;
        public VertexTextureBatch(GraphicsDevice device)
        {
            effect = new Effect(GraphicsDevice = device, EffectResource.SpriteEffect.Bytecode);
            matrixParameter = effect.Parameters["MatrixTransform"];
            pass = effect.CurrentTechnique.Passes[0];
            vertices = new ExposedList<VertexType>();
            indices = new ExposedList<short>();
            CurrentCoord = Vector2.One;
            declaration = VertexType.declaration;
        }
        public override void AddTrail(Vector2[] positions, float[] rotations, Color color, float width, int? requiredLength = null)
        {
            int length = requiredLength ?? positions.Length;
            int arrayLength = length;
            length <<= 1;
            if (length > 3)
            {
                int start = vertices.Count;
                for (int i = 0; i < arrayLength; i++)
                {
                    float progress = i / (length - 1f);
                    CurrentCoord.X = progress;
                    AddVertexWithSides(positions[i], rotations[i], color, width * progress);
                }
                ManageIndices(start, length);
            }
        }
        public override void AddTrail(Vector2[] positions, float[] rotations, Func<float, Color> colorFunction, Func<float, float> widthFunction, int? requiredLength = null)
        {
            int length = requiredLength ?? positions.Length;
            int arrayLength = length;
            length <<= 1;
            if (length > 3)
            {
                int start = vertices.Count;
                for (int i = 0; i < arrayLength; i++)
                {
                    float progress = i / (arrayLength - 1f);
                    CurrentCoord.X = progress;
                    AddVertexWithSides(positions[i], rotations[i], colorFunction(progress), widthFunction(progress));
                }
                ManageIndices(start, length);
            }
        }
        public override void AddVertexWithSides(Vector2 pos, float rotation, Color color, float width)
        {
            Vector2 sideAdd = new Vector2(width, 0).RotateRad(rotation);
            CurrentCoord.Y = 0f;
            AddVertex(pos + sideAdd, color);
            CurrentCoord.Y = 1f;
            AddVertex(pos - sideAdd, color);
        }
        public override void AddVertex(Vector2 pos, Color color) => vertices.Add(new VertexType(pos, CurrentCoord, color));
        public override void PreDraw()
        {
            GraphicsDevice.Textures[0] = Texture;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct VertexType : IVertexType
        {
            public Vector2 Position;
            public Vector2 TexCoord;
            public Color Color;
            public static readonly VertexDeclaration declaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
            public VertexDeclaration VertexDeclaration => declaration;
            public VertexType(Vector2 position, Vector2 coordinate, Color color)
            {
                Position = position;
                TexCoord = coordinate;
                Color = color;
            }
        }
    }
}
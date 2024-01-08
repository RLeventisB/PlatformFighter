using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Rendering
{
    public class CustomizedSpriteBatch : DepthlessSpriteBatch
    {
        public static Effect glowEffect;
        public static EffectPass glowEffectPass;
        public static EffectParameter glowParameter;
        public static Texture2D glowTexture;
        public static Texture2D glowTextureNoShader;
        public static HashSet<int> shaderKeySet = new HashSet<int>();
        public HashSet<Vector2> glowPositions = new HashSet<Vector2>();
        public Rectangle oldViewportBounds;
        public Matrix oldTransformMatrix;
        public CustomizedSpriteBatch(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
            spriteEffect.Dispose();
            spriteEffect = glowEffect;
            spritePass = glowEffectPass;
            transformationMatrixParameter = glowEffect.Parameters["WorldViewProjection"];
            batcher = new CustomizedSpriteBatcher(graphicsDevice);
            CustomizedSpriteBatcher.GlowParameter = glowEffect.Parameters["isGlow"];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGlow(Vector2 center, float size, Color color, DustDrawType type, bool ignoreGlowPosition = false) => DrawGlow(center, size, color, (type & DustDrawType.Inverse) == DustDrawType.Inverse, (type & DustDrawType.Faint) == DustDrawType.Faint, (type & DustDrawType.NoShader) == DustDrawType.NoShader, ignoreGlowPosition);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGlow(Vector2 center, Vector2 size, Color color, DustDrawType type, bool ignoreGlowPosition = false) => DrawGlow(center, size, color, (type & DustDrawType.Inverse) == DustDrawType.Inverse, (type & DustDrawType.Faint) == DustDrawType.Faint, (type & DustDrawType.NoShader) == DustDrawType.NoShader, ignoreGlowPosition);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGlow(Vector2 center, float size, Color color, bool inverse = false, bool faint = false, bool noShader = false, bool ignoreGlowPosition = false) => DrawGlow(center, new Vector2(size), color, inverse, faint, noShader, ignoreGlowPosition);
        public void DrawGlow(Vector2 position, Vector2 size, Color color, bool inverse = false, bool faint = false, bool noShader = false, bool ignoreGlowPosition = false)
        {
            if (ignoreGlowPosition || !glowPositions.Contains(position))
            {
                Texture2D texture = noShader ? glowTextureNoShader : glowTexture;
                var item = batcher.CreateBatchItem();
                item.Texture = texture;
                float sizeX = 16f * size.X;
                float sizeY = 16f * size.Y;
                item.vertexTL.Position.X = position.X - sizeX;
                item.vertexTL.Position.Y = position.Y - sizeY;
                item.vertexTR.Position.X = position.X + sizeX;
                item.vertexTR.Position.Y = position.Y - sizeY;
                item.vertexBL.Position.X = position.X - sizeX;
                item.vertexBL.Position.Y = position.Y + sizeY;
                item.vertexBR.Position.X = position.X + sizeX;
                item.vertexBR.Position.Y = position.Y + sizeY;
                if (faint)
                {
                    item.vertexTL.TextureCoordinate.X = 0f;
                    item.vertexTR.TextureCoordinate.X = 0.5f;
                    item.vertexBL.TextureCoordinate.X = 0f;
                    item.vertexBR.TextureCoordinate.X = 0.5f;
                }
                else
                {
                    item.vertexTL.TextureCoordinate.X = 0.5f;
                    item.vertexTR.TextureCoordinate.X = 1f;
                    item.vertexBL.TextureCoordinate.X = 0.5f;
                    item.vertexBR.TextureCoordinate.X = 1f;
                }
                if (inverse)
                {
                    item.vertexTL.TextureCoordinate.Y = 0.5f;
                    item.vertexTR.TextureCoordinate.Y = 0.5f;
                    item.vertexBL.TextureCoordinate.Y = 1f;
                    item.vertexBR.TextureCoordinate.Y = 1f;
                }
                else
                {
                    item.vertexTL.TextureCoordinate.Y = 0f;
                    item.vertexTR.TextureCoordinate.Y = 0f;
                    item.vertexBL.TextureCoordinate.Y = 0.5f;
                    item.vertexBR.TextureCoordinate.Y = 0.5f;
                }
                item.vertexTL.Color = color;
                item.vertexTR.Color = color;
                item.vertexBL.Color = color;
                item.vertexBR.Color = color;

                FlushIfNeeded();
                glowPositions.Add(position);
            }
        }

        public void PushCharacter(Vector2 sourceSize, Vector2 pos, Vector4 sourcePercents, Vector2 scale, Color color, float sin, float cos, ushort shadowWidth = 0)
        {
            float num = scale.X * sourceSize.X;
            float num2 = scale.Y * sourceSize.Y;
            float num3 = num * cos;
            float num4 = num * sin;
            float num5 = -num2 * sin;
            float num6 = num2 * cos;
            DepthlessSpriteBatchItem depthlessSpriteBatchItem;
            if (shadowWidth > 0)
            {
                Color black = Color.Black;
                black.A = color.A;
                Vector2 zero = Vector2.Zero;
                for (int i = -shadowWidth; i <= shadowWidth; i++)
                {
                    for (int j = -shadowWidth; j <= shadowWidth; j++)
                    {
                        if ((i | j) != 0)
                        {
                            zero.X = i;
                            zero.Y = j;
                            Utils.RotateRadPreCalc(ref zero, sin, cos);
                            depthlessSpriteBatchItem = batcher.CreateBatchItem();
                            GenerateVertexInfoForChar(ref depthlessSpriteBatchItem, sourcePercents.X, sourcePercents.Y, sourcePercents.Z, sourcePercents.W, pos + zero, black, num3, num4, num5, num6);
                            FlushIfNeeded();
                        }
                    }
                }
            }
            depthlessSpriteBatchItem = batcher.CreateBatchItem();
            GenerateVertexInfoForChar(ref depthlessSpriteBatchItem, sourcePercents.X, sourcePercents.Y, sourcePercents.Z, sourcePercents.W, pos, color, num3, num4, num5, num6);
            FlushIfNeeded();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GenerateVertexInfoForChar(ref DepthlessSpriteBatchItem item, float sourceX, float sourceY, float sourceW, float sourceH, Vector2 destination, Color color, float cosW, float sinW, float minusSinH, float cosH)
        {
            float var1 = minusSinH + destination.X;
            float var2 = cosH + destination.Y;
            float var3 = sourceH + sourceY;
            float var4 = sourceW + sourceX;

            item.Texture = TextRenderer.TextureInfo;
            item.vertexTL.Position.X = destination.X;
            item.vertexTL.Position.Y = destination.Y;
            item.vertexTR.Position.X = cosW + destination.X;
            item.vertexTR.Position.Y = sinW + destination.Y;
            item.vertexBL.Position.X = var1;
            item.vertexBL.Position.Y = var2;
            item.vertexBR.Position.X = var1 + cosW;
            item.vertexBR.Position.Y = var2 + sinW;
            item.vertexTL.TextureCoordinate.X = sourceX;
            item.vertexTL.TextureCoordinate.Y = sourceY;
            item.vertexTR.TextureCoordinate.X = var4;
            item.vertexTR.TextureCoordinate.Y = sourceY;
            item.vertexBL.TextureCoordinate.X = sourceX;
            item.vertexBL.TextureCoordinate.Y = var3;
            item.vertexBR.TextureCoordinate.X = var4;
            item.vertexBR.TextureCoordinate.Y = var3;
            item.vertexTL.Color = color;
            item.vertexTR.Color = color;
            item.vertexBL.Color = color;
            item.vertexBR.Color = color;
        }
        public sealed override void End()
        {
            if (!beginCalled)
            {
                throw new InvalidOperationException("Begin must be called before calling End.");
            }
            glowPositions.Clear();
            beginCalled = false;
            if (!immediate)
            {
                Setup();
            }
            batcher.DrawBatch(effect);
        }
        public override unsafe void Setup()
        {
            var gd = GraphicsDevice;
            gd.BlendState = blendState;
            gd.DepthStencilState = depthStencilState;
            gd.RasterizerState = rasterizerState;
            gd.SamplerStates[0] = samplerState;
            Viewport viewport = GraphicsDevice.Viewport;

            if (viewport.Bounds != oldViewportBounds || oldTransformMatrix != TransformationMatrix)
            {
                float num = (float)(2.0 / viewport.Width);
                float num2 = (float)(-2.0 / viewport.Height);
                float* ptr = (float*)transformationMatrixParameter.Data.ToPointer();
                ptr[0] = num * TransformationMatrix.M11 - TransformationMatrix.M14;
                ptr[1] = num * TransformationMatrix.M21 - TransformationMatrix.M24;
                ptr[2] = num * TransformationMatrix.M31 - TransformationMatrix.M34;
                ptr[3] = num * TransformationMatrix.M41 - TransformationMatrix.M44;
                ptr[4] = num2 * TransformationMatrix.M12 + TransformationMatrix.M14;
                ptr[5] = num2 * TransformationMatrix.M22 + TransformationMatrix.M24;
                ptr[6] = num2 * TransformationMatrix.M32 + TransformationMatrix.M34;
                ptr[7] = num2 * TransformationMatrix.M42 + TransformationMatrix.M44;
                ptr[8] = TransformationMatrix.M13;
                ptr[9] = TransformationMatrix.M23;
                ptr[10] = TransformationMatrix.M33;
                ptr[11] = TransformationMatrix.M43;
                ptr[12] = TransformationMatrix.M14;
                ptr[13] = TransformationMatrix.M24;
                ptr[14] = TransformationMatrix.M34;
                ptr[15] = TransformationMatrix.M44;
                transformationMatrixParameter.AdvanceState();
                oldTransformMatrix = TransformationMatrix;

                oldViewportBounds = viewport.Bounds;
            }
            spritePass.Apply();
        }
    }
    [Flags]
    public enum DustDrawType : byte
    {
        Inverse = 1,
        Faint = 2,
        NoShader = 4
    }
}

//#define UseBE

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.OpenGL;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PlatformFighter.Rendering
{
    public class Spritebatch3D : IDisposable
    {
        public const int InitialBatchSize = 256, BatchStep = 64;
        public GraphicsDevice GraphicsDevice;

        private Vector3 position;
        private Vector3 rotation;
        private float fieldOfView;
        private bool cameraDirty = true, projectionDirty = true;

        public Spritebatch3DItem[] sprites;
        public Texture2D[] textures;
#if UseBE
        public BasicEffect spriteEffect;
#else
        public SpriteEffect3D spriteEffect;
#endif
        public IntPtr indexPointer;
        public ushort nextSprite, indexCount;
        public bool applyEffectOnBatch;
        public unsafe Spritebatch3DTexturePositionColor* vertexPointer;
        public Matrix world, projection, finalProjection;
        public VertexDeclaration vertexDeclaration;
        public Effect customEffect;
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                cameraDirty = true;
            }
        }
        public float PositionX
        {
            get => position.X;
            set
            {
                position.X = value;
                cameraDirty = true;
            }
        }
        public float PositionY
        {
            get => position.Y;
            set
            {
                position.Y = value;
                cameraDirty = true;
            }
        }
        public float PositionZ
        {
            get => position.Z;
            set
            {
                position.Z = value;
                cameraDirty = true;
            }
        }
        public Vector3 Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                cameraDirty = true;
                //projectionDirty = true;
            }
        }
        public float FieldOfView
        {
            get => fieldOfView;
            set
            {
                fieldOfView = value;
                projectionDirty = true;
            }
        }
        public Spritebatch3D(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
#if UseBE
            spriteEffect = new BasicEffect(graphicsDevice);
            spriteEffect.LightingEnabled = false;
            spriteEffect.TextureEnabled = true;
#else
            spriteEffect = new SpriteEffect3D(graphicsDevice);
#endif
            sprites = new Spritebatch3DItem[InitialBatchSize];
            textures = new Texture2D[InitialBatchSize];
            vertexDeclaration = Spritebatch3DTexturePositionColor.vertexDeclaration;
            vertexDeclaration.GraphicsDevice = graphicsDevice;
            ResizeBatch(InitialBatchSize);
        }
        public void Begin(Effect effect = null)
        {
            customEffect = effect;
            ConstructMatrices();
        }
        public void Draw(Texture2D texture, Vector3 position, Vector3 rotation, Vector2 scale, Color color)
        {
            textures[nextSprite] = texture;
            Vector3 add = new Vector3((texture.Width >> 1) * scale.X, (texture.Height >> 1) * scale.Y, 0).RotateZ(rotation.Z).RotateX(rotation.X).RotateY(rotation.Y);
            //Vector3 addAlt = add;
            //addAlt.X = -add.Y;
            //addAlt.Y = add.X;
            //addAlt.Z = -add.Z;

            Vector3 addAlt = new Vector3((texture.Width >> 1) * scale.X, (texture.Height >> 1) * scale.Y, 0).RotateZ(rotation.Z + MathHelper.PiOver2).RotateX(rotation.X).RotateY(rotation.Y);

            ref Spritebatch3DItem item = ref sprites[nextSprite];
            item.PositionTopLeft.Position = position + add;
            item.PositionTopLeft.Color = color;
            item.PositionTopLeft.TextureCoordinates = Vector2.Zero;

            item.PositionTopRight.Position = position + addAlt;
            item.PositionTopRight.Color = color;
            item.PositionTopRight.TextureCoordinates = Vector2.UnitX;

            item.PositionBottomLeft.Position = position - addAlt;
            item.PositionBottomLeft.Color = color;
            item.PositionBottomLeft.TextureCoordinates = Vector2.UnitY;

            item.PositionBottomRight.Position = position - add;
            item.PositionBottomRight.Color = color;
            item.PositionBottomRight.TextureCoordinates = Vector2.One;

            AdvanceSprite();
        }
        public unsafe void End()
        {
            if (nextSprite == 0)
                return;

            GraphicsDevice.BlendState = Renderer.PixelBlendState;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.SamplerStates[0] = Renderer.PixelSamplerState;

            ApplyEffects();

            fixed (Spritebatch3DItem* arrayPointer = &sprites[0])
            {
                fixed (Texture2D* texturePointer = &textures[0])
                {
                    Texture2D lastTexture = textures[0];
                    var vertexArrayPointer = vertexPointer;
                    int count = 0;
                    for (ushort i = 0; i < nextSprite; i++, vertexArrayPointer += 4, count += 6)
                    {
                        ref Spritebatch3DItem item = ref arrayPointer[i];
                        ref Texture2D texture = ref texturePointer[i];
                        var shouldFlush = !ReferenceEquals(texture, lastTexture);
                        if (shouldFlush)
                        {
                            FlushVertexArray(count, lastTexture);
                            vertexArrayPointer = vertexPointer;

                            lastTexture = texture;
                            count = 0;
                        }
                        *(vertexArrayPointer + 0) = item.PositionTopLeft;
                        *(vertexArrayPointer + 1) = item.PositionTopRight;
                        *(vertexArrayPointer + 2) = item.PositionBottomLeft;
                        *(vertexArrayPointer + 3) = item.PositionBottomRight;

                        texture = null;
                    }
                    FlushVertexArray(count, lastTexture);
                }
            }
            nextSprite = 0;
        }
        private void FlushVertexArray(int end, Texture texture)
        {
            if (end != 0)
            {
                GraphicsDevice.Textures[0] = texture;
                if (customEffect is not null && applyEffectOnBatch)
                {
                    foreach (EffectPass effectPass in customEffect.CurrentTechnique.Passes)
                    {
                        effectPass.Apply();
                        DrawBatch(end);
                    }
                }
                else
                {
                    DrawBatch(end);
                }
            }
        }
        public void ConstructMatrices()
        {
            if (projectionDirty)
            {
                projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fieldOfView), GraphicsDevice.Viewport.AspectRatio, 0.05f, 1000f);

                finalProjection = world * projection;
                projectionDirty = false;
            }
            if (cameraDirty)
            {
                world = Matrix.CreateLookAt(position, position - Vector3.Forward.RotateX(rotation.Y).RotateY(rotation.X), Vector3.Up) * Matrix.CreateRotationZ(rotation.Z);
                finalProjection = world * projection;
                cameraDirty = false;
            }
        }
        public unsafe void ApplyEffects()
        {
#if UseBE
            spriteEffect.World = world;
            spriteEffect.View = view;
            spriteEffect.Projection = projection;
#else
            float* matrixPtr;
            EffectParameter matrixParam;
            EffectPass effPass;
            if (customEffect is not null && (matrixParam = customEffect.Parameters["WorldViewProj"]) != null && !customEffect.IsDisposed)
            {
                matrixPtr = (float*)matrixParam.Data.ToPointer();
                effPass = customEffect.CurrentTechnique.Passes[0];
            }
            else
            {
                matrixParam = spriteEffect.matrixParam;
                matrixPtr = (float*)spriteEffect.matrixParamPtr.ToPointer();
                effPass = spriteEffect.CurrentTechnique.Passes[0];
            }
            matrixPtr[0] = finalProjection.M11;
            matrixPtr[1] = finalProjection.M21;
            matrixPtr[2] = finalProjection.M31;
            matrixPtr[3] = finalProjection.M41;
            matrixPtr[4] = finalProjection.M12;
            matrixPtr[5] = finalProjection.M22;
            matrixPtr[6] = finalProjection.M32;
            matrixPtr[7] = finalProjection.M42;
            matrixPtr[8] = finalProjection.M13;
            matrixPtr[9] = finalProjection.M23;
            matrixPtr[10] = finalProjection.M33;
            matrixPtr[11] = finalProjection.M43;
            matrixPtr[12] = finalProjection.M14;
            matrixPtr[13] = finalProjection.M24;
            matrixPtr[14] = finalProjection.M34;
            matrixPtr[15] = finalProjection.M44;
            matrixParam.AdvanceState();
            effPass.Apply();
#endif
        }
        private unsafe void DrawBatch(int primitiveCount)
        {
            GraphicsDevice.ApplyState(true);

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            GraphicsDevice.indexBufferDirty = true;
            //_indexBufferDirty = true;

            // Pin the buffers.

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.Apply(GraphicsDevice.VertexShader, (nint)vertexPointer, GraphicsDevice.VertexShader.HashKey ^ GraphicsDevice.PixelShader.HashKey);

            //Draw
            GL.DrawElements(
                GLPrimitiveType.Triangles,
                primitiveCount,
                DrawElementsType.UnsignedShort,
                indexPointer);
            GraphicsExtensions.CheckGLError();
        }
        public void AdvanceSprite()
        {
            nextSprite++;
            if (nextSprite >= sprites.Length || nextSprite >= textures.Length)
            {
                ResizeBatch(sprites.Length + BatchStep);
            }
        }
        public unsafe void ResizeBatch(int newSize)
        {
            if (sprites.Length != newSize)
                Array.Resize(ref sprites, newSize);
            if (textures.Length != newSize)
                Array.Resize(ref textures, newSize);

            for (int i = 0; i < newSize; i++)
            {
                if (sprites[i] == default)
                {
                    sprites[i] = new Spritebatch3DItem();
                }
            }
            ushort indexSize = (ushort)(newSize * 6);
            ushort* newPtr = Utils.AllocateZeroed<ushort>(indexSize);

            if (indexPointer != nint.Zero)
            {
                NativeMemory.Copy((void*)indexPointer, newPtr, new nuint(indexCount));
                NativeMemory.Free((void*)indexPointer);
            }
            indexPointer = (IntPtr)newPtr;

            for (int i = indexCount / 6; i < newSize; i++, newPtr += 6)
            {
                int v = i << 2;
                *(newPtr + 0) = (ushort)v;
                *(newPtr + 1) = (ushort)(v + 1);
                *(newPtr + 2) = (ushort)(v + 2);
                *(newPtr + 3) = (ushort)(v + 1);
                *(newPtr + 4) = (ushort)(v + 3);
                *(newPtr + 5) = (ushort)(v + 2);
            }
            indexCount = indexSize;

            if ((nint)vertexPointer != IntPtr.Zero)
            {
                NativeMemory.Free(vertexPointer);
            }
            vertexPointer = Utils.AllocateZeroed<Spritebatch3DTexturePositionColor>(newSize << 2);
        }
        public unsafe void Dispose()
        {
            if (indexPointer != nint.Zero)
            {
                NativeMemory.Free((void*)indexPointer);
            }
            if ((nint)vertexPointer != IntPtr.Zero)
            {
                NativeMemory.Free(vertexPointer);
            }
            spriteEffect.Dispose();
        }
        public void LookAt(Vector3 position)
        {
            Vector3 to = position - this.position;
            rotation.Y = -MathF.Atan2(to.Z, to.Y) - MathHelper.PiOver2;
            rotation.X = MathF.Acos(Vector3.Dot(position, this.position) / (position.Length() * this.position.Length())) - MathHelper.PiOver2;
            cameraDirty = true;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Spritebatch3DItem
    {
        public Spritebatch3DTexturePositionColor PositionTopLeft;
        public Spritebatch3DTexturePositionColor PositionTopRight;
        public Spritebatch3DTexturePositionColor PositionBottomLeft;
        public Spritebatch3DTexturePositionColor PositionBottomRight;

        public override bool Equals(object obj)
        {
            return obj is Spritebatch3DItem item &&
                   EqualityComparer<Spritebatch3DTexturePositionColor>.Default.Equals(PositionTopLeft, item.PositionTopLeft) &&
                   EqualityComparer<Spritebatch3DTexturePositionColor>.Default.Equals(PositionTopRight, item.PositionTopRight) &&
                   EqualityComparer<Spritebatch3DTexturePositionColor>.Default.Equals(PositionBottomLeft, item.PositionBottomLeft) &&
                   EqualityComparer<Spritebatch3DTexturePositionColor>.Default.Equals(PositionBottomRight, item.PositionBottomRight);
        }
        public override int GetHashCode()
        {
            return PositionTopLeft.GetHashCode() ^ PositionTopRight.GetHashCode() ^ PositionBottomLeft.GetHashCode() ^ PositionBottomRight.GetHashCode();
        }
        public static bool operator ==(Spritebatch3DItem item1, Spritebatch3DItem item2) => item1.Equals(item2);
        public static bool operator !=(Spritebatch3DItem item1, Spritebatch3DItem item2) => !item1.Equals(item2);
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Spritebatch3DTexturePositionColor : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinates;
        public Color Color;
        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return vertexDeclaration;
            }
        }
        public static readonly VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        });
    }
}

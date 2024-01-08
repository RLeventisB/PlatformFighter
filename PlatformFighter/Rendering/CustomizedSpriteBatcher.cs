using Microsoft.Xna.Framework.Graphics;

using MonoGame.OpenGL;

using PlatformFighter.Miscelaneous;

using System;
using System.Runtime.InteropServices;

namespace PlatformFighter.Rendering
{
    public unsafe class CustomizedSpriteBatcher : IDepthlessSpriteBatcher
    {
        public static EffectParameter GlowParameter;
        private const int InitialBatchSize = 2048;
        private const int MaxBatchSize = 5461;
        private const int InitialVertexArraySize = 2048;
        private DepthlessSpriteBatchItem[] _batchItemList;
        internal int _batchItemCount, _indexCount;
        public readonly GraphicsDevice GraphicsDevice;
        private IntPtr _indexPtr;
        private DepthlessSpriteBatchItem.VertexPositionColorTexture* _vertexPtr;
        private VertexDeclaration vertexDeclaration;
        public CustomizedSpriteBatcher(GraphicsDevice device)
        {
            GraphicsDevice = device;
            _batchItemList = new DepthlessSpriteBatchItem[InitialBatchSize];
            _batchItemCount = 0;
            for (int i = 0; i < InitialBatchSize; i++)
            {
                _batchItemList[i] = new DepthlessSpriteBatchItem();
            }
            EnsureArrayCapacity(InitialVertexArraySize);
            vertexDeclaration = DepthlessSpriteBatchItem.VertexPositionColorTexture.VertexDeclaration;
            vertexDeclaration.GraphicsDevice = GraphicsDevice;
        }
        public DepthlessSpriteBatchItem CreateBatchItem()
        {
            if (_batchItemCount >= _batchItemList.Length)
            {
                int oldSize = _batchItemList.Length;
                int newSize = oldSize + 1024;
                Array.Resize(ref _batchItemList, newSize.Log());
                for (int i = oldSize; i < newSize; i++)
                {
                    _batchItemList[i] = new DepthlessSpriteBatchItem();
                }
                EnsureArrayCapacity(Math.Min(newSize, MaxBatchSize));
            }
            return _batchItemList[_batchItemCount++];
        }

        private void EnsureArrayCapacity(int numBatchItems)
        {
            int neededCapacity = 6 * numBatchItems;
            if (neededCapacity <= _indexCount)
            {
                // Short circuit out of here because we have enough capacity.
                return;
            }
            short* newPtr = Utils.Allocate<short>(neededCapacity);
            // hare esto cuando mi cerbro funcooen ayuda
            //int start = 0;
            if (_indexPtr != nint.Zero)
            {
                //NativeMemory.Copy(_indexPtr, newPtr, new nuint((uint)_indexCount / 3));
                NativeMemory.Free(_indexPtr.ToPointer());
                //start = _indexCount;
            }
            _indexCount = neededCapacity;
            _indexPtr = (IntPtr)newPtr;
            //newPtr += start;
            //for (var i = start / 6; i < numBatchItems; i++, newPtr += 6)
            for (int i = 0; i < numBatchItems; i++, newPtr += 6)
            {
                /*
                 *  TL    TR
                 *   0----1 0,1,2,3 = index offsets for vertex indices
                 *   |   /| TL,TR,BL,BR are vertex references in SpriteBatchItem.
                 *   |  / |
                 *   | /  |
                 *   |/   |
                 *   2----3
                 *  BL    BR
                 */
                // Triangle 1
                int v = i << 2;
                *(newPtr + 0) = (short)v;
                *(newPtr + 1) = (short)(v + 1);
                *(newPtr + 2) = (short)(v + 2);
                // Triangle 2
                *(newPtr + 3) = (short)(v + 1);
                *(newPtr + 4) = (short)(v + 3);
                *(newPtr + 5) = (short)(v + 2);
            }

            if ((nint)_vertexPtr != IntPtr.Zero)
            {
                NativeMemory.Free(_vertexPtr);
            }
            _vertexPtr = Utils.Allocate<DepthlessSpriteBatchItem.VertexPositionColorTexture>(numBatchItems << 2);
        }
        public void DrawBatch(Effect effect)
        {
            bool isGlowCache = false;
            if (effect?.IsDisposed == true)
                throw new ObjectDisposedException("effect");

            // nothing to do
            if (_batchItemCount == 0)
                return;

            // Determine how many iterations through the drawing code we need to make
            int batchIndex = 0;
            int batchCount = _batchItemCount;

            // Iterate through the batches, doing short.MaxValue sets of vertices only.
            while (batchCount > 0)
            {
                // setup the vertexArray array
                int index = 0;
                if (_batchItemList[0] is null)
                    goto end;
                Texture2D tex = _batchItemList[batchIndex].Texture;

                int numBatchesToProcess = batchCount;
                if (numBatchesToProcess > MaxBatchSize)
                {
                    numBatchesToProcess = MaxBatchSize;
                }
                DepthlessSpriteBatchItem.VertexPositionColorTexture* vertexArrayPtr = _vertexPtr;
                // Draw the batches
                for (int i = 0; i < numBatchesToProcess; i++, batchIndex++, index += 4, vertexArrayPtr += 4)
                {
                    ref DepthlessSpriteBatchItem item = ref _batchItemList[batchIndex];
                    // if the texture changed, we need to flush and bind the new texture
                    bool shouldFlush = !ReferenceEquals(item.Texture, tex);
                    if (shouldFlush)
                    {
                        bool isGlow = CustomizedSpriteBatch.shaderKeySet.Contains(tex.SortingKey);
                        if (isGlow != isGlowCache)
                        {
                            isGlowCache = isGlow;
                            GlowParameter.SetValue(isGlow);
                            CustomizedSpriteBatch.glowEffectPass.Apply();
                        }
                        FlushVertexArray(index, effect, tex);
                        tex = item.Texture;
                        index = 0;
                        vertexArrayPtr = _vertexPtr;
                    }
                    // store the SpriteBatchItem data in our vertexArray
                    *(vertexArrayPtr + 0) = item.vertexTL;
                    *(vertexArrayPtr + 1) = item.vertexTR;
                    *(vertexArrayPtr + 2) = item.vertexBL;
                    *(vertexArrayPtr + 3) = item.vertexBR;

                    item.Texture = null;
                }
                bool isGlowFinal = CustomizedSpriteBatch.shaderKeySet.Contains(tex.SortingKey);
                if (isGlowFinal != isGlowCache)
                {
                    GlowParameter.SetValue(isGlowFinal);
                    CustomizedSpriteBatch.glowEffectPass.Apply();
                }
                FlushVertexArray(index, effect, tex);
                batchCount -= numBatchesToProcess;
            }
            // return items to the pool.  
        end: ;
            _batchItemCount = 0;
            GlowParameter.SetValue(false);
        }
        private void FlushVertexArray(int end, Effect effect, Texture texture)
        {
            if (end == 0)
                return;
            GraphicsDevice.Textures[0] = texture;
            if (effect is not null)
            {
                foreach (EffectPass effectPass in effect.CurrentTechnique.Passes)
                {
                    effectPass.Apply();
                    Draw(end >> 1);
                    //GraphicsDevice.DrawUserIndexedPrimitives(0, _vertexArray, 0, num, _index, 0, num >> 1, DepthlessSpriteBatchItem.VertexPositionColorTexture.VertexDeclaration);
                }
            }
            else
            {
                Draw(end >> 1);
                //GraphicsDevice.DrawUserIndexedPrimitives(0, _vertexArray, 0, num, _index, 0, num >> 1, DepthlessSpriteBatchItem.VertexPositionColorTexture.VertexDeclaration);
            }
        }
        private void Draw(int primitiveCount)
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
            vertexDeclaration.Apply(GraphicsDevice.VertexShader, (nint)_vertexPtr, GraphicsDevice.VertexShader.HashKey ^ GraphicsDevice.PixelShader.HashKey);

            //Draw
            GL.DrawElements(
                GLPrimitiveType.Triangles,
                primitiveCount * 3,
                DrawElementsType.UnsignedShort,
                _indexPtr);
            GraphicsExtensions.CheckGLError();
        }
    }
}
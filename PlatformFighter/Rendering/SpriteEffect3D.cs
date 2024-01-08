using Microsoft.Xna.Framework.Graphics;

namespace PlatformFighter.Rendering
{
    public class SpriteEffect3D : Effect
    {
        public EffectParameter matrixParam;
        public nint matrixParamPtr;
        public SpriteEffect3D(GraphicsDevice device)
            : base(device, EffectResource.SpriteEffect.Bytecode)
        {
            CacheEffectParameters();
        }
        protected SpriteEffect3D(SpriteEffect3D cloneSource)
            : base(cloneSource)
        {
            CacheEffectParameters();
        }

        unsafe void CacheEffectParameters()
        {
            matrixParam = Parameters["MatrixTransform"];
            matrixParamPtr = matrixParam.Data;
        }
    }
}

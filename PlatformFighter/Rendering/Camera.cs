using Microsoft.Xna.Framework;

using System;

namespace PlatformFighter.Rendering
{
    public static class Camera
    {
        private static bool remakeMatrix = true;
        private static Vector2 _position = Vector2.Zero, _scale = Vector2.One;
        private static float _angle;
        public static Vector2 Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    remakeMatrix = true;
                    _position.X = value.X;
                    _position.Y = value.Y;
                    // _position = value; no se si esto hara que haya menos garbage collection porque estas cambiando los valores en vez de asignar un valor o el valor llamado value sera basura de memoria AAAAAAAAAA
                }
            }
        }
        public static Vector2 Scale
        {
            get => _scale;
            set
            {
                if (_scale != value)
                {
                    remakeMatrix = true;
                    _scale = value;
                }
            }
        }
        public static float ScaleSingle
        {
            get => (_scale.X + _scale.Y) / 2;
            set
            {
                Vector2 VectorScale = new Vector2(value);
                if (_scale != VectorScale)
                {
                    remakeMatrix = true;
                    _scale = VectorScale;
                }
            }
        }
        public static float Angle
        {
            get => _angle;
            set
            {
                if (_angle != value)
                {
                    remakeMatrix = true;
                    _angle = value;
                }
            }
        }
        public static Vector2 CameraShakeOffset = Vector2.Zero;
        public static float CameraShake;
        public static float? CameraShakeMinus;
        public static bool IsShakeTimed = false;
        public static TimedAction CameraShakeTimedAction = new TimedAction(TimedCameraShake, 1f);
        public static void AddCameraShake(float add, float? minus = null)
        {
            CameraShake += add;
            CameraShakeMinus = minus;
        }
        public static Matrix matrix;
        public static void Update()
        {
            {
                if (CameraShake > 0)
                {
                    if (IsShakeTimed)
                    {
                        CameraShakeTimedAction.Update(in Renderer.FinalTimeDelta);
                    }
                    else
                    {
                        Position -= CameraShakeOffset;
                        CameraShakeOffset.X = Main.cosmeticRandom.NextFloat(-CameraShake, CameraShake);
                        CameraShakeOffset.Y = Main.cosmeticRandom.NextFloat(-CameraShake, CameraShake);
                        Position += CameraShakeOffset;

                        CameraShake -= MathF.Min(CameraShake, CameraShakeMinus ?? 1f * Renderer.InternalTimeDelta);
                        if (CameraShake <= 0)
                        {
                            Position -= CameraShakeOffset;
                            CameraShakeOffset.X = 0;
                            CameraShakeOffset.Y = 0;
                            CameraShake = 0;
                        }
                        remakeMatrix = true;

                    }
                }
            }
            if (remakeMatrix)
            {
                matrix = Matrix.Identity *
                Matrix.CreateTranslation(-_position.X, _position.Y, 0) *
                Matrix.CreateRotationZ(MathHelper.ToRadians(Angle)) *
                Matrix.CreateTranslation(VirtualMidResolution.X / Scale.X, VirtualMidResolution.Y / Scale.Y, 0) *
                Matrix.CreateScale(_scale.X, _scale.Y, 0);
                remakeMatrix = false;
            }
        }
        public static void TimedCameraShake(in float offset)
        {
            Position -= CameraShakeOffset;
            CameraShakeOffset.X = Main.cosmeticRandom.NextFloat(-CameraShake, CameraShake);
            CameraShakeOffset.Y = Main.cosmeticRandom.NextFloat(-CameraShake, CameraShake);
            Position += CameraShakeOffset;

            CameraShake -= MathF.Min(CameraShake, CameraShakeMinus ?? CameraShakeTimedAction.Interval);
            if (CameraShake <= 0)
            {
                Position -= CameraShakeOffset;
                CameraShakeOffset.X = 0;
                CameraShakeOffset.Y = 0;
                CameraShake = 0;
            }
            remakeMatrix = true;
        }
    }
}
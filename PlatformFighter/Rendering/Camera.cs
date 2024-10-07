using Microsoft.Xna.Framework;

using System.Collections.Generic;

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
        public static List<CameraShake> CameraShakes = new List<CameraShake>();
        public static Matrix matrix;
        public static void Update()
        {
            CameraShakeOffset = Vector2.Zero;
            for (int i = 0; i < CameraShakes.Count; i++)
            {
                remakeMatrix = true;

                CameraShake shake = CameraShakes[i];
                CameraShakeOffset += shake.GetOutset();
                if (shake.Time != 0)
                    continue;
                CameraShakes.RemoveAt(i);
                i--;
            }
            if (remakeMatrix)
            {
                matrix = Matrix.Identity *
                         Matrix.CreateTranslation(-_position.X + CameraShakeOffset.X, _position.Y + CameraShakeOffset.Y, 0) *
                         Matrix.CreateRotationZ(MathHelper.ToRadians(Angle)) *
                         Matrix.CreateTranslation(VirtualMidResolution.X / _scale.X, VirtualMidResolution.Y / _scale.Y, 0) *
                         Matrix.CreateScale(_scale.X, _scale.Y, 0);
                remakeMatrix = false;
            }
        }
    }
    public struct CameraShake
    {
        public CameraShake(float magnitude, ushort time, byte interval, float? magnitudeLoss = null)
        {
            Magnitude = magnitude;
            Time = time;
            Interval = interval;
            MagnitudeLoss = magnitudeLoss ?? magnitude / time;
        }
        public float Magnitude { get; internal set; }
        public float MagnitudeLoss { get; init; }
        public ushort Time { get; internal set; }
        public byte Interval { get; init; }
        internal byte IntervalCounter { get; set; }
        private Vector2 Outset;
        public Vector2 GetOutset()
        {
            if (++IntervalCounter >= Interval)
            {
                IntervalCounter = 0;
                Outset.X = Main.cosmeticRandom.NextFloat(-Magnitude, Magnitude);
                Outset.Y = Main.cosmeticRandom.NextFloat(-Magnitude, Magnitude);
            }
            Magnitude -= MagnitudeLoss;
            if (Time > 0)
                Time--;
            return Outset;
        }
    }
}
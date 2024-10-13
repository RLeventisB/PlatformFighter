using Microsoft.Xna.Framework;

using System.Collections.Generic;

namespace PlatformFighter.Rendering
{
    public static class Camera
    {
        private static bool remakeMatrix = true;
        private static Vector2 _position = Vector2.Zero;
        private static float _zoom = 1;
        public static Vector2 Position
        {
            get => _position;
            set
            {
                if (_position == value)
                    return;

                remakeMatrix = true;
                _position = value;
            }
        }
        public static float Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom == value)
                    return;

                remakeMatrix = true;
                _zoom = value;
            }
        }
        public static Vector2 CameraShakeOffset = Vector2.Zero;
        public static List<CameraShake> CameraShakes = new List<CameraShake>();
        public static Matrix ViewMatrix;
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
                ViewMatrix = Matrix.Identity *
                         Matrix.CreateTranslation(-_position.X + CameraShakeOffset.X, _position.Y + CameraShakeOffset.Y, 0) *
                         Matrix.CreateTranslation(VirtualMidResolution.X / _zoom, VirtualMidResolution.Y / _zoom, 0) *
                         Matrix.CreateScale(_zoom, _zoom, 0);
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
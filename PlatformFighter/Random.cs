using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PlatformFighter
{
    public struct GameRandom
    {
        public readonly uint[] thing = new uint[4];
        private int _seed = 0;
        public GameRandom()
        {
            Reseed(Environment.TickCount + Environment.ProcessorCount * (byte)Environment.OSVersion.Platform);
        }
        public int Seed
        {
            get => _seed;
            set => Reseed(value);
        }
        public void Reseed(int seed)
        {
            _seed = seed;
            uint old = thing[1];
            thing[0] = (uint)seed;
            thing[1] = BitOperations.RotateLeft(thing[0], (int)old);
            thing[2] = BitOperations.RotateLeft(thing[1], (int)-old);
            thing[3] = BitOperations.RotateLeft(thing[0], (int)old >> 2);
        }
        public void Reseed() => Reseed(GetSampleSigned());
        public uint GetSample()
        {
            uint result = rol64(thing[1] * 5, 7) * 9;
            uint t = thing[1] << 17;
            thing[2] ^= thing[0];
            thing[3] ^= thing[1];
            thing[1] ^= thing[2];
            thing[0] ^= thing[3];

            thing[2] ^= t;
            thing[3] = rol64(thing[3], 45);
            return result;

            static uint rol64(uint x, int k)
            {
                return x << k | x >> 64 - k;
            }
        }
        public int GetSampleSigned() => Utils.Abs((int)GetSample());
        public T SelectRandom<T>(IEnumerable<T> enumerable)
        {
            T[] enumerable1 = enumerable as T[] ?? enumerable.ToArray();
            int count = enumerable1.Length;
            switch (count)
            {
                case 0:
                    throw new IndexOutOfRangeException("Collection is empty");
                case 1:
                    return enumerable1.ElementAt(0);
                default:
                    return enumerable1.ElementAt(Next(count));
            }
        }
        public bool TrySelectValue<T>(IEnumerable<T> enumerable, out T value)
        {
            T[] enumerable1 = enumerable as T[] ?? enumerable.ToArray();
            int count = enumerable1.Length;
            switch (count)
            {
                case 0:
                    value = default;
                    return false;
                case 1:
                    value = enumerable1.ElementAt(0);
                    return true;
                default:
                    value = enumerable1.ElementAt(Next(count));
                    return true;
            }
        }
        public byte NextByte() => (byte)(GetSampleSigned() >> 24);
        public int Next() => GetSampleSigned();
        public int Next(int max) => (int)Math.Round((max - 1) * NextDouble());
        public int Next(int min, int max) => min + (int)((max - min) * NextDouble());
        public float NextAngle() => GetSampleSigned() / 5965232.352777778f; // int.MaxValue / 360
        public float NextRadian() => GetSampleSigned() / 341782637.62906086f; // int.MaxValue / Math.PI
        public float NextFloatDirection(float min, float max) => NextFloat(min, max) * NextBoolean().GetDirection();
        public float NextFloat() => (float)GetSampleSigned() / int.MaxValue;
        public float NextFloat(float max) => max * NextFloat();
        public float NextFloat(float min, float max) => min + (max - min) * NextFloat();
        public bool NextBoolean() => NextDouble() >= 0.5;
        public bool NextBoolean(double percent) => percent > NextDouble();
        public bool NextBoolean(float percent) => percent > NextFloat();
        public double NextDouble() => (double)GetSampleSigned() / int.MaxValue;
        public Vector2 NextSquareUnitVector2() => new Vector2(NextFloat(-1f, 1f), NextFloat(-1f, 1f));
        public Vector2 NextUnitVector2() => Vector2.UnitY.RotateRad(NextRadian());
        public void SaveState(BinaryWriter writer)
        {
            writer.Write(thing[0]);
            writer.Write(thing[1]);
            writer.Write(thing[2]);
            writer.Write(thing[3]);
            writer.Write(_seed);
        }
        public void ReadState(BinaryReader reader)
        {
            thing[0] = reader.ReadUInt32();
            thing[1] = reader.ReadUInt32();
            thing[2] = reader.ReadUInt32();
            thing[3] = reader.ReadUInt32();
            _seed = reader.ReadInt32();
        }
    }
}
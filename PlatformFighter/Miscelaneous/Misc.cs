using ExtraProcessors.GameTexture;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using PlatformFighter.Rendering;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace PlatformFighter.Miscelaneous
{
    [Flags]
    public enum DeathReason : ushort
    {
        NotSpecified = 0,
        TileCollide = 1,
        PlayerInteract = 2,
        EnemyInteract = 4,
        BossClear = 8,
        Hit = 16,
        Timeleft = 32,
        DustDespawn = 64,
        OnCommand = 128,
        ZeroHitsLeft = 256,
        WorldClear = 512,
        OutOfBound = 1024,
        ZeroHealth = 2048
    }
    public static class Utils
    {
        public const sbyte Zero = 0;
        public static readonly Point[] directionPointMap = { Up, Down, Left, Right, Point.Zero, Point.Zero };
        public static readonly Vector2[] directionVectorMap = { Up, Down, Left, Right, Vector2.Zero, Vector2.Zero };
        public static readonly string[] MemoryPrefixes = { "B", "KB", "MB", "GB", "TB" };
        // public static Vector3 ApplyTranslationMatrix(Vector3 vector, Matrix matrix) => vector - new Vector3(matrix.M41, matrix.M42, matrix.M43);
        // public static Vector2 ApplyTranslationMatrix(Vector2 vector, Matrix matrix) => vector - new Vector2(matrix.M41, matrix.M42);
        // public static Vector3 ApplyScaleMatrix(Vector3 vector, Matrix matrix) => vector * new Vector3(matrix.M11, matrix.M22, matrix.M33);
        // public static Vector2 ApplyScaleMatrix(Vector2 vector, Matrix matrix) => vector * new Vector2(matrix.M11, matrix.M22);
        public static readonly Action emptyDelegate = delegate { };
        public static readonly Color TransparentWhite = new Color(255, 255, 255, 0);
        public static float DivideOrHalf(int x, float divisor)
        {
            if (divisor == 0)
            {
                return 0.5f;
            }

            return x / divisor;
        }
        public static bool TryDequeueWhere<T>(ref Queue<T> queue, Func<T, bool> selector, out T value)
        {
            List<T> listedQueue = new List<T>(queue);
            for (int i = 0; i < queue.Count; i++)
            {
                value = listedQueue[i];

                if (selector(value))
                {
                    listedQueue.RemoveAt(i);
                    queue = new Queue<T>(listedQueue);
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public static unsafe void* Allocate(int size) => Allocate((uint)size);
        public static unsafe void* AllocateZeroed(int size) => AllocateZeroed((uint)size);
        public static unsafe void* Allocate(uint size) => NativeMemory.Alloc(new nuint(size));
        public static unsafe void* AllocateZeroed(uint size) => NativeMemory.AllocZeroed(new nuint(size));
        public static ref T GetReference<T>(ref T? nullable) where T : struct // me enoje >:(
            => ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(ref nullable));
        public static bool ShiftLeft<T>(ref T i, T zero) where T : IShiftOperators<T, int, T>, IEqualityOperators<T, T, bool>
        {
            i <<= 1;
            return i != zero;
        }
        public static void RunDelayed(Action action, float delay)
        {
            Task.Run(delegate
            {
                Thread.Sleep(Frames(delay));
                action();
            });
        }
        public static void RotateRadPreCalc(ref Vector2 v, float sin, float cos)
        {
            float tx = v.X;
            float ty = v.Y;
            v.X = cos * tx - sin * ty;
            v.Y = sin * tx + cos * ty;
        }
        public static T[] GetPow2Flags<T>(T value) where T : Enum
        {
            List<T> list = new List<T>();

            switch (value.GetTypeCode())
            {
                case TypeCode.SByte:
                    sbyte Int8 = Unsafe.As<T, sbyte>(ref value);
                {
                    sbyte i = 1;
                    do
                    {
                        if ((Int8 & i) == i)
                        {
                            list.Add(Unsafe.As<sbyte, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (sbyte)0));
                }
                    break;
                case TypeCode.Byte:
                    byte UInt8 = Unsafe.As<T, byte>(ref value);
                {
                    byte i = 1;
                    do
                    {
                        if ((UInt8 & i) == i)
                        {
                            list.Add(Unsafe.As<byte, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (byte)0));
                }
                    break;
                case TypeCode.Int16:
                    short Int16 = Unsafe.As<T, short>(ref value);
                {
                    short i = 1;
                    do
                    {
                        if ((Int16 & i) == i)
                        {
                            list.Add(Unsafe.As<short, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (short)0));
                }
                    break;
                case TypeCode.UInt16:
                    ushort UInt16 = Unsafe.As<T, ushort>(ref value);
                {
                    ushort i = 1;
                    do
                    {
                        if ((UInt16 & i) == i)
                        {
                            list.Add(Unsafe.As<ushort, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (ushort)0));
                }
                    break;
                case TypeCode.Int32:
                    int Int32 = Unsafe.As<T, int>(ref value);
                {
                    int i = 1;
                    do
                    {
                        if ((Int32 & i) == i)
                        {
                            list.Add(Unsafe.As<int, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (int)0));
                }
                    break;
                case TypeCode.UInt32:
                    uint UInt32 = Unsafe.As<T, uint>(ref value);
                {
                    uint i = 1;
                    do
                    {
                        if ((UInt32 & i) == i)
                        {
                            list.Add(Unsafe.As<uint, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (uint)0));
                }
                    break;
                case TypeCode.Int64:
                    long Int64 = Unsafe.As<T, long>(ref value);
                {
                    long i = 1;
                    do
                    {
                        if ((Int64 & i) == i)
                        {
                            list.Add(Unsafe.As<long, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (long)0));
                }
                    break;
                case TypeCode.UInt64:
                    ulong UInt64 = Unsafe.As<T, ulong>(ref value);
                {
                    ulong i = 1;
                    do
                    {
                        if ((UInt64 & i) == i)
                        {
                            list.Add(Unsafe.As<ulong, T>(ref i));
                        }
                    } while (ShiftLeft(ref i, (ulong)0));
                }
                    break;
            }

            return list.ToArray();
        }
        public static string ButtonToText(Buttons button, IntPtr controllerAddress = 0)
        {
#if DESKTOPGL
            switch (Sdl.GameController.GetName(controllerAddress))
#else
            switch ("")
#endif
            {
                case "PS4 Controller":
                default:
                    switch (button) // Playstation 4
                    {
                        case Buttons.DPadUp:
                            return "D-PAD ARRIBA";
                        case Buttons.DPadDown:
                            return "D-PAD ABAJO";
                        case Buttons.DPadLeft:
                            return "D-PAD IZQUIERDA";
                        case Buttons.DPadRight:
                            return "D-PAD DERECHA";
                        case Buttons.Start:
                            return "OPTIONS";
                        case Buttons.Back:
                            return "SHARE";
                        case Buttons.LeftStick:
                            return "APRETAR STICK IZQUIERDO";
                        case Buttons.RightStick:
                            return "APRETAR STICK DERECHO";
                        case Buttons.LeftShoulder:
                            return "L1";
                        case Buttons.RightShoulder:
                            return "R1";
                        case Buttons.BigButton:
                            return "BOTON PS";
                        case Buttons.A:
                            return "X";
                        case Buttons.B:
                            return "CIRCULO";
                        case Buttons.X:
                            return "CUADRADO";
                        case Buttons.Y:
                            return "TRIANGULO";
                        case Buttons.RightTrigger:
                            return "R2";
                        case Buttons.LeftTrigger:
                            return "L2";
                        case Buttons.RightThumbstickUp:
                            return "STICK DERECHO A ARRIBA";
                        case Buttons.RightThumbstickDown:
                            return "STICK DERECHO A ABAJO";
                        case Buttons.RightThumbstickRight:
                            return "STICK DERECHO A DERECHA";
                        case Buttons.RightThumbstickLeft:
                            return "STICK DERECHO A IZQUIERDA";
                        case Buttons.LeftThumbstickLeft:
                            return "STICK IZQUIERDO A IZQUIERDA";
                        case Buttons.LeftThumbstickUp:
                            return "STICK IZQUIERDO A ARRIBA";
                        case Buttons.LeftThumbstickDown:
                            return "STICK IZQUIERDO A ABAJO";
                        case Buttons.LeftThumbstickRight:
                            return "STICK IZQUIERDO A DERECHA";
                        //case Buttons.Misc1EXT:
                        //    return "MISCELANEO";
                        //case Buttons.Paddle1EXT:
                        //    return "PADDLE 1";
                        //case Buttons.Paddle2EXT:
                        //    return "PADDLE 2";
                        //case Buttons.Paddle3EXT:
                        //    return "PADDLE 3";
                        //case Buttons.Paddle4EXT:
                        //    return "PADDLE 4";
                        //case Buttons.TouchPadEXT:
                        //    return "TOUCH PAD";
                    }
                    break;
            }
            return string.Empty;
        }
        public static string KeyToText(Keys key)
        {
            switch (key)
            {
                case Keys.Enter:
                    return "ENTER";
                case Keys.NumPad0:
                    return "NUMPAD 0";
                case Keys.NumPad1:
                    return "NUMPAD 1";
                case Keys.NumPad2:
                    return "NUMPAD 2";
                case Keys.NumPad3:
                    return "NUMPAD 3";
                case Keys.NumPad4:
                    return "NUMPAD 4";
                case Keys.NumPad5:
                    return "NUMPAD 5";
                case Keys.NumPad6:
                    return "NUMPAD 6";
                case Keys.NumPad7:
                    return "NUMPAD 7";
                case Keys.NumPad8:
                    return "NUMPAD 8";
                case Keys.NumPad9:
                    return "NUMPAD 9";
                case Keys.NumLock:
                    return "NUMPAD LOCK";
                case Keys.F1:
                    return "F1";
                case Keys.F2:
                    return "F2";
                case Keys.F3:
                    return "F3";
                case Keys.F4:
                    return "F4";
                case Keys.F5:
                    return "F5";
                case Keys.F6:
                    return "F6";
                case Keys.F7:
                    return "F7";
                case Keys.F8:
                    return "F8";
                case Keys.F9:
                    return "F9";
                case Keys.F10:
                    return "F10";
                case Keys.F11:
                    return "F11";
                case Keys.F12:
                    return "F12";
                case Keys.PrintScreen:
                    return "IMPRIMIR PANTALLA";
                case Keys.Delete:
                    return "SUPRIMIR";
                case Keys.Space:
                    return "ESPACIO";
                case Keys.Up:
                    return "FLECHA ARRIBA";
                case Keys.Left:
                    return "FLECHA IZQUIERDA";
                case Keys.Down:
                    return "FLECHA ABAJO";
                case Keys.Right:
                    return "FLECHA DERECHA";
                case Keys.LeftAlt:
                    return "ALT IZQUIERDO";
                case Keys.RightAlt:
                    return "ALT DERECHO";
                case Keys.LeftControl:
                    return "CONTROL IZQUIERDO";
                case Keys.RightControl:
                    return "CONTROL DERECHO";
                case Keys.LeftShift:
                    return "SHIFT IZQUIERDO";
                case Keys.RightShift:
                    return "SHIFT DERECHO";
                case Keys.LeftWindows:
                    return "WINDOWS DERECHO";
                case Keys.RightWindows:
                    return "WINDOWS IZQUIERDO";
                case Keys.Tab:
                    return "TABULADOR";
                case Keys.CapsLock:
                    return "CAPS";
                case Keys.OemComma:
                    return "COMA";
                case Keys.OemPeriod:
                    return "PUNTO";
                case Keys.Back:
                    return "ATRAS";
                case Keys.Escape:
                    return "ESCAPE";
                case Keys.D0:
                    return "0";
                case Keys.D1:
                    return "1";
                case Keys.D2:
                    return "2";
                case Keys.D3:
                    return "3";
                case Keys.D4:
                    return "4";
                case Keys.D5:
                    return "5";
                case Keys.D6:
                    return "6";
                case Keys.D7:
                    return "7";
                case Keys.D8:
                    return "8";
                case Keys.D9:
                    return "9";
                case Keys.OemMinus:
                    return "-";
                default:
                    return Enum.GetName(key);
            }
        }
        public static string FormatMemory(uint bytes, byte index = 0)
        {
            while (bytes > 1024)
            {
                bytes >>= 10;
                index++;
            }
            return bytes + MemoryPrefixes[index];
        }
        public static string FormatMemory(long bytes, byte index = 0)
        {
            while (bytes > 1024)
            {
                bytes >>= 10;
                index++;
            }
            return bytes + MemoryPrefixes[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(int v) => Unsafe.As<int, uint>(ref v) >> 31 == 1;
        public static int Abs(int i) => i + (i >> 31) ^ i >> 31;
        public static float WrapAngle(float angle)
        {
            if (angle > -180 && angle <= 180)
            {
                return angle;
            }
            angle %= 360;
            if (angle <= -180)
            {
                return angle + 360;
            }
            if (angle > 180)
            {
                return angle - 360;
            }
            return angle;
        }
        public static float WrapRadian(float radian)
        {
            if (radian > -MathHelper.Pi && radian <= MathHelper.Pi)
            {
                return radian;
            }
            radian %= MathHelper.TwoPi;
            if (radian <= -MathHelper.Pi)
            {
                return radian + MathHelper.TwoPi;
            }
            if (radian > MathHelper.Pi)
            {
                return radian - MathHelper.TwoPi;
            }
            return radian;
        }
        public static Point DirectionToPoint(Direction direction) => directionPointMap[(int)direction];
        public static Vector2 DirectionToVector(Direction direction) => directionVectorMap[(int)direction];
        public static Direction VectorToDirection(Vector2 direction)
        {
            if (direction.X == 1 && direction.Y == 0)
            {
                return Direction.Left;
            }
            if (direction.X == -1 && direction.Y == 0)
            {
                return Direction.Right;
            }
            if (direction.X == 0 && direction.Y == 1)
            {
                return Direction.Down;
            }
            if (direction.X == 0 && direction.Y == -1)
            {
                return Direction.Up;
            }
            return Direction.None;
        }
        public static Direction InvertDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
            }
            return Direction.None;
        }
        public static Direction PointToDirection(Point direction)
        {
            if (direction == Left)
            {
                return Direction.Left;
            }
            if (direction == Right)
            {
                return Direction.Right;
            }
            if (direction == Down)
            {
                return Direction.Down;
            }
            if (direction == Up)
            {
                return Direction.Up;
            }
            return Direction.None;
        }
        public static bool Intersects(Rectangle rectangle1, Rectangle rectangle2, out Rectangle intersectionRectangle)
        {
            int num = Math.Max(rectangle1.Left, rectangle2.Left);
            int num2 = Math.Min(rectangle1.Right, rectangle2.Right);
            int num3 = Math.Max(rectangle1.Top, rectangle2.Top);
            int num4 = Math.Min(rectangle1.Bottom, rectangle2.Bottom);
            if (num2 >= num && num4 >= num3)
            {
                intersectionRectangle = new Rectangle(num, num3, num2 - num, num4 - num3);
                return true;
            }
            intersectionRectangle = Rectangle.Empty;
            return false;
        }

        public static int Round(double value)
        {
            double truc = Math.Truncate(value);
            if (truc == value) return (int)truc;
            double frac = Math.Abs(value - truc);
            if (value > 0)
            {
                return (int)(frac > 0.5 ? truc + 1 : truc);
            }
            return (int)(frac > 0.5 ? truc - 1 : truc);
        }
        public static float Modulas(float input, float divisor) => (input % divisor + divisor) % divisor;
        public static int Modulas(int input, int divisor) => (input % divisor + divisor) % divisor;
        public static short Modulas(short input, int divisor) => (short)((input % divisor + divisor) % divisor);
        internal static float Clamp01(float v) => MathHelper.Clamp(v, 0, 1);
        internal static float ClampMinus1Plus1(float v) => MathHelper.Clamp(v, -1, 1);
        /// <summary>
        ///     Returns a TimeSpan that lasts this amount of frames
        /// </summary>
        public static TimeSpan Frames(int frames) => TimeSpan.FromTicks(166666 * frames);
        /// <summary>
        ///     Returns a TimeSpan that lasts this amount of frames
        /// </summary>
        public static TimeSpan Frames(float frames) => TimeSpan.FromTicks((int)(166666f * frames));
        /// <summary>
        ///     Returns a color where the angle is the hue of this color.
        /// </summary>
        public static Color GetAngleColor(float angle) => HSLtoRGB(angle / 360, .5f, .5f);
        /// <summary>
        ///     Returns a color where the radian is the hue of this color.
        /// </summary>
        public static Color GetRadianColor(float radian) => HSLtoRGB(radian / MathHelper.TwoPi, .5f, .5f);
        public static Color HSLtoRGB(float hue, float sat, float lum)
        {
            if (sat == 0f)
                return new Vector3((byte)Math.Round(lum * 255.0)).Vector3ToColor();
            double num2 = !(lum < 0.5) ? lum + sat - lum * sat : lum * (1.0 + sat);
            double t = 2.0 * lum - num2;
            double c = hue + 0.33333333333333331;
            double c2 = hue;
            double c3 = hue - 0.33333333333333331;
            c = HUEtoRGB(c, t, num2);
            c2 = HUEtoRGB(c2, t, num2);
            c3 = HUEtoRGB(c3, t, num2);
            return new Color((byte)Math.Round(c * 255), (byte)Math.Round(c2 * 255), (byte)Math.Round(c3 * 255));
        }
        public static double HUEtoRGB(double c, double t1, double t2)
        {
            if (c < 0)
                c++;
            if (c > 1)
                c--;
            if (6.0 * c < 1)
                return t1 + (t2 - t1) * 6.0 * c;
            if (2.0 * c < 1)
                return t2;
            if (3.0 * c < 2)
                return t1 + (t2 - t1) * (2.0 / 3 - c) * 6.0;
            return t1;
        }
        public static void IfLowerThanSet<T>(ref T reference, T value) where T : IComparisonOperators<T, T, bool>
        {
            if (reference < value)
                reference = value;
        }
        public static void IfHigherThanSet<T>(ref T reference, T value) where T : IComparisonOperators<T, T, bool>
        {
            if (reference > value)
                reference = value;
        }
        public static void RandomMakeTrue(ref bool[] boolArray, int trueCount, ref GameRandom random)
        {
            StackList<int> falseIndices = new StackList<int>();
            for (int i = 0; i < boolArray.Length; i++)
            {
                if (!boolArray[i])
                {
                    falseIndices.Add(i);
                }
            }
            for (int i = 0; i < trueCount; i++)
            {
                int index = random.Next(falseIndices.Count);
                falseIndices.RemoveAt(index);
                boolArray[index] = true;
            }
        }
#pragma warning disable CS8500 // esto me esta rompiendo la error list pero es util asi que sera suprimido por estas 2 lineas :)
        public static unsafe T* Allocate<T>(int size) => (T*)Allocate((uint)(size * sizeof(T)));
        public static unsafe T* AllocateZeroed<T>(int size) => (T*)AllocateZeroed((uint)(size * sizeof(T)));
#pragma warning restore CS8500
    }
    public static class Extensions
    {
        public static readonly ushort[] PowersOf2 = { 2, 4, 8, 16, 32, 64, 128, 256 };
        public static unsafe ref T2 CastWithRef<T1, T2>(this T1 obj) => ref Unsafe.As<T1, T2>(ref obj);
        public static T AddDelegateOnce<T>(this T action, T addedAction) where T : Delegate
        {
            if (!action.GetInvocationList().Contains(addedAction))
            {
                return (T)Delegate.Combine(action, addedAction);
            }
            return action;
        }
        // https://github.com/kescherCode/RotatingPlatformFighter/blob/master/RotatingPlatformFighter/Program.cs#L197
        public static Vector3 RotateX(this Vector3 vector, float angle)
        {
            float cosa = MathF.Cos(angle);
            float sina = MathF.Sin(angle);

            float oldY = vector.Y;
            float oldZ = vector.Z;

            vector.Y = oldY * cosa - oldZ * sina;
            vector.Z = oldY * sina + oldZ * cosa;
            return vector;
        }
        public static Vector3 RotateY(this Vector3 vector, float angle)
        {
            float cosa = MathF.Cos(angle);
            float sina = MathF.Sin(angle);

            float oldX = vector.X;
            float oldZ = vector.Z;
            // New X and Z axis'
            vector.X = oldZ * sina + oldX * cosa;
            vector.Z = oldZ * cosa - oldX * sina;
            return vector;
        }
        public static Vector3 RotateZ(this Vector3 vector, float angle)
        {
            float cosa = MathF.Cos(angle);
            float sina = MathF.Sin(angle);

            float oldX = vector.X;
            float oldY = vector.Y;
            // New X and Y axis'
            vector.X = oldX * cosa - oldY * sina;
            vector.Y = oldY * cosa + oldX * sina;
            return vector;
        }
        public static Vector3 ToVector3(this Vector2 vector, float z = 0) => new Vector3(vector.X, vector.Y, z);
        /// <summary>
        ///     Converts the current Vector3 to a Vector2
        /// </summary>
        public static Vector2 ToVector2(this Vector3 vector) => new Vector2(vector.X, vector.Y);
        /// <summary>
        ///     Normalizes the current Vector2
        /// </summary>
        public static Vector2 Normalized(this Vector2 vector)
        {
            float mag = vector.Length();
            vector.X /= mag;
            vector.Y /= mag;
            return vector;
        }
        /// <summary>
        ///     Normalizes the current Vector2, in the case that the vector is of length 0, returns a zero vector
        /// </summary>
        public static Vector2 NormalizedOrZero(this Vector2 vector)
        {
            float mag = vector.Length();
            if (mag == 0)
                return Vector2.Zero;
            vector.X /= mag;
            vector.Y /= mag;
            return vector;
        }
        /// <summary>
        ///     Sets the length of this Vector2 if it passes the MaxLength threshold
        /// </summary>
        public static Vector2 MaxLength(this Vector2 vector, float MaxLength) => vector.Length() > MaxLength ? vector.Normalized() * MaxLength : vector;
        /// <summary>
        ///     Sets the length of this Vector2 if is lower than MaxLength
        /// </summary>
        public static Vector2 MinLength(this Vector2 vector, float MinLength) => vector.Length() < MinLength ? vector.Normalized() * MinLength : vector;
        /// <summary>
        ///     Sets the length of this Vector2
        /// </summary>
        public static Vector2 SetLength(this Vector2 vector, float length) => vector.Normalized() * length;
        public static Vector2 AddLength(this Vector2 vector, float addedLength, float limit = 0)
        {
            float length = MathF.Sqrt(vector.LengthSquared());
            if (length != 0 && addedLength != 0)
            {
                vector /= length;
                if (addedLength < 0 && length + addedLength < limit)
                {
                    addedLength = limit - length;
                }
                if (addedLength > 0 && length + addedLength > limit)
                {
                    addedLength = limit - length;
                }
                addedLength += length;
                vector *= addedLength;
            }
            return vector;
        }
        /// <summary>
        ///     Converts the given Vector3 to a Color
        /// </summary>
        public static Color Vector3ToColor(this Vector3 vector) => new Color(vector.X, vector.Y, vector.Z);
        /// <summary>
        ///     Writes 8 booleans to the current stream, packed in one byte
        /// </summary>
        public static void Write(this BinaryWriter writer, ISaveableObject saveableObject)
        {
            saveableObject.Save(writer);
        }
        /// <summary>
        ///     Writes 8 booleans to the current stream, packed in one byte
        /// </summary>
        public static void Write(this BinaryWriter writer, bool bool1 = false, bool bool2 = false, bool bool3 = false, bool bool4 = false, bool bool5 = false, bool bool6 = false, bool bool7 = false, bool bool8 = false)
        {
            byte value = (byte)(0 | (bool1 ? 1 : 0) | (bool2 ? 2 : 0) | (bool3 ? 4 : 0) | (bool4 ? 8 : 0) | (bool5 ? 16 : 0) | (bool6 ? 32 : 0) | (bool7 ? 64 : 0) | (bool8 ? 128 : 0));
            writer.Write(value);
        }
        /// <summary>
        ///     Writes a Point value to the current stream.
        /// </summary>
        public static void Write(this BinaryWriter writer, Point point)
        {
            writer.Write(point.X);
            writer.Write(point.Y);
        }
        /// <summary>
        ///     Writes a Vector2 value to the current stream.
        /// </summary>
        public static void Write(this BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
        }
        /// <summary>
        ///     Writes a Color value to the current stream.
        /// </summary>
        public static void Write(this BinaryWriter writer, Color vector)
        {
            writer.Write(vector.R);
            writer.Write(vector.G);
            writer.Write(vector.B);
            writer.Write(vector.A);
        }
        /// <summary>
        ///     Reads a Point value from the current stream and advances the current position.
        /// </summary>
        public static Point ReadPoint(this BinaryReader reader) => new Point(reader.ReadInt32(), reader.ReadInt32());
        /// <summary>
        ///     Reads a Vector2 value from the current stream and advances the current position.
        /// </summary>
        public static Vector2 ReadVector2(this BinaryReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
        /// <summary>
        ///     Reads a Color value from the current stream and advances the current position.
        /// </summary>
        public static Color ReadColor(this BinaryReader reader) => new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        /// <summary>
        ///     Reads 8 booleans packed in a byte from the current stream and advances the current position.
        /// </summary>
        public static bool[] ReadPackedBoolean(this BinaryReader reader)
        {
            byte value = reader.ReadByte();
            return new[]
            {
                (value & 1) == 1,
                (value & 2) == 2,
                (value & 4) == 4,
                (value & 8) == 8,
                (value & 16) == 16,
                (value & 32) == 32,
                (value & 64) == 64,
                (value & 128) == 128
            };
        }
        /// <summary>
        ///     Reads 8 booleans packed in a byte from the current stream and advances the current position.
        /// </summary>
        public static void ReadPackedBoolean(this BinaryReader reader, out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7, out bool v8)
        {
            byte value = reader.ReadByte();
            v1 = (value & 1) == 1;
            v2 = (value & 2) == 2;
            v3 = (value & 4) == 4;
            v4 = (value & 8) == 8;
            v5 = (value & 16) == 16;
            v6 = (value & 32) == 32;
            v7 = (value & 64) == 64;
            v8 = (value & 128) == 128;
        }
        /// <summary>
        ///     Gives the size of this Texture2D.
        /// </summary>
        public static Vector2 Size(this Texture2D texture) => texture.Bounds.Size();
        /// <summary>
        ///     Gives the size of this Rectangle.
        /// </summary>
        public static Vector2 Size(this Rectangle rectangle) => new Vector2(rectangle.Width, rectangle.Height);
        public static Vector2 ToVector2(this Point vector2) => new Vector2(vector2.X, vector2.Y);
        public static Point ToPoint(this Vector2 vector2) => new Point((int)vector2.X, (int)vector2.Y);
        public static int GetDirection(this bool val) => (int)~(Unsafe.As<bool, uint>(ref val) << 31) >> 30;
        public static int GetDirection(this int val) => GetDirection((double)val);
        public static int GetDirection(this float val) => GetDirection((double)val);
        public static int GetDirection(this double val) => Math.Sign(val);
        public static float ToZero(this float value, in float add)
        {
            if (value > 0 && value - add < 0) return 0;
            if (value < 0 && value + add > 0) return 0;
            if (value > 0) value -= add;
            if (value < 0) value += add;
            return value;
        }
        public static bool TrySetValue<T>(this Effect effect, string parameter, T value)
        {
            EffectParameter param = effect.Parameters[parameter];
            if (param is null) return false;
            switch (value)
            {
                case Vector3 vector3:
                    param.SetValue(vector3);
                    break;
                case Vector4[] vector4Array:
                    param.SetValue(vector4Array);
                    break;
                case Vector4 vector4:
                    param.SetValue(vector4);
                    break;
                case Vector3[] vector3Array:
                    param.SetValue(vector3Array);
                    break;
                case Vector2[] vector2Array:
                    param.SetValue(vector2Array);
                    break;
                case Quaternion quaternion:
                    param.SetValue(quaternion);
                    break;
                case Texture2D texture:
                    param.SetValue(texture);
                    break;
                case float[] singleArray:
                    param.SetValue(singleArray);
                    break;
                case float single:
                    param.SetValue(single);
                    break;
                case Matrix[] matrixArray:
                    param.SetValue(matrixArray);
                    break;
                case Matrix matrix:
                    param.SetValue(matrix);
                    break;
                case int integer:
                    param.SetValue(integer);
                    break;
                case bool boolean:
                    param.SetValue(boolean);
                    break;
                case Vector2 vector2:
                    param.SetValue(vector2);
                    break;
                case int[] integerArray:
                    param.SetValue(integerArray);
                    break;
            }
            return true;
        }
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            switch (degrees % 360)
            {
                case 0f:
                    return v;
                case 90f:
                    (v.X, v.Y) = (-v.Y, v.X);
                    return v;
                case 180f:
                    return -v;
                case 270f:
                    (v.X, v.Y) = (v.Y, -v.X);
                    return v;
                default:
                    (float Sin, float Cos) = MathF.SinCos(degrees * (MathHelper.Pi / 180f));

                    float tx = v.X;
                    float ty = v.Y;
                    v.X = Cos * tx - Sin * ty;
                    v.Y = Sin * tx + Cos * ty;
                    return v;
            }
        }
        public static Vector2 RotateWithAnchor(this Vector2 v, float degrees, Vector2 spinningAnchor = default)
        {
            (float Sin, float Cos) = MathF.SinCos(degrees * (MathHelper.Pi / 180f));
            Vector2 vector = v - spinningAnchor;
            v.X = spinningAnchor.X + vector.X * Cos - vector.Y * Sin;
            v.Y = spinningAnchor.Y + vector.X * Sin + vector.Y * Cos;
            return v;
        }
        public static Vector2 RotateRad(this Vector2 v, float radians)
        {
            switch (radians % MathHelper.TwoPi)
            {
                case 0:
                    return v;
                case MathHelper.PiOver2:
                    (v.X, v.Y) = (-v.Y, v.X);
                    return v;
                case MathHelper.Pi:
                    return -v;
                case MathHelper.PiOver2 * 3:
                    (v.X, v.Y) = (v.Y, -v.X);
                    return v;
                default:
                    (float Sin, float Cos) = MathF.SinCos(radians);

                    float tx = v.X;
                    float ty = v.Y;
                    v.X = Cos * tx - Sin * ty;
                    v.Y = Sin * tx + Cos * ty;
                    return v;
            }
        }
        public static Vector2 RotateRad(this Vector2 v, double radians)
        {
            switch (radians % MathHelper.TwoPi)
            {
                case 0:
                    return v;
                case MathHelper.PiOver2:
                    (v.X, v.Y) = (-v.Y, v.X);
                    return v;
                case MathHelper.Pi:
                    return -v;
                case MathHelper.PiOver2 * 3:
                    (v.X, v.Y) = (v.Y, -v.X);
                    return v;
                default:
                    (double Sin, double Cos) = Math.SinCos(radians);

                    float tx = v.X;
                    float ty = v.Y;
                    v.X = (float)(Cos * tx - Sin * ty);
                    v.Y = (float)(Sin * tx + Cos * ty);
                    return v;
            }
        }
        public static Vector2 RotateRadWithAnchor(this Vector2 v, float radians, Vector2 spinningAnchor = default)
        {
            (float Sin, float Cos) = MathF.SinCos(radians);
            Vector2 vector = v - spinningAnchor;
            v.X = spinningAnchor.X + vector.X * Cos - vector.Y * Sin;
            v.Y = spinningAnchor.Y + vector.X * Sin + vector.Y * Cos;
            return v;
        }
        public static void SafelyExecute(Action action) => SafelyExecute(action, delegate { });
        public static void SafelyExecute(Action action, Action<Exception> errorAction)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                errorAction(e);
            }
        }

        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None, float layerDepth = 0)
        {
            spriteBatch.Draw(texture, position, sourceRectangle, color, rotation, origin, scale, spriteEffects, layerDepth);
        }

        public static Color[] GetColors(this Texture2D texture)
        {
            Color[] colors = new Color[texture.Width * texture.Height];
            texture.GetData(colors);
            return colors;
        }
        public static Color[,] GetColors2D(this Texture2D texture)
        {
            Color[,] colors = new Color[texture.Width, texture.Height];
            Color[] colorDirect = texture.GetColors();
            for (int x = 0; x < texture.Width; x++)
                for (int y = 0; y < texture.Height; y++)
                    colors[x, y] = colorDirect[y * texture.Width + x];
            return colors;
        }
        public static Vector3 ToHSL(this Color color)
        {
            float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;

            float val = Math.Max(Math.Max(r, g), b);
            float val2 = Math.Min(Math.Min(r, g), b);
            float num4 = 0f;
            float num5 = (val + val2) / 2f;
            float y = 0;
            if (val != val2)
            {
                float num6 = val - val2;
                y = num5 > 0.5 ? num6 / (2f - val - val2) : num6 / (val + val2);
                if (val == r)
                    num4 = (g - b) / num6 + (g < b ? 6 : 0);
                if (val == g)
                    num4 = (b - r) / num6 + 2f;
                if (val == b)
                    num4 = (r - g) / num6 + 4f;
                num4 /= 6f;
            }
            return new Vector3(num4, y, num5);
        }
        public static Vector2 RoundToNearestDegree(this Vector2 vector2, float degree)
        {
            float angle = MathHelper.ToDegrees(MathF.Atan2(vector2.Y, vector2.X));

            if (angle % degree == 0) return vector2;
            float newAngle = MathHelper.ToRadians(MathF.Round(angle / degree) * degree);
            (float sin, float cos) = MathF.SinCos(newAngle);
            vector2 = new Vector2(cos, sin) * vector2.Length();
            return vector2;
        }
        public static void DrawHollowCircle(this DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 radius = default, Color? color = null)
        {
            Vector2 add = radius;
            DrawHollowCircleCommon(spriteBatch, position, MathF.Max(radius.X, radius.Y), color, add);
        }
        public static void DrawHollowCircle(this DepthlessSpriteBatch spriteBatch, Vector2 position, float radius = 1, Color? color = null)
        {
            Vector2 add = new Vector2(radius);
            DrawHollowCircleCommon(spriteBatch, position, radius, color, add);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawHollowCircleCommon(DepthlessSpriteBatch spriteBatch, Vector2 position, float size, Color? color, Vector2 add)
        {
            string textureName;
            switch (size)
            {
                case <= 2:
                    textureName = "Circle2";
                    break;
                case <= 4:
                    textureName = "HollowCircle4";
                    break;
                case <= 8:
                    textureName = "HollowCircle8";
                    break;
                case <= 16:
                    textureName = "HollowCircle16";
                    break;
                case <= 32:
                    textureName = "HollowCircle32";
                    break;
                case <= 64:
                    textureName = "HollowCircle64";
                    break;
                case <= 128:
                    textureName = "HollowCircle128";
                    break;
                default:
                    textureName = "HollowCircle256";
                    break;
            }
            PushCircle(spriteBatch, position, add, textureName, color ?? Color.White);
        }

        public static void DrawCircle(this DepthlessSpriteBatch spriteBatch, Vector2 position, float size = 1, Color? color = null)
        {
            Vector2 add = new Vector2(size / 2);
            DrawCircleCommon(spriteBatch, position, add, size, color);
        }
        public static void DrawCircle(this DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 size = default, Color? color = null)
        {
            Vector2 add = size / 2;
            DrawCircleCommon(spriteBatch, position, add, MathF.Max(size.X, size.Y), color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawCircleCommon(DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 add, float size, Color? color)
        {
            string textureName;
            switch (size)
            {
                case <= 2:
                    textureName = "Circle2";
                    break;
                case <= 4:
                    textureName = "Circle4";
                    break;
                case <= 8:
                    textureName = "Circle8";
                    break;
                case <= 16:
                    textureName = "Circle16";
                    break;
                case <= 32:
                    textureName = "Circle32";
                    break;
                case <= 64:
                    textureName = "Circle64";
                    break;
                case <= 128:
                    textureName = "Circle128";
                    break;
                default:
                    textureName = "Circle256";
                    break;
            }
            PushCircle(spriteBatch, position, add, textureName, color ?? Color.White);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushCircle(DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 add, string textureName, Color color)
        {
            DepthlessSpriteBatchItem depthlessSpriteBatchItem = spriteBatch.batcher.CreateBatchItem();
            depthlessSpriteBatchItem.Texture = Assets.Textures[textureName];
            depthlessSpriteBatchItem.vertexTL.Position.X = position.X - add.X;
            depthlessSpriteBatchItem.vertexTL.Position.Y = position.Y - add.Y;
            depthlessSpriteBatchItem.vertexTR.Position.X = position.X + add.X;
            depthlessSpriteBatchItem.vertexTR.Position.Y = position.Y - add.Y;
            depthlessSpriteBatchItem.vertexBL.Position.X = position.X - add.X;
            depthlessSpriteBatchItem.vertexBL.Position.Y = position.Y + add.Y;
            depthlessSpriteBatchItem.vertexBR.Position.X = position.X + add.X;
            depthlessSpriteBatchItem.vertexBR.Position.Y = position.Y + add.Y;
            depthlessSpriteBatchItem.vertexTL.TextureCoordinate.X = 0f;
            depthlessSpriteBatchItem.vertexTL.TextureCoordinate.Y = 0f;
            depthlessSpriteBatchItem.vertexTR.TextureCoordinate.X = 1f;
            depthlessSpriteBatchItem.vertexTR.TextureCoordinate.Y = 0f;
            depthlessSpriteBatchItem.vertexBL.TextureCoordinate.X = 0f;
            depthlessSpriteBatchItem.vertexBL.TextureCoordinate.Y = 1f;
            depthlessSpriteBatchItem.vertexBR.TextureCoordinate.X = 1f;
            depthlessSpriteBatchItem.vertexBR.TextureCoordinate.Y = 1f;
            depthlessSpriteBatchItem.vertexTL.Color = color;
            depthlessSpriteBatchItem.vertexTR.Color = color;
            depthlessSpriteBatchItem.vertexBL.Color = color;
            depthlessSpriteBatchItem.vertexBR.Color = color;
            spriteBatch.FlushIfNeeded();
        }
        public static Color MultiplyIgnoreAlpha(this Color color, float rgbMult, bool clamp = false)
        {
            if (clamp)
            {
                color.R = (byte)MathHelper.Clamp(color.R * rgbMult, 0, byte.MaxValue);
                color.G = (byte)MathHelper.Clamp(color.G * rgbMult, 0, byte.MaxValue);
                color.B = (byte)MathHelper.Clamp(color.B * rgbMult, 0, byte.MaxValue);
            }
            else
            {
                color.R = (byte)(color.R * rgbMult);
                color.G = (byte)(color.G * rgbMult);
                color.B = (byte)(color.B * rgbMult);
            }
            return color;
        }
        public static Color MultiplyAlpha(this Color color, float aMult, bool clamp = false)
        {
            if (clamp)
                color.A = (byte)MathHelper.Clamp(color.A * aMult, 0, byte.MaxValue);
            else
                color.A = (byte)(color.A * aMult);
            return color;
        }
        public static ref TValue GetReference<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) => ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
        public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 position, Vector2 size = default, Color? color = null, float rotation = 0)
        {
            size = size == default ? Vector2.One : size;
            color ??= Color.Red;
            spriteBatch.Draw(Assets.Textures["SinglePixel"], position, null, color.Value, rotation, Vector2.One / 2, size);
        }
        public static void DrawHollowLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, float width, Color? color = null)
        {
            Color color2 = color ?? Color.Red;
            Vector2 vector2 = new Vector2(0f, width / 2f).RotateRad((end - start).ToRadians());
            ref GameTexture singlePixel = ref Assets.Textures["SinglePixel"];
            PushLine(ref spriteBatch, singlePixel, start, end, color2, vector2);
        }
        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, float width, Color? color = null)
        {
            Color color2 = color ?? Color.Red;
            Vector2 vector2 = new Vector2(0f, width / 2f).RotateRad((end - start).ToRadians());
            PushLine(ref spriteBatch, Assets.Textures["SinglePixel"], start, end, color2, vector2);
        }
        public static void DrawLine(this SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, float width, Color? color = null)
        {
            Color color2 = color ?? Color.Red;
            Vector2 vector2 = new Vector2(0f, width / 2f).RotateRad((end - start).ToRadians());
            PushLine(ref spriteBatch, texture ?? Assets.Textures["SinglePixel"], start, end, color2, vector2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushLine(ref SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, Vector2 vector2)
        {
            SpriteBatchItem spriteBatchItem = spriteBatch.batcher.CreateBatchItem();
            spriteBatchItem.Texture = texture;
            spriteBatchItem.vertexTL.Position.X = start.X + vector2.X;
            spriteBatchItem.vertexTL.Position.Y = start.Y + vector2.Y;
            spriteBatchItem.vertexTR.Position.X = start.X - vector2.X;
            spriteBatchItem.vertexTR.Position.Y = start.Y - vector2.Y;
            spriteBatchItem.vertexBL.Position.X = end.X + vector2.X;
            spriteBatchItem.vertexBL.Position.Y = end.Y + vector2.Y;
            spriteBatchItem.vertexBR.Position.X = end.X - vector2.X;
            spriteBatchItem.vertexBR.Position.Y = end.Y - vector2.Y;
            spriteBatchItem.vertexTL.TextureCoordinate.X = 0f;
            spriteBatchItem.vertexTL.TextureCoordinate.Y = 0f;
            spriteBatchItem.vertexTR.TextureCoordinate.X = 1f;
            spriteBatchItem.vertexTR.TextureCoordinate.Y = 0f;
            spriteBatchItem.vertexBL.TextureCoordinate.X = 0f;
            spriteBatchItem.vertexBL.TextureCoordinate.Y = 1f;
            spriteBatchItem.vertexBR.TextureCoordinate.X = 1f;
            spriteBatchItem.vertexBR.TextureCoordinate.Y = 1f;
            spriteBatchItem.vertexTL.Color = color;
            spriteBatchItem.vertexTR.Color = color;
            spriteBatchItem.vertexBL.Color = color;
            spriteBatchItem.vertexBR.Color = color;
            spriteBatch.FlushIfNeeded();
        }
        public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, float width = 1f, Color? color = null)
        {
            float halfWidth = 0.5f * width;
            Texture2D texture = Assets.Textures["SinglePixel"];
            Color finalColor = color ?? Color.Red;
            GenerateRectangleCornerItem(rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Top, rectangle.Right, rectangle.Top, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(rectangle.Right, rectangle.Top, rectangle.Right, rectangle.Bottom, rectangle.Right, rectangle.Top, rectangle.Right, rectangle.Bottom, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(rectangle.Left, rectangle.Bottom, rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Bottom, rectangle.Right, rectangle.Bottom, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom, spriteBatch, halfWidth, texture, finalColor);
        }
        public static void DrawRectangle(this SpriteBatch spriteBatch, float left, float right, float top, float bottom, float width = 1f, Color? color = null)
        {
            float halfWidth = 0.5f * width;
            Texture2D texture = Assets.Textures["SinglePixel"];
            Color finalColor = color ?? Color.Red;
            GenerateRectangleCornerItem(left, top, left, top, right, top, right, top, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(right, top, right, bottom, right, top, right, bottom, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(left, bottom, left, bottom, right, bottom, right, bottom, spriteBatch, halfWidth, texture, finalColor);
            GenerateRectangleCornerItem(left, top, left, bottom, left, top, left, bottom, spriteBatch, halfWidth, texture, finalColor);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateRectangleCornerItem(float pos0X, float pos0Y, float pos2X, float pos2Y, float pos1X, float pos1Y, float pos3X, float pos3Y, SpriteBatch spriteBatch, float halfWidth, Texture2D texture, Color finalColor)
        {
            SpriteBatchItem spriteBatchItem = spriteBatch.batcher.CreateBatchItem();
            spriteBatchItem.Texture = texture;
            spriteBatchItem.vertexTL.Position.X = pos0X - halfWidth;
            spriteBatchItem.vertexTL.Position.Y = pos0Y - halfWidth;
            spriteBatchItem.vertexTR.Position.X = pos1X + halfWidth;
            spriteBatchItem.vertexTR.Position.Y = pos1Y - halfWidth;
            spriteBatchItem.vertexBL.Position.X = pos2X - halfWidth;
            spriteBatchItem.vertexBL.Position.Y = pos2Y + halfWidth;
            spriteBatchItem.vertexBR.Position.X = pos3X + halfWidth;
            spriteBatchItem.vertexBR.Position.Y = pos3Y + halfWidth;
            spriteBatchItem.vertexTL.TextureCoordinate.X = 0f;
            spriteBatchItem.vertexTL.TextureCoordinate.Y = 0f;
            spriteBatchItem.vertexTR.TextureCoordinate.X = 1f;
            spriteBatchItem.vertexTR.TextureCoordinate.Y = 0f;
            spriteBatchItem.vertexBL.TextureCoordinate.X = 0f;
            spriteBatchItem.vertexBL.TextureCoordinate.Y = 1f;
            spriteBatchItem.vertexBR.TextureCoordinate.X = 1f;
            spriteBatchItem.vertexBR.TextureCoordinate.Y = 1f;
            spriteBatchItem.vertexTL.Color = finalColor;
            spriteBatchItem.vertexTR.Color = finalColor;
            spriteBatchItem.vertexBL.Color = finalColor;
            spriteBatchItem.vertexBR.Color = finalColor;
            spriteBatch.FlushIfNeeded();
        }
        public static void DrawLifeBarCentered(this DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 size, float progress, Color aliveColor, Color deadColor)
        {
            float diff = size.X * progress;
            position -= size / 2;
            Vector2 aliveSize = new Vector2(diff, size.Y);
            Vector2 deadSize = new Vector2(size.X - diff, size.Y);
            Vector2 deadPos = new Vector2(position.X + aliveSize.X, position.Y);
            if (aliveSize.X > 0)
                spriteBatch.Draw(Assets.Textures["SinglePixel"], position, null, aliveColor, 0f, Vector2.Zero, aliveSize);
            if (deadSize.X > 0)
                spriteBatch.Draw(Assets.Textures["SinglePixel"], deadPos, null, deadColor, 0f, Vector2.Zero, deadSize);
        }
        public static void DrawLifeBar(this DepthlessSpriteBatch spriteBatch, Vector2 position, Vector2 size, float progress, Color aliveColor, Color deadColor)
        {
            Vector2 aliveSize = new Vector2(size.X * progress, size.Y);
            Vector2 deadPos = new Vector2(position.X + aliveSize.X, position.Y);
            Vector2 deadSize = new Vector2(size.X * (1f - progress), size.Y);
            if (aliveSize.X > 0)
                spriteBatch.Draw(Assets.Textures["SinglePixel"], position, null, aliveColor, 0f, Vector2.Zero, aliveSize);
            if (deadSize.X > 0)
                spriteBatch.Draw(Assets.Textures["SinglePixel"], deadPos, null, deadColor, 0f, Vector2.Zero, deadSize);
        }
        public static void DrawLifeBar(this DepthlessSpriteBatch spriteBatch, Rectangle rectangle, float progress, Color aliveColor, Color deadColor)
        {
            Rectangle aliveRect = rectangle;
            aliveRect.Width = (int)(aliveRect.Width * progress);
            Rectangle deadRect = rectangle;
            deadRect.X += aliveRect.Width;
            deadRect.Width = (int)(deadRect.Width * (1f - progress));
            spriteBatch.Draw(Assets.Textures["SinglePixel"], aliveRect, null, aliveColor);
            spriteBatch.Draw(Assets.Textures["SinglePixel"], deadRect, null, deadColor);
        }
        public static void DrawLines(this SpriteBatch spriteBatch, PolygonLineData[] datas)
        {
            foreach (PolygonLineData line in datas)
            {
                foreach (ushort index in line.connectedIndexs.Where(v => v < datas.Length))
                {
                    spriteBatch.DrawLine(line.point, datas[index].point, line.width, line.color);
                }
            }
        }
        public static void DrawLines(this SpriteBatch spriteBatch, ReadOnlySpan<Vector2> positions, float width = 1, Color? color = null)
        {
            int length = positions.Length;
            for (int i = 0; i < length; i++)
            {
                Vector2 next;
                if (i + 1 == length)
                {
                    next = positions[0];
                }
                else
                {
                    next = positions[i + 1];
                }
                spriteBatch.DrawLine(positions[i], next, width, color);
            }
        }
        public static void DrawLines(this SpriteBatch spriteBatch, Vector2[] positions, float width = 1, Color? color = null)
        {
            int length = positions.Length;
            for (int i = 0; i < length; i++)
            {
                Vector2 next;
                if (i + 1 == length)
                {
                    next = positions[0];
                }
                else
                {
                    next = positions[i + 1];
                }
                spriteBatch.DrawLine(positions[i], next, width, color);
            }
        }
        public static void DrawLines(this SpriteBatch spriteBatch, Vector2[] positions, Func<float, float> widthFunction, Func<float, Color> colorFunction)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                float progress = (float)i / positions.Length;
                spriteBatch.DrawLine(positions[i], positions[(i + 1) % positions.Length], widthFunction(progress), colorFunction(progress));
            }
        }
        public static float ToAngle(this Vector2 vector) => MathHelper.ToDegrees(MathF.Atan2(vector.Y, vector.X));
        public static float ToRadians(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);
        public static Point RoundVector2WithNegativeCheck(this Vector2 vector2, int divisor)
        {
            Point point = new Point((int)(vector2.X / divisor), (int)(vector2.Y / divisor));
            if (vector2.X < 0)
                point.X--;
            if (vector2.Y < 0)
                point.Y--;
            return point;
        }
        public static Point RoundVector2WithNegativeCheck(this Vector2 vector2, Vector2 divisor)
        {
            Point point = new Point(Utils.Round(vector2.X / divisor.X), Utils.Round(vector2.Y / divisor.X));
            if (vector2.X < 0)
                point.X--;
            if (vector2.Y < 0)
                point.Y--;
            return point;
        }
        public static string WrapText(this string text, out Vector2 measure, float lineWidth, float scale = 1)
        {
            float spaceLeft = lineWidth, lastSpaceWidth = 0;
            StringBuilder result = new StringBuilder();
            int lastSpace = 0;
            ReadOnlySpan<char> span = text.AsSpan();
            Vector2 totalMeasure = Vector2.Zero, currentMeasure = Vector2.Zero;
            for (int i = 0; i < span.Length; i++)
            {
                char chr = span[i];
                switch (chr)
                {
                    case ' ':
                        float width = TextRenderer.spaceWidth * scale;
                        currentMeasure.X += width;
                        spaceLeft -= width;
                        lastSpaceWidth = 0;
                        lastSpace = i;
                        break;
                    case '\n':
                        spaceLeft = lineWidth;
                        currentMeasure.X -= TextRenderer.spacing;
                        if (totalMeasure.X < currentMeasure.X)
                        {
                            totalMeasure.X = currentMeasure.X;
                        }
                        totalMeasure.Y += currentMeasure.Y + TextRenderer.lineSpacing * scale;
                        currentMeasure.X = 0;
                        currentMeasure.Y = 0;
                        break;
                    default:
                        (float chrWidth, float y) = TextRenderer.MeasureChar(chr) * scale;
                        currentMeasure.X += chrWidth + TextRenderer.spacing * scale;
                        if (y > currentMeasure.Y)
                        {
                            currentMeasure.Y = y;
                        }
                        spaceLeft -= chrWidth;
                        lastSpaceWidth += chrWidth;
                        if (spaceLeft <= 0)
                        {
                            result[lastSpace] = '\n';
                            currentMeasure.X -= TextRenderer.spacing;
                            if (totalMeasure.X < currentMeasure.X)
                            {
                                totalMeasure.X = currentMeasure.X;
                            }
                            totalMeasure.Y += currentMeasure.Y + TextRenderer.lineSpacing * scale;
                            currentMeasure.X = 0;
                            currentMeasure.Y = 0;
                            spaceLeft += lineWidth + lastSpaceWidth;
                        }
                        break;
                }
                result.Append(chr);
            }
            if (totalMeasure.Y > TextRenderer.lineSpacing * scale)
                totalMeasure.Y -= TextRenderer.lineSpacing * scale;
            measure = totalMeasure;
            return result.ToString();
        }
        public static string WrapText(this string text, float lineWidth, float scale = 1)
        {
            float spaceLeft = lineWidth, lastSpaceWidth = 0;
            StringBuilder result = new StringBuilder();
            int lastSpace = 0;
            ReadOnlySpan<char> span = text.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                char chr = span[i];
                switch (chr)
                {
                    case ' ':
                        spaceLeft -= TextRenderer.spaceWidth * scale;
                        lastSpaceWidth = 0;
                        lastSpace = i;
                        break;
                    case '\n':
                        spaceLeft = lineWidth;
                        break;
                    default:
                        float chrWidth = TextRenderer.MeasureChar(chr).X * scale;
                        spaceLeft -= chrWidth;
                        lastSpaceWidth += chrWidth;
                        if (spaceLeft <= 0)
                        {
                            result[lastSpace] = '\n';
                            spaceLeft += lineWidth - lastSpaceWidth;
                            lastSpaceWidth = 0;
                        }
                        break;
                }
                result.Append(chr);
            }
            return result.ToString();
        }
        public static string WrapText(ref string text, float lineWidth, float scale = 1)
        {
            string[] words = text.Split(' ');
            float spaceLeft = lineWidth;
            StringBuilder result = new StringBuilder();

            foreach (string word in words)
            {
                float wordWidth = TextRenderer.MeasureString(word).X * scale;
                if (wordWidth + 6 > spaceLeft)
                {
                    result.AppendLine();
                    spaceLeft += lineWidth - wordWidth;
                }
                else
                {
                    spaceLeft -= wordWidth + 6;
                }
                result.Append(word);
                result.Append(' ');
            }
            return result.ToString();
        }
        public static IEnumerable<T> WhereRemove<T>(this List<T> list, Func<T, bool> selector)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T value = list[i];
                if (!selector(value)) continue;
                list.RemoveAt(i);
                yield return value;
                i--;
            }
        }
        public static T[] DequeueRangeWhere<T>(this Queue<T> queue, Func<T, bool> selector)
        {
            List<T> result = new List<T>(), listedQueue = new List<T>(queue);
            for (int i = 0; i < queue.Count; i++)
            {
                T value = listedQueue[i];
                if (selector(value))
                    result.Add(value);
            }
            return result.ToArray();
        }
        public static bool DequeueRange<T>(this Queue<T> queue, out T[] result)
        {
            List<T> _result = new List<T>();
            while (queue.Count > 0)
            {
                _result.Add(queue.Dequeue());
            }
            result = _result.ToArray();
            return result.Length > 0;
        }
        public static IEnumerable<T> DequeueRange<T>(this Queue<T> queue)
        {
            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }
        public static T ValueOrFallback<T>(this T value, T fallback) => value is null || value.Equals(default(T)) ? fallback : value;
        public static bool DequeueRange<T>(this Queue<T> queue, int size, out T[] result)
        {
            List<T> _result = new List<T>();
            for (int i = 0; i < size && queue.Count > 0; i++)
            {
                _result.Add(queue.Dequeue());
            }
            result = _result.ToArray();
            return result.Length > 0;
        }
        public static IEnumerable<T> DequeueRange<T>(this Queue<T> queue, int size)
        {
            for (int i = 0; i < size && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }
        public static T Log<T>(this T obj)
        {
            lock (obj)
            {
                Logger.LogMessage(obj + Environment.NewLine);
            }

            return obj;
        }
    }
    public static class Constants
    {
        public const int VirtualWidth = 1280, VirtualHeight = 720;
        public const string saveFile = "./Settings.dat", ThemeCacheFolder = "./CachedThemes";
        public static readonly Rectangle WorldRectangle = new Rectangle(VirtualWidth / -2, VirtualHeight / -2, VirtualWidth, VirtualHeight);
        public static readonly Rectangle VirtualRectangle = new Rectangle(0, 0, VirtualWidth, VirtualHeight);
        public static readonly ReadonlyVector Up = new ReadonlyVector(0, -1), Down = new ReadonlyVector(0, 1), Left = new ReadonlyVector(-1, 0), Right = new ReadonlyVector(1, 0);
        public static readonly ReadonlyVector VirtualResolution = new ReadonlyVector(VirtualWidth, VirtualHeight);
        public static readonly ReadonlyVector VirtualMidResolution = new ReadonlyVector(VirtualWidth / 2f, VirtualHeight / 2f);
        public static readonly ReadonlyVector[] Directions = [Up, Down, Left, Right];
    }
    [DebuggerDisplay("X = {X}, Y = {Y}")]
    public readonly struct ReadonlyVector
    {
        public ReadonlyVector(float x, float y)
        {
            X = x;
            Y = y;
            vector2 = new Vector2(x, y);
            point = new Point((int)x, (int)y);
        }
        public ReadonlyVector(Vector2 vector)
        {
            X = vector.X;
            Y = vector.Y;
            vector2 = vector;
            point = new Point((int)X, (int)Y);
        }
        public readonly float X, Y;
        public readonly Vector2 vector2;
        public readonly Point point;
        public static implicit operator Vector2(ReadonlyVector read) => read.vector2;
        public static implicit operator Point(ReadonlyVector read) => read.point;
    }
    public interface ISaveableObject
    {
        public void Save(BinaryWriter writer);
        public void Read(BinaryReader reader);
        public void SaveSnippet(BinaryWriter writer);
        public void ReadSnippet(BinaryReader reader);
    }
    [Flags]
    public enum Direction : byte
    {
        None,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
    }
    public ref struct StackList<T>
    {
        public const int DefaultCapacity = 4;
        public T[] items;
        public int size;
        public StackList()
        {
            items = Array.Empty<T>();
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public StackList(int capacity)
        {
            items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        }

        public int Capacity
        {
            get => items.Length;
            set
            {
                if (value == items.Length) return;
                if (value > 0)
                {
                    T[] newItems = new T[value];
                    if (size > 0)
                    {
                        Array.Copy(items, 0, newItems, 0, size);
                    }
                    items = newItems;
                }
                else
                {
                    items = Array.Empty<T>();
                }
            }
        }

        public int Count => size;
        public ref T this[int index] => ref items[index];

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (size == items.Length) EnsureCapacity(size + 1);
            items[size++] = item;
        }

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => Array.BinarySearch(items, index, count, item, comparer);
        public int BinarySearch(T item) => BinarySearch(0, Count, item, null);
        public int BinarySearch(T item, IComparer<T> comparer) => BinarySearch(0, Count, item, comparer);
        // Clears the contents of List.
        public void Clear()
        {
            if (size <= 0) return;
            Array.Clear(items, 0, size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            size = 0;
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < size; i++)
                    if (items[i] == null)
                        return true;
                return false;
            }
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < size; i++)
            {
                if (c.Equals(items[i], item)) return true;
            }
            return false;
        }

        public StackList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            StackList<TOutput> list = new StackList<TOutput>(size);
            for (int i = 0; i < size; i++)
            {
                list.items[i] = converter(items[i]);
            }
            list.size = size;
            return list;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        public void CopyTo(T[] array) => CopyTo(array, 0);

        public void CopyTo(int index, T[] array, int arrayIndex, int count) =>
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(items, index, array, arrayIndex, count);

        public void CopyTo(T[] array, int arrayIndex) =>
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(items, 0, array, arrayIndex, size);

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        public void EnsureCapacity(int min)
        {
            if (items.Length < min)
            {
                int newCapacity = items.Length == 0 ? DefaultCapacity : items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        public bool Exists(Predicate<T> match) => FindIndex(match) != -1;

        public T Find(Predicate<T> match)
        {
            for (int i = 0; i < size; i++)
            {
                if (match(items[i]))
                {
                    return items[i];
                }
            }
            return default;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < size; i++)
            {
                if (match(items[i]))
                {
                    list.Add(items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match) => FindIndex(0, size, match);

        public int FindIndex(int startIndex, Predicate<T> match) => FindIndex(startIndex, size - startIndex, match);

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(items[i])) return i;
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            for (int i = size - 1; i >= 0; i--)
            {
                if (match(items[i]))
                {
                    return items[i];
                }
            }
            return default;
        }

        public int FindLastIndex(Predicate<T> match) => FindLastIndex(size - 1, size, match);

        public int FindLastIndex(int startIndex, Predicate<T> match) => FindLastIndex(startIndex, startIndex + 1, match);

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            for (int i = 0; i < size; i++)
            {
                action(items[i]);
            }
        }

        public StackList<T> GetRange(int index, int count)
        {
            StackList<T> list = new StackList<T>(count);
            Array.Copy(items, index, list.items, 0, count);
            list.size = count;
            return list;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item) => Array.IndexOf(items, item, 0, size);

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index) => Array.IndexOf(items, item, index, size - index);

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index, int count) => Array.IndexOf(items, item, index, count);

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            if (size == items.Length) EnsureCapacity(size + 1);
            if (index < size)
            {
                Array.Copy(items, index, items, index + 1, size - index);
            }
            items[index] = item;
            size++;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
            if (size == 0)
            {
                // Special case for empty list
                return -1;
            }
            return LastIndexOf(item, size - 1, size);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index) => LastIndexOf(item, index, index + 1);

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count)
        {
            if (size == 0)
            {
                // Special case for empty list
                return -1;
            }
            return Array.LastIndexOf(items, item, index, count);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match)
        {
            int freeIndex = 0; // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < size && !match(items[freeIndex])) freeIndex++;
            if (freeIndex >= size) return 0;

            int current = freeIndex + 1;
            while (current < size)
            {
                // Find the first item which needs to be kept.
                while (current < size && match(items[current])) current++;

                if (current < size)
                {
                    // copy item to the free slot.
                    items[freeIndex++] = items[current++];
                }
            }

            Array.Clear(items, freeIndex, size - freeIndex);
            int result = size - freeIndex;
            size = freeIndex;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            size--;
            if (index < size)
            {
                Array.Copy(items, index + 1, items, index, size - index);
            }
            items[size] = default;
        }

        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                size -= count;
                if (index < size)
                {
                    Array.Copy(items, index + count, items, index, size - index);
                }
                Array.Clear(items, size, count);
            }
        }

        // Reverses the elements in this list.
        public void Reverse() => Reverse(0, Count);

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // 
        // This method uses the Array.Reverse method to reverse the
        // elements.
        // 
        public void Reverse(int index, int count) => Array.Reverse(items, index, count);

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort() => Sort(0, Count, null);

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer) => Sort(0, Count, comparer);

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer) => Array.Sort(items, index, count, comparer);

        // ToArray returns a new Object array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] GetArray() => items;
        public T[] ToArray()
        {
            T[] array = new T[size];
            Array.Copy(items, 0, array, 0, size);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess() => Capacity = size;

        public bool TrueForAll(Predicate<T> match)
        {
            for (int i = 0; i < size; i++)
            {
                if (!match(items[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public Enumerator GetEnumerator() => new Enumerator(this);
        public ref struct Enumerator
        {
            // Token: 0x06001B26 RID: 6950 RVA: 0x005F5BD4 File Offset: 0x005F5BD4
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(StackList<T> list)
            {
                _list = list;
                _index = -1;
            }

            // Token: 0x06001B27 RID: 6951 RVA: 0x005F5BE4 File Offset: 0x005F5BE4
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int num = _index + 1;
                if (num < _list.Count)
                {
                    _index = num;
                    return true;
                }
                return false;
            }

            // Token: 0x17000260 RID: 608
            // (get) Token: 0x06001B28 RID: 6952 RVA: 0x005F5C14 File Offset: 0x005F5C14
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _list[_index];
            }
            // Token: 0x040005A3 RID: 1443
            public readonly StackList<T> _list;
            // Token: 0x040005A4 RID: 1444
            public int _index;
        }
        public Span<T> AsSpan() => new Span<T>(items, 0, size);
        public ReadOnlySpan<T> AsReadonlySpan() => new ReadOnlySpan<T>(items, 0, size);
    }
    [DebuggerDisplay("${ToString(),nq}")]
    public struct SmallPoint
    {
        public sbyte X
        {
            get
            {
                sbyte val = (sbyte)((data & 0xF0) >> 4);
                if (val >> 3 != 0)
                {
                    val |= unchecked((sbyte)0xF0);
                }
                return val;
            }
            set => data = (byte)(data & 0x0F | value << 4);
        }
        public sbyte Y
        {
            get
            {
                sbyte val = (sbyte)(data & 0x0F);
                if (val >> 3 != 0)
                {
                    val |= unchecked((sbyte)0xF0);
                }
                return val;
            }
            set => data = (byte)(data & 0xF0 | value & 0x0F);
        }
        public byte data;
        public string Bits => Convert.ToString(data, 2).PadLeft(8, '0');
        public override string ToString() => $"{{X: {X}, Y: {Y}}}";
    }
}
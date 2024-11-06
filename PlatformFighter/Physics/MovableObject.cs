using Microsoft.Xna.Framework;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PlatformFighter.Physics
{
	public interface IMovableObject<T>
	{
		public ref float VelocityX { get; }
		public ref float VelocityY { get; }
		public ref float PositionX { get; }
		public ref float PositionY { get; }
		public float CenterX { get; set; }
		public float CenterY { get; set; }
		public ref Vector2 Velocity { get; }
		public Vector2 Center { get; set; }
		public Vector2 Size { get; set; }
		public ref Vector2 Position { get; }
		public T Width { get; set; }
		public T Height { get; set; }
		public ref MovableObjectRectangle Rectangle { get; }
		public Vector2 BottomRight => Position + Size;
		public Vector2 TopLeft => Position;
	}
	[StructLayout(LayoutKind.Explicit)]
	public struct CompressedMovableObject : IMovableObject<float>
	{
		public CompressedMovableObject(Vector2 Position, Vector2 Size)
		{
			_position = Position;
			_size = Size;
			_velocity = Vector2.Zero;
		}

		public CompressedMovableObject(Vector2 Position, float Width, float Height)
		{
			_position = Position;
			_size = new Vector2(Width, Height);
			_velocity = Vector2.Zero;
		}

		[FieldOffset(0)]
		internal MovableObjectRectangle _rectangle;
		[FieldOffset(0)]
		internal Vector2 _position;
		[FieldOffset(8)]
		internal Vector2 _size;
		[FieldOffset(20)]
		internal Vector2 _velocity;
		public unsafe ref float VelocityX => ref _velocity.X;
		public unsafe ref float VelocityY => ref _velocity.Y;
		public unsafe ref float PositionX => ref _position.X;
		public unsafe ref float PositionY => ref _position.Y;
		public unsafe ref Vector2 Velocity => ref _velocity;
		public unsafe ref Vector2 Position => ref _position;
		public float Width
		{
			get => _size.X;
			set => _size.X = value;
		}
		public float Height
		{
			get => _size.Y;
			set => _size.Y = value;
		}
		public Vector2 Size
		{
			get => _size;
			set => _size = value;
		}
		public float CenterX
		{
			get => _position.X + _size.X / 2;
			set => _position.X = value - _size.X / 2;
		}
		public float CenterY
		{
			get => _position.Y + _size.Y / 2;
			set => _position.Y = value - _size.Y / 2;
		}
		public Vector2 Center
		{
			get => _position + _size / 2;
			set => _position = value - _size / 2;
		}
		public unsafe ref MovableObjectRectangle Rectangle => ref _rectangle;

		public override bool Equals(object obj)
		{
			if (obj is CompressedMovableObject movable)
				return movable.Rectangle == Rectangle && movable.Velocity == Velocity;

			return false;
		}

		public override int GetHashCode() => Rectangle.GetHashCode() ^ Velocity.GetHashCode();

		public override string ToString() => "{X: " + PositionX + " Y: " + PositionY + " Width: " + Width + " Height: " + Height + "VelocityX: " + VelocityX + " VelocityY:" + VelocityY + "}";

		public static bool operator ==(CompressedMovableObject left, CompressedMovableObject right) => left.Equals(right);

		public static bool operator !=(CompressedMovableObject left, CompressedMovableObject right) => !(left == right);
	}

	// :DDDDDDDDDDDDDDDDDDDDDDDD codigo copiado gracias monogame y xna por ser opensoruc e
	[DataContract]
	[DebuggerDisplay("{DebugDisplayString,nq}")]
	public struct MovableObjectRectangle : IEquatable<MovableObjectRectangle>
	{
		#region Public Fields
		[DataMember]
		public float X;
		[DataMember]
		public float Y;
		[DataMember]
		public float Width;
		[DataMember]
		public float Height;
		#endregion

		#region Public Properties
		public static MovableObjectRectangle Empty => new MovableObjectRectangle();
		public float Left => X;
		public float Right => X + Width;
		public float Top => Y;
		public float Bottom => Y + Height;
		public bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;
		public Vector2 Location
		{
			get => new Vector2(X, Y);
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}
		public Vector2 Size
		{
			get => new Vector2(Width, Height);
			set
			{
				Width = value.X;
				Height = value.Y;
			}
		}
		public Vector2 Center => new Vector2(X + Width / 2f, Y + Height / 2f);
		#endregion

		#region Internal Properties
		internal string DebugDisplayString => string.Concat(
			X, "  ",
			Y, "  ",
			Width, "  ",
			Height
		);
		#endregion

		#region Constructors
		public MovableObjectRectangle(Rectangle rectangle)
		{
			X = rectangle.X;
			Y = rectangle.Y;
			Width = (ushort)rectangle.Width;
			Height = (ushort)rectangle.Height;
		}

		public MovableObjectRectangle(float x, float y, float width, float height)
		{
			X = x;
			Y = y;
			Width = (ushort)width;
			Height = (ushort)height;
		}

		public MovableObjectRectangle(Vector2 location, Vector2 size)
		{
			X = location.X;
			Y = location.Y;
			Width = size.X;
			Height = size.Y;
		}
		#endregion

		#region Operators
		public static implicit operator Rectangle(MovableObjectRectangle mor) => new Rectangle((int)mor.X, (int)mor.Y, (int)mor.Width, (int)mor.Height);

		public static implicit operator MovableObjectRectangle(Rectangle rectangle) => new MovableObjectRectangle(rectangle);

		public static bool operator ==(MovableObjectRectangle a, MovableObjectRectangle b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

		public static bool operator !=(MovableObjectRectangle a, MovableObjectRectangle b) => !(a == b);
		#endregion

		#region Public Methods
		public bool Contains(int x, int y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

		public bool Contains(float x, float y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

		public bool Contains(Point value) => X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

		public bool Contains(Vector2 value) => X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

		public bool Contains(MovableObjectRectangle value) => X <= value.X && value.Right <= Right && Y <= value.Y && value.Bottom <= Bottom;

		public override bool Equals(object obj) => obj is MovableObjectRectangle movableObjectRectangle && this == movableObjectRectangle;

		public bool Equals(MovableObjectRectangle other) => this == other;

		public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

		public void Inflate(float horizontalAmount, float verticalAmount)
		{
			X -= horizontalAmount;
			Y -= verticalAmount;
			Width += horizontalAmount * 2;
			Height += verticalAmount * 2;
		}

		public bool Intersects(MovableObjectRectangle value) => value.Left < Right &&
		                                                        Left < value.Right &&
		                                                        value.Top < Bottom &&
		                                                        Top < value.Bottom;

		public void Intersects(ref MovableObjectRectangle value, out bool result)
		{
			result = value.Left < Right &&
			         Left < value.Right &&
			         value.Top < Bottom &&
			         Top < value.Bottom;
		}

		public static MovableObjectRectangle Intersect(MovableObjectRectangle value1, MovableObjectRectangle value2)
		{
			Intersect(ref value1, ref value2, out MovableObjectRectangle rectangle);

			return rectangle;
		}

		public static void Intersect(ref MovableObjectRectangle value1, ref MovableObjectRectangle value2, out MovableObjectRectangle result)
		{
			if (value1.Intersects(value2))
			{
				float right_side = Math.Min(value1.X + value1.Width, value2.X + value2.Width);
				float left_side = Math.Max(value1.X, value2.X);
				float top_side = Math.Max(value1.Y, value2.Y);
				float bottom_side = Math.Min(value1.Y + value1.Height, value2.Y + value2.Height);
				result = new MovableObjectRectangle(left_side, top_side, right_side - left_side, bottom_side - top_side);
			}
			else
			{
				result = Empty;
			}
		}

		public void Offset(float offsetX, float offsetY)
		{
			X += (int)offsetX;
			Y += (int)offsetY;
		}

		public void Offset(Point amount)
		{
			X += amount.X;
			Y += amount.Y;
		}

		public void Offset(Vector2 amount)
		{
			X += amount.X;
			Y += amount.Y;
		}

		public override string ToString() => "{X:" + X + " Y:" + Y + " Width:" + Width + " Height:" + Height + "}";

		public static MovableObjectRectangle Union(MovableObjectRectangle value1, MovableObjectRectangle value2)
		{
			float x = Math.Min(value1.X, value2.X);
			float y = Math.Min(value1.Y, value2.Y);

			return new MovableObjectRectangle(x, y,
				MathF.Max(value1.Right, value2.Right) - x,
				MathF.Max(value1.Bottom, value2.Bottom) - y);
		}

		public static void Union(ref MovableObjectRectangle value1, ref MovableObjectRectangle value2, out MovableObjectRectangle result)
		{
			result.X = Math.Min(value1.X, value2.X);
			result.Y = Math.Min(value1.Y, value2.Y);
			result.Width = MathF.Max(value1.Right, value2.Right) - result.X;
			result.Height = MathF.Max(value1.Bottom, value2.Bottom) - result.Y;
		}

		public void Deconstruct(out float x, out float y, out float width, out float height)
		{
			x = X;
			y = Y;
			width = Width;
			height = Height;
		}
		#endregion

		public static MovableObjectRectangle FromCenter(float x, float y, float width, float height) => new MovableObjectRectangle(x - width / 2, y - height / 2, width, height);
	}
}
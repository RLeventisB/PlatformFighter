using Editor.Objects;

using Microsoft.Xna.Framework;

using PlatformFighter.Entities;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Stages;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Physics
{
	public static class Collision
	{
		public static bool LineRectangleCollision(Rectangle rectangle, Vector2 lineStart, Vector2 lineEnd)
		{
			bool
				left = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom),
				bottom = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Bottom),
				right = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Right, rectangle.Bottom, rectangle.Right, rectangle.Top),
				top = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Right, rectangle.Top, rectangle.Left, rectangle.Top);

			if (!left && !bottom && !right && !top)
			{
				return rectangle.Contains(lineStart) || rectangle.Contains(lineEnd);
			}

			return true;
		}

		public static bool LineRectangleCollision(Rectangle rectangle, Vector2 lineStart, Vector2 lineEnd, out Vector2[] intersections)
		{
			bool
				left = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Left, rectangle.Top, rectangle.Left, rectangle.Bottom, out float leftX, out float leftY),
				bottom = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Bottom, out float bottomX, out float bottomY),
				right = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Right, rectangle.Bottom, rectangle.Right, rectangle.Top, out float rightX, out float rightY),
				top = LineLineCollision(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, rectangle.Right, rectangle.Top, rectangle.Left, rectangle.Top, out float topX, out float topY);

			if (!left && !bottom && !right && !top)
			{
				intersections = Array.Empty<Vector2>();

				return rectangle.Contains(lineStart) && rectangle.Contains(lineEnd);
			}

			List<Vector2> intList = new List<Vector2>(4);
			if (left)
				intList.Add(new Vector2(leftX, leftY));

			if (bottom)
				intList.Add(new Vector2(bottomX, bottomY));

			if (right)
				intList.Add(new Vector2(rightX, rightY));

			if (top)
				intList.Add(new Vector2(topX, topY));

			intersections = intList.ToArray();

			return true;
		}

		public static bool LineLineCollision(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, out float intersectionX, out float intersectionY)
		{
			LineLineCollisionCommon(x1, y1, x2, y2, x3, y3, x4, y4, out float v4, out float v5, out float uA, out float uB);

			bool colliding = uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
			intersectionX = x1 + uA * v4;
			intersectionY = y1 + uA * v5;

			return colliding;
		}

		public static bool LineLineCollision(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
		{
			LineLineCollisionCommon(x1, y1, x2, y2, x3, y3, x4, y4, out float _, out float _, out float uA, out float uB);

			return uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
		}

		public static void LineLineCollisionCommon(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, out float v4, out float v5, out float uA, out float uB)
		{
			// ?????????? https://www.jeffreythompson.org/collision-detection/line-rect.php
			float v = x4 - x3, v1 = y1 - y3, v2 = y4 - y3, v3 = x1 - x3;
			v4 = x2 - x1;
			v5 = y2 - y1;
			uA = (v * v1 - v2 * v3) / (v2 * v4 - v * v5);
			uB = (v4 * v1 - v5 * v3) / (v2 * v4 - v * v5);
		}

		// https://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection/402010#402010
		public static bool CircleRectangleCollision(Vector2 position, float size, Vector2 rectangleCenter, Vector2 rectangleSize)
		{
			Vector2 sizeByTwo = rectangleSize / 2;
			Vector2 circleDistance = (position - rectangleCenter).Abs();

			return CircleRectangleCollisionCheck(size, sizeByTwo, circleDistance);
		}

		public static bool CircleRectangleCollision(Vector2 position, Vector2 size, Vector2 rectangleCenter, Vector2 rectangleSize)
		{
			Vector2 sizeByTwo = rectangleSize / 2;
			Vector2 circleDistance = (position - rectangleCenter).Abs();

			return CircleRectangleCollisionCheck(size, circleDistance, sizeByTwo);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CircleRectangleCollisionCheck(Vector2 size, Vector2 circleDistance, Vector2 sizeByTwo)
		{
			if (circleDistance.X > sizeByTwo.X + size.X)
			{
				return false;
			}

			if (circleDistance.Y > sizeByTwo.Y + size.Y)
			{
				return false;
			}

			if (circleDistance.X <= sizeByTwo.X)
			{
				return true;
			}

			if (circleDistance.Y <= sizeByTwo.Y)
			{
				return true;
			}

			if (MathF.Pow(circleDistance.X - sizeByTwo.X, 2) <= MathF.Pow(size.X / 2, 2))
				return true;

			if (MathF.Pow(circleDistance.Y - sizeByTwo.Y, 2) <= MathF.Pow(size.Y / 2, 2))
				return true;

			return false;

			//float cornerDistance_sq = MathF.Pow(circleDistance.X - sizeByTwo.X, 2) + MathF.Pow(circleDistance.Y - sizeByTwo.Y, 2);
			//return cornerDistance_sq <= MathF.Pow((size.X + size.Y) / 2, 2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CircleRectangleCollisionCheck(float size, Vector2 sizeByTwo, Vector2 circleDistance)
		{
			if (circleDistance.X > sizeByTwo.X + size)
			{
				return false;
			}

			if (circleDistance.Y > sizeByTwo.Y + size)
			{
				return false;
			}

			if (circleDistance.X <= sizeByTwo.X)
			{
				return true;
			}

			if (circleDistance.Y <= sizeByTwo.Y)
			{
				return true;
			}

			float cornerDistance_sq = MathF.Pow(circleDistance.X - sizeByTwo.X, 2) + MathF.Pow(circleDistance.Y - sizeByTwo.Y, 2);

			return cornerDistance_sq <= MathF.Pow(size, 2);
		}

		public static bool CircleCircleCollision(Vector2 circle1Pos, float circle1Radius, Vector2 circle2Pos, float circle2Radius)
			=> MathF.Pow(circle2Pos.X - circle1Pos.X, 2) + MathF.Pow(circle2Pos.Y - circle1Pos.Y, 2) <= MathF.Pow(circle1Radius + circle2Radius, 2);

		public static bool CircleCircleCollision(Vector2 circle1Pos, float circle1Radius, Vector2 circle2Pos, Vector2 circle2Radius)
			=> MathF.Pow(circle2Pos.X - circle1Pos.X, 2) + MathF.Pow(circle2Pos.Y - circle1Pos.Y, 2) <= (circle1Radius + circle2Radius.X) * (circle1Radius + circle2Radius.Y);

		public static bool CircleCircleCollision(Vector2 circle1Pos, Vector2 circle1Radius, Vector2 circle2Pos, float circle2Radius)
			=> MathF.Pow(circle2Pos.X - circle1Pos.X, 2) + MathF.Pow(circle2Pos.Y - circle1Pos.Y, 2) <= (circle1Radius.X + circle2Radius) * (circle1Radius.Y + circle2Radius);

		public static bool CircleCircleCollision(Vector2 circle1Pos, Vector2 circle1Radius, Vector2 circle2Pos, Vector2 circle2Radius)
			=> MathF.Pow(circle2Pos.X - circle1Pos.X, 2) + MathF.Pow(circle2Pos.Y - circle1Pos.Y, 2) <= (circle1Radius.X + circle2Radius.X) * (circle1Radius.Y + circle2Radius.Y);

		public static CollisionPrecalculation GetCollisionCalculations(IMovableObject<float> movableObject) => new CollisionPrecalculation(movableObject);

		public static void GetCollidingObjects(IMovableObject<float> movableObject, in CollisionPrecalculation calc, out CollisionData[] collidingObjects)
		{
			StackList<CollisionData> result = new StackList<CollisionData>();

			foreach (WorldObject stageObject in GameWorld.CurrentStage.objects)
			{
				MovableObjectRectangle positionRectangle = movableObject.Rectangle;
				MovableObjectRectangle newPositionRectangle = calc.Rectangle;
				MovableObjectRectangle stageObjectRectangle = stageObject.MovableObject.Rectangle;
				MovableObjectRectangle overlappingRectangle = MovableObjectRectangle.Intersect(stageObjectRectangle, newPositionRectangle);
				
				if (!overlappingRectangle.IsEmpty)
				{
					Direction collidedDirections = Direction.None;
					if (positionRectangle.Bottom < stageObjectRectangle.Top && newPositionRectangle.Bottom >= stageObjectRectangle.Top)
					{
						collidedDirections |= Direction.Up;
					}
					if (positionRectangle.Right < stageObjectRectangle.Left && newPositionRectangle.Right >= stageObjectRectangle.Left)
					{
						collidedDirections |=Direction.Right;
					}
					if (positionRectangle.Left > stageObjectRectangle.Right && newPositionRectangle.Left <= stageObjectRectangle.Right)
					{
						collidedDirections |=Direction.Left;
					}
					if (positionRectangle.Top > stageObjectRectangle.Bottom && newPositionRectangle.Top <= stageObjectRectangle.Bottom)
					{
						 collidedDirections |= Direction.Down;
					}
					result.Add(new CollisionData(stageObject, overlappingRectangle, collidedDirections));
				}
			}

			collidingObjects = result.ToArray();
		}

		public static Direction ResolveCollisions(ref IMovableObject<float> movableObject, in CollisionPrecalculation calc, in CollisionData[] collidingObjects)
		{
			Direction direction = Direction.None;
			foreach (CollisionData collisionData in collidingObjects)
			{
				Direction localDirection = Direction.None;
				WorldObject worldObject = collisionData.WorldObject;
				const float epsilonBcThisIsDumb = 0.001f;

				foreach (Direction flag in Utils.GetPow2Flags(collisionData.CollidedDirections))
				{
					localDirection |= flag;
					switch (flag)
					{
						case Direction.Up:
							movableObject.PositionY = worldObject.MovableObject.Rectangle.Top - movableObject.Height - epsilonBcThisIsDumb;
							movableObject.VelocityY = 0;
							break;
						case Direction.Left:
							movableObject.PositionX = worldObject.MovableObject.Rectangle.Right + epsilonBcThisIsDumb;
							movableObject.VelocityX = 0;
							break;
						case Direction.Right:
							movableObject.VelocityX = (worldObject.MovableObject.Rectangle.Left - movableObject.Width) + epsilonBcThisIsDumb;
							movableObject.VelocityX = 0;
							break;
						case Direction.Down:
							movableObject.VelocityY = 0;
							movableObject.PositionY = worldObject.MovableObject.Rectangle.Bottom + epsilonBcThisIsDumb;
							break;
					}
				}

				direction |= localDirection;
			}

			return direction;
		}

		public readonly ref struct CollisionPrecalculation
		{
			public readonly MovableObjectRectangle Rectangle;

			public CollisionPrecalculation(IMovableObject<float> movableObject)
			{
				Rectangle = movableObject.Rectangle;
				Rectangle.Location += movableObject.Velocity;
			}
		}
		public readonly struct CollisionData
		{
			public readonly WorldObject WorldObject;
			public readonly MovableObjectRectangle OverlappingRectangle;
			public readonly Direction CollidedDirections;
			public CollisionData(WorldObject WorldObject, MovableObjectRectangle OverlappingRectangle, Direction CollidedDirections)
			{
				this.WorldObject = WorldObject;
				this.OverlappingRectangle = OverlappingRectangle;
				this.CollidedDirections = CollidedDirections;
			}
		}

		#region Codigo por IA el cual no puedo manifestar de aqui a 60 a�os mas (ya estare jubilado)
		// todos los metodos adentro de esta region fueron creados por ia de perplexity (https://www.perplexity.ai/search/7eba8d60-a7d3-4e85-a2da-2172fb17a741?s=c)
		// me da paja citar todas las fuentes porque no todas son lo que requeria pero todo esta ahi!!! (incluyendo mis datos personales (espero que no))

		#region Colision punto adentro de poligono
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointInsidePolygon(Vector2 point, Span<Vector2> polygon) => IsPointInsidePolygon(point.X, point.Y, polygon);

		public static bool IsPointInsidePolygon(float pointX, float pointY, Span<Vector2> polygon)
		{
			// https://www.perplexity.ai/search/d7b195a3-927e-466a-a8aa-3230e51af8f1?s=c
			// Check if the point is inside the polygon using the winding number algorithm
			int windingNumber = 0;

			for (int i = 0; i < polygon.Length; i++)
			{
				ref Vector2 point = ref polygon[i];
				ref Vector2 nextPoint = ref polygon[i + 1 & polygon.Length];

				if (point.Y <= pointY)
				{
					if (nextPoint.Y > pointY && IsLeft(pointX, pointY, point.X, point.Y, nextPoint.X, nextPoint.Y) > 0)
					{
						windingNumber++;
					}
				}
				else
				{
					if (nextPoint.Y <= pointY && IsLeft(pointX, pointY, point.X, point.Y, nextPoint.X, nextPoint.Y) < 0)
					{
						windingNumber--;
					}
				}
			}

			return windingNumber != 0;
		}

		private static float IsLeft(float pointX, float pointY, float startX, float startY, float endX, float endY) => (endX - startX) * (pointY - startY) - (pointX - startX) * (endY - startY);
		#endregion

		#region Codigo elipse adentro de linea con ancho
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEllipseInsideLine(Vector2 ellipsePos, Vector2 ellipseSize, Vector2 lineStart, Vector2 lineEnd, float lineWidth) => IsEllipseInsideLine(ellipsePos.X, ellipsePos.Y, ellipseSize.X, ellipseSize.Y, lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, lineWidth);

		public static bool IsEllipseInsideLine(float ellipseX, float ellipseY, float ellipseWidth, float ellipseHeight, float lineStartX, float lineStartY, float lineEndX, float lineEndY, float lineWidth) =>

			// Calculate the distance from the center of the ellipse to the line
			// Check if the distance is less than the radius of the ellipse minus half the line width
			DistanceFromPointToLine(ellipseX, ellipseY, lineStartX, lineStartY, lineEndX, lineEndY) <= MathF.Max(ellipseWidth, ellipseHeight) / 2 + lineWidth / 2;
		#endregion

		#region Codigo linea con ancho adentro de linea con ancho (esto es necesario te lo juro)
		public static bool IsLineInsideLine(Vector2 line1Start, Vector2 line1End, float line1Width, Vector2 line2Start, Vector2 line2End, float line2Width) => IsLineInsideLine(line1Start.X, line1Start.Y, line1End.X, line1End.Y, line1Width, line2Start.X, line2Start.Y, line2End.X, line2End.Y, line2Width);

		public static bool IsLineInsideLine(float line1StartX, float line1StartY, float line1EndX, float line1EndY, float line1Width, float line2StartX, float line2StartY, float line2EndX, float line2EndY, float line2Width)
		{
			// Calculate the direction of the lines
			float thisLineDirX = line1EndX - line1StartX;
			float thisLineDirY = line1EndY - line1StartY;
			float otherLineDirX = line2EndX - line2StartX;
			float otherLineDirY = line2EndY - line2StartY;

			// Calculate the perpendicular vectors
			float thisLinePerpX = -thisLineDirY;
			float thisLinePerpY = thisLineDirX;
			float otherLinePerpX = -otherLineDirY;
			float otherLinePerpY = otherLineDirX;

			// Normalize the perpendicular vectors
			float thisLinePerpLength = MathF.Sqrt(thisLinePerpX * thisLinePerpX + thisLinePerpY * thisLinePerpY);
			thisLinePerpX /= thisLinePerpLength;
			thisLinePerpY /= thisLinePerpLength;
			float otherLinePerpLength = MathF.Sqrt(otherLinePerpX * otherLinePerpX + otherLinePerpY * otherLinePerpY);
			otherLinePerpX /= otherLinePerpLength;
			otherLinePerpY /= otherLinePerpLength;

			// Calculate the half widths
			float thisHalfWidth = line1Width / 2;
			float otherHalfWidth = line2Width / 2;

			// Calculate the gap between the lines
			float distX = (line2StartX + line2EndX - line1StartX - line1EndX) / 2;
			float distY = (line2StartY + line2EndY - line1StartY - line1EndY) / 2;
			float gap = Math.Abs(distX * thisLinePerpX + distY * thisLinePerpY) - thisHalfWidth - otherHalfWidth;

			// Check for a gap
			if (gap > 0)
			{
				return false; // No collision
			}

			// Calculate the gap between the lines in the other direction
			distX = (line1StartX + line1EndX - line2StartX - line2EndX) / 2;
			distY = (line1StartY + line1EndY - line2StartY - line2EndY) / 2;
			gap = Math.Abs(distX * otherLinePerpX + distY * otherLinePerpY) - thisHalfWidth - otherHalfWidth;

			// Check for a gap in the other direction
			return gap > 0;
		}
		#endregion

		#region Codigo rectangulo adentro de linea con ancho
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRectangleInsideLine(Vector2 rectanglePos, Vector2 rectangleSize, Vector2 lineStart, Vector2 lineEnd, float lineWidth) => IsRectangleInsideLine(rectanglePos.X, rectanglePos.Y, rectangleSize.X, rectangleSize.Y, lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, lineWidth);

		public static bool IsRectangleInsideLine(float rectLeft, float rectTop, float rectRight, float rectBottom, float lineStartX, float lineStartY, float lineEndX, float lineEndY, float lineWidth)
		{
			// Calculate the distance from each corner of the rectangle to the line
			float distanceTopLeft = DistanceFromPointToLine(rectLeft, rectTop, lineStartX, lineStartY, lineEndX, lineEndY);
			float distanceTopRight = DistanceFromPointToLine(rectRight, rectTop, lineStartX, lineStartY, lineEndX, lineEndY);
			float distanceBottomLeft = DistanceFromPointToLine(rectLeft, rectBottom, lineStartX, lineStartY, lineEndX, lineEndY);
			float distanceBottomRight = DistanceFromPointToLine(rectRight, rectBottom, lineStartX, lineStartY, lineEndX, lineEndY);

			// Check if all distances are less than half the line width
			float lineWidthHalf = lineWidth / 2;

			return distanceTopLeft <= lineWidthHalf || distanceTopRight <= lineWidthHalf || distanceBottomLeft <= lineWidthHalf || distanceBottomRight <= lineWidthHalf;
		}
		#endregion

		#region Codigo circulo adentro de linea con ancho
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsCircleInsideLine(Vector2 circlePos, float circleRadius, Vector2 lineStart, Vector2 lineEnd, float lineWidth) => IsCircleInsideLine(circlePos.X, circlePos.Y, circleRadius, lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, lineWidth);

		public static bool IsCircleInsideLine(float circleX, float circleY, float circleRadius, float lineStartX, float lineStartY, float lineEndX, float lineEndY, float lineWidth)
		{
			// Calculate the distance from the center of the circle to the line
			// Check if the distance is less than the radius of the circle minus half the line width
			float lineWidthHalf = lineWidth / 2;

			return DistanceFromPointToLine(circleX, circleY, lineStartX, lineStartY, lineEndX, lineEndY) <= circleRadius + lineWidthHalf;
		}
		#endregion

		#region Codigo punto adentro de linea con ancho
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPointInsideOfLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float lineWidth) => IsPointInsideOfLine(point.X, point.Y, lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y, lineWidth);

		public static bool IsPointInsideOfLine(float pointX, float pointY, float lineStartX, float lineStartY, float lineEndX, float lineEndY, float lineWidth) =>

			// Calculate the distance from the point to the line
			// Check if the distance is less than half the line width
			DistanceFromPointToLine(pointX, pointY, lineStartX, lineStartY, lineEndX, lineEndY) <= lineWidth / 2f;
		#endregion

		public static float DistanceFromPointToLine(float pointX, float pointY, float lineStartX, float lineStartY, float lineEndX, float lineEndY)
		{
			// que

			// Calculate the distance from the point to the line using the formula for the distance between a point and a line
			float lineDiffY = lineEndY - lineStartY;
			float lineDiffX = lineEndX - lineStartX;
			float numerator = Math.Abs(lineDiffY * pointX - lineDiffX * pointY + lineEndX * lineStartY - lineEndY * lineStartX);
			float denominatorPow2 = MathF.Pow(lineDiffY, 2) + MathF.Pow(lineDiffX, 2);
			float denominator = MathF.Sqrt(denominatorPow2);
			float distance = numerator / denominator;

			// Calculate the distance between the point and the endpoints of the line line
			float distance1 = MathF.Pow(lineStartX - pointX, 2) + MathF.Pow(lineStartY - pointY, 2);
			float distance2 = MathF.Pow(lineEndX - pointX, 2) + MathF.Pow(lineEndY - pointY, 2);

			// Check if the point is outside the line line
			if (distance1 > denominatorPow2 || distance2 > denominatorPow2)
			{
				// Calculate the distance between the point and the closest endpoint of the line line
				distance = MathF.Sqrt(MathF.Min(distance1, distance2));
			}

			return distance;
		}

		#region Codigo punto adentro de rectangulo rotado
		public static bool IsPointInsideOfRectangle(float pointX, float pointY, float rectX, float rectY, float rectWidth, float rectHeight, float rectRadians)
		{
			// gracias a dios que las ias sirven para buscar esto rapido porque llevo intentando aprender como mierda se hace esto desde 40 millones de a�os

			// Calculate the half width and half height of the rectangle
			float halfWidth = rectWidth / 2f;
			float halfHeight = rectHeight / 2f;

			// Calculate the center point of the rectangle
			float centerX = rectX + halfWidth;
			float centerY = rectY + halfHeight;

			// Calculate the coordinates of the four corners of the rectangle
			float x1 = -halfWidth;
			float y1 = -halfHeight;
			float x2 = halfWidth;
			float y2 = -halfHeight;
			float x3 = halfWidth;
			float y3 = halfHeight;
			float x4 = -halfWidth;
			float y4 = halfHeight;

			// Rotate the coordinates of the corners around the center of the rectangle
			(float sin, float cos) = MathF.SinCos(rectRadians);
			float rotatedline1StartX = cos * x1 - sin * y1 + centerX;
			float rotatedline1StartY = sin * x1 + cos * y1 + centerY;
			float rotatedline1EndX = cos * x2 - sin * y2 + centerX;
			float rotatedline1EndY = sin * x2 + cos * y2 + centerY;
			float rotatedX3 = cos * x3 - sin * y3 + centerX;
			float rotatedY3 = sin * x3 + cos * y3 + centerY;
			float rotatedX4 = cos * x4 - sin * y4 + centerX;
			float rotatedY4 = sin * x4 + cos * y4 + centerY;

			// Check if the point is inside the rectangle
			return
				pointX >= Math.Min(rotatedline1StartX, Math.Min(rotatedline1EndX, Math.Min(rotatedX3, rotatedX4))) &&
				pointX <= Math.Max(rotatedline1StartX, Math.Max(rotatedline1EndX, Math.Max(rotatedX3, rotatedX4))) &&
				pointY >= Math.Min(rotatedline1StartY, Math.Min(rotatedline1EndY, Math.Min(rotatedY3, rotatedY4))) &&
				pointY <= Math.Max(rotatedline1StartY, Math.Max(rotatedline1EndY, Math.Max(rotatedY3, rotatedY4)));
		}
		#endregion
		#endregion
	}
}
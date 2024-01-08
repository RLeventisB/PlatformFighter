using Microsoft.Xna.Framework;

using System;

namespace PlatformFighter.Rendering
{
    public readonly struct PolygonLineData
    {
        public PolygonLineData(Vector2 point, ushort[] connectedLines)
        {
            this.point = point;
            connectedIndexs = connectedLines ?? Array.Empty<ushort>();
            color = Color.Black;
            width = 1;
        }
        public PolygonLineData(Vector2 point, Color color, ushort[] connectedLines = null)
        {
            this.point = point;
            connectedIndexs = connectedLines ?? Array.Empty<ushort>();
            this.color = color;
            width = 1;
        }
        public PolygonLineData(Vector2 point, float width, ushort[] connectedLines = null)
        {
            this.point = point;
            connectedIndexs = connectedLines ?? Array.Empty<ushort>();
            color = Color.Black;
            this.width = width;
        }
        public PolygonLineData(Vector2 point, Color color, float width, ushort[] connectedLines = null)
        {
            this.point = point;
            connectedIndexs = connectedLines ?? Array.Empty<ushort>();
            this.color = color;
            this.width = width;
        }
        public readonly Vector2 point;
        public readonly Color color;
        public readonly float width;
        public readonly ushort[] connectedIndexs;
    }
}
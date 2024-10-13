using Editor.Objects;

using System;

namespace PlatformFighter
{
    public struct HitData
    {
        public HitData(ushort? owner, HitboxAnimationObject hitbox)
        {
            Owner = owner;
            Hitbox = hitbox;
        }

        public ushort? Owner { get; init; }
        public HitboxAnimationObject Hitbox { get; init; }
    }
}
using ExtraProcessors.GameTexture;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Rendering
{
    public readonly struct MeasuredText
    {
        public readonly string Text;
        public readonly Vector2 Measure;
        public MeasuredText(string text, bool upperCheck = true, bool formated = false)
        {
            Text = upperCheck ? text.ToUpperInvariant() : text;
            Measure = TextRenderer.MeasureString(Text, upperCheck, formated);
        }
        public MeasuredText(string text, Vector2 setMeasure)
        {
            Text = text;
            Measure = setMeasure;
        }
        public static explicit operator MeasuredText(string text) => new MeasuredText(text);
        public static implicit operator string(MeasuredText measuredText) => measuredText.Text;
        public static implicit operator ReadOnlySpan<char>(MeasuredText measuredText) => measuredText.Text;
    }
    public readonly struct TextData
    {
        public TextData(Vector2 position, string text)
        {
            Position = position;
            Text = text;
            Origin = TextRenderer.MeasureString(text, false) / 2;
            Size = Vector2.One;
        }
        public TextData(Vector2 position, string text, float size)
        {
            Position = position;
            Text = text;
            Origin = TextRenderer.MeasureString(text, false) / 2;
            Size = new Vector2(size);
        }
        public TextData(Vector2 position, string text, Vector2 size)
        {
            Position = position;
            Text = text;
            Origin = TextRenderer.MeasureString(text, false) / 2;
            Size = size;
        }
        public readonly string Text;
        public readonly Vector2 Origin, Size, Position;
    }
    public static class TextRenderer
    {
        public static int LineSpacing = 4, Spacing = 2, SpaceWidth = 6;
        public const float DefaultDepth = 0.5f;
        public const ushort DefaultShadowWidth = 2;
        public static FrozenDictionary<char, CharData> CharacterDictionary;
        public static Texture2D TextureInfo { get; internal set; }
        public static void Initialize()
        {
            using (FileStream pixelFont = File.Open("./Content/PixelFont.cfont", FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(pixelFont))
                {
                    Dictionary<char, CharData> characters = new Dictionary<char, CharData>();
                    switch (reader.ReadUInt16())
                    {
                        case 0:
                            while (pixelFont.Position + 5 < pixelFont.Length)
                            {
                                char chr = reader.ReadChar();
                                Rectangle rect = new Rectangle(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

                                CharData newChar = new CharData(rect, new Vector2(reader.ReadSByte(), reader.ReadSByte()));
                                characters.Add(chr, newChar);
                            }
                            break;
                    }
                    CharacterDictionary = characters.ToFrozenDictionary();
                }
            }
        }
        public static bool TryGetData(char chr, out CharData data) => CharacterDictionary.TryGetValue(chr, out data);
        public static Vector2 MeasureChar(char character) => TryGetData(character, out CharData data) ? data.size : Vector2.Zero;
        public static Vector2 MeasureString(string text, bool doCheck = true, bool detectFormat = false) => MeasureString(text.AsSpan(), doCheck, detectFormat);
        public static Vector2 MeasureString(ReadOnlySpan<char> span, bool doCheck = true, bool detectFormat = false)
        {
            Vector2 size = Vector2.Zero, currentLine = Vector2.Zero;
            Span<char> parsedSpan = stackalloc char[span.Length];
            if (doCheck)
            {
                span.ToUpperInvariant(parsedSpan);
            }
            else
            {
                unsafe
                {
                    fixed (char* ptr = &span.GetPinnableReference())
                    {
                        parsedSpan = new Span<char>(ptr, span.Length);
                    }
                }
            }
            for (ushort i = 0; i < parsedSpan.Length; i++)
            {
                ref char character = ref parsedSpan[i];
                switch (character)
                {
                    case '\n':
                        size.Y += currentLine.Y + LineSpacing;
                        if (size.X < currentLine.X)
                            size.X = currentLine.X;
                        currentLine.X = 0;
                        currentLine.Y = 0;
                        break;
                    case ' ':
                        currentLine.X += SpaceWidth;
                        break;
                    case '[' when detectFormat && TryReadFormatting(ref span, ref i, ref currentLine):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            AdvanceCharacter(ref currentLine.X, ref currentLine.Y, in data);
                        }
                        break;
                }
            }
            if (size.X < currentLine.X)
                size.X = currentLine.X;
            size.Y += currentLine.Y;
            size.X -= Spacing;
            return size;
        }
        #region Normal Text Rendering
        public static void DrawText(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawText(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawText(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            Span<char> upperSpan = stackalloc char[span.Length];
            span.ToUpper(upperSpan, null);

            DrawTextNoCheck(spriteBatch, position, upperSpan, scale, rotation, color, origin, singleSpacing, shadowWidth, layerDepth);
        }
        public static void DrawTextNoCheck(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawTextNoCheck(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawTextNoCheck(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            rotation *= MathHelper.Pi / 180;
            Vector2 pos = -origin;
            float height = 0;
            Color black = Color.Black;
            black.A = color.A;
            (float sin, float cos) = MathF.SinCos(rotation);

            foreach (var character in span)
            {
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + LineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += SpaceWidth;
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth, layerDepth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheckNoRot(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawTextNoCheckNoRot(spriteBatch, position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawTextNoCheckNoRot(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            Vector2 pos = -origin;
            float height = 0;
            Color black = Color.Black;
            black.A = color.A;

            foreach (var character in span)
            {
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + LineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += SpaceWidth;
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 posFinal = position + (data.offset + pos) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth, layerDepth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        #endregion
        #region Formated Text Rendering
        public static void DrawTextFormated(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawTextFormated(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawTextFormated(SpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            Span<char> upperSpan = stackalloc char[span.Length];
            span.ToUpper(upperSpan, null);

            DrawTextNoCheckFormated(spriteBatch, position, upperSpan, scale, rotation, color ,origin, singleSpacing, shadowWidth, layerDepth);
        }
        public static void DrawTextNoCheckFormated(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawTextNoCheckFormated(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawTextNoCheckFormated(SpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            rotation *= MathHelper.Pi / 180;
            Vector2 pos = -origin;
            float height = 0;
            (float sin, float cos) = MathF.SinCos(rotation);

            for (ushort i = 0; i < span.Length; i++)
            {
                ref readonly char character = ref span[i];
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + LineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += SpaceWidth;
                        break;
                    case '[' when TryReadFormatting(ref span, ref i, ref color, ref pos, ref position):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth, layerDepth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheckNoRotFormated(SpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth) 
            => DrawTextNoCheckNoRotFormated(spriteBatch, position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth, layerDepth);
        public static void DrawTextNoCheckNoRotFormated(SpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing, ushort shadowWidth = DefaultShadowWidth, float layerDepth = DefaultDepth)
        {
            Vector2 pos = -origin;
            float height = 0;

            for (ushort i = 0; i < span.Length; i++)
            {
                ref readonly char character = ref span[i];
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + LineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += SpaceWidth;
                        break;
                    case '[' when TryReadFormatting(ref span, ref i, ref color, ref pos, ref position):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 posFinal = position + (data.offset + pos) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth, layerDepth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        #endregion
        #region Extensions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AdvanceCharacter(ref float posX, ref float height, in CharData data)
        {
            posX += data.size.X + Spacing + data.offset.X;
            float y = data.size.Y + data.offset.Y;
            if (y > height)
                height = y;
        }
        public static bool TryReadFormatting(ref ReadOnlySpan<char> span, ref ushort i, ref Vector2 currentLine)
        {
            Color color = default;
            Vector2 position = default;
            return TryReadFormatting(ref span, ref i, ref color, ref currentLine, ref position);
        }
        public static bool TryReadFormatting(ref Span<char> span, ref ushort i, ref Color color, ref Vector2 offset, ref Vector2 position)
        {
            ReadOnlySpan<char> readonlySpan = span;
            return TryReadFormatting(ref readonlySpan, ref i, ref color, ref offset, ref position);
        }
        public static bool TryReadFormatting(ref ReadOnlySpan<char> span, ref ushort i, ref Color color, ref Vector2 offset, ref Vector2 position)
        {
            if (i + 2 < span.Length && span[i + 2] == ':')
            {
                int dataStart = i + 3;
                int searchIndex = dataStart;
                ushort dataLength = 0;
                while (searchIndex < span.Length)
                {
                    if (span[searchIndex] == ']')
                    {
                        dataLength = (ushort)(searchIndex - i - 3);
                        break;
                    }
                    searchIndex++;
                }
                switch (span[i + 1])
                {
                    case 'c' when dataLength <= 8:
                        if (dataLength == 8)
                        {
                            byte[] bytes = Convert.FromHexString(span.Slice(dataStart, 8));
                            color.R = bytes[0];
                            color.G = bytes[1];
                            color.B = bytes[2];
                            color.A = bytes[3];
                            i += 12;
                        }
                        else
                        {
                            byte[] bytes = Convert.FromHexString(span.Slice(dataStart, 6));
                            color.R = bytes[0];
                            color.G = bytes[1];
                            color.B = bytes[2];
                            i += 9;
                        }
                        return true;
                    case 't':
                        string[] parameters = new string(span.Slice(dataStart, dataLength)).Split(',', StringSplitOptions.TrimEntries);
                        Vector2? origin = null;
                        Vector2 size = Vector2.One;
                        GameTexture data;
                        ushort index = 0;

                        if (parameters.Length > 0)
                        {
                            data = Assets.Textures[parameters[0]];
                            if (TryReadVector2(parameters, ref index, out Vector2 vector2))
                            {
                                size = vector2;
                                if (TryReadVector2(parameters, ref index, out vector2))
                                {
                                    origin = vector2;
                                }
                            }
                            Main.spriteBatch.Draw(data, position + offset, null, color, 0, origin ?? data.origin, size);
                            i += (ushort)(dataLength + 3);
                            return true;
                        }

                        i += (ushort)(dataLength + 3);
                        return false;
                    case 'b':

                        break;
                }
            }
            return false;
        }
        public static bool TryReadVector2(string[] strings, ref ushort index, out Vector2 vector2)
        {
            vector2 = default;
            if (strings.Length > index)
            {
                string[] values = strings[index].Split(' ', StringSplitOptions.TrimEntries);
                index++;
                if (values.Length == 1)
                {
                    if (float.TryParse(values[0], out vector2.X))
                    {
                        if (strings.Length > index + 1)
                        {
                            index++;
                            return float.TryParse(strings[index], out vector2.Y);
                        }
                        return false;
                    }
                    return false;
                }
                else if (values.Length == 2)
                {
                    return float.TryParse(values[0], out vector2.X) && float.TryParse(values[1], out vector2.X);
                }
                return true;
            }
            return false;
        }
        #endregion
    }
    public readonly struct CharData
    {
        public CharData(Rectangle src, Vector2 offset)
        {
            size = src.Size();
            this.offset = offset;
            percents = new Vector4(
                src.X / 64f,
                src.Y / 64f,
                Math.Sign(src.Width) * MathF.Max(Math.Abs(src.Width), MathHelper.MachineEpsilonFloat) / 64f,
                Math.Sign(src.Height) * MathF.Max(Math.Abs(src.Height), MathHelper.MachineEpsilonFloat) / 64f
            );
        }
        public readonly Vector2 size;
        public readonly Vector2 offset;
        public readonly Vector4 percents;
        public override bool Equals([NotNullWhen(true)] object obj) => obj is CharData data && data.size == size && data.percents == percents && data.offset == offset;
        public override int GetHashCode() => percents.GetHashCode();
        public override string ToString() => percents.ToString();
        public static bool operator ==(CharData left, CharData right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CharData left, CharData right)
        {
            return !(left == right);
        }
        // sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 4
    }
}
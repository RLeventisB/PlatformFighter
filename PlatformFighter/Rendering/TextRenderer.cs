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
        public MeasuredText(string text, bool upperCheck = false, bool formated = false)
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
        public const int lineSpacing = 4, spacing = 2, spaceWidth = 6;
        public delegate bool TryGetCharDataDelegate(char chr, out CharData data);
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
                MemoryExtensions.ToUpperInvariant(span, parsedSpan);
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
                        size.Y += currentLine.Y + lineSpacing;
                        if (size.X < currentLine.X)
                            size.X = currentLine.X;
                        currentLine.X = 0;
                        currentLine.Y = 0;
                        break;
                    case ' ':
                        currentLine.X += spaceWidth;
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
            size.X -= spacing;
            return size;
        }
        #region Normal Text Rendering
        #region String Overloads
        public static void DrawText(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawText(spriteBatch, position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheck(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheck(spriteBatch, position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRot(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRot(spriteBatch, position, text.AsSpan(), new Vector2(scale), color, origin, singleSpacing, shadowWidth);
        public static void DrawText(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawText(spriteBatch, position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheck(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheck(spriteBatch, position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRot(CustomizedSpriteBatch spriteBatch,Vector2 position, string text, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRot(spriteBatch, position, text.AsSpan(), scale, color, origin, singleSpacing, shadowWidth);
        #endregion
        public static void DrawText(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawText(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheck(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheck(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRot(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRot(spriteBatch, position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth);
        public static void DrawText(CustomizedSpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
        {
            rotation *= MathHelper.Pi / 180;
            Vector2 pos = -origin;
            float height = 0;
            bool doShadow = shadowWidth > 0;
            Color black = Color.Black;
            black.A = color.A;
            Span<char> upperSpan = stackalloc char[span.Length];
            MemoryExtensions.ToUpper(span, upperSpan, null);
            (float sin, float cos) = MathF.SinCos(rotation);
            foreach (var character in upperSpan)
            {
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheck(CustomizedSpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
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
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheckNoRot(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
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
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 posFinal = position + (data.offset + pos) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        #endregion
        #region Formated Text Rendering
        #region String Overloads
        public static void DrawTextFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextFormated(spriteBatch, position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckFormated(spriteBatch, position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRotFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRotFormated(spriteBatch, position, text.AsSpan(), new Vector2(scale), color, origin, singleSpacing, shadowWidth);
        public static void DrawTextFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextFormated(spriteBatch, position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckFormated(spriteBatch, position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRotFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, string text, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRotFormated(spriteBatch, position, text.AsSpan(), scale, color, origin, singleSpacing, shadowWidth);
        #endregion
        public static void DrawTextFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextFormated(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckFormated(spriteBatch, position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
        public static void DrawTextNoCheckNoRotFormated(CustomizedSpriteBatch spriteBatch, Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0) => DrawTextNoCheckNoRotFormated(spriteBatch, position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth);
        public static void DrawTextFormated(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
        {
            rotation *= MathHelper.Pi / 180;
            Vector2 pos = -origin;
            float height = 0;
            bool doShadow = shadowWidth > 0;
            Span<char> upperSpan = stackalloc char[span.Length];
            MemoryExtensions.ToUpper(span, upperSpan, null);
            (float sin, float cos) = MathF.SinCos(rotation);
            for (ushort i = 0; i < upperSpan.Length; i++)
            {
                ref char character = ref upperSpan[i];
                switch (character)
                {
                    case '\n':
                        if (height == 0)
                            height = 10;
                        pos.X = -origin.X;
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    case '[' when TryReadFormatting(ref span, ref i, ref color, ref pos, ref position):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheckFormated(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
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
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    case '[' when TryReadFormatting(ref span, ref i, ref color, ref pos, ref position):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 copy = pos;
                            Utils.RotateRadPreCalc(ref copy, sin, cos);
                            Vector2 posFinal = position + (data.offset + copy) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
                            AdvanceCharacter(ref pos.X, ref height, in data);
                        }
                        break;
                }
            }
        }
        public static void DrawTextNoCheckNoRotFormated(CustomizedSpriteBatch spriteBatch,Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
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
                        pos.Y += height + lineSpacing + singleSpacing;
                        height = 0;
                        break;
                    case ' ':
                        pos.X += spaceWidth;
                        break;
                    case '[' when TryReadFormatting(ref span, ref i, ref color, ref pos, ref position):
                        break;
                    default:
                        if (TryGetData(character, out CharData data))
                        {
                            Vector2 posFinal = position + (data.offset + pos) * scale;
                            spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth);
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
            posX += data.size.X + spacing + data.offset.X;
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
    //public static class TextRendererEX
    //{
    //    public static SpriteFont Small5x3 { get; internal set; }
    //    public static SpriteFont PixelSans { get; internal set; }
    //    public static Vector2 MeasureChar(char character) => TextRenderer.TryGetData(character, out CharData data) ? data.size : Vector2.Zero;
    //    public static Vector2 MeasureString(string text, bool doCheck = true, bool detectFormat = false, UseFallback fallback = UseFallback.No) => MeasureString(text.AsSpan(), doCheck, detectFormat, fallback);
    //    public static Vector2 MeasureString(ReadOnlySpan<char> span, bool doCheck = true, bool detectFormat = false, UseFallback fallback = UseFallback.No)
    //    {
    //        Vector2 size = Vector2.Zero, currentLine = Vector2.Zero;
    //        Span<char> parsedSpan = stackalloc char[span.Length];
    //        if (doCheck)
    //        {
    //            MemoryExtensions.ToUpperInvariant(span, parsedSpan);
    //        }
    //        else
    //        {
    //            unsafe
    //            {
    //                fixed (char* ptr = &span.GetPinnableReference())
    //                {
    //                    parsedSpan = new Span<char>(ptr, span.Length);
    //                }
    //            }
    //        }
    //        bool hasSmallFlag = fallback.HasFlag(UseFallback.Small5x3), hasSansFlag = fallback.HasFlag(UseFallback.PixelSans);
    //        Color dummy = default;
    //        Vector2 dummy1 = default;
    //        for (ushort i = 0; i < parsedSpan.Length; i++)
    //        {
    //            ref char character = ref parsedSpan[i];
    //            switch (character)
    //            {
    //                case '\n':
    //                    size.Y += currentLine.Y + TextRenderer.lineSpacing;
    //                    if (size.X < currentLine.X)
    //                        size.X = currentLine.X;
    //                    currentLine.X = 0;
    //                    currentLine.Y = 0;
    //                    break;
    //                case ' ':
    //                    currentLine.X += TextRenderer.spaceWidth;
    //                    break;
    //                case '[' when detectFormat && TextRenderer.TryReadFormatting(ref parsedSpan, ref i, ref dummy, ref dummy1):
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        TextRenderer.AdvanceCharacter(ref currentLine.X, ref currentLine.Y, in data);
    //                    }
    //                    else if (hasSmallFlag && TryMeasureCharFont(Small5x3, in character, ref currentLine.X, ref currentLine.Y))
    //                    {

    //                    }
    //                    else if (hasSansFlag)
    //                    {
    //                        TryMeasureCharFont(PixelSans, in character, ref currentLine.X, ref currentLine.Y);
    //                    }
    //                    break;
    //            }
    //        }
    //        if (size.X < currentLine.X)
    //            size.X = currentLine.X;
    //        size.Y += currentLine.Y;
    //        size.X -= TextRenderer.spacing;
    //        return size;
    //    }
    //    #region Normal Text Rendering
    //    #region String Overloads
    //    public static void DrawText(Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawText(position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheck(Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheck(position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRot(Vector2 position, string text, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRot(position, text.AsSpan(), new Vector2(scale), color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawText(Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawText(position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheck(Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheck(position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRot(Vector2 position, string text, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRot(position, text.AsSpan(), scale, color, origin, singleSpacing, shadowWidth);
    //    }
    //    #endregion
    //    public static void DrawText(Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawText(position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheck(Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheck(position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRot(Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRot(position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawText(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        rotation *= MathHelper.Pi / 180;
    //        Vector2 pos = -origin;
    //        float height = 0;
    //        bool doShadow = shadowWidth > 0;
    //        Color black = Color.Black;
    //        black.A = color.A;
    //        Span<char> upperSpan = stackalloc char[span.Length];
    //        MemoryExtensions.ToUpper(span, upperSpan, null);
    //        (float sin, float cos) = MathF.SinCos(rotation);
    //        foreach (var character in upperSpan)
    //        {
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 copy = pos;
    //                        Utils.RotateRadPreCalc(ref copy, sin, cos);
    //                        Vector2 posFinal = position + (data.offset + copy) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    public static void DrawTextNoCheck(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        rotation *= MathHelper.Pi / 180;
    //        Vector2 pos = -origin;
    //        float height = 0;
    //        Color black = Color.Black;
    //        black.A = color.A;
    //        (float sin, float cos) = MathF.SinCos(rotation);

    //        foreach (var character in span)
    //        {
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 copy = pos;
    //                        Utils.RotateRadPreCalc(ref copy, sin, cos);
    //                        Vector2 posFinal = position + (data.offset + copy) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    public static void DrawTextNoCheckNoRot(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        Vector2 pos = -origin;
    //        float height = 0;
    //        Color black = Color.Black;
    //        black.A = color.A;

    //        foreach (var character in span)
    //        {
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 posFinal = position + (data.offset + pos) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    #endregion
    //    #region Formated Text Rendering
    //    #region String Overloads
    //    public static void DrawTextFormated(Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextFormated(position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckFormated(Vector2 position, string text, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckFormated(position, text.AsSpan(), new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRotFormated(Vector2 position, string text, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRotFormated(position, text.AsSpan(), new Vector2(scale), color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextFormated(Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextFormated(position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckFormated(Vector2 position, string text, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckFormated(position, text.AsSpan(), scale, rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRotFormated(Vector2 position, string text, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRotFormated(position, text.AsSpan(), scale, color, origin, singleSpacing, shadowWidth);
    //    }
    //    #endregion
    //    public static void DrawTextFormated(Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextFormated(position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckFormated(Vector2 position, ReadOnlySpan<char> span, float scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckFormated(position, span, new Vector2(scale), rotation, color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextNoCheckNoRotFormated(Vector2 position, ReadOnlySpan<char> span, float scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        DrawTextNoCheckNoRotFormated(position, span, new Vector2(scale), color, origin, singleSpacing, shadowWidth);
    //    }
    //    public static void DrawTextFormated(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        rotation *= MathHelper.Pi / 180;
    //        Vector2 pos = -origin;
    //        float height = 0;
    //        bool doShadow = shadowWidth > 0;
    //        Span<char> upperSpan = stackalloc char[span.Length];
    //        MemoryExtensions.ToUpper(span, upperSpan, null);
    //        (float sin, float cos) = MathF.SinCos(rotation);
    //        for (ushort i = 0; i < upperSpan.Length; i++)
    //        {
    //            char character = upperSpan[i];
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                case '[' when TextRenderer.TryReadFormatting(ref upperSpan, ref i, ref color, ref pos):
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 copy = pos;
    //                        Utils.RotateRadPreCalc(ref copy, sin, cos);
    //                        Vector2 posFinal = position + (data.offset + copy) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    public static void DrawTextNoCheckFormated(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, float rotation, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        rotation *= MathHelper.Pi / 180;
    //        Vector2 pos = -origin;
    //        float height = 0;
    //        (float sin, float cos) = MathF.SinCos(rotation);

    //        for (ushort i = 0; i < span.Length; i++)
    //        {
    //            char character = span[i];
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                case '[' when TextRenderer.TryReadFormatting(ref span, ref i, ref color, ref pos):
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 copy = pos;
    //                        Utils.RotateRadPreCalc(ref copy, sin, cos);
    //                        Vector2 posFinal = position + (data.offset + copy) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, sin, cos, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    public static void DrawTextNoCheckNoRotFormated(Vector2 position, ReadOnlySpan<char> span, Vector2 scale, Color color, Vector2 origin, float singleSpacing = 0, ushort shadowWidth = 0)
    //    {
    //        Vector2 pos = -origin;
    //        float height = 0;

    //        for (ushort i = 0; i < span.Length; i++)
    //        {
    //            char character = span[i];
    //            switch (character)
    //            {
    //                case '\n':
    //                    if (height == 0)
    //                        height = 10;
    //                    pos.X = -origin.X;
    //                    pos.Y += height + TextRenderer.lineSpacing + singleSpacing;
    //                    height = 0;
    //                    break;
    //                case ' ':
    //                    pos.X += TextRenderer.spaceWidth;
    //                    break;
    //                case '[' when TextRenderer.TryReadFormatting(ref span, ref i, ref color, ref pos):
    //                    break;
    //                default:
    //                    if (TextRenderer.TryGetData(character, out CharData data))
    //                    {
    //                        Vector2 posFinal = position + (data.offset + pos) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth);
    //                        TextRenderer.AdvanceCharacter(ref pos.X, ref height, in data);
    //                    }
    //                    else if (Small5x3.characterIndexMap.TryGetValue(character, out ushort index)) // TODO: hacer metodo alterno de renderizaod!!
    //                    {
    //                        Vector3 kerning = Small5x3.kerning[index];
    //                        data = new CharData(Small5x3.glyphData[index], new Vector2(kerning.Y, kerning.Z));
    //                        Vector2 posFinal = position + (data.offset + pos) * scale;
    //                        Main.spriteBatch.PushCharacter(data.size, posFinal, data.percents, scale, color, 0, 1, shadowWidth, Small5x3.textureValue);
    //                        AdvanceCharFont(Small5x3, ref pos.X, ref height, index);
    //                    }
    //                    else if (PixelSans.characterIndexMap.TryGetValue(character, out index))
    //                    {
    //                        AdvanceCharFont(PixelSans, ref pos.X, ref height, index);
    //                    }
    //                    break;
    //            }
    //        }
    //    }
    //    #endregion
    //    public static bool TryMeasureCharFont(SpriteFont font, in char character, ref float x, ref float y)
    //    {
    //        if (font.characterIndexMap.TryGetValue(character, out ushort index))
    //        {
    //            AdvanceCharFont(font, ref x, ref y, index);
    //            return true;
    //        }
    //        return false;
    //    }

    //    private static void AdvanceCharFont(SpriteFont font, ref float x, ref float y, ushort index)
    //    {
    //        Vector3 kerning = font.kerning[index];
    //        x += kerning.X + kerning.Y + kerning.Z;
    //        float height = font.croppingData[index].Height;
    //        if (y < height)
    //        {
    //            y = height;
    //        }
    //    }
    //}
    //[Flags]
    //public enum UseFallback : byte
    //{
    //    No = 0,
    //    Small5x3 = 1,
    //    PixelSans = 2,
    //}
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
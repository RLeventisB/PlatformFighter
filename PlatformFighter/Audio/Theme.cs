using Microsoft.Xna.Framework.Audio;

using MonoGame.OpenAL;

using System;
using System.IO;

namespace PlatformFighter.Audio
{
    public struct Theme
    {
        public SoundEffectInstance instance;
        public SoundEffect soundEffect;
        private float _volume;
        public readonly string InternalName, Album, Artist, Title;
        public readonly ushort Year;
        public readonly int count;
        public Theme(SoundEffect soundEffect, string InternalName, string Album, string Artist, string Title, ushort Year, int count)
        {
            this.soundEffect = soundEffect;
            instance = soundEffect.CreateInstance();
            instance.IsLooped = true;
            this.InternalName = InternalName;
            this.Album = Album;
            this.Artist = Artist;
            this.Title = Title;
            this.Year = Year;
            this.count = count;
        }
        public void Stop()
        {
            instance.Stop();
        }
        public void Play()
        {
            instance.Play();
        }
        public void SetVolume(float volume)
        {
            if (volume != _volume)
            {
                _volume = volume;
                instance.Volume = volume;
            }
        }
        public void Pause()
        {
            instance.Pause();
        }
        public void Resume()
        {
            instance.Resume();
        }
        public unsafe void SaveToFile(FileStream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Title);
                writer.Write(Album);
                writer.Write(Artist);
                writer.Write(Year);
                writer.Write(soundEffect.LoopStart);
                writer.Write(soundEffect.SampleRate);
                writer.Write((byte)soundEffect.Channels);
                void* ptr = AL.GetBufferPtrSOFT(soundEffect.SoundBuffer.openALDataBuffer);
                ReadOnlySpan<byte> dataAsSpan = new ReadOnlySpan<byte>((byte*)ptr, count);
                writer.Write(dataAsSpan.Length);
                writer.Write(dataAsSpan);
                writer.Write(count);
            }
        }
    }
}
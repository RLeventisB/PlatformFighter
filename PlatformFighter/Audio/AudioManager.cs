using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using PlatformFighter.Miscelaneous;

using System;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Audio
{
    public static unsafe class AudioManager
    {
        public const int MaxSoundEffects = 50, MaxDelayedAudios = 40;
        public static Theme CurrentTheme => Assets.Themes[CurrentThemeName];
        public static readonly DelayedAudioData* delayedAudios;
        private static float _musicVolume, _soundVolume;
        public static byte TransitionCount = 255;
        public static string CurrentThemeName;
        public static string NextThemeName { get; private set; }
        public static SoundSlot[] sfxs = new SoundSlot[MaxSoundEffects];
        static AudioManager()
        {
            for (ushort i = 0; i < MaxSoundEffects; i++)
            {
                sfxs[i] = new SoundSlot(null, false);
            }
            delayedAudios = Utils.AllocateZeroed<DelayedAudioData>(MaxDelayedAudios);
            ClearDelayedSounds();
        }
        public static FrozenDictionary<string, ushort> AudioNameMap { get; internal set; }
        public static bool MusicEnabled { get; private set; }
        public static float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;
                MusicEnabled = value > 0;
            }
        }
        public static bool SoundEnabled { get; private set; }
        public static float SoundVolume
        {
            get => _soundVolume;
            set
            {
                _soundVolume = value;
                SoundEnabled = value > 0;
            }
        }
        public static bool IsCurrentThemeValid { get; private set; }
        public static bool PlayTheme(string name, bool showThemeName = true)
        {
            if (name == "Quiet" || string.IsNullOrEmpty(name))
            {
                NextThemeName = string.Empty;
                TransitionCount = 0;
                return true;
            }
            if (!Assets.Themes.ContainsKey(name) || NextThemeName == name)
                return false;
            if (CurrentThemeName == name)
            {
                CurrentTheme.Stop();
                if (MusicEnabled)
                    CurrentTheme.Play();
            }
            else
                TransitionCount = 0;
            NextThemeName = name;
            return true;
        }
        public static void Update()
        {
            Span<SoundSlot> span = sfxs;

            for (int i = 0; i < 100; i++)
            {
                ref SoundSlot soundSlot = ref span[i];
                if (!soundSlot.active)
                    continue;
                if (soundSlot.instance.State == SoundState.Stopped)
                {
                    soundSlot.active = false;
                }
            }
            if (!Main.GamePaused)
            {
                for (int i = 0; i < MaxDelayedAudios; i++)
                {
                    ref DelayedAudioData delayedAudioData = ref delayedAudios[i];
                    if (!delayedAudioData.Active)
                        continue;
                    delayedAudioData.time--;
                    if (delayedAudioData.time <= 0 &&
                        TryPlaySound(delayedAudioData.id, out _, delayedAudioData.volume, delayedAudioData.listener, delayedAudioData.source, delayedAudioData.SearchForSame, delayedAudioData.pitch, delayedAudioData.playLayer))
                    {
                        delayedAudioData.Reset();
                    }
                }
            }
            if (!MusicEnabled)
            {
                if (IsCurrentThemeValid && CurrentTheme.instance.State != SoundState.Stopped)
                    CurrentTheme.Stop();
            }
            else if (!Main.GamePaused)
            {
                switch (TransitionCount)
                {
                    case byte.MaxValue when IsCurrentThemeValid:
                        CurrentTheme.SetVolume(MusicVolume);
                        break;
                    case 180:
                        if (IsCurrentThemeValid)
                            CurrentTheme.Stop();

                        if (IsCurrentThemeValid = Assets.Themes.ContainsKey(NextThemeName))
                        {
                            CurrentThemeName = NextThemeName;
                            CurrentTheme.SetVolume(MusicVolume);
                            CurrentTheme.Play();
                        }
                        else
                        {
                            CurrentThemeName = NextThemeName;
                        }

                        TransitionCount = byte.MaxValue;
                        break;
                    case < 180:
                        if (IsCurrentThemeValid)
                            CurrentTheme.SetVolume(MathHelper.Lerp(MusicVolume, 0, TransitionCount / 180f));

                        TransitionCount++;
                        break;
                }
            }
        }
        public static bool QueueDelayedSound(DelayedAudioData data)
        {
            for (int i = 0; i < MaxDelayedAudios; i++)
            {
                ref DelayedAudioData delayedAudioData = ref delayedAudios[i];
                if (delayedAudioData.Active)
                    continue;
                delayedAudioData = data;
                return true;
            }
            return false;
        }
        public static bool TryPlaySound(string name, out SoundSlot instance, float volume = 1f, Vector2? listener = null, Vector2? position = null, bool limitToOne = false, float pitch = 0f, byte playLayer = 0)
            => TryPlaySound(AudioNameMap[name], out instance, volume, listener, position, limitToOne, pitch, playLayer);
        public static bool TryPlaySound(ushort id, out SoundSlot instance, float volume = 1f, Vector2? listener = null, Vector2? position = null, bool limitToOne = false, float pitch = 0f, byte playLayer = 0)
        {
            Span<SoundSlot> span = sfxs;
            if (limitToOne)
            {
                foreach (SoundSlot sfx in span)
                {
                    if (!sfx.active || !sfx.canBeOverwritten || sfx.id != id || sfx.playLayer != playLayer)
                        continue;
                    instance = sfx;
                    RePlaySound(ref instance, pitch, listener, position, volume);
                    instance.canBeOverwritten = true;
                    instance.playLayer = playLayer;

                    return true;
                }
            }
            foreach (SoundSlot sfx in span)
            {
                if (sfx.active)
                    continue;
                instance = sfx;
                StartSound(ref instance, id, pitch, volume, listener, position);
                instance.canBeOverwritten = limitToOne;
                instance.playLayer = playLayer;

                return true;
            }
            instance = default;
            return false;
        }
        public static ushort PlaySound(string name, float volume = 1f, Vector2? listener = null, Vector2? position = null, bool limitToOne = false, float pitch = 0f, byte playLayer = 0)
            => PlaySound(AudioNameMap[name], volume, listener, position, limitToOne, pitch, playLayer);
        public static ushort PlaySound(ushort id, float volume = 1f, Vector2? listener = null, Vector2? position = null, bool limitToOne = false, float pitch = 0f, byte playLayer = 0)
        {
            Span<SoundSlot> span = sfxs;
            if (limitToOne)
            {
                for (ushort i = 0; i < MaxSoundEffects; i++)
                {
                    ref SoundSlot soundSlot = ref span[i];
                    if (!soundSlot.active || !soundSlot.canBeOverwritten || soundSlot.id != id || soundSlot.playLayer != playLayer)
                        continue;
                    RePlaySound(ref soundSlot, pitch, listener, position, volume);
                    soundSlot.canBeOverwritten = true;
                    soundSlot.playLayer = playLayer;
                    return i;
                }
            }
            for (ushort i = 0; i < MaxSoundEffects; i++)
            {
                ref SoundSlot soundSlot = ref span[i];
                if (soundSlot.active)
                    continue;
                StartSound(ref soundSlot, id, pitch, volume, listener, position);
                soundSlot.canBeOverwritten = limitToOne;
                soundSlot.playLayer = playLayer;
                return i;
            }
            return ushort.MaxValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartSound(ref SoundSlot slot, int id, float pitch, float volume, Vector2? listener, Vector2? position)
        {
            slot.active = true;
            slot.instance?.Dispose();
            SoundEffect soundEffect = Assets.SoundEffects.GetValueByIndex(id);
            slot.instance = soundEffect.CreateInstance();
            slot.SetSoundName(soundEffect.Name);
            slot.instance.Pitch = pitch;
            slot.instance.Volume = volume * _soundVolume;
            slot.instance.Play();
            if (listener is null && position is null)
            {
                slot.instance.Pan = 0f;
            }
            else
            {
                position ??= Vector2.Zero;
                listener ??= Vector2.Zero;
                slot.instance.Pan = Utils.ClampMinus1Plus1((position.Value.X - listener.Value.X) / (VirtualWidth / 2f));
            }
        }
        public static void RePlaySound(ref SoundSlot slot, float pitch, Vector2? listener, Vector2? position, float volume)
        {
            slot.instance.Pause();
            slot.instance.Stop();
            slot.active = true;
            slot.instance.Pitch = pitch;
            slot.instance.Volume = volume * _soundVolume;
            if (listener is null && position is null)
            {
                slot.instance.Pan = 0f;
            }
            else
            {
                position ??= Vector2.Zero;
                listener ??= Vector2.Zero;
                slot.instance.Pan = Utils.ClampMinus1Plus1((position.Value.X - listener.Value.X) / (VirtualWidth / 2f));
            }
            slot.instance.Play();
        }
        public static void PauseTheme()
        {
            if (IsCurrentThemeValid && MusicEnabled)
                CurrentTheme.Pause();
        }
        public static void ClearDelayedSounds()
        {
            for (int i = 0; i < MaxDelayedAudios; i++)
            {
                ref DelayedAudioData delayedAudioData = ref delayedAudios[i];
                delayedAudioData.Reset();
            }
        }
        public static void ClearDelayedSounds(params string[] names)
        {
            foreach (string item in names)
            {
                ClearDelayedSounds(item);
            }
        }
        public static void ClearDelayedSounds(params ushort[] ids)
        {
            foreach (ushort item in ids)
            {
                ClearDelayedSounds(item);
            }
        }
        public static void ClearDelayedSounds(string name) => ClearDelayedSounds(AudioNameMap[name]);
        public static void ClearDelayedSounds(ushort id)
        {
            for (int i = 0; i < MaxDelayedAudios; i++)
            {
                ref DelayedAudioData delayedAudioData = ref delayedAudios[i];
                if (!delayedAudioData.Active || delayedAudioData.id != id)
                    continue;
                delayedAudioData.Reset();
            }
        }
        public static void ResumeTheme()
        {
            if (IsCurrentThemeValid && MusicEnabled)
                CurrentTheme.Resume();
        }
        public static bool PlayDelayedSound(string name, float delay = 60f, float volume = 1f, Vector2? listener = null, Vector2? position = null, bool searchForSame = false, float pitch = 0f, byte playLayer = 0) => QueueDelayedSound(new DelayedAudioData(name, delay, searchForSame, volume, listener, position, playLayer)
        {
            pitch = pitch
        });
    }
    public struct DelayedAudioData
    {
        public Vector2? listener, source;
        public float volume, time, pitch;
        public bool SearchForSame
        {
            get => (extraData & 1) == 1;
            set => extraData |= 1;
        }
        public bool Active
        {
            get => (extraData & 2) == 2;
            set => extraData |= 2;
        }
        public ushort id;
        public byte playLayer, extraData;
        public DelayedAudioData()
        {
            Reset();
        }
        public DelayedAudioData(string soundName, float time = 60f, bool searchForSame = false, float volume = 1f, Vector2? listener = null, Vector2? source = null, byte playLayer = 0)
        {
            this.listener = listener;
            this.source = source;
            pitch = 0f;
            this.playLayer = playLayer;
            this.volume = volume;
            this.time = time;
            SearchForSame = searchForSame;
            Active = true;
            id = AudioManager.AudioNameMap[soundName];
        }
        public void Reset()
        {
            listener = null;
            source = null;
            volume = 1f;
            time = float.NaN;
            pitch = 0;
            id = ushort.MaxValue;
            playLayer = 0;
            extraData = 0;
        }
    }
    public struct SoundSlot
    {
        public SoundEffectInstance instance;
        public bool active, canBeOverwritten;
        public ushort id;
        public byte playLayer;
        public SoundSlot(SoundEffectInstance instance, bool active)
        {
            this.instance = instance;
            this.active = active;
            id = ushort.MaxValue;
        }
        public void SetSoundName(string sfxName)
        {
            id = AudioManager.AudioNameMap[sfxName];
        }
    }
}
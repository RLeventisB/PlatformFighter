using ExtraProcessors.GameTexture;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using NVorbis;

using PlatformFighter.Audio;
using PlatformFighter.ExternalLibraries;
using PlatformFighter.Miscelaneous;
using PlatformFighter.Rendering;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformFighter
{
    public static class Assets
    {
        public static object themeLock = new object();
        public static int ProcessorCountForLoading;
        public static ushort ThemeLoadCount, TotalThemeCount, ClearScratchPoolTimer;
        public static bool LoadedThemes, SkippedThemeLoad;
        public static DirectoryInfo ContentInfo, TextureInfo, ShadersInfo, SFXInfo, ThemesInfo;
        public static AssetDictionary<SoundEffect> SoundEffects = new AssetDictionary<SoundEffect>();
        public static AssetDictionary<GameTexture> Textures = new AssetDictionary<GameTexture>();
        public static AssetDictionary<Effect> Effects = new AssetDictionary<Effect>();
        public static ConcurrentDictionary<string, Theme> Themes = new ConcurrentDictionary<string, Theme>();
        public static ContentManager Content => Main.instance.Content;
        public static void LoadAssets()
        {
            string baseContent = Main.BaseDirectory + Content.RootDirectory;
            ContentInfo = new DirectoryInfo(baseContent);
            TextureInfo = new DirectoryInfo(baseContent + "/Textures");
            ShadersInfo = new DirectoryInfo(baseContent + "/Effects");
            SFXInfo = new DirectoryInfo(baseContent + "/SFX");
            ThemesInfo = new DirectoryInfo(baseContent + "/Themes");
            if (TextureInfo.Exists)
                foreach (FileInfo file in TextureInfo.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    string path = file.FullName.Replace(file.Extension, string.Empty), name = Path.GetFileNameWithoutExtension(file.Name);
                    Asset<GameTexture> asset = new Asset<GameTexture>(path)
                    {
                        OnUnload = Textures.SubstractToCounter
                    };
                    asset.OnLoad += AddToScratchBufferTimer;
                    asset.OnLoad += Textures.AddToCounter;

                    Textures.Add(name, asset);
                }

            GameTexture noise100x100 = new GameTexture(Main.Graphics, 100, 100)
            {
                Name = "noise100x100"
            };
            Color[] data = new Color[10000];
            FastNoiseLite noise = new FastNoiseLite(Main.mainRandom.Next());
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            int iOffset = Main.mainRandom.Next(10, 40), jOffset = Main.mainRandom.Next(10, 40);
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    float value = noise.GetNoise(i * 3 + iOffset, j * 3 + jOffset);
                    data[j + i * 100] = new Color(value, 0, 0);
                }
            }
            noise100x100.SetData(data);
            Textures.Add("noise100x100", new GeneratedAsset<GameTexture>(noise100x100));
            Textures.GetAsset("Glow").CanBeUnloaded =
                Textures.GetAsset("GlowNoShader").CanBeUnloaded = false;
            CustomizedSpriteBatch.glowTexture = Textures["Glow"];
            CustomizedSpriteBatch.glowTextureNoShader = Textures["GlowNoShader"];
            CustomizedSpriteBatch.shaderKeySet.Add(CustomizedSpriteBatch.glowTexture.SortingKey);

            Textures.Remove("Glow");
            Textures.Remove("GlowNoShader");

            Textures.Freeze();

            AnimationRenderer.LoadAnimations(baseContent);

            if (ShadersInfo.Exists)
                foreach (FileInfo file in ShadersInfo.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    string path = file.FullName.Replace(file.Extension, string.Empty), name = Path.GetFileNameWithoutExtension(file.Name);
                    Asset<Effect> asset = new Asset<Effect>(path)
                    {
                        OnUnload = Effects.SubstractToCounter
                    };
                    asset.OnLoad += AddToScratchBufferTimer;
                    asset.OnLoad += Effects.AddToCounter;
                    Effects.Add(name, asset);
                }
            Effects.Freeze();

            CustomizedSpriteBatch.glowEffect = Effects["GlowShader"];
            CustomizedSpriteBatch.glowEffectPass = CustomizedSpriteBatch.glowEffect.CurrentTechnique.Passes[0];
            CustomizedSpriteBatch.glowParameter = CustomizedSpriteBatch.glowEffect.Parameters["isGlow"];

            if (SFXInfo.Exists)
                foreach (FileInfo file in SFXInfo.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    string path = file.FullName.Replace(file.Extension, string.Empty), name = Path.GetFileNameWithoutExtension(file.Name);
                    Asset<SoundEffect> asset = new Asset<SoundEffect>(path)
                    {
                        OnUnload = SoundEffects.SubstractToCounter
                    };
                    asset.OnLoad += AddToScratchBufferTimer;
                    asset.OnLoad += SoundEffects.AddToCounter;

                    SoundEffects.Add(name, asset);
                }
            SoundEffects.OnFreeze += delegate
            {
                Dictionary<string, ushort> audioIdMap = new Dictionary<string, ushort>();
                for (ushort i = 0; i < SoundEffects.dictionary.Count; i++)
                {
                    audioIdMap.Add(SoundEffects.GetValueByIndex(i).Name, i);
                }
                AudioManager.AudioNameMap = audioIdMap.ToFrozenDictionary();
            };
            SoundEffects.Freeze();

            if (ThemesInfo.Exists)
            {
                FileInfo[] themeFiles = ThemesInfo.GetFiles("*.ogg", SearchOption.AllDirectories);
                TotalThemeCount = (ushort)themeFiles.Length;
                Themes = new ConcurrentDictionary<string, Theme>(ProcessorCountForLoading, TotalThemeCount);
                if (Settings.cacheThemes && Directory.Exists("./CachedThemes/"))
                {
                    foreach (string item in Directory.GetFiles("./CachedThemes/", "*.dat"))
                    {
                        if (themeFiles.All(v => Path.GetFileNameWithoutExtension(v.Name) != Path.GetFileNameWithoutExtension(item).Replace("_Cached", string.Empty)))
                        {
                            File.Delete(item);
                        }
                    }
                }
                Task.Run(async delegate
                {
                    await Parallel.ForEachAsync(themeFiles, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = ProcessorCountForLoading
                    }, ProcessTheme).ConfigureAwait(false);
                    LoadedThemes = true;
                    GC.Collect(2);
#if DEBUG
                    "Loaded Themes!".Log();
#endif
                });
            }

            //TextRenderer.TextureInfo = Textures["PixelFont"];

            ContentManager.FreeMemory();
        }
        public static void Update()
        {
            if (ClearScratchPoolTimer == 0)
                return;
            if (--ClearScratchPoolTimer == 0)
            {
                ContentManager.FreeMemory();
            }
        }
        public static void AddToScratchBufferTimer<T>(IAsset<T> asset) where T : class, IDisposable
        {
            if (ClearScratchPoolTimer == 0)
            {
                ClearScratchPoolTimer = 120;
            }
            else
            {
                ClearScratchPoolTimer += (ushort)(120 / MathF.Sqrt(ClearScratchPoolTimer));
            }
        }
        public static ValueTask ProcessTheme(FileInfo file, CancellationToken arg2)
        {
            $"Loading {file.FullName}".Log();
            string internalName = file.Name, album, artist, name, finalName = internalName.Replace(".ogg", string.Empty);
            ushort year;
            int splitStart, sampleRate, count;
            AudioChannels channels;
            bool loadedFromFile = false;
            byte[] buffer;
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            string cachePath = ThemeCacheFolder + "/" + finalName + "_Cached.dat";
            if (Settings.cacheThemes && File.Exists(cachePath))
            {
                try
                {
                    using (FileStream compressedFile = File.OpenRead(cachePath))
                    {
                        using (BinaryReader reader = new BinaryReader(compressedFile))
                        {
                            name = reader.ReadString();
                            album = reader.ReadString();
                            artist = reader.ReadString();
                            year = reader.ReadUInt16();
                            splitStart = (int)reader.ReadUInt32();
                            sampleRate = (int)reader.ReadUInt32();
                            channels = (AudioChannels)reader.ReadByte();
                            int length = reader.ReadInt32();
                            buffer = reader.ReadBytes(length);
                            count = reader.ReadInt32();
                        }
                    }
                    loadedFromFile = true;
                    goto registerFile;
                }
                catch
                {
                    $"Error loading cached theme {internalName}, deleting and loading theme via .ogg".Log();
                    File.Delete(cachePath);
                }
            }

            using (MemoryStream pcmData = new MemoryStream())
            {
                using (FileStream fileStream = file.OpenRead())
                {
                    try
                    {
                        using (VorbisReader decoder = new VorbisReader(fileStream))
                        {
                            sampleRate = decoder.SampleRate;
                            channels = (AudioChannels)decoder.Channels;
                            name = decoder.Tags.GetTagSingle("title").ToUpper();
                            album = decoder.Tags.GetTagSingle("album").ToUpper();
                            artist = decoder.Tags.GetTagSingle("artist").ToUpper();
                            year = ushort.Parse(decoder.Tags.GetTagSingle("year"));
                            splitStart = int.Parse(decoder.Tags.GetTagSingle("splitStart"));
                            float[] sampleBuffer = new float[8192];
                            int cnt;
                            while ((cnt = decoder.ReadSamples(sampleBuffer, 0, Math.Min((int)decoder.TotalSamples - (int)decoder.SamplePosition, 4096))) > 0)
                            {
                                for (int i = 0; i < cnt; i++)
                                {
                                    short temp = (short)(short.MaxValue * sampleBuffer[i]);
                                    pcmData.WriteByte((byte)temp);
                                    pcmData.WriteByte((byte)(temp >> 8));
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        $"Error loading theme {internalName}, internal exception:\n{e}".Log();
                        ThemeLoadCount++;
                        return ValueTask.CompletedTask;
                    }
                    buffer = pcmData.GetBuffer();
                    count = (int)pcmData.Length;
                }
            }
        registerFile:
            Theme theme;
            try
            {
                lock (themeLock)
                {
                    theme = new Theme(new SoundEffect(buffer, 0, count, sampleRate, channels, splitStart, 0), finalName, album, artist, name, year, count);
                }
            }
            catch (Exception e)
            {
                $"Error upon creating theme instance {internalName},\ninternal exception: {e}".Log();
                ThemeLoadCount++;
#if DEBUG
                stopwatch.Stop();
#endif

                return ValueTask.CompletedTask;
            }
#if DEBUG
            stopwatch.Stop();
            if (Themes.TryAdd(finalName, theme))
            {
                $"Loaded theme {name} asyncroniously. Wait time: {stopwatch.Elapsed.TotalSeconds}s".Log();
            }
            else
            {
                $"Loaded theme {name} asyncroniously but couldn't add to dictionary, Wait time: {stopwatch.Elapsed.TotalSeconds}s".Log();
            }
#else
            Themes.TryAdd(finalName, theme);
#endif

            ThemeLoadCount++;
            if (!Settings.cacheThemes || loadedFromFile)
                return ValueTask.CompletedTask;
            {
                Directory.CreateDirectory(ThemeCacheFolder);
                using (FileStream compressedFile = File.OpenWrite(cachePath))
                {
                    theme.SaveToFile(compressedFile);
                }
            }
            return ValueTask.CompletedTask;
        }
    }
    public class Settings
    {
        public static bool cacheThemes = true;
    }
    public class AssetDictionary<T> where T : class, IDisposable
    {
        public ExposedDictionary<string, IAsset<T>> creationDictionary = new ExposedDictionary<string, IAsset<T>>();
        public FrozenDictionary<string, IAsset<T>> dictionary;
        public ushort LoadedAssetCount;
        public Action OnFreeze, OnUnfreeze;
        public ref T this[string key]
        {
            get
            {
                ref IAsset<T> data = ref GetAsset(key);
                if (!data.Loaded)
                    data.Load();
                return ref data.ExposeValue();
            }
        }
        public bool Frozen { get; private set; }
        public ushort Count => (ushort)(Frozen ? dictionary.Count : creationDictionary.Count);
        public ref IAsset<T> GetAsset(string key)
        {
            if (!Frozen)
                return ref creationDictionary.FindValue(key);
            return ref Unsafe.AsRef(in dictionary.GetValueRefOrNullRef(key));
        }
        public void Load(string key)
        {
            ref IAsset<T> data = ref GetAsset(key);
            if (!data.Loaded)
                data.Load();
        }
        public T GetWithoutLoad(string key) => GetAsset(key).ExposeValue();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsValue(IAsset<T> value) => Frozen ? creationDictionary.ContainsValue(value) : dictionary.Values.Contains(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => Frozen ? creationDictionary.ContainsKey(key) : dictionary.ContainsKey(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string key, IAsset<T> data)
        {
            if (Frozen)
                Unfreeze();
            creationDictionary.Add(key, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(string key)
        {
            if (Frozen)
                Unfreeze();

            creationDictionary.Remove(key);
        }
        public ref T GetValueByIndex(int index)
        {
            IAsset<T> asset = dictionary.Values[index];
            if (!asset.Loaded)
                asset.Load();
            return ref asset.ExposeValue();
        }
        public bool TryGetValue(string key, out T data)
        {
            if (dictionary.TryGetValue(key, out IAsset<T> asset))
            {
                if (!asset.Loaded)
                    asset.Load();
                data = asset.Value;
                return true;
            }
            data = default;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValueWithoutLoading(string key, out T data)
        {
            if (dictionary.TryGetValue(key, out IAsset<T> asset))
            {
                data = asset.Value;
                return true;
            }
            data = default;
            return false;
        }
        public bool TryGetAsset(string key, out IAsset<T> data) => dictionary.TryGetValue(key, out data);
        public void ClearEverything()
        {
            foreach (IAsset<T> asset in dictionary.Values)
            {
                if (asset.CanBeUnloaded && asset.Loaded)
                    asset.Unload();
            }
        }
        public void AddToCounter(IAsset<T> asset)
        {
            if (LoadedAssetCount < ushort.MaxValue)
                LoadedAssetCount++;
        }
        public void SubstractToCounter(IAsset<T> asset)
        {
            if (LoadedAssetCount > 0)
                LoadedAssetCount--;
        }
        public void Freeze()
        {
            if (Frozen)
                return;
            dictionary = creationDictionary.ToFrozenDictionary();
            creationDictionary = null;
            OnFreeze?.Invoke();
            Frozen = true;
        }
        public void Unfreeze()
        {
            if (!Frozen)
                return;
            creationDictionary = new ExposedDictionary<string, IAsset<T>>(dictionary);
            foreach (KeyValuePair<string, IAsset<T>> pair in dictionary)
            {
                creationDictionary.Add(pair.Key, pair.Value);
            }
            dictionary = null;
            OnUnfreeze?.Invoke();
            Frozen = false;
        }
    }
    public interface IAsset<T> where T : class, IDisposable
    {
        Action<IAsset<T>> OnLoad { get; set; }
        Action<IAsset<T>> OnUnload { get; set; }
        bool Loaded { get; }
        bool CanBeUnloaded { get; set; }
        string FilePath { get; }
        T Value { get; }
        void Load();
        void Unload();
        ref T ExposeValue();
    }
    public struct GeneratedAsset<T> : IAsset<T> where T : class, IDisposable
    {
        public GeneratedAsset(T asset)
        {
            Value = asset;
        }
        public Action<IAsset<T>> OnLoad { get; set; }
        public Action<IAsset<T>> OnUnload { get; set; }
        public bool Loaded => true;
        public bool CanBeUnloaded { get => false; set { } }
        public string FilePath => "Generated";
        public T Value { get => _value; set => _value = value; }
        internal T _value;
        public void Load()
        {
            OnLoad?.Invoke(this);
        }
        public void Unload()
        {
            OnUnload?.Invoke(this);
        }
        public unsafe ref T ExposeValue() => ref _value;
        public static implicit operator T(GeneratedAsset<T> asset) => asset.Value;
    }
    public struct Asset<T> : IAsset<T> where T : class, IDisposable
    {
        public Asset(string path)
        {
            _value = null;
            FilePath = path;
        }
        public Action<IAsset<T>> OnLoad { get; set; }
        public Action<IAsset<T>> OnUnload { get; set; }
        public bool Loaded { get; private set; }
        public bool CanBeUnloaded { get; set; } = true;
        public string FilePath { get; }
        internal T _value;
        public T Value
        {
            get
            {
                if (!Loaded)
                    Load();
                return _value;
            }
        }
        public void Load()
        {
            bool isGraphicsResource = _value is GraphicsResource;
            ref GraphicsResource resource = ref Unsafe.As<T, GraphicsResource>(ref _value);

            if (_value == null)
                _value = Assets.Content.ReadAsset<T>(FilePath, null);
            else
            {
                if (isGraphicsResource)
                    resource.GraphicsDevice = Main.Graphics;
                Assets.Content.ReloadAsset(FilePath, _value);
            }

            string name = Path.GetFileNameWithoutExtension(FilePath);
            if (_value is SoundEffect sfx)
            {
                sfx.Name = name;
            }

            if (isGraphicsResource)
            {
                resource.Name = name;
            }
#if DEBUG
            //$"Loaded asset {name}".Log();
#endif
            OnLoad?.Invoke(this);
            Loaded = true;
        }
        public void Unload()
        {
            _value.Dispose();
            OnUnload?.Invoke(this);
            Loaded = false;
        }
        public unsafe ref T ExposeValue() => ref _value;
        public static implicit operator T(Asset<T> asset) => asset._value;
    }
    public static class InstanceManager
    {
        public static FrozenSet<Type> AssemblyTypes;
        public static Assembly ExecutingAssembly;
        [RequiresUnreferencedCode("Types might be removed")]
        public static void Initialize()
        {
            ExecutingAssembly = Assembly.GetExecutingAssembly();
            Type compilerGenerated = typeof(CompilerGeneratedAttribute);
            AssemblyTypes = ExecutingAssembly.GetTypes().Where(v => Attribute.GetCustomAttributes(v, compilerGenerated).Length != 0).ToFrozenSet();

            Span<IInstanceDictionary> dictionaries = new Span<IInstanceDictionary>();
            foreach (Type type in AssemblyTypes.Where(type => !type.IsAbstract && type.IsClass))
            {
                foreach (IInstanceDictionary dictionary in dictionaries)
                {
                    if (dictionary.TryAdd(type))
                    {
                        break;
                    }
                }
            }
            foreach (IInstanceDictionary dictionary in dictionaries)
            {
                dictionary.FreezeData();
            }
        }
    }
    public interface IInstanceDictionary
    {
        public bool Contains(Type type);
        public bool TryAdd(Type type);
        public void Add(Type type);
        public void FreezeData();
    }
    public class InstanceDictionary<T> : IInstanceDictionary, IEnumerable<Instance<T>> where T : class
    {
        internal IEnumerator instanceEnumerator;
        internal ExposedList<Instance<T>> preLoadList;
        public InstanceDictionary()
        {
            Type = typeof(T);
            Count = 0;
            Instances = null;
            instanceEnumerator = null;
            preLoadList = new ExposedList<Instance<T>>();
        }
        public Type Type { get; }
        public Instance<T>[] Instances { get; private set; }
        public Instance<T> this[ushort ID] => Instances[ID];
        public ushort Count { get; private set; }
        public IEnumerator<Instance<T>> GetEnumerator() => ((IEnumerable<Instance<T>>)Instances).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => instanceEnumerator;
        public bool Contains(Type type) => Instances.Any(v => v.Type == type);
        public bool TryAdd(Type type)
        {
            if (!type.IsSubclassOf(Type) && !Type.IsAssignableFrom(type))
                return false;
            Add(type);
            return true;
        }
        public void Add(Type type)
        {
            preLoadList.Add(new Instance<T>(type, Count));
            typeof(ContentHolder<>).MakeGenericType(type).GetProperty("ID").SetValue(null, Count);
            Count++;
        }
        public void FreezeData()
        {
#if !DEBUG
            Instances = preLoadList.ToArray();
            instanceEnumerator = Instances.GetEnumerator();
            preLoadList.Clear();
            preLoadList = null;
#else
            preLoadList.Capacity = Count;
            preLoadList.TrimExcess();
            Instances = preLoadList.items;
            instanceEnumerator = Instances.GetEnumerator();
#endif
        }
        [RequiresUnreferencedCode("Types might be removed")]
        public ushort GetID(Type type) => (ushort)typeof(ContentHolder<>).MakeGenericType(type).GetProperty("ID").GetValue(null);
        public ushort GetID<T2>() => ContentHolder<T2>.ID;
        public T2 CreateInstance<T2>() where T2 : T => Activator.CreateInstance<T2>();
        public T CreateInstance(ushort ID) => (T)Activator.CreateInstance(Instances[ID].Type);
        public bool TryGetID(string internalName, out int ID)
        {
            foreach (Instance<T> item in new ReadOnlySpan<Instance<T>>(Instances))
            {
                if (internalName == item.Type.Name)
                {
                    ID = item.ID;
                    return true;
                }
            }
            ID = -1;
            return false;
        }
        public ReadOnlySpan<T> GetInstanceSpan()
        {
            T[] array = new T[Count];
            Span<T> span = new Span<T>(array);
            for (ushort i = 0; i < Count; i++)
            {
                span[i] = Instances[i].Inst;
            }
            return span;
        }
        public ReadOnlySpan<Type> GetTypeSpan()
        {
            Type[] array = new Type[Count];
            Span<Type> span = new Span<Type>(array);
            for (ushort i = 0; i < Count; i++)
            {
                span[i] = Instances[i].Type;
            }
            return span;
        }
        public ReadOnlySpan<Instance<T>> GetInstanceArraySpan() => new ReadOnlySpan<Instance<T>>(Instances);
    }
    [DebuggerDisplay("${Inst}")]
    public readonly struct Instance<T>
    {
        public Instance(Type type, ushort ID)
        {
            this.ID = ID;
            Type = type;
            Inst = (T)Activator.CreateInstance(type);
            SizeOf = new nuint((uint)Marshal.ReadInt32(type.TypeHandle.Value, 4));
        }
        public ushort ID { get; init; }
        public nuint SizeOf { get; init; }
        public Type Type { get; init; }
        public T Inst { get; init; }
        public T CreateInstance() => (T)Activator.CreateInstance(Type);
    }
    public static class ContentHolder<T>
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public static ushort ID { get; private set; }
    }
}
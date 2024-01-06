using Cube.Miscelaneous;

using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
#if DEBUG
using Cube;
#endif

#if RELEASE
using System.Linq;
#endif

#if DEBUG
[assembly: MetadataUpdateHandler(typeof(HotReloadManager))]

internal static class HotReloadManager
{
    public static void UpdateApplication(Type[] types)
    {
        Main.instance?.HotReload();
        Span<IInstanceDictionary> dictionaries = new Span<IInstanceDictionary>(Dictionaries);

        foreach (Type type in new Span<Type>(types))
        {
            if (type.IsAbstract || !type.IsClass) continue;
            foreach (IInstanceDictionary dictionary in dictionaries)
            {
                if (!dictionary.Contains(type) && dictionary.TryAdd(type))
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
#endif

namespace Cube
{
    // https://stackoverflow.com/questions/2779746/is-there-a-textwriter-interface-to-the-system-diagnostics-debug-class
    public interface IOutputWrapper
    {
        public void Write(string text, byte[] buffer);
    }
    public readonly struct ConsoleInOutWrapper : IOutputWrapper
    {
        public void Write(string text, byte[] buffer) => Console.Out.Write(text);
    }
    public readonly struct LogWrapper : IOutputWrapper
    {
        public readonly string Path;
        public LogWrapper(string path)
        {
            Path = path;
        }
        public void Write(string text, byte[] buffer)
        {
            using (FileStream stream = File.OpenWrite(Path))
            {
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }
    public readonly struct DebugWrapper : IOutputWrapper
    {
        public void Write(string text, byte[] buffer) => Debug.Write(text);
    }
    public static class Program
    {
        public static readonly List<IOutputWrapper> LogStreams = new List<IOutputWrapper>();
        public static bool IsRunningSafe, IsRelease;
        [STAThread]
        private static void Main(string[] args)
        {
#if RELEASE
            IsRelease = true;
#endif
#if DESKTOPGL
            if (Debugger.IsAttached)
            {
                LogStreams.Add(new DebugWrapper());
            }
            else
            {
                LogStreams.Add(new ConsoleInOutWrapper());
            }
#endif
            Input.Gamepads = new ExposedList<GamepadInfo>(4);
            ref EventRegisterPair pair = ref SdlGamePlatform.OnEventDictionary.GetReference(Sdl.EventType.JoyDeviceAdded);
            pair.AfterEvent += _ =>
            {
                Input.BuildGamepadDictionary();
            };
            pair = ref SdlGamePlatform.OnEventDictionary.GetReference(Sdl.EventType.JoyDeviceRemoved);
            pair.AfterEvent += _ =>
            {
                Input.BuildGamepadDictionary();
            };
#if RELEASE
            bool dontShowException = args.Contains("-rununsafe");
            if (dontShowException)
            {
                Sdl.MessageBox.SDL_ShowSimpleMessageBox(Sdl.MessageBox.MessageBoxFlags.Information,
                "Aviso!!!",
@"Cuidado!!! estas ejecutando el juego de forma insegura!!!!!
esto hace que si el juego crashea fatalmente, no muestra el error y se puto cierra!!!!
solo es recomendado usar esto si quieres como, 2 fps mas, o si el juego esta en una build estable.
A cambio de no saber que crasheo",
                IntPtr.Zero);
                using (var game = new Main())
                {
                    game.Run();
                }
            }
            else
            {
                IsRunningSafe = true;
                try
                {
                    using (var game = new Main())
                    {
                        game.Run();
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        string errorDirectory = AppDomain.CurrentDomain.BaseDirectory + "Errores";
                        if (!Directory.Exists(errorDirectory))
                            Directory.CreateDirectory(errorDirectory);
                        string fileName = $"{AppDomain.CurrentDomain.BaseDirectory}Errores/Error{Directory.GetFiles(errorDirectory).Length + 1}.txt";
                        string text = "Error fatal ha ocurrido!!! Excepcion: \n" + e + "\nSe ha generado un archivo para guardarlo!!\nNombre del archivo: " + fileName;
                        File.WriteAllText(fileName, text);
                        Sdl.MessageBox.SDL_ShowSimpleMessageBox(Sdl.MessageBox.MessageBoxFlags.Error,
                        "Error! Juegito ha crasheado de forma irrecuperable!",
                        text, IntPtr.Zero);
                    }
                    catch (Exception e2)
                    {
                        Sdl.MessageBox.SDL_ShowSimpleMessageBox(Sdl.MessageBox.MessageBoxFlags.Error,
                        "Error! Juegito ha crasheado y no pudo loggear error!",
                        $"Ok algo malo paso aqui nisiquiera se pudo guardar el error >:(\nError de guardado: {e2}\nError original: {e}", IntPtr.Zero);
                    }
                }
            }
#else
            IsRunningSafe = true;
            using (Main game = new Main())
            {
                game.Run();
            }
#endif
        }
    }
}
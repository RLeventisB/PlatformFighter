using System;
using System.Diagnostics;
using System.IO;

namespace PlatformFighter
{
    public interface IOutputWrapper
    {
        public void Write(string text);
    }
    public readonly struct ConsoleInOutWrapper : IOutputWrapper
    {
        public void Write(string text) => Console.Out.Write(text);
    }
    public readonly struct LogWrapper : IOutputWrapper
    {
        public readonly string Path;
        public readonly StreamWriter Stream;
        public LogWrapper(string path)
        {
            Path = path;
            Stream = new StreamWriter(File.OpenWrite(path));
        }
        public void Write(string text)
        {
            Stream.Write(text);
        }
    }
#if ANDROID
    public readonly struct AndroidLogWrapper : IOutputWrapper
    {
        public void Write(string text) => Android.Util.Log.Info("LogCommand", text);
    }
#endif
    public readonly struct DebugWrapper : IOutputWrapper
    {
        public void Write(string text)
        {
            // Debugger.Log(0, "LogCommand", text + Environment.NewLine);
            Debug.WriteLine(text);
        }
    }
}
using System.Collections.Generic;

namespace PlatformFighter
{
	public static class Logger
	{
		public static readonly List<IOutputWrapper> LogStreams = new List<IOutputWrapper>();

		public static void LogMessage(string text)
		{
			foreach (IOutputWrapper stream in LogStreams)
			{
				stream.Write(text);
			}
		}
	}
}
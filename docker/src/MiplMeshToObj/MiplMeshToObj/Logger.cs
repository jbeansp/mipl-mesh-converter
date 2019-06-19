using System;
using System.Collections.Generic;
using System.Text;

namespace MiplMeshToObj
{
	class Logger
	{
		public static void Log(string message, params object[] args)
		{
			Console.WriteLine(message, args);
		}

		public static void Log(string message)
		{
			Console.WriteLine(message);
		}

		public static void Error(string message, params object[] args)
		{
			Console.Error.WriteLine(message, args);
		}

		public static void Error(string message)
		{
			Console.Error.WriteLine(message);
		}

	}
}

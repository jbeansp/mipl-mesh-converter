using System;
using System.IO;
using System.Threading;

namespace MiplMeshToObj
{
	public class Program
	{
		public static void Main(string[] args)
		{
			try
			{
				if (args.Length < 3 || args[0] == "--help" || args[0] == "-h")
				{
					PrintUsage();
					return;
				}

				string miplMeshPath = args[0];
				if (!File.Exists(miplMeshPath))
				{
					Logger.Error($"File not found: {miplMeshPath}");
					return;
				}

				string outputDirectory = args[1];
				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}


				Configuration configuration = Configuration.GetConfiguration();
				Converter converter = new Converter(configuration);

				CancellationTokenSource cts = new CancellationTokenSource();
				UnixExitSignalMonitor unixExitSignalMonitor = new UnixExitSignalMonitor();
				unixExitSignalMonitor.cancelEvent += (o, a) => { cts.Cancel(); };
				var inputInfo = new Converter.InputInfo(miplMeshPath, outputDirectory);

				converter.ProcessMeshAsync(inputInfo, cts.Token).GetAwaiter().GetResult();

				Logger.Log("Done running MiplMeshToObj");
			}
			catch (Exception e)
			{
				Logger.Error("Caught exception: {0}", e);
			}
		}

		private static void PrintUsage()
		{
			Logger.Log(
@"Usage:
MiplMeshToObj <MIPL mesh path (iv or pfb file)> <output obj directory>"
			);
		}


	}
}

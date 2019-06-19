using System;
using System.IO;

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

				string roverName = args[2].ToUpper();
				IRover rover;
				switch (roverName)
				{
					case "MER":
						{
							rover = new MerRover();
							break;
						}
					case "MSL":
						{
							rover = new MslRover();
							break;
						}
					default:
						{
							Logger.Error($"Rover not recognized: {roverName}");
							return;
						}
				}

				Configuration configuration = Configuration.GetConfiguration();

				Converter converter = new Converter(configuration);

				var inputInfo = new Converter.InputInfo(miplMeshPath, outputDirectory, rover);

				converter.ProcessMeshAsync(inputInfo).GetAwaiter().GetResult();

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
MiplMeshToObj <MIPL mesh path (iv or pfb file)> <output obj directory> <rover: MER | MSL>"
			);
		}


	}
}

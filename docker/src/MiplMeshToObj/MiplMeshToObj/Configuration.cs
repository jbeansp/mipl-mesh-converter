
using Newtonsoft.Json;
using System.IO;

namespace MiplMeshToObj
{
	class Configuration
	{

		private const string configFilename = "config.json";
		private const string defaultAppPath = "/app";

		public string PfbToOsgx { get; private set; }
		public string ConvertRgb { get; private set; }

		/// <summary>
		/// Use this factory method to get the configuration.  If the config file doesn't exist, one will be created with default values.
		/// </summary>
		public static Configuration GetConfiguration()
		{
			//look for a config file in exe directory
			string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;

			string directory = Path.GetDirectoryName(exePath);

			string configPath = Path.Combine(directory, configFilename);


			if (!File.Exists(configPath))
			{
				GenerateDefaultConfig(configPath);
			}

			return ReadConfig(configPath);
		}

		private Configuration()
		{
			//set some reasonable default values
			PfbToOsgx = Path.Combine(defaultAppPath, "pfb2osgx");
			ConvertRgb = "/usr/bin/convert";
		}

		private static void GenerateDefaultConfig(string configPath)
		{
			Logger.Log($"Creating a config file with default values: {configPath}");
			Configuration configuration = new Configuration();
			string text = JsonConvert.SerializeObject(configuration, Newtonsoft.Json.Formatting.Indented);
			Logger.Log(text);
			File.WriteAllText(configPath, text);
		}
		
		private	static Configuration ReadConfig(string configPath)
		{
			Logger.Log($"Loading config file: {configPath}");
			string text = File.ReadAllText(configPath);
			return JsonConvert.DeserializeObject<Configuration>(text);
		}

	}
}

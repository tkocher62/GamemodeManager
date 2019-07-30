using Smod2;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamemodeManager
{
	public abstract class GamemodeManager
	{
		public static Dictionary<Plugin, string> ModeList = new Dictionary<Plugin, string>();
		public static Dictionary<Plugin, string> ShuffledList = new Dictionary<Plugin, string>();

		public static void RegisterMode(Plugin gamemode, string config = null)
		{
			ModeList.Add(gamemode, config);
			gamemode.Info($"[GamemodeManager] {gamemode.Details.name} ({gamemode.Details.id}) has been registered.");
		}

		public static Plugin GetCurrentMode()
		{
			return CurrentMode;
		}

		// Internals

		internal static void SetNextMode(Plugin gamemode)
		{
			NextMode = gamemode;
			gamemode?.Info($"[GamemodeManager] The next gamemode will be {gamemode.Details.name} ({gamemode.Details.id}).");
		}

		internal static Plugin GetNextModeInRegistry(Plugin curMode)
		{
			if (curMode == null) return ModeList.ElementAt(0).Key;
			for (int i = 0; i < ModeList.Count; i++)
			{
				if (ModeList.ElementAt(i).Key == curMode) return ModeList.ElementAt(i == ModeList.Count - 1 ? 0 : i + 1).Key;
			}
			return null;
		}

		internal static void WriteConfig(string[] data)
		{
			File.WriteAllText(DefaultConfigPath, string.Empty);
			File.WriteAllLines(DefaultConfigPath, data);
		}

		internal static void ReloadConfig(string[] data)
		{
			WriteConfig(data);
			ConfigFile.ServerConfig.LoadConfigFile(DefaultConfigPath);
			ConfigFile.ReloadGameConfig(DefaultConfigPath);
		}

		internal static void SetupDirectories()
		{
			if (!Directory.Exists(ConfigFolderPath))
			{
				Directory.CreateDirectory(ConfigFolderPath);
				PluginManager.Manager.Logger.Info("cyan.gamemode.manager", $"Config folder {ConfigFolderPath} doesn't exist, creating...");
				
			}
			if (!isGlobalConfigs && !Directory.Exists($"{ConfigFolderPath}/{PluginManager.Manager.Server.Port}"))
			{
				Directory.CreateDirectory($"{ConfigFolderPath}/{PluginManager.Manager.Server.Port}");
				PluginManager.Manager.Logger.Info("cyan.gamemode.manager", $"Port folder {ConfigFolderPath}/{PluginManager.Manager.Server.Port} doesn't exist, creating...");
			}
		}

		internal enum ChoosingMethod
		{
			NONE,
			CYCLE,
			SHUFFLE,
			VOTE
		}

		internal static Plugin CurrentMode;
		internal static Plugin NextMode;
		internal static Plugin LastMode;
		internal static Plugin LastGamemode;
		internal static ChoosingMethod method;
		internal static int methodFreq;
		internal static int freqCount;
		internal static string DefaultConfigPath;
		internal static string[] DefaultConfigData;
		internal static string ConfigFolderPath = $"{FileManager.GetAppFolder()}GamemodeManager";

		// Configs
		internal static bool isGlobalConfigs;
	}
}

using EXILED;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace GamemodeManager
{
	public abstract class GamemodeManager
	{
		public static Dictionary<Plugin, string> ModeList = new Dictionary<Plugin, string>();
		public static Dictionary<Plugin, string> ShuffledList = new Dictionary<Plugin, string>();

		private static Random rand = new Random();

		public static void RegisterMode(Plugin gamemode, string config = null)
		{
			ModeList.Add(gamemode, config);
			Log($"{gamemode.getName} has been registered.");
		}

		public static Plugin GetCurrentMode()
		{
			return CurrentMode;
		}

		// Internals

		internal static void SetNextMode(Plugin gamemode)
		{
			NextMode = gamemode;
			if (gamemode != null) Log($"The next gamemode will be {gamemode.getName}.");
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
			Plugin.Config.Reload();
		}

		internal static void ReloadDefaultConfig()
		{
			DefaultConfigPath = $"{EXILEDConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port}-config.yml";
			DefaultConfigData = File.ReadAllLines(DefaultConfigPath);
		}

		internal static void SetupDirectories()
		{
			if (!Directory.Exists(PluginConfigFolderPath))
			{
				Directory.CreateDirectory(PluginConfigFolderPath);
				Log($"Config folder {PluginConfigFolderPath} doesn't exist, creating...");
				
			}
			if (!Configs.isGlobalConfigs && !Directory.Exists($"{PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port}"))
			{
				Directory.CreateDirectory($"{PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port}");
				Log($"Port folder {PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port} doesn't exist, creating...");
			}
		}

		internal static void Log(string msg)
		{
			EXILED.Log.Info($"[GamemodeManager] {msg}");
		}

		internal static void ChangeMode(ChoosingMethod mode)
		{
			switch (mode)
			{
				case ChoosingMethod.NONE:
					{
						method = ChoosingMethod.NONE;
						SetNextMode(null);
						freqCount = 0;
						LastGamemode = null;
						break;
					}
				case ChoosingMethod.CYCLE:
					{
						method = ChoosingMethod.CYCLE;
						ShuffledList = ModeList.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);
						break;
					}
				case ChoosingMethod.SHUFFLE:
					{
						method = ChoosingMethod.SHUFFLE;
						break;
					}
				case ChoosingMethod.VOTE:
					{
						method = ChoosingMethod.VOTE;
						break;
					}
				case ChoosingMethod.PERSIST:
					{
						method = ChoosingMethod.PERSIST;
						break;
					}
			}
		}

		internal static void SetFrequency(int freq, bool runNext = true)
		{
			methodFreq = freq;
			if (runNext) freqCount = freq;
			else freqCount = 0;
		}

		internal static void LoadDefaultSettings()
		{
			isFirstRound = false;
			string settings = Configs.defaultMode.ToUpper().Trim();
			int indx = settings.IndexOf(":");
			bool isFreq = indx != -1;
			if (Enum.TryParse(isFreq ? settings.Substring(0, indx) : settings, out ChoosingMethod method))
			{
				if (method == ChoosingMethod.NONE) return;
				ChangeMode(method);
				Log($"Setting mode to {method}.");

				if (isFreq)
				{
					if (int.TryParse(settings.Substring(indx + 1), out int a))
					{
						SetFrequency(a, false);
						Log($"Setting frequency to {a}.");
					}
					else
					{
						Log("Config error: Invalid frequency, setting to 0.");
					}
				}
			}
			else
			{
				Log("Config error: Invalid method.");
			}
		}

		internal enum ChoosingMethod
		{
			NONE,
			CYCLE,
			SHUFFLE,
			VOTE,
			PERSIST
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
		internal static string PluginConfigFolderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}Plugins{Path.DirectorySeparatorChar}GamemodeManager";
		internal static string EXILEDConfigFolderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}EXILED";
		internal static bool isFirstRound;
	}
}

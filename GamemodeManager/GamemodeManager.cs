using Exiled.API.Features;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Exiled.API.Interfaces;
using System.Reflection;
using Exiled.Loader;

namespace GamemodeManager
{
	public abstract class GamemodeManager
	{
		internal static List<IPlugin<IConfig>> ModeList = new List<IPlugin<IConfig>>();
		internal static List<IPlugin<IConfig>> ShuffledList = new List<IPlugin<IConfig>>();

		private static Random rand = new Random();

		// Internals

		internal static void SetNextMode(IPlugin<IConfig> gamemode)
		{
			NextMode = gamemode;
			if (gamemode != null) Log.Info($"The next gamemode will be {gamemode.Name}.");
		}

		internal static IPlugin<IConfig> GetNextModeInRegistry(IPlugin<IConfig> curMode)
		{
			if (curMode == null) return ModeList[0];
			for (int i = 0; i < ModeList.Count; i++)
			{
				if (ModeList[i] == curMode) return ModeList[i == ModeList.Count - 1 ? 0 : i + 1];
			}
			return null;
		}

		internal static void WriteConfig(string[] data) => File.WriteAllLines(DefaultConfigPath, data);

		/***************************************************************************************
		*    Title: Exiled
		*    Author: Galaxy119 & iopietro
		*    Date: 7/19/2020
		*    Code version: 2.1.0
		*    Availability: https://github.com/galaxy119/EXILED/commits/00ca4619cf8ed3898d597e1e04150046fced4f07/Exiled.Loader/Loader.cs
		*
		***************************************************************************************/
		internal static IPlugin<IConfig> PluginToIPlugin(Assembly plugin)
		{
			try
			{
				foreach (Type type in plugin.GetTypes().Where(type => !type.IsAbstract && !type.IsInterface))
				{
					if (
						!type.BaseType.IsGenericType ||
						type.BaseType.GetGenericTypeDefinition() != typeof(Plugin<>) ||
						type.BaseType.GetGenericArguments()?[0]?.GetInterface(nameof(IConfig)) != typeof(IConfig))
					{
						continue;
					}

					if (type.GetConstructor(Type.EmptyTypes) != null)
					{
						return (IPlugin<IConfig>)Activator.CreateInstance(type);
					}
				}
			}
			catch { }
			return null;
		}

		internal static void ReloadConfig(string[] data)
		{
			WriteConfig(data);
			ConfigManager.Reload()
		}

		internal static void ReloadDefaultConfig()
		{
			DefaultConfigPath = Path.Combine(EXILEDConfigFolderPath, $"{ServerConsole.Port}-config.yml");
			DefaultConfigData = File.ReadAllLines(DefaultConfigPath);
		}

		internal static void SetupDirectories()
		{
			if (!Directory.Exists(PluginConfigFolderPath))
			{
				Directory.CreateDirectory(PluginConfigFolderPath);
				Log.Info($"Config folder {PluginConfigFolderPath} doesn't exist, creating...");
				
			}
			if (!GMPlugin.instance.Config.GlobalConfigs && !Directory.Exists($"{PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port}"))
			{
				Directory.CreateDirectory($"{PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port}");
				Log.Info($"Port folder {PluginConfigFolderPath}{Path.DirectorySeparatorChar}{ServerConsole.Port} doesn't exist, creating...");
			}
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
						ShuffledList = ModeList.OrderBy(x => rand.Next()).ToList();
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
			string settings = GMPlugin.instance.Config.DefaultMode.ToUpper().Trim();
			int indx = settings.IndexOf(":");
			bool isFreq = indx != -1;
			if (Enum.TryParse(isFreq ? settings.Substring(0, indx) : settings, out ChoosingMethod method))
			{
				if (method == ChoosingMethod.NONE) return;
				ChangeMode(method);
				Log.Info($"Setting mode to {method}.");

				if (isFreq)
				{
					if (int.TryParse(settings.Substring(indx + 1), out int a))
					{
						SetFrequency(a, false);
						Log.Info($"Setting frequency to {a}.");
					}
					else
					{
						Log.Info("Config error: Invalid frequency, setting to 0.");
					}
				}
			}
			else
			{
				Log.Info("Config error: Invalid method.");
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

		internal static IPlugin<IConfig> CurrentMode;
		internal static IPlugin<IConfig> NextMode;
		internal static IPlugin<IConfig> LastMode;
		internal static IPlugin<IConfig> LastGamemode;
		internal static ChoosingMethod method;
		internal static int methodFreq;
		internal static int freqCount;
		internal static string DefaultConfigPath;
		internal static string[] DefaultConfigData;
		internal static string PluginConfigFolderPath = Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EXILED"), "Plugins"), "GamemodeManager");
		internal static string EXILEDConfigFolderPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EXILED"), "Configs");
		internal static bool isFirstRound;
	}
}

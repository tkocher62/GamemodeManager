using Smod2;
using Smod2.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamemodeManager
{
	class CommandHandler : ICommandHandler
	{
		private Random rand = new Random();

		public string GetCommandDescription()
		{
			return "";
		}

		public string GetUsage()
		{
			return "";
		}

		public string[] HelpMessage()
		{
			return new[]
			{
				"GamemodeManager Commands",
				"LIST - Lists all registered gamemodes.",
				"SET [Gamemode ID] - Sets the next gamemode.",
				"SETMODE [Mode] (FREQ) - Sets the method to choose the next gamemode.",
				"RELOAD - Reloads the default config data for the server from gameplay_config.txt. Cannot be done during gamemodes.",
				"",
				"GamemodeManager Modes",
				"NONE - Disable automatic gamemode choosing.",
				"CYCLE - Cycles through all gamemodes in order, restarting the order once complete.",
				"SHUFFLE - Picks a random gamemode every round, gamemodes will not be played twice in a row.",
				"VOTE - Prompts all players to vote for the next gamemode at the end of the round."
			};
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (args.Length <= 0 || args == null) return HelpMessage();

			switch (args[0].ToUpper())
			{
				case "LIST":
					{
						string s = "Registered Gamemodes:\n - Standard (standard)\n";
						for (int i = 0; i < GamemodeManager.ModeList.Count; i++)
						{
							Plugin gm = GamemodeManager.ModeList.ElementAt(i).Key;
							s += $" - {gm.Details.name} ({gm.Details.id})";
							if (i < GamemodeManager.ModeList.Count - 1) s += "\n";
						}
						return new[] { s };
					}
				case "SET":
					{
						if (args.Length != 2) return HelpMessage();
						if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE) return new[] { $"Cannot set next mode while in {GamemodeManager.method.ToString()} mode." };
						string id = args[1].ToLower();
						if (id == "standard")
						{
							GamemodeManager.SetNextMode(null);
							return new[] { $"Set next gamemode to Standard." };
						}
						foreach (KeyValuePair<Plugin, string> entry in GamemodeManager.ModeList)
						{
							if (entry.Key.Details.id.ToLower() == id)
							{
								GamemodeManager.SetNextMode(entry.Key);
								return new[] { $"Set next gamemode to {entry.Key.Details.name} ({entry.Key.Details.id})." };
							}
						}
						return new[] { $"Unknown gamemode {id}." };
					}
				case "SETMODE":
					{
						if (args.Length < 2) return HelpMessage();
						string cmd = args[1].ToUpper();
						int freq = 0;
						if (args.Length == 3)
						{
							if (int.TryParse(args[2], out int a))
							{
								freq = a;
								GamemodeManager.methodFreq = a;
								GamemodeManager.freqCount = a;
							}
							else
							{
								return new[] { "Invalid frequency." };
							}
						}
						else
						{
							GamemodeManager.methodFreq = 0;
						}
						switch (cmd)
						{
							case "NONE":
								{
									GamemodeManager.method = GamemodeManager.ChoosingMethod.NONE;
									GamemodeManager.SetNextMode(null);
									GamemodeManager.freqCount = 0;
									GamemodeManager.LastGamemode = null;
									break;
								}
							case "CYCLE":
								{
									GamemodeManager.method = GamemodeManager.ChoosingMethod.CYCLE;
									GamemodeManager.ShuffledList = GamemodeManager.ModeList.OrderBy(x => rand.Next()).ToDictionary(item => item.Key, item => item.Value);
									break;
								}
							case "SHUFFLE":
								{
									GamemodeManager.method = GamemodeManager.ChoosingMethod.SHUFFLE;
									break;
								}
							case "VOTE":
								{
									if (GamemodeManager.ModeList.Count <= 1) return new[] { "There are not enough gamemodes registered to hold a vote." };
									GamemodeManager.method = GamemodeManager.ChoosingMethod.VOTE;
									break;
								}
							default:
								{
									return new[] { $"Unknown gamemode method." };
								}
						}
						return new[] { $"Set gamemode method to {cmd}{(freq != 0 ? $" with frequency {freq}" : string.Empty)}." };
					}
				case "RELOAD":
					{
						if (GamemodeManager.CurrentMode == null)
						{
							GamemodeManager.ReloadDefaultConfig();
							return new[] { "Default config data reloaded." };
						}
						else
						{
							return new[] { "Cannot reload default config while a gamemode config is laoded." };
						}
					}
				default:
					{
						return HelpMessage();
					}
			}
		}
	}
}

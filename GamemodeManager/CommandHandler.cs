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
				"SETMODE [Mode] (Freq) (RunNext) - Sets the method to choose the next gamemode.",
				"RELOAD - Reloads the default config data for the server from gameplay_config.txt. Cannot be done during gamemodes.",
				"",
				"GamemodeManager Modes",
				"NONE - Disable automatic gamemode choosing.",
				"CYCLE - Cycles through all gamemodes in order, restarting the order once complete.",
				"SHUFFLE - Picks a random gamemode every round, gamemodes will not be played twice in a row.",
				"VOTE - Prompts all players to vote for the next gamemode at the end of the round.",
				"PERSIST - Keeps the current or next gamemode running until turned off."
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
						if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE && GamemodeManager.method != GamemodeManager.ChoosingMethod.PERSIST) return new[] { $"Cannot set next mode while in {GamemodeManager.method.ToString()} mode." };
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
						switch (cmd)
						{
							case "NONE":
								{
									GamemodeManager.ChangeMode(GamemodeManager.ChoosingMethod.NONE);
									break;
								}
							case "CYCLE":
								{
									GamemodeManager.ChangeMode(GamemodeManager.ChoosingMethod.CYCLE);
									break;
								}
							case "SHUFFLE":
								{
									GamemodeManager.ChangeMode(GamemodeManager.ChoosingMethod.SHUFFLE);
									break;
								}
							case "VOTE":
								{
									GamemodeManager.ChangeMode(GamemodeManager.ChoosingMethod.VOTE);
									break;
								}
							case "PERSIST":
								{
									GamemodeManager.ChangeMode(GamemodeManager.ChoosingMethod.PERSIST);
									break;
								}
							default:
								{
									return new[] { $"Unknown gamemode method." };
								}
						}

						int freq = 0;
						if (args.Length >= 3)
						{
							if (int.TryParse(args[2], out int a))
							{
								if (args.Length == 4 && GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE)
								{
									if (bool.TryParse(args[3].ToLower(), out bool b))
									{
										GamemodeManager.SetFrequency(a, b);
									}
									else
									{
										return new[] { "RunNext must be true or false." };
									}
								}
								else
								{
									GamemodeManager.SetFrequency(a);
								}
								freq = a;
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

using EXILED;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EXILED.Extensions;

namespace GamemodeManager
{
	class EventHandler
	{
		// TODO:
		// Reload EXILED config instead of main game config
		// Ensure config swapping works as intended (briefly touched this, not fully done or checked over)

		private Random rand = new Random();

		private bool isVoting = false;
		private bool isRoundRestarting = false;
		private bool isRoundStarted = false;
		private Dictionary<ReferenceHub, int> votelog = new Dictionary<ReferenceHub, int>();

		private string helpMessage = 
				"GamemodeManager Commands\n" +
				"LIST - Lists all registered gamemodes.\n" +
				"SET [Gamemode ID] - Sets the next gamemode.\n" +
				"SETMODE [Mode] (Freq) (RunNext) - Sets the method to choose the next gamemode.\n" +
				"RELOAD - Reloads the default config data for the server from gameplay_config.txt. Cannot be done during gamemodes.\n\n" +
				"GamemodeManager Modes\n" +
				"NONE - Disable automatic gamemode choosing.\n" +
				"CYCLE - Cycles through all gamemodes in order, restarting the order once complete.\n" +
				"SHUFFLE - Picks a random gamemode every round, gamemodes will not be played twice in a row.\n" +
				"VOTE - Prompts all players to vote for the next gamemode at the end of the round.\n" +
				"PERSIST - Keeps the current or next gamemode running until turned off.";

		public List<int> GetIndexStartingWith(List<string> values, string val)
		{
			List<int> indxs = new List<int>();
			for (int i = 0; i < values.Count; i++)
			{
				if (values[i].StartsWith(val)) indxs.Add(i);
			}
			return indxs;
		}

		public void OnRoundStart()
		{
			isRoundStarted = true;
		}

		public void OnRoundRestart()
		{
			isRoundRestarting = true;
			if (GamemodeManager.CurrentMode != null) GamemodeManager.LastGamemode = GamemodeManager.CurrentMode;
			GamemodeManager.LastMode = GamemodeManager.CurrentMode;

			// Determine next mode
			if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE)
			{
				if (GamemodeManager.methodFreq == GamemodeManager.freqCount)
				{
					GamemodeManager.freqCount = 0;
					switch (GamemodeManager.method)
					{
						case GamemodeManager.ChoosingMethod.CYCLE:
							{
								GamemodeManager.SetNextMode(GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastGamemode));
								break;
							}
						case GamemodeManager.ChoosingMethod.SHUFFLE:
							{
								EXILED.Plugin nextMode = GamemodeManager.ModeList.ElementAt(rand.Next(GamemodeManager.ModeList.Count)).Key;
								GamemodeManager.SetNextMode(GamemodeManager.LastGamemode == nextMode ? GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastGamemode) : nextMode);
								break;
							}
						case GamemodeManager.ChoosingMethod.VOTE:
							{
								isVoting = false;
								GamemodeManager.SetNextMode(votelog.Count > 0 
								? GamemodeManager.ModeList.ElementAt(votelog.GroupBy(i => i.Value).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First()).Key
								: GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastGamemode));
								break;
							}
						case GamemodeManager.ChoosingMethod.PERSIST:
							{
								EXILED.Plugin p= null;
								if (GamemodeManager.methodFreq != 0) p = GamemodeManager.LastGamemode;
								else p = GamemodeManager.CurrentMode ?? GamemodeManager.NextMode;
								if (p != null) GamemodeManager.SetNextMode(p);
								break;
							}
					}
				}
				else
				{
					GamemodeManager.freqCount++;
				}
			}

			// Register next mode
			if (GamemodeManager.NextMode != null)
			{
				GamemodeManager.CurrentMode = GamemodeManager.NextMode;
				GamemodeManager.NextMode = null;
				if (GamemodeManager.ModeList[GamemodeManager.CurrentMode] != null)
				{
					string config = $"{GamemodeManager.PluginConfigFolderPath}{Path.DirectorySeparatorChar}{(!Configs.isGlobalConfigs ? $"{ServerConsole.Port}{Path.DirectorySeparatorChar}" : "")}{GamemodeManager.ModeList[GamemodeManager.CurrentMode]}";
					if (File.Exists(config))
					{
						if (GamemodeManager.LastMode != null) GamemodeManager.WriteConfig(GamemodeManager.DefaultConfigData);
						List<string> newConfig = GamemodeManager.DefaultConfigData.ToList();
						List<string> overrideConfig = File.ReadAllLines(config).ToList();
						for (int i = 0; i < overrideConfig.Count; i++)
						{
							string line = overrideConfig[i];
							List<int> indx = GetIndexStartingWith(newConfig, line.Split(':')[0]);
							if (indx.Count > 0) foreach (int a in indx) newConfig[a] = line;
							else newConfig.Add(line);
						}
						GamemodeManager.Log($"Loading config '{config}' for gamemode {GamemodeManager.CurrentMode.getName}...");
						GamemodeManager.ReloadConfig(newConfig.ToArray());
					}
				}
			}
			else
			{
				GamemodeManager.CurrentMode = null;
				if (GamemodeManager.LastMode != null)
				{
					GamemodeManager.Log("Loading default config...");
					GamemodeManager.ReloadConfig(GamemodeManager.DefaultConfigData);
				}
			}
		}

		public void OnRoundEnd()
		{
			if (GamemodeManager.CurrentMode != null) GamemodeManager.LastGamemode = GamemodeManager.CurrentMode;

			if (GamemodeManager.method == GamemodeManager.ChoosingMethod.VOTE && GamemodeManager.methodFreq == GamemodeManager.freqCount && !isRoundRestarting)
			{
				Map.Broadcast("<b>Gamemode Voting</b>\nPress [`] or [~] to open your console to vote for the gamemode for next round!", 30, false);
				string s = "Type '.gm number' to vote for the gamemode you want to play!\n";
				for (int i = 1; i <= GamemodeManager.ModeList.Count; i++)
				{
					EXILED.Plugin gm = GamemodeManager.ModeList.ElementAt(i - 1).Key;
					s += $"{i}. {gm.getName}{(!GamemodeManager.isVoteRepeat && gm == GamemodeManager.LastGamemode ? " | Unavailable - Last Played" : "")}";
					if (i < GamemodeManager.ModeList.Count) s += "\n";
				}
				foreach (ReferenceHub player in Player.GetHubs())
				{
					player.SendConsoleMessage(s, "yellow");
				}
				votelog.Clear();
				isVoting = true;
				isRoundStarted = false;
			}
		}

		public void OnConsoleCommand(ConsoleCommandEvent ev)
		{
			if (ev.Command.StartsWith("gm"))
			{
				if (isVoting)
				{
					string num = ev.Command.Substring(3);
					if (int.TryParse(num, out int a))
					{
						if (a < 1 || a > GamemodeManager.ModeList.Count)
						{
							ev.ReturnMessage = "Invalid option.";
							return;
						}
						EXILED.Plugin mode = GamemodeManager.ModeList.ElementAt(a - 1).Key;
						if (!GamemodeManager.isVoteRepeat && mode == GamemodeManager.LastGamemode)
						{
							ev.ReturnMessage = "Cannot vote for the last played gamemode.";
							return;
						}
						string gmName = mode.getName;
						if (votelog.ContainsKey(ev.Player))
						{
							votelog[ev.Player] = a - 1;
							ev.ReturnMessage = $"You have changed your vote to {gmName}.";
						}
						else
						{
							votelog.Add(ev.Player, a - 1);
							ev.ReturnMessage = $"Vote casted for {gmName}.";
						}
					}
					else
					{
						ev.ReturnMessage = "Invalid option!";
						return;
					}
				}
				else
				{
					ev.ReturnMessage = "Gamemode voting is not open.";
				}
			}
		}

		public void OnWaitingForPlayers()
		{
			Configs.Reload();

			GamemodeManager.SetupDirectories();
			isRoundRestarting = false;

			// Load default settings
			if (GamemodeManager.isFirstRound && Configs.defaultMode != string.Empty)
			{
				GamemodeManager.LoadDefaultSettings();
			}
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (GamemodeManager.method == GamemodeManager.ChoosingMethod.VOTE && GamemodeManager.CurrentMode != null && !isRoundStarted)
			{
				ev.Player.BroadcastMessage($"<b>Winning Gamemode</b>\n{GamemodeManager.CurrentMode.getName}", 5);
			}
		}

		public void OnRACommand(ref RACommandEvent ev)
		{
			string cmd = ev.Command.ToLower();
			if (cmd.StartsWith("gm"))
			{
				ev.Allow = false;
				//ReferenceHub sender = Player.GetPlayer(ev.Sender.SenderId);
				string[] args = cmd.Replace("gm", "").Split(' ');
				if (args.Length == 0 || args == null)
				{
					ev.Sender.RAMessage(helpMessage, false);
					return;
				}

				switch (args[0].ToUpper())
				{
					case "LIST":
						{
							string s = "Registered Gamemodes:\n - Standard (standard)\n";
							for (int i = 0; i < GamemodeManager.ModeList.Count; i++)
							{
								Plugin gm = GamemodeManager.ModeList.ElementAt(i).Key;
								s += $" - {gm.getName}";
								if (i < GamemodeManager.ModeList.Count - 1) s += "\n";
							}
							ev.Sender.RAMessage(s, true);
							break;
						}
					case "SET":
						{
							if (args.Length != 2)
							{
								ev.Sender.RAMessage(helpMessage, false);
								return;
							}
							if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE && GamemodeManager.method != GamemodeManager.ChoosingMethod.PERSIST)
							{
								ev.Sender.RAMessage($"Cannot set next mode while in {GamemodeManager.method.ToString()} mode.", false);
								return;
							}
							string id = args[1].ToLower();
							if (id == "standard")
							{
								GamemodeManager.SetNextMode(null);
								ev.Sender.RAMessage("Set next gamemode to Standard.", true);
								return;
							}
							foreach (KeyValuePair<Plugin, string> entry in GamemodeManager.ModeList)
							{
								if (entry.Key.getName.ToLower() == id)
								{
									GamemodeManager.SetNextMode(entry.Key);
									ev.Sender.RAMessage($"Set next gamemode to {entry.Key.getName}.", true);
									return;
								}
							}
							ev.Sender.RAMessage($"Unknown gamemode {id}.", true);
							break;
						}
					case "SETMODE":
						{
							if (args.Length < 2)
							{
								ev.Sender.RAMessage(helpMessage, false);
								return;
							}
							string cmd2 = args[1].ToUpper();
							switch (cmd2)
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
										ev.Sender.RAMessage("Unknown gamemode method.", false);
										break;
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
											ev.Sender.RAMessage("RunNext must be true or false.", false);
											return;
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
									ev.Sender.RAMessage("Invalid frequency", false);
									return;
								}
							}
							else
							{
								GamemodeManager.methodFreq = 0;
							}
							ev.Sender.RAMessage($"Set gamemode method to {cmd}{(freq != 0 ? $" with frequency {freq}" : string.Empty)}.", true);
							break;
						}
					case "RELOAD":
						{
							if (GamemodeManager.CurrentMode == null)
							{
								GamemodeManager.ReloadDefaultConfig();
								ev.Sender.RAMessage("Default config data reloaded.", true);
								return;
							}
							else
							{
								ev.Sender.RAMessage("Cannot reload default config while a gamemode config is loaded.", false);
								return;
							}
						}
					default:
						{
							ev.Sender.RAMessage(helpMessage, false);
							break;
						}
				}
			}
		}
	}
}

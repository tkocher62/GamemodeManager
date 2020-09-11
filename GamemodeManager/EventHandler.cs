using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs;

namespace GamemodeManager
{
	class EventHandler
	{
		private Random rand = new Random();

		private bool isVoting = false;
		private bool isRoundRestarting = false;
		private bool isWarheadFresh = true;
		private Dictionary<Player, int> votelog = new Dictionary<Player, int>();

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

		private List<int> GetIndexStartingWith(List<string> values, string val)
		{
			List<int> indxs = new List<int>();
			for (int i = 0; i < values.Count; i++)
			{
				// todo: make this this is per plugin somehow
				if (values[i].StartsWith(val))
				{
					indxs.Add(i);
					Log.Warn(i);
				}
			}
			return indxs;
		}

		internal void OnRoundRestart()
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
								IPlugin<IConfig> nextMode = GamemodeManager.ModeList.ElementAt(rand.Next(GamemodeManager.ModeList.Count));
								GamemodeManager.SetNextMode(GamemodeManager.LastGamemode == nextMode ? GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastGamemode) : nextMode);
								break;
							}
						case GamemodeManager.ChoosingMethod.VOTE:
							{
								isVoting = false;
								GamemodeManager.SetNextMode(votelog.Count > 0 
								? GamemodeManager.ModeList.ElementAt(votelog.GroupBy(i => i.Value).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First())
								: GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastGamemode));
								break;
							}
						case GamemodeManager.ChoosingMethod.PERSIST:
							{
								IPlugin<IConfig> p = null;
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
				string config = $"{GamemodeManager.PluginConfigFolderPath}{Path.DirectorySeparatorChar}{(!GMPlugin.instance.Config.GlobalConfigs ? $"{ServerConsole.Port}{Path.DirectorySeparatorChar}" : "")}{GamemodeManager.CurrentMode.Name}.yml";
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
					Log.Info($"Loading config '{config}' for gamemode {GamemodeManager.CurrentMode.Name}...");
					GamemodeManager.WriteConfig(newConfig.ToArray());
				}
			}
			else
			{
				GamemodeManager.CurrentMode = null;
				if (GamemodeManager.LastMode != null)
				{
					Log.Info("Loading default config...");
					GamemodeManager.WriteConfig(GamemodeManager.DefaultConfigData);
				}
			}
		}

		internal void OnRoundEnd(RoundEndedEventArgs ev)
		{
			ev.TimeToRestart = GMPlugin.instance.Config.VoteTime;

			if (GamemodeManager.CurrentMode != null) GamemodeManager.LastGamemode = GamemodeManager.CurrentMode;

			if (GamemodeManager.method == GamemodeManager.ChoosingMethod.VOTE && GamemodeManager.methodFreq == GamemodeManager.freqCount && !isRoundRestarting)
			{
				Map.Broadcast((ushort)(GMPlugin.instance.Config.VoteTime + 3), "<b>Gamemode Voting</b>\n<i>Press [`] or [~] to open your console to vote for the gamemode for next round!</i>");
				string s = "Type '.gm number' to vote for the gamemode you want to play! You can change your vote by voting several times.\n";
				for (int i = 1; i <= GamemodeManager.ModeList.Count; i++)
				{
					IPlugin<IConfig> gm = GamemodeManager.ModeList[i - 1];
					s += $"{i}. {gm.Name}{(!GMPlugin.instance.Config.VoteRepeat && gm == GamemodeManager.LastGamemode ? " | Unavailable - Last Played" : "")}";
					if (i < GamemodeManager.ModeList.Count) s += "\n";
				}
				foreach (Player player in Player.List)
				{
					player.SendConsoleMessage(s, "yellow");
				}
				votelog.Clear();
				isVoting = true;
			}
		}

		internal void OnConsoleCommand(SendingConsoleCommandEventArgs ev)
		{
			if (ev.Name.ToLower().StartsWith("gm"))
			{
				if (isVoting)
				{
					if (ev.Arguments.Count == 1)
					{
						if (int.TryParse(ev.Arguments[0], out int a))
						{
							if (a < 1 || a > GamemodeManager.ModeList.Count)
							{
								ev.ReturnMessage = "Invalid option.";
								return;
							}
							IPlugin<IConfig> mode = GamemodeManager.ModeList[a - 1];
							if (!GMPlugin.instance.Config.VoteRepeat && mode == GamemodeManager.LastGamemode)
							{
								ev.ReturnMessage = "Cannot vote for the last played gamemode.";
								return;
							}
							string gmName = mode.Name;
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
						ev.ReturnMessage = "USAGE: .gm [id]";
					}
				}
				else
				{
					ev.ReturnMessage = "Gamemode voting is not open.";
				}
			}
		}

		internal void OnWaitingForPlayers()
		{
			// Load team respawn queue
			if (GMPlugin.instance.Config.TeamRespawnQueue != string.Empty)
			{
				PlayerManager.localPlayer.GetComponent<CharacterClassManager>().ClassTeamQueue = GMPlugin.instance.Config.TeamRespawnQueue.ToCharArray().Select(x =>
				{
					string team = x.ToString();
					if (int.TryParse(team, out int a))
					{
						return (Team)a;
					}
					else
					{
						Log.Warn($"Invalid team \"{team}\", defaulting to Class-D...");
						return Team.CDP;
					}
				}).ToList();
			}

			GamemodeManager.SetupDirectories();
			isRoundRestarting = false;

			// Load default settings
			if (GamemodeManager.isFirstRound && GMPlugin.instance.Config.DefaultMode != string.Empty)
			{
				GamemodeManager.LoadDefaultSettings();
			}

			foreach (IPlugin<IConfig> gamemode in GamemodeManager.ModeList)
			{
				string config = $"{GamemodeManager.PluginConfigFolderPath}{Path.DirectorySeparatorChar}{(!GMPlugin.instance.Config.GlobalConfigs ? $"{ServerConsole.Port}{Path.DirectorySeparatorChar}" : "")}{gamemode.Name}.yml";

				if (!File.Exists(config))
				{
					File.Create(config);
					Log.Warn($"No config file for '{gamemode.Name}' detected, creating...");
				}
			}
		}

		internal void OnPlayerJoin(JoinedEventArgs ev)
		{
			if (GamemodeManager.method == GamemodeManager.ChoosingMethod.VOTE && GamemodeManager.CurrentMode != null && !Round.IsStarted)
			{
				ev.Player.Broadcast(5, $"<b>Winning Gamemode</b>\n<i>{GamemodeManager.CurrentMode.Name}</i>");
			}
		}

		internal void OnWarheadStart(StartingEventArgs ev)
		{
			if (isWarheadFresh)
			{
				isWarheadFresh = false;
				Warhead.DetonationTimer = GMPlugin.instance.Config.NukeTimer;
			}
		}

		internal void OnRACommand(SendingRemoteAdminCommandEventArgs ev)
		{
			string cmd = ev.Name.ToLower();
			if (cmd.StartsWith("gm"))
			{
				ev.IsAllowed = false;
				if (ev.Arguments.Count == 0)
				{
					ev.Sender.RemoteAdminMessage(helpMessage, false);
					return;
				}
				switch (ev.Arguments[0].ToUpper())
				{
					case "LIST":
						{
							string s = "Registered Gamemodes:\n - Standard\n";
							for (int i = 0; i < GamemodeManager.ModeList.Count; i++)
							{ 
								IPlugin<IConfig> gm = GamemodeManager.ModeList[i];
								s += $" - {gm.Name}";
								if (i < GamemodeManager.ModeList.Count - 1) s += "\n";
							}
							ev.Sender.RemoteAdminMessage(s, true);
							break;
						}
					case "SET":
						{
							if (ev.Arguments.Count != 2)
							{
								ev.Sender.RemoteAdminMessage(helpMessage, false);
								return;
							}
							if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE && GamemodeManager.method != GamemodeManager.ChoosingMethod.PERSIST)
							{
								ev.Sender.RemoteAdminMessage($"Cannot set next mode while in {GamemodeManager.method.ToString()} mode.", false);
								return;
							}
							string id = ev.Arguments[1].ToLower();
							if (id == "standard")
							{
								GamemodeManager.SetNextMode(null);
								ev.Sender.RemoteAdminMessage("Set next gamemode to Standard.", true);
								return;
							}
							foreach (IPlugin<IConfig> plugin in GamemodeManager.ModeList)
							{
								if (plugin.Name.ToLower() == id)
								{
									GamemodeManager.SetNextMode(plugin);
									ev.Sender.RemoteAdminMessage($"Set next gamemode to {plugin.Name}.", true);
									return;
								}
							}
							ev.Sender.RemoteAdminMessage($"Unknown gamemode {id}.", true);
							break;
						}
					case "SETMODE":
						{
							if (ev.Arguments.Count < 2)
							{
								ev.Sender.RemoteAdminMessage(helpMessage, false);
								return;
							}
							string cmd2 = ev.Arguments[1].ToUpper();
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
										ev.Sender.RemoteAdminMessage("Unknown gamemode method.", false);
										return;
									}
							}

							int freq = 0;
							if (ev.Arguments.Count >= 3)
							{
								if (int.TryParse(ev.Arguments[2], out int a))
								{
									if (ev.Arguments.Count == 4 && GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE)
									{
										if (bool.TryParse(ev.Arguments[3].ToLower(), out bool b))
										{
											GamemodeManager.SetFrequency(a, b);
										}
										else
										{
											ev.Sender.RemoteAdminMessage("RunNext must be true or false.", false);
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
									ev.Sender.RemoteAdminMessage("Invalid frequency", false);
									return;
								}
							}
							else
							{
								GamemodeManager.methodFreq = 0;
							}
							ev.Sender.RemoteAdminMessage($"Set gamemode method to {cmd2}{(freq != 0 ? $" with frequency {freq}" : string.Empty)}.", true);
							break;
						}
					case "RELOAD":
						{
							if (GamemodeManager.CurrentMode == null)
							{
								GamemodeManager.ReloadDefaultConfig();
								ev.Sender.RemoteAdminMessage("Default config data reloaded.", true);
								return;
							}
							else
							{
								ev.Sender.RemoteAdminMessage("Cannot reload default config while a gamemode config is loaded.", false);
								return;
							}
						}
					default:
						{
							ev.Sender.RemoteAdminMessage(helpMessage, false);
							break;
						}
				}
			}
		}
	}
}

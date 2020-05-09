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
		private Random rand = new Random();

		private bool isVoting = false;
		private bool isRoundRestarting = false;
		private bool isRoundStarted = false;
		private Dictionary<ReferenceHub, int> votelog = new Dictionary<ReferenceHub, int>();

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
								Plugin nextMode = GamemodeManager.ModeList.ElementAt(rand.Next(GamemodeManager.ModeList.Count)).Key;
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
								Plugin p= null;
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
					string config = $"{GamemodeManager.ConfigFolderPath}{Path.DirectorySeparatorChar}{(!GamemodeManager.isGlobalConfigs ? $"{ServerConsole.Port}{Path.DirectorySeparatorChar}" : "")}{GamemodeManager.ModeList[GamemodeManager.CurrentMode]}";
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
					Plugin gm = GamemodeManager.ModeList.ElementAt(i - 1).Key;
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
						Plugin mode = GamemodeManager.ModeList.ElementAt(a - 1).Key;
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
			if (GamemodeManager.isFirstRound && GamemodeManager.defaultSettings != string.Empty)
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
	}
}

using Smod2;
using Smod2.EventHandlers;
using Smod2.Events;
using System;
using Smod2.API;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace GamemodeManager
{
	class EventHandler : IEventHandlerRoundRestart, IEventHandlerRoundEnd, IEventHandlerCallCommand, IEventHandlerWaitingForPlayers
	{
		private readonly Plugin instance;

		private Random rand = new Random();

		private bool isVoting = false;
		private bool isRoundRestarting = false;
		private Dictionary<int, int> votelog = new Dictionary<int, int>();

		public EventHandler(Plugin plugin) => instance = plugin;

		public void OnRoundRestart(RoundRestartEvent ev)
		{
			isRoundRestarting = true;
			if (GamemodeManager.CurrentMode != null) GamemodeManager.LastMode = GamemodeManager.CurrentMode;

			if (GamemodeManager.method != GamemodeManager.ChoosingMethod.NONE)
			{
				if (GamemodeManager.methodFreq == GamemodeManager.freqCount)
				{
					GamemodeManager.freqCount = 0;
					switch (GamemodeManager.method)
					{
						case GamemodeManager.ChoosingMethod.CYCLE:
							{
								GamemodeManager.SetNextMode(GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastMode));
								break;
							}
						case GamemodeManager.ChoosingMethod.SHUFFLE:
							{
								Plugin nextMode = GamemodeManager.ModeList.ElementAt(rand.Next(GamemodeManager.ModeList.Count)).Key;
								GamemodeManager.SetNextMode(GamemodeManager.LastMode == nextMode ? GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastMode) : nextMode);
								break;
							}
						case GamemodeManager.ChoosingMethod.VOTE:
							{
								isVoting = false;

								if (votelog.Count > 0)
								{ 
									GamemodeManager.SetNextMode(GamemodeManager.ModeList.ElementAt(votelog.GroupBy(i => i.Value).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First()).Key);
								}
								else
								{
									GamemodeManager.SetNextMode(GamemodeManager.GetNextModeInRegistry(GamemodeManager.LastMode));
								}
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
					string config = $"{GamemodeManager.ConfigFolderPath}{Path.DirectorySeparatorChar}{instance.Server.Port}{Path.DirectorySeparatorChar}{GamemodeManager.ModeList[GamemodeManager.CurrentMode]}";
					if (File.Exists(config))
					{
						instance.Info($"Loading config '{config}' for gamemode {GamemodeManager.CurrentMode.Details.name}...");
						ConfigFile.ServerConfig.LoadConfigFile(config);
						ConfigFile.ReloadGameConfig(config);
					}
					else
					{
						instance.Info($"Config '{GamemodeManager.ModeList[GamemodeManager.CurrentMode]}' not found, loading default config...");
						GamemodeManager.LoadDefaultConfig();
					}
				}
			}
			else
			{
				GamemodeManager.CurrentMode = null;
				GamemodeManager.LoadDefaultConfig();
				instance.Info("Loading default config...");
			}
		}

		public void OnRoundEnd(RoundEndEvent ev)
		{
			if (GamemodeManager.method == GamemodeManager.ChoosingMethod.VOTE && GamemodeManager.methodFreq == GamemodeManager.freqCount && !isRoundRestarting)
			{
				instance.Server.Map.Broadcast(30, "<b>Gamemode Voting</b>\nPress [`] or [~] to open your console to vote for the next gamemode!", false);
				string s = "Type '.gm number' to vote for the gamemode you want to play next!\n";
				for (int i = 1; i <= GamemodeManager.ModeList.Count; i++)
				{
					Plugin gm = GamemodeManager.ModeList.ElementAt(i - 1).Key;
					s += $"{i}. {gm.Details.name} - By {gm.Details.author}";
					if (i < GamemodeManager.ModeList.Count - 1) s += "\n";
				}
				foreach (Player player in ev.Server.GetPlayers())
				{
					player.SendConsoleMessage(s, "yellow");
				}
				votelog.Clear();
				isVoting = true;
			}
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			if (ev.Command.StartsWith("gm"))
			{
				if (isVoting)
				{
					string num = ev.Command.Substring(3);
					if (int.TryParse(num, out int a))
					{
						string gmName = GamemodeManager.ModeList.ElementAt(a - 1).Key.Details.name;
						if (votelog.ContainsKey(ev.Player.PlayerId))
						{
							votelog[ev.Player.PlayerId] = a - 1;
							ev.ReturnMessage = $"You have changed your vote to {gmName}.";
						}
						else
						{
							votelog.Add(ev.Player.PlayerId, a - 1);
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

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			GamemodeManager.SetupDirectories();
			isRoundRestarting = false;
		}
	}
}

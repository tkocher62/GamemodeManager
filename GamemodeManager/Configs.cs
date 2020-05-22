using EXILED;
using EXILED.Extensions;
using System.Linq;

namespace GamemodeManager
{
	internal static class Configs
	{
		// GamemodeManager Configs
		internal static bool isGlobalConfigs;
		internal static bool isVoteRepeat;

		internal static string defaultMode;

		// Overrides
		internal static bool isLaterJoin;

		internal static int nukeTimer;

		internal static string teamRespawnQueue;

		internal static void Reload()
		{
			// GamemodeManager Configs
			isGlobalConfigs = Plugin.Config.GetBool("gm_global_gamemode_configs", true);
			isVoteRepeat = Plugin.Config.GetBool("gm_vote_repeat", false);

			defaultMode = Plugin.Config.GetString("gm_default_mode");

			// Overrides
			isLaterJoin = Plugin.Config.GetBool("gm_laterjoin_enabled", true);

			nukeTimer = Plugin.Config.GetInt("gm_nuke_timer", -1);
			if (nukeTimer != -1)
			{
				for (int index = 0; index < Map.AlphaWarheadController.scenarios_start.Length; ++index)
				{
					if (Map.AlphaWarheadController.scenarios_start[index].tMinusTime == nukeTimer)
						Map.AlphaWarheadController.Networksync_startScenario = index;
				}
			}

			teamRespawnQueue = Plugin.Config.GetString("gm_team_respawn_queue", string.Empty);
			if (teamRespawnQueue != string.Empty)
			{
				PlayerManager.localPlayer.GetPlayer().characterClassManager.ClassTeamQueue = teamRespawnQueue.ToCharArray().Select(x =>
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
		}
	}
}

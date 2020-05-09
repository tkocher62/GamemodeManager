﻿using EXILED;

namespace GamemodeManager
{
	internal static class Configs
	{
		internal static bool isGlobalGamemodeConfigs;
		internal static bool isVoteRepeat;

		internal static string defaultMode;

		internal static void Reload()
		{
			isGlobalGamemodeConfigs = Plugin.Config.GetBool("gm_global_gamemode_configs");
			isVoteRepeat = Plugin.Config.GetBool("gm_vote_repeat");

			defaultMode = Plugin.Config.GetString("gm_default_mode");
		}
	}
}

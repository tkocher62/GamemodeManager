using Smod2.Attributes;
using Smod2;
using System.IO;

namespace GamemodeManager
{
	[PluginDetails(
	author = "Cyanox",
	name = "GamemodeManager",
	description = "",
	id = "cyan.gamemode.manager",
	version = "1.0.0",
	SmodMajor = 3,
	SmodMinor = 4,
	SmodRevision = 0
	)]
	public class GMPlugin : Plugin
    {
		public override void OnDisable() { }

		public override void OnEnable()
		{
			GamemodeManager.ReloadDefaultConfig();
			GamemodeManager.isFirstRound = true;
		}

		public override void Register()
		{
			AddEventHandlers(new EventHandler(this));
			AddConfig(new Smod2.Config.ConfigSetting("gm_global_gamemode_configs", true, true, "Should GamemodeManager use gamemode configs in a global file or separate them by port."));
			AddConfig(new Smod2.Config.ConfigSetting("gm_default_mode", string.Empty, true, "The mode GamemodeManager should be set to on server startup."));
			AddCommands(new[] {"gamemode", "gm"}, new CommandHandler());
		}
	}
}

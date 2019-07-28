using Smod2.Attributes;
using Smod2;

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
			GamemodeManager.DefaultConfigPath = ConfigFile.ServerConfig.Path;
		}

		public override void Register()
		{
			AddEventHandlers(new EventHandler(this));
			AddCommands(new[] {"gamemode", "gm"}, new CommandHandler());
		}
	}
}

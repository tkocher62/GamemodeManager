using EXILED;

namespace GamemodeManager
{
	public class GMPlugin : EXILED.Plugin
    {
		private EventHandler ev;

		public override void OnEnable()
		{
			ev = new EventHandler();

			Events.RoundRestartEvent += ev.OnRoundRestart;
			Events.RoundEndEvent += ev.OnRoundEnd;
			Events.ConsoleCommandEvent += ev.OnConsoleCommand;
			Events.WaitingForPlayersEvent += ev.OnWaitingForPlayers;
			Events.PlayerJoinEvent += ev.OnPlayerJoin;
			Events.RoundStartEvent += ev.OnRoundStart;

			GamemodeManager.ReloadDefaultConfig();
			GamemodeManager.isFirstRound = true;
		}

		public override void OnDisable()
		{
			Events.RoundRestartEvent -= ev.OnRoundRestart;
			Events.RoundEndEvent -= ev.OnRoundEnd;
			Events.ConsoleCommandEvent -= ev.OnConsoleCommand;
			Events.WaitingForPlayersEvent -= ev.OnWaitingForPlayers;
			Events.PlayerJoinEvent -= ev.OnPlayerJoin;
			Events.RoundStartEvent -= ev.OnRoundStart;
		}

		public override void OnReload() { }

		public override string getName { get; } = "GamemodeManager";

		/*public override void Register()
		{
			AddEventHandlers(new EventHandler(this));
			AddConfig(new Smod2.Config.ConfigSetting("gm_global_gamemode_configs", true, true, "Should GamemodeManager use gamemode configs in a global file or separate them by port."));
			AddConfig(new Smod2.Config.ConfigSetting("gm_default_mode", string.Empty, true, "The mode GamemodeManager should be set to on server startup."));
			AddConfig(new Smod2.Config.ConfigSetting("gm_vote_repeat", false, true, "If the Vote mode can repeat gamemodes back to back."));
			AddCommands(new[] {"gamemode", "gm"}, new CommandHandler());
		}*/
	}
}

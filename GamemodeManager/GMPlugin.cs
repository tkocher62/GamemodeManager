using EXILED;
using Harmony;

namespace GamemodeManager
{
	public class GMPlugin : Plugin
    {
		private EventHandler ev;

		public override void OnEnable()
		{
			HarmonyInstance.Create($"cyanox.gamemodemanager").PatchAll();

			ev = new EventHandler();

			Events.RoundRestartEvent += ev.OnRoundRestart;
			Events.RoundEndEvent += ev.OnRoundEnd;
			Events.ConsoleCommandEvent += ev.OnConsoleCommand;
			Events.WaitingForPlayersEvent += ev.OnWaitingForPlayers;
			Events.PlayerJoinEvent += ev.OnPlayerJoin;
			Events.RoundStartEvent += ev.OnRoundStart;
			Events.RemoteAdminCommandEvent += ev.OnRACommand;

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
			Events.RemoteAdminCommandEvent -= ev.OnRACommand;
		}

		public override void OnReload() { }

		public override string getName { get; } = "GamemodeManager";
	}
}

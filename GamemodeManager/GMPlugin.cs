using Exiled.API.Features;
using HarmonyLib;

namespace GamemodeManager
{
	public class GMPlugin : Plugin<Config>
    {
		private EventHandler ev;
		private Harmony hInstance;
		internal static GMPlugin instance;

		public override void OnEnabled()
		{
			base.OnEnabled();

			hInstance = new Harmony($"cyanox.gamemodemanager");
			hInstance.PatchAll();

			instance = this;

			ev = new EventHandler();

			Exiled.Events.Handlers.Server.RestartingRound += ev.OnRoundRestart;
			Exiled.Events.Handlers.Server.RoundEnded += ev.OnRoundEnd;
			Exiled.Events.Handlers.Server.SendingConsoleCommand += ev.OnConsoleCommand;
			Exiled.Events.Handlers.Server.WaitingForPlayers += ev.OnWaitingForPlayers;
			Exiled.Events.Handlers.Player.Joined += ev.OnPlayerJoin;
			Exiled.Events.Handlers.Server.SendingRemoteAdminCommand += ev.OnRACommand;
			Exiled.Events.Handlers.Warhead.Starting += ev.OnWarheadStart;

			GamemodeManager.ReloadDefaultConfig();
			GamemodeManager.isFirstRound = true;
		}

		public override void OnDisabled()
		{
			base.OnDisabled();

			Exiled.Events.Handlers.Server.RestartingRound -= ev.OnRoundRestart;
			Exiled.Events.Handlers.Server.RoundEnded -= ev.OnRoundEnd;
			Exiled.Events.Handlers.Server.SendingConsoleCommand -= ev.OnConsoleCommand;
			Exiled.Events.Handlers.Server.WaitingForPlayers -= ev.OnWaitingForPlayers;
			Exiled.Events.Handlers.Player.Joined -= ev.OnPlayerJoin;
			Exiled.Events.Handlers.Server.SendingRemoteAdminCommand -= ev.OnRACommand;
			Exiled.Events.Handlers.Warhead.Starting -= ev.OnWarheadStart;

			ev = null;
		}

		public override string Author => "Cyanox";
	}
}

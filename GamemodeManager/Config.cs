using Exiled.API.Interfaces;
using System.ComponentModel;
using System.Linq;

namespace GamemodeManager
{
	public class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		// GamemodeManager Configs
		[Description("Determines if gamemode configs should be saved in a global folder or per server.")]
		public bool GlobalConfigs { get; set; } = true;
		[Description("Determines if a gamemode can be voted for twice in a row on the vote mode.")]
		public bool VoteRepeat { get; set; } = false;

		[Description("Sets the default mode when the server starts.")]
		public string DefaultMode { get; set; } = string.Empty;

		[Description("Sets the amount of time for players to vote.")]
		public int VoteTime = 15;

		// Overrides
		[Description("Determines if later join should be enabled.")]
		public bool LaterJoin { get; set; } = true;

		[Description("Sets the nuke timer for this round, set to -1 to leave default.")]
		public int NukeTimer { get; set; } = -1;

		[Description("Overrides the team respawn queue.")]
		public string TeamRespawnQueue { get; set; } = string.Empty;
	}
}

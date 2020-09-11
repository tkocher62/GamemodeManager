using Exiled.API.Features;
using Exiled.API.Interfaces;
using System.Reflection;

namespace GamemodeManager.API
{
	public static class GMM
	{
		public static void RegisterMode(Assembly assembly)
		{
			IPlugin<IConfig> gamemode = GamemodeManager.PluginToIPlugin(assembly);
			GamemodeManager.ModeList.Add(gamemode);
			Log.Info($"{gamemode.Name} has been registered.");
		}

		public static IPlugin<IConfig> GetCurrentMode()
		{
			return GamemodeManager.CurrentMode;
		}
	}
}

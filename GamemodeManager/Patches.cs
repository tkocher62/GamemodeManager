using HarmonyLib;

namespace GamemodeManager
{
	[HarmonyPatch(typeof(CharacterClassManager), "LaterJoinPossible")]
	class LaterJoinPatch
	{
		public static void Postfix(ref bool __result)
		{
			__result = __result == true ? GMPlugin.instance.Config.LaterJoin : false;
		}
	}
}

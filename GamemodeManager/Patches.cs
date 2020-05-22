using Harmony;

namespace GamemodeManager
{
	[HarmonyPatch(typeof(CharacterClassManager), "LaterJoinPossible")]
	class LaterJoinPatch
	{
		public static void Postfix(bool __result)
		{
			__result = __result == true ? Configs.isLaterJoin : false;
		}
	}
}
